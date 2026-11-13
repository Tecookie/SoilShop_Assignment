"""Real dashboard and Redis poison-message checks in a dedicated isolated stack."""
from pathlib import Path
from datetime import datetime, timedelta, timezone
import json, os, subprocess, time, uuid
from playwright.sync_api import sync_playwright, expect

ROOT=Path(__file__).resolve().parents[1]
if os.environ.get('SHOP_CHECK_PROJECT')!='soilshop-worker-verification':
    raise SystemExit('Use only the isolated soilshop-worker-verification project.')
settings=dict(line.split('=',1) for line in (ROOT/'.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
base='http://127.0.0.1:'+os.environ.get('SHOP_CHECK_PORT','18087')
def docker(*args):
    r=subprocess.run(['docker',*args],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=40)
    assert r.returncode==0,r.stderr[-500:]
    return r.stdout.strip()
def find(service):
    ids=docker('ps','-q','--filter','label=com.docker.compose.project=soilshop-worker-verification','--filter','label=com.docker.compose.service='+service).splitlines()
    assert len(ids)==1,(service,ids);return ids[0]
redis=find('redis');worker=find('worker')
def command(*args):return docker('exec',redis,'redis-cli','--raw',*args)
def wait_for(fn,seconds=90):
    deadline=time.monotonic()+seconds
    while time.monotonic()<deadline:
        value=fn()
        if value:return value
        time.sleep(.5)
    raise AssertionError('Timed out waiting for worker or API')
with sync_playwright() as p:
    req=p.request.new_context(base_url=base)
    def login():
        try:
            r=req.post('/api/auth/login',data={'username':settings['BOOTSTRAP_USERNAME'],'password':settings['BOOTSTRAP_PASSWORD']},timeout=3000)
            return r.json()['token'] if r.ok else None
        except Exception:return None
    headers={'Authorization':'Bearer '+wait_for(login)}
    def call(path,data=None,method='GET'):
        r=req.fetch('/api'+path,method=method,headers=headers,data=data)
        assert r.ok,(path,r.status,r.text()[:400]);return r.json()
    def post(path,data):return call(path,data,'POST')
    today=datetime.now(timezone(timedelta(hours=7))).date();start=today-timedelta(days=6)
    baseline=call(f'/dashboard?from={today}&to={today}&groupBy=day')['totals']
    baseline_week=call(f'/dashboard?from={start}&to={today}&groupBy=week')['totals']
    code='DASH-'+uuid.uuid4().hex[:10]
    product=post('/products',{'code':code,'name':'Dashboard fixture'})
    supplier=post('/suppliers',{'code':code,'name':'Dashboard fixture supplier'})
    sku=post('/skus',{'code':code,'productId':product['id'],'packageAmount':5,'packageUnit':'kg','salePriceVnd':50000})
    receipt=post('/purchase-receipts',{'supplierId':supplier['id'],'lines':[{'skuId':sku['id'],'quantity':2,'unitCostVnd':30000}]})
    post('/purchase-receipts/'+str(receipt['id'])+'/confirm',{'actionKey':str(uuid.uuid4()),'expectedVersion':receipt['version']})
    sale=post('/sales',{'lines':[{'skuId':sku['id'],'quantity':1}]})
    sale=post('/sales/'+str(sale['id'])+'/confirm',{'actionKey':str(uuid.uuid4()),'expectedVersion':sale['version']})
    day=call(f'/dashboard?from={today}&to={today}&groupBy=day')
    assert day['totals']['saleVnd']==baseline['saleVnd']+50000 and day['totals']['receiptVnd']==baseline['receiptVnd']+60000
    assert day['totals']['soldBags']==baseline['soldBags']+1 and day['totals']['receivedBags']==baseline['receivedBags']+2
    week=call(f'/dashboard?from={start}&to={today}&groupBy=week')
    assert week['totals']['saleVnd']==baseline_week['saleVnd']+50000 and sum(x['receiptVnd'] for x in week['periods'])==baseline_week['receiptVnd']+60000
    browser=p.chromium.launch(channel='chrome',headless=True)
    try:
        page=browser.new_page(viewport={'width':1440,'height':1000});errors=[]
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.goto(base);page.get_by_label('Username',exact=True).fill(settings['BOOTSTRAP_USERNAME']);page.get_by_label('Password',exact=True).fill(settings['BOOTSTRAP_PASSWORD']);page.get_by_role('button',name='Sign in →',exact=True).click()
        page.get_by_role('navigation',name='Main navigation').get_by_role('button',name='Dashboard',exact=True).click()
        expect(page.get_by_role('heading',name='Dashboard',exact=True)).to_be_visible()
        expect(page.get_by_label('Overview metrics')).to_contain_text('50.000')
        page.get_by_label('Group by',exact=True).select_option('week')
        page.get_by_role('button',name='View statistics',exact=True).click();expect(page.get_by_role('heading',name='Sales and purchases by week',exact=True)).to_be_visible()
        for width in (390,1440):
            page.set_viewport_size({'width':width,'height':1000});assert page.evaluate('document.documentElement.scrollWidth<=innerWidth+1')
            page.screenshot(path=str(ROOT/'test-results'/f'dashboard-{width}.png'),full_page=True)
        assert not errors,errors
    finally:browser.close()
    post('/sales/'+str(sale['id'])+'/cancel',{'actionKey':str(uuid.uuid4()),'expectedVersion':sale['version'],'reason':'Isolated dashboard cancellation'})
    assert call(f'/dashboard?from={today}&to={today}')['totals']['saleVnd']==baseline['saleVnd']
    def add(kind,payload):return command('XADD','soilshop:events','*','id',str(uuid.uuid4()),'kind',kind,'payload',payload)
    bad_unknown=add('UnknownFixture','{}');bad_payload=add('InventoryChanged','invalid-json');good=add('InventoryChanged',json.dumps({'skuId':sku['id']}))
    summary=add('SummaryRequested',json.dumps({'businessDate':str(today)}));backup=add('BackupRequested',json.dumps({'backupId':str(uuid.uuid4())}))
    def quarantined():
        raw=docker('exec',redis,'redis-cli','--json','XRANGE','soilshop:events:dead-letter','-','+')
        try: rows=json.loads(raw)
        except json.JSONDecodeError:return None
        found={dict(zip(fields[::2],fields[1::2])).get('sourceId'):dict(zip(fields[::2],fields[1::2])) for _,fields in rows}
        return found if bad_unknown in found and bad_payload in found else None
    found=wait_for(quarantined)
    assert found[bad_unknown]['attempts']=='3' and found[bad_payload]['attempts']=='3'
    logs=docker('logs',worker)
    for message in (good,summary,backup):assert 'Consumed event '+message in logs,message
    assert logs.index('Consumed event '+good)<logs.index('Quarantined event '+bad_unknown)
    for message in (bad_unknown,bad_payload,good,summary,backup):
        assert not command('XPENDING','soilshop:events','soilshop-worker',message,message,'1'),message
    result={'passed':6,'checks':['Daily and weekly confirmed sales and purchase totals','Cancelled sale excluded on refresh','Dashboard visible at mobile and desktop widths','Known event kinds dispatched independently','Unknown/malformed messages quarantined after two retries','Later valid event processed before failed message was quarantined'],'scope':'isolated soilshop-worker-verification only'}
    (ROOT/'test-results'/'dashboard-worker-checks.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result))
