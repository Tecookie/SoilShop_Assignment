"""Exercise selectors beyond 100 records on the isolated completion stack only."""
from pathlib import Path
import json, os, uuid
from playwright.sync_api import sync_playwright, expect

ROOT=Path(__file__).resolve().parents[1]
project=os.environ.get('SHOP_CHECK_PROJECT')
if project not in ('soilshop-completion-check','soilshop-worker-verification'):
    raise SystemExit('Use only an isolated SoilShop verification project.')
settings=dict(line.split('=',1) for line in (ROOT/'.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
base='http://127.0.0.1:'+os.environ.get('SHOP_CHECK_PORT','18086')
suffix=uuid.uuid4().hex[:8];prefix='Z-LARGE-'+suffix
with sync_playwright() as p:
    browser=p.chromium.launch(channel='chrome',headless=True)
    try:
        request=p.request.new_context(base_url=base)
        response=request.post('/api/auth/login',data={'username':settings['BOOTSTRAP_USERNAME'],'password':settings['BOOTSTRAP_PASSWORD']});assert response.ok
        headers={'Authorization':'Bearer '+response.json()['token']}
        def post(path,data):
            result=request.post('/api'+path,headers=headers,data=data)
            assert result.ok,(path,result.status,result.text()[:400]);return result.json()
        product=post('/products',{'code':prefix,'name':'Fictional large catalogue'})
        for i in range(101):
            sku=post('/skus',{'productId':product['id'],'code':f'{prefix}-{i:03}','packageAmount':i+1,'packageUnit':'kg','salePriceVnd':1000})
            supplier=post('/suppliers',{'code':f'{prefix}-{i:03}','name':f'Fictional supplier {prefix}-{i:03}'})
        page=browser.new_page(viewport={'width':1440,'height':1000});page.goto(base)
        page.get_by_label('Username',exact=True).fill(settings['BOOTSTRAP_USERNAME']);page.get_by_label('Password',exact=True).fill(settings['BOOTSTRAP_PASSWORD']);page.get_by_role('button',name='Sign in →',exact=True).click()
        expect(page.get_by_role('heading',name='Inventory',exact=True)).to_be_visible()
        page.get_by_role('navigation',name='Main navigation').get_by_role('button',name='Purchase receipts',exact=True).click()
        page.get_by_role('button',name='+ New receipt',exact=True).click()
        code=f'{prefix}-100'
        page.get_by_label('Search Supplier',exact=True).fill(code);page.get_by_label('Supplier',exact=True).select_option(str(supplier['id']))
        page.get_by_label('Search SKU 1',exact=True).fill(code);page.get_by_label('SKU 1',exact=True).select_option(str(sku['id']))
        page.get_by_label('Search Supplier',exact=True).fill('NO-SUCH-SUPPLIER');page.get_by_label('Search SKU 1',exact=True).fill('NO-SUCH-SKU')
        page.wait_for_timeout(600)
        expect(page.get_by_label('Supplier',exact=True)).to_have_value(str(supplier['id']))
        expect(page.get_by_label('SKU 1',exact=True)).to_have_value(str(sku['id']))
        page.get_by_label('Bags 1',exact=True).fill('1')
        with page.expect_response(lambda r:r.url.endswith('/api/purchase-receipts') and r.request.method=='POST') as saved:
            page.get_by_role('button',name='Save draft',exact=True).click()
        assert saved.value.status==201,saved.value.status
        receipt=saved.value.json();assert receipt['supplierId']==supplier['id'] and receipt['lines'][0]['skuId']==sku['id']
        assert receipt['lines'][0]['skuCode']==sku['code']
        expect(page.get_by_role('row').filter(has_text=sku['code'])).to_have_count(1)
        page.get_by_role('navigation',name='Main navigation').get_by_role('button',name='Replenishment',exact=True).click()
        page.get_by_label('Search suggestions by SKU or product',exact=True).fill(code)
        row=page.get_by_role('row').filter(has_text=f"SKU #{sku['id']}")
        for _ in range(20):
            if row.count():break
            page.get_by_role('button',name='Refresh',exact=True).click();page.wait_for_timeout(1000)
        expect(row).to_have_count(1)
        result={'passed':4,'skuCountCreated':101,'supplierCountCreated':101,'checks':['Search and select SKU beyond first 100 and show its code on the saved receipt','Search and select supplier beyond first 100','Preserve both selections when search results change','Search suggestions beyond first 100'],'scope':'isolated '+project+' only'}
        (ROOT/'test-results'/'large-catalogue-checks.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
        print(json.dumps(result))
    finally:browser.close()
