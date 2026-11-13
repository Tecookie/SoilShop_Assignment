"""End-to-end remaining workflows, only on the explicitly isolated completion stack.

Run with SHOP_CHECK_PORT and SHOP_CHECK_PROJECT=soilshop-completion-check.
Creates fictional data; broker/worker fault checks never target the normal shop.
"""
from pathlib import Path
from datetime import datetime, timedelta, timezone
import json, os, subprocess, time, uuid
from playwright.sync_api import sync_playwright, expect

ROOT=Path(__file__).resolve().parents[1]; OUT=ROOT/'test-results'; OUT.mkdir(exist_ok=True)
PROJECT=os.environ.get('SHOP_CHECK_PROJECT','')
if PROJECT!='soilshop-completion-check': raise SystemExit('Use only the isolated soilshop-completion-check project.')
settings=dict(line.split('=',1) for line in (ROOT/'.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
BASE='http://127.0.0.1:'+os.environ.get('SHOP_CHECK_PORT','18086')
checks=[]; errors=[]
def docker(*args):
    result=subprocess.run(['docker',*args],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
    if result.returncode: raise AssertionError(result.stderr[-1500:])
    return result.stdout.strip()
def container(service):
    names=docker('ps','-aq','--filter',f'label=com.docker.compose.project={PROJECT}','--filter',f'label=com.docker.compose.service={service}').splitlines()
    assert len(names)==1,(service,names)
    return names[0]
def wait_for(check,seconds=90):
    deadline=time.monotonic()+seconds
    while time.monotonic()<deadline:
        value=check()
        if value:return value
        time.sleep(1)
    raise AssertionError('Timed out waiting for background result')
def sql(statement):return docker('exec',container('postgres'),'psql','-U','soilshop','-d','soilshop','-Atc',statement)

with sync_playwright() as p:
 browser=p.chromium.launch(channel='chrome',headless=True)
 try:
    request=p.request.new_context(base_url=BASE)
    login=request.post('/api/auth/login',data={'username':settings['BOOTSTRAP_USERNAME'],'password':settings['BOOTSTRAP_PASSWORD']});assert login.ok,login.status
    headers={'Authorization':'Bearer '+login.json()['token']}
    def call(path,body=None,method='GET'):
        response=request.fetch('/api'+path,headers=headers,method=method,data=body)
        assert response.ok,(path,response.status,response.text()[:400])
        return response.json() if response.status!=204 else None
    def post(path,body):return call(path,body,'POST')
    suffix=uuid.uuid4().hex[:8];code='OPS-'+suffix
    product=post('/products',{'code':code,'name':'Operations soil [fictional test]'})
    supplier=post('/suppliers',{'code':code,'name':'Operations supplier '+suffix})
    sku=post('/skus',{'productId':product['id'],'code':code,'packageAmount':5,'packageUnit':'kg','salePriceVnd':50000})
    page=browser.new_page(viewport={'width':1440,'height':1000})
    page.on('pageerror',lambda e:errors.append(str(e)))
    page.on('response',lambda r:errors.append(f'{r.status} {r.url}') if r.status>=500 else None)
    page.goto(BASE)
    page.get_by_label('Username',exact=True).fill(settings['BOOTSTRAP_USERNAME']);page.get_by_label('Password',exact=True).fill(settings['BOOTSTRAP_PASSWORD'])
    page.get_by_role('button',name='Sign in →',exact=True).click();expect(page.get_by_role('heading',name='Inventory',exact=True)).to_be_visible()
    def nav(name):page.get_by_role('navigation',name='Main navigation').get_by_role('button',name=name,exact=True).click()
    def choose(label,text,value):
        page.get_by_label('Search '+label,exact=True).fill(text)
        page.get_by_label(label,exact=True).select_option(str(value))
    nav('Stock counts');choose('SKU',code,sku['id']);page.get_by_role('button',name='Capture current stock',exact=True).click()
    count_reason='Fictional opening count '+suffix
    page.get_by_label('Actual bags',exact=True).fill('7');page.get_by_label('Count reason',exact=True).fill(count_reason)
    page.get_by_role('button',name='Submit for approval',exact=True).click()
    row=page.get_by_role('row').filter(has_text=count_reason);row.get_by_role('button',name='Review',exact=True).click()
    page.get_by_label('Review note',exact=True).fill('Physically checked fictional fixture');page.get_by_role('button',name='Submit review',exact=True).click()
    expect(row).to_contain_text('Approved');assert call(f"/skus/{sku['id']}")['stockQty']==7
    checks.append('Browser count submission and Admin approval with ledger')
    page.get_by_role('button',name='Capture current stock',exact=True).click();page.get_by_text('Direct Admin adjustment',exact=True).click()
    page.get_by_label('Bag change',exact=True).fill('-2');page.get_by_label('Adjustment reason',exact=True).fill('Fictional damage check')
    page.get_by_role('button',name='Record adjustment',exact=True).click();expect(page.get_by_role('status')).to_contain_text('Adjustment recorded')
    assert call(f"/skus/{sku['id']}")['stockQty']==5;checks.append('Browser direct adjustment preserves nonnegative stock and history')
    wait_for(lambda:next((x for x in call('/restock-suggestions/search?search='+code)['items'] if x['skuId']==sku['id']),None))
    nav('Replenishment');page.get_by_label('Search suggestions by SKU or product',exact=True).fill(code);row=page.get_by_role('row').filter(has_text=f"SKU #{sku['id']}");row.get_by_role('button',name='Prepare receipt',exact=True).click()
    choose('Supplier',code,supplier['id']);page.get_by_role('button',name='Create receipt draft',exact=True).click()
    expect(page.get_by_role('heading',name='Purchase receipts',exact=True)).to_be_visible()
    assert call(f"/skus/{sku['id']}")['stockQty']==5
    checks.append('Browser replenishment conversion creates one receipt Draft without changing stock')
    nav('Suppliers');page.get_by_label('Search suppliers',exact=True).fill(code);row=page.get_by_role('row').filter(has_text=code);row.get_by_role('button',name='Edit supplier',exact=True).click()
    page.get_by_label('Contact note',exact=True).fill('Fictional contact updated');page.get_by_role('button',name='Save supplier',exact=True).click();expect(row).to_contain_text('Fictional contact updated')
    nav('SKU prices & stock rules');page.get_by_label('Search SKUs',exact=True).fill(code);page.get_by_role('row').filter(has_text=code).get_by_role('button',name='Edit SKU',exact=True).click()
    page.get_by_label('Sale price (whole VND)',exact=True).fill('60000');page.get_by_role('button',name='Save SKU',exact=True).click();expect(page.get_by_role('status')).to_contain_text('SKU saved')
    assert call(f"/skus/{sku['id']}")['salePriceVnd']==60000
    nav('Staff accounts');page.get_by_role('button',name='New account',exact=True).click();employee='ops-'+suffix;password=uuid.uuid4().hex+'!aA1'
    page.get_by_label('Username',exact=True).fill(employee);page.get_by_label('Display name',exact=True).fill('Fictional operations employee');page.get_by_label('Initial password',exact=True).fill(password);page.get_by_role('button',name='Save account',exact=True).click();expect(page.get_by_role('status')).to_contain_text('Account saved')
    employee_login=request.post('/api/auth/login',data={'username':employee,'password':password});assert employee_login.ok
    employee_headers={'Authorization':'Bearer '+employee_login.json()['token']}
    for path in ['/accounts','/backups']:assert request.get('/api'+path,headers=employee_headers).status==403
    checks.append('Browser supplier/SKU/account management; Employee admin boundaries')
    nav('Daily summaries');today=datetime.now(timezone(timedelta(hours=7))).date().isoformat();page.get_by_label('Business date',exact=True).fill(today);page.get_by_role('button',name='Request summary refresh',exact=True).click()
    wait_for(lambda:next((x for x in call('/summaries')['items'] if x['businessDate']==today and x['status']=='Completed'),None))
    page.get_by_role('button',name='Reload results',exact=True).click();page.get_by_role('row').filter(has_text=today).get_by_role('button',name='View stock snapshot',exact=True).click()
    expect(page.get_by_role('heading',name='Stock captured for '+today,exact=True)).to_be_visible();checks.append('Browser asynchronous summary with dated stock snapshot')
    nav('Database backups');wait_for(lambda:not any(x['status'] in ('Queued','Processing') for x in call('/backups')['items']))
    page.get_by_role('button',name='Refresh',exact=True).click();page.get_by_role('button',name='Request backup',exact=True).click();expect(page.get_by_role('status')).to_contain_text('Backup queued')
    wait_for(lambda:any(x['kind']=='Manual' and x['status']=='Completed' for x in call('/backups')['items']))
    page.get_by_role('button',name='Refresh',exact=True).click();expect(page.get_by_role('cell',name='Retained',exact=True).first).to_be_visible();checks.append('Browser backup request produces verified PostgreSQL 16 archive')
    for width in [390,1440]:
        page.set_viewport_size({'width':width,'height':1000});nav('Stock counts');expect(page.get_by_role('heading',name='Stock counts',exact=True)).to_be_visible()
        assert page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'),width
        page.screenshot(path=str(OUT/f'operations-{width}.png'),full_page=True)
    checks.append('Mobile 390px and desktop 1440px without page overflow')
    # Broker outage is constrained to this script's isolated Compose project.
    redis=container('redis');worker=container('worker');docker('stop',redis)
    try:
        sale=post('/sales',{'lines':[{'skuId':sku['id'],'quantity':1}]});post(f"/sales/{sale['id']}/confirm",{'actionKey':str(uuid.uuid4()),'expectedVersion':sale['version']})
        assert call(f"/skus/{sku['id']}")['stockQty']==4
        assert int(sql("SELECT count(*) FROM outbox_messages WHERE published_at IS NULL"))>0
    finally:docker('start',redis)
    wait_for(lambda:int(sql("SELECT count(*) FROM outbox_messages WHERE published_at IS NULL"))==0)
    assert int(sql(f"SELECT count(*) FROM restock_suggestions WHERE sku_id={sku['id']} AND status IN ('Open','ReceiptDrafted')"))==1
    checks.append('Isolated Redis outage: sale commits, outbox recovers, no duplicate active suggestion')
    docker('stop',worker)
    try:
        post('/summaries/'+today+'/refresh',{});queued=post('/backups',{'requestKey':str(uuid.uuid4())})
    finally:docker('start',worker)
    wait_for(lambda:next((x for x in call('/backups')['items'] if x['id']==queued['id'] and x['status']=='Completed'),None))
    wait_for(lambda:call('/summaries/'+today)['summary']['status']=='Completed')
    checks.append('Worker restart recovers queued summary and backup')
    for _ in range(8):
        run=post('/backups',{'requestKey':str(uuid.uuid4())})
        wait_for(lambda:next((x for x in call('/backups')['items'] if x['id']==run['id'] and x['status']=='Completed'),None))
    retained=sql("SELECT count(*) FROM backup_runs WHERE retained");assert retained=='7',retained
    checks.append('Retention keeps exactly seven successful Daily/Manual archives combined')
    worker=container('worker');docker('exec','--user','root',worker,'chmod','555','/backups')
    try:
        failed=post('/backups',{'requestKey':str(uuid.uuid4())})
        wait_for(lambda:next((x for x in call('/backups')['items'] if x['id']==failed['id'] and x['status']=='Failed'),None))
        assert sql("SELECT count(*) FROM backup_runs WHERE retained")=='7'
    finally:docker('exec','--user','root',worker,'chmod','755','/backups')
    checks.append('Failed backup preserves all seven previous successful archives')
    # Independent restore in a uniquely named disposable PostgreSQL container.
    archive=sql("SELECT filename FROM backup_runs WHERE retained ORDER BY completed_at DESC LIMIT 1")
    assert archive.startswith('soilshop-') and archive.endswith('.dump') and '/' not in archive
    dump=OUT/'operations-restore.dump';docker('cp',worker+':/backups/'+archive,str(dump))
    expected=sql("SELECT json_build_array((SELECT count(*) FROM skus),(SELECT sum(stock_qty) FROM skus),(SELECT count(*) FROM stock_movements),(SELECT count(*) FROM stock_adjustments),(SELECT count(*) FROM purchase_receipts),(SELECT count(*) FROM sales))")
    restore_name='soilshop-restore-'+uuid.uuid4().hex[:10];env=dict(os.environ,POSTGRES_PASSWORD=uuid.uuid4().hex)
    result=subprocess.run(['docker','run','-d','--name',restore_name,'-e','POSTGRES_PASSWORD','--tmpfs','/var/lib/postgresql/data','postgres:16-alpine'],env=env,capture_output=True,text=True);assert result.returncode==0,result.stderr
    try:
        wait_for(lambda:subprocess.run(['docker','exec',restore_name,'pg_isready','-U','postgres'],capture_output=True).returncode==0)
        docker('cp',str(dump),restore_name+':/restore.dump');docker('exec',restore_name,'createdb','-U','postgres','restored')
        docker('exec',restore_name,'pg_restore','-U','postgres','-d','restored','--no-owner','--no-acl','--exit-on-error','/restore.dump')
        actual=docker('exec',restore_name,'psql','-U','postgres','-d','restored','-Atc',"SELECT json_build_array((SELECT count(*) FROM skus),(SELECT sum(stock_qty) FROM skus),(SELECT count(*) FROM stock_movements),(SELECT count(*) FROM stock_adjustments),(SELECT count(*) FROM purchase_receipts),(SELECT count(*) FROM sales))")
        assert json.loads(expected)==json.loads(actual),(expected,actual)
    finally:docker('rm','-f','-v',restore_name)
    checks.append('Real isolated pg_restore matches catalogue, stock, ledger, adjustments and documents')
    assert not errors,errors
    result={'checks':checks,'passed':len(checks),'errors':errors,'scope':'isolated soilshop-completion-check only','pgClient':docker('exec',worker,'pg_dump','--version'),'restoreCounts':json.loads(actual)}
    (OUT/'operations-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(result,ensure_ascii=False,indent=2))
 finally:browser.close()
