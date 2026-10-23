"""Execute the DESIGN schema/prototype in a disposable, isolated PostgreSQL container."""
from pathlib import Path
import subprocess
import secrets
import uuid
import time
import os
import json

ROOT=Path(__file__).resolve().parents[2]
name='potting-design-'+uuid.uuid4().hex[:10]
env=os.environ.copy(); env['POSTGRES_PASSWORD']=secrets.token_urlsafe(24)
checks=[]

def run(args,check=True,stdin=None):
    r=subprocess.run(args,input=stdin,text=True,encoding='utf-8',capture_output=True,env=env)
    if check and r.returncode: raise RuntimeError(r.stderr[-1200:])
    return r

def command(sql,db='potting_design',check=True):
    return run(['docker','exec','-i',name,'psql','-U','postgres','-d',db,'-v','ON_ERROR_STOP=1','-At'],check,sql)

def ok(label,condition):
    if not condition: raise AssertionError(label)
    checks.append({'check':label,'passed':True})

started=False
try:
    run(['docker','run','--detach','--name',name,'--network','none','--memory','512m','-e','POSTGRES_PASSWORD','postgres:16-alpine'])
    started=True
    for attempt in range(60):
        ready=run(['docker','exec',name,'pg_isready','-U','postgres'],False)
        if ready.returncode==0: break
        time.sleep(.5)
    else: raise RuntimeError('PostgreSQL did not become ready.')
    run(['docker','exec',name,'createdb','-U','postgres','potting_design'])
    command((ROOT/'docs/database/schema.sql').read_text(encoding='utf-8'))
    ok('PostgreSQL 16 schema execution',command("SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';").stdout.strip()=='18')
    command("""
INSERT INTO users(username,display_name,password_hash,role) VALUES('design-staff','Design Staff','NON_AUTHENTICATING_TEST_VALUE','Employee');
INSERT INTO products(code,name) VALUES('SOIL','Design-only bagged soil');
INSERT INTO skus(product_id,code,package_amount,package_unit,sale_price_vnd,stock_qty) VALUES(1,'SOIL-5KG',5,'kg',50000,1);
INSERT INTO sales(code,created_by) VALUES('SALE-A',1),('SALE-B',1);
INSERT INTO sale_lines(sale_id,sku_id,quantity) VALUES(1,1,1),(2,1,1);
""")
    command((ROOT/'docs/database/concurrency-demo.sql').read_text(encoding='utf-8'))
    key_a=str(uuid.uuid4()); key_b=str(uuid.uuid4())
    args=['docker','exec','-i',name,'psql','-U','postgres','-d','potting_design','-v','ON_ERROR_STOP=1','-At']
    a=subprocess.Popen(args,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8',env=env)
    a.stdin.write(f"SELECT design_confirm_sale_demo(1,'{key_a}',2);\n"); a.stdin.close()
    # Wait until the prototype is actually in pg_sleep while holding the stock lock.
    for attempt in range(60):
        if command("SELECT count(*) FROM pg_stat_activity WHERE wait_event='PgSleep';").stdout.strip()=='1': break
        time.sleep(.05)
    else: raise RuntimeError('First transaction did not acquire its test lock.')
    b=command(f"SELECT design_confirm_sale_demo(2,'{key_b}',0);",check=False)
    a.wait(timeout=15)
    ok('Two concurrent connections: first succeeds, second reports insufficient stock',a.returncode==0 and b.returncode!=0 and 'INSUFFICIENT_STOCK' in b.stderr)
    state=command("SELECT stock_qty FROM skus WHERE id=1; SELECT count(*) FROM sales WHERE status='Confirmed'; SELECT count(*) FROM stock_movements; SELECT count(*) FROM outbox_messages;").stdout.strip().splitlines()
    ok('Last bag: stock 0, one confirmed sale, one movement and one outbox event',state==['0','1','1','1'])
    retried=command(f"SELECT design_confirm_sale_demo(1,'{key_a}',0); SELECT count(*) FROM stock_movements;").stdout.strip().splitlines()
    ok('Repeated confirmation key does not deduct twice',retried==['AlreadyConfirmed','1'])
    command("""
UPDATE skus SET stock_qty=1,stock_version=stock_version+1 WHERE id=1;
INSERT INTO skus(product_id,code,package_amount,package_unit,sale_price_vnd,stock_qty) VALUES(1,'SOIL-10KG',10,'kg',90000,0);
INSERT INTO sales(code,created_by) VALUES('SALE-MULTI',1);
INSERT INTO sale_lines(sale_id,sku_id,quantity) VALUES(3,1,1),(3,2,1);
""")
    multi=command(f"SELECT design_confirm_sale_demo(3,'{uuid.uuid4()}',0);",check=False)
    ok('Multi-SKU failure rolls back earlier deductions',multi.returncode!=0 and command("SELECT stock_qty FROM skus WHERE id=1; SELECT status FROM sales WHERE id=3; SELECT count(*) FROM stock_movements;").stdout.strip().splitlines()==['1','Draft','1'])
    stale=command("UPDATE skus SET stock_qty=stock_qty-1,stock_version=stock_version+1 WHERE id=1 AND stock_version=0 RETURNING id;").stdout
    ok('Stale stock-count version updates zero rows','UPDATE 0' in stale)
    invalid=command('UPDATE skus SET stock_qty=-1 WHERE id=1;',check=False)
    ok('Database rejects negative stock',invalid.returncode!=0 and 'check constraint' in invalid.stderr)
    command("""
INSERT INTO stock_counts(sku_id,expected_qty,expected_stock_version,actual_qty,reason,counted_by)
SELECT id,stock_qty,stock_version,3,'Design stale-count fixture',1 FROM skus WHERE id=1;
UPDATE skus SET stock_qty=stock_qty+1,stock_version=stock_version+1 WHERE id=1;
""")
    stale_count=command(f"SELECT design_approve_count_demo(1,'{uuid.uuid4()}',1); SELECT stock_qty FROM skus WHERE id=1; SELECT count(*) FROM stock_adjustments;").stdout.strip().splitlines()
    ok('Stale physical count becomes RecountRequired without changing stock',stale_count==['RecountRequired','2','0'])
    command("""
INSERT INTO stock_counts(sku_id,expected_qty,expected_stock_version,actual_qty,reason,counted_by)
SELECT id,stock_qty,stock_version,4,'Design valid count fixture',1 FROM skus WHERE id=1;
""")
    count_key=str(uuid.uuid4())
    approved=command(f"SELECT design_approve_count_demo(2,'{count_key}',1); SELECT design_approve_count_demo(2,'{count_key}',1); SELECT stock_qty FROM skus WHERE id=1; SELECT count(*) FROM stock_adjustments; SELECT count(*) FROM stock_movements;").stdout.strip().splitlines()
    ok('Valid physical count adjusts stock once; repeated approval is idempotent',approved==['Approved','AlreadyApproved','4','1','2'])
    command("INSERT INTO restock_suggestions(sku_id,observed_qty,suggested_qty) VALUES(1,1,29);")
    duplicate=command("INSERT INTO restock_suggestions(sku_id,observed_qty,suggested_qty) VALUES(1,1,29);",check=False)
    ok('Duplicate active replenishment suggestion is rejected',duplicate.returncode!=0 and 'ux_restock_active' in duplicate.stderr)
    missing_source=command('UPDATE products SET reviewed_by=1,reviewed_at=now() WHERE id=1;',check=False)
    ok('Metadata review without a source is rejected',missing_source.returncode!=0 and 'check constraint' in missing_source.stderr)
    run(['docker','exec',name,'pg_dump','-U','postgres','-d','potting_design','-Fc','--no-owner','--no-acl','-f','/tmp/design.dump'])
    run(['docker','exec',name,'pg_restore','--list','/tmp/design.dump'])
    run(['docker','exec',name,'createdb','-U','postgres','potting_restore'])
    run(['docker','exec',name,'pg_restore','-U','postgres','-d','potting_restore','--no-owner','--no-acl','--exit-on-error','/tmp/design.dump'])
    ok('pg_dump custom archive restores into a separate database',command('SELECT count(*) FROM sales; SELECT count(*) FROM stock_movements;',db='potting_restore').stdout.strip().splitlines()==['3','2'])
    report={'tested_on':'2026-10-23','engine':'PostgreSQL 16 / isolated disposable Docker container','scope':'Design schema and SQL prototype only; not an application integration test','checks':checks}
    (ROOT/'docs/database/runtime-checks.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))
finally:
    if started: run(['docker','rm','--force','--volumes',name],False)
