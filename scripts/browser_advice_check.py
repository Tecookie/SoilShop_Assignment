"""Advice smoke using fictional fixtures, real Compose gRPC and staff browser UI.
Reads local credentials without printing them. Leaves labelled fixtures/history.
"""
from pathlib import Path
import json, uuid
from playwright.sync_api import sync_playwright, expect
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'test-results'
settings=dict(line.split('=',1) for line in (ROOT/'.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
base='http://localhost:'+settings.get('WEB_PORT','8080')
checks=[]; errors=[]
with sync_playwright() as p:
 browser=p.chromium.launch(channel='chrome',headless=True)
 try:
  req=p.request.new_context(base_url=base)
  def login(username,password):
   r=req.post('/api/auth/login',data={'username':username,'password':password});assert r.ok,r.status
   return {'Authorization':'Bearer '+r.json()['token']}
  admin=login(settings['BOOTSTRAP_USERNAME'],settings['BOOTSTRAP_PASSWORD'])
  def call(path,body=None,method='POST',headers=admin):
   r=req.fetch('/api'+path,method=method,headers=headers,data=body)
   assert r.ok,(path,r.status,r.text()[:500]);return r.json() if r.status!=204 else None
  # Recover review cleanup from an interrupted earlier run of this same smoke.
  for old in call('/products?search=ADVICE-DEMO-&pageSize=100',method='GET')['items']:
   prior=call('/products/'+str(old['id'])+'/metadata',method='GET')
   if prior['reviewedAt'] and (prior['sourceReference'] or '').startswith('DỮ LIỆU GIẢ LẬP'):
    edit={k:v for k,v in prior.items() if k not in ['productId','version','reviewedAt','reviewedBy']};edit['expectedVersion']=prior['version'];call('/products/'+str(old['id'])+'/metadata',edit,'PUT')
  suffix=uuid.uuid4().hex[:8];code='ADVICE-DEMO-'+suffix
  prod=call('/products',{'code':code,'name':'Đất tư vấn [mẫu giả lập '+suffix+']','productType':'FoliageMix'})
  sku=call('/skus',{'code':code+'-5L','productId':prod['id'],'packageAmount':5,'packageUnit':'L','salePriceVnd':50000})
  supplier=call('/suppliers',{'code':code,'name':'Nhà cung cấp mẫu giả lập'})
  receipt=call('/purchase-receipts',{'supplierId':supplier['id'],'lines':[{'skuId':sku['id'],'quantity':3,'unitCostVnd':30000}]})
  call('/purchase-receipts/'+str(receipt['id'])+'/confirm',{'actionKey':str(uuid.uuid4()),'expectedVersion':receipt['version']})
  page=browser.new_page(viewport={'width':1440,'height':1050})
  page.on('pageerror',lambda e:errors.append(str(e)))
  page.on('console',lambda m:errors.append(m.text) if m.type=='error' else None)
  def ui_login(user,pw):
   page.goto(base);page.get_by_label('Username',exact=True).fill(user);page.get_by_label('Password',exact=True).fill(pw);page.get_by_role('button',name='Sign in →',exact=True).click();expect(page.get_by_role('heading',name='Inventory',exact=True)).to_be_visible()
  ui_login(settings['BOOTSTRAP_USERNAME'],settings['BOOTSTRAP_PASSWORD'])
  page.get_by_role('button',name='Catalogue',exact=True).click()
  # Newly created product may be on another page: narrow its type, then page until found.
  with page.expect_response(lambda r: '/api/products?' in r.url and 'productType=FoliageMix' in r.url):
   page.get_by_label('Filter catalogue by type',exact=True).select_option('FoliageMix')
  page.wait_for_load_state('networkidle')
  row=page.get_by_role('row').filter(has_text=code)
  while row.count()==0:
   nxt=page.get_by_role('button',name='Next',exact=True);expect(nxt).to_be_enabled();nxt.click();page.wait_for_timeout(200)
  row.get_by_role('button',name='Thông số đất',exact=True).click()
  expect(page.get_by_role('heading',name='Thông số đất · sản phẩm #'+str(prod['id']))).to_be_visible()
  for label,value in [('Thoát nước','High'),('Thoáng khí','High'),('Giữ ẩm','Medium'),('Phương pháp đo EC','SME'),('Vị trí trồng','Pot'),('Có mẫu thử','false')]: page.get_by_label(label,exact=True).select_option(value)
  for label,value in [('pH thấp nhất','5.5'),('pH cao nhất','6.5'),('EC thấp nhất (mS/cm)','0.4'),('EC cao nhất (mS/cm)','1.2')]:page.get_by_label(label,exact=True).fill(value)
  for label in ['Cấu trúc và độ bền','Dinh dưỡng / NPK','Chất hữu cơ','Vi sinh','Độ sạch / xử lý','Thành phần']:
   page.get_by_label(label,exact=True).fill('Thông tin GIẢ LẬP chỉ kiểm thử, không phải công bố chất lượng.')
  page.get_by_label('Nguồn thông tin',exact=True).fill('DỮ LIỆU GIẢ LẬP chỉ kiểm thử UI/gRPC, không dùng để tư vấn thực tế.')
  page.get_by_label('Kiểng lá',exact=True).check()
  expect(page.get_by_role('button',name='Duyệt thông tin theo nguồn',exact=True)).to_be_disabled()
  page.get_by_role('button',name='Lưu thông số',exact=True).click()
  expect(page.get_by_role('button',name='Duyệt thông tin theo nguồn',exact=True)).to_be_enabled()
  with page.expect_response(lambda r: r.url.endswith('/metadata/review') and r.request.method=='POST') as reviewed:
   page.get_by_role('button',name='Duyệt thông tin theo nguồn',exact=True).click()
  assert reviewed.value.ok
  expect(page.get_by_role('button',name='Duyệt thông tin theo nguồn',exact=True)).to_be_disabled()
  d=call('/products/'+str(prod['id'])+'/metadata',method='GET')
  assert d['reviewedAt'] and d['sampleAvailable'] is False and d['ecMax']==1.2 and d['plantGroups']==['Foliage']
  checks.append('Admin UI saves all nine factors, preserves false sample, explicitly reviews saved version')
  page.screenshot(path=str(OUT/'soil-metadata.png'),full_page=True)
  page.get_by_role('button',name='Tìm đất phù hợp',exact=True).click()
  page.get_by_label('Nhóm cây',exact=True).select_option('Foliage')
  for label,value in [('Thoát nước','High'),('Thoáng khí','High'),('Giữ ẩm','Medium')]:page.get_by_label(label,exact=True).select_option(value)
  page.get_by_label('Ngân sách mỗi bao (VND)',exact=True).fill('60000')
  page.get_by_role('button',name='Tìm sản phẩm',exact=True).click()
  card=page.get_by_role('article').filter(has_text=code)
  expect(card).to_be_visible();expect(card).to_contain_text('12/12')
  card.get_by_text('Chín yếu tố, thành phần và nguồn',exact=True).click();expect(card).to_contain_text('GIẢ LẬP')
  for width in [320,768,1440]:
   page.set_viewport_size({'width':width,'height':1000});assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'),width
  page.set_viewport_size({'width':1440,'height':1050});page.screenshot(path=str(OUT/'soil-advice.png'),full_page=True)
  checks.append('Real gRPC advice UI shows positive match, reasons, source, all facts and responsive layout')
  card.get_by_role('button',name='Tạo phiếu bán nháp · 1 bao',exact=True).click();expect(page.get_by_role('heading',name='Sales',exact=True)).to_be_visible()
  fresh=call('/skus?search='+sku['code'],method='GET')['items'];assert next(x for x in fresh if x['id']==sku['id'])['stockQty']==3
  checks.append('Choosing result creates one-bag sale draft without reserving or decrementing stock')
  employee=login(settings['DEMO_EMPLOYEE_USERNAME'],settings['DEMO_EMPLOYEE_PASSWORD'])
  prefs={'plantGroup':'Foliage','drainage':'High','maxPriceVnd':60000,'packageUnit':'L','packageAmount':5}
  def contains(response): return any(x['candidate']['sku']['id']==sku['id'] for x in response['results'])
  assert contains(call('/product-advice',prefs,headers=employee))
  assert not contains(call('/product-advice',dict(prefs,maxPriceVnd=100),headers=employee))
  assert not contains(call('/product-advice',dict(prefs,maxEc=2,ecMethod='PourThrough'),headers=employee))
  incomplete=call('/product-advice',dict(prefs,maxEc=2,ecMethod='PourThrough',allowIncomplete=True),headers=employee)
  match=next(x for x in incomplete['results'] if x['candidate']['sku']['id']==sku['id']);assert match['missing'] and match['score']==6
  denied=req.put('/api/products/'+str(prod['id'])+'/metadata',headers=employee,data={'expectedVersion':d['version']});assert denied.status==403
  checks.append('Employee advice/read allowed; write forbidden; budget and EC method filters honor strict/incomplete rules')
  # Invalidate review after smoke: synthetic metadata must not remain recommendation eligible.
  body={k:v for k,v in d.items() if k not in ['productId','version','reviewedAt','reviewedBy']};body['expectedVersion']=d['version'];body['aeration']='Medium'
  changed=call('/products/'+str(prod['id'])+'/metadata',body,'PUT');assert changed['reviewedAt'] is None
  assert not contains(call('/product-advice',prefs,headers=employee))
  checks.append('Metadata edit invalidates review and removes synthetic fixture from recommendations after smoke')
  assert not errors,errors
  (OUT/'soil-browser-checks.json').write_text(json.dumps({'checks':checks,'consoleErrors':errors,'fixtureCode':code,'fixtureReviewCleared':True},indent=2)+'\n',encoding='utf-8')
  print(json.dumps({'passed':len(checks),'checks':checks}))
 finally:browser.close()
