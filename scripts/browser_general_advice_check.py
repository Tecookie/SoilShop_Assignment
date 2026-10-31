"""Read-only General advice regression. Run seed-demo.ps1 -WithAdvice first."""
from pathlib import Path
import json
from playwright.sync_api import sync_playwright, expect
ROOT=Path(__file__).resolve().parents[1]
settings=dict(line.split('=',1) for line in (ROOT/'.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
checks=[];errors=[]
with sync_playwright() as p:
 browser=p.chromium.launch(channel='chrome',headless=True)
 try:
  page=browser.new_page(viewport={'width':1440,'height':1050})
  page.on('pageerror',lambda e:errors.append(str(e)))
  page.on('console',lambda m:errors.append(m.text) if m.type=='error' else None)
  page.goto('http://localhost:'+settings.get('WEB_PORT','8080'))
  page.get_by_label('Username',exact=True).fill(settings['DEMO_EMPLOYEE_USERNAME'])
  page.get_by_label('Password',exact=True).fill(settings['DEMO_EMPLOYEE_PASSWORD'])
  page.get_by_role('button',name='Sign in →',exact=True).click()
  page.get_by_role('button',name='Tìm đất phù hợp',exact=True).click()
  page.get_by_label('Nhóm cây',exact=True).select_option('General')
  def search():
   with page.expect_response(lambda r:r.url.endswith('/api/product-advice') and r.request.method=='POST') as response:
    page.get_by_role('button',name='Tìm sản phẩm',exact=True).click()
   assert response.value.ok
  search()
  card=page.get_by_role('article').filter(has_text='ADVICE-V1-P01-5L')
  expect(card).to_be_visible();expect(card).to_contain_text('3/3');expect(card).to_contain_text('[mẫu tư vấn]')
  checks.append('Employee selecting only General receives a positive source-reviewed demo match over real gRPC')
  card.get_by_text('Chín yếu tố, thành phần và nguồn',exact=True).click()
  expect(card).to_contain_text('DỮ LIỆU GIẢ LẬP');expect(card).to_contain_text('Chưa có thông tin')
  page.screenshot(path=str(ROOT/'test-results/general-advice.png'),full_page=True)
  checks.append('Fictional source is visible; scientific measurements remain explicitly unknown')
  page.get_by_label('Thoát nước',exact=True).select_option('High');search()
  expect(card).to_have_count(0)
  page.get_by_role('checkbox',name='Cho phép thiếu một số tiêu chí (hiển thị rõ, không cộng điểm)',exact=True).check();search()
  expect(card).to_be_visible();expect(card).to_contain_text('3/6');expect(card).to_contain_text('Thoát nước: chưa có thông tin')
  checks.append('Strict physical requirement excludes unknown data; opt-in incomplete returns missing reason without points')
  page.get_by_label('Ngân sách mỗi bao (VND)',exact=True).fill('49000');search();expect(card).to_have_count(0)
  checks.append('Per-bag budget remains a hard filter')
  assert not errors,errors
  (ROOT/'test-results/general-advice-checks.json').write_text(json.dumps({'checks':checks,'consoleErrors':errors,'readOnly':True},indent=2)+'\n',encoding='utf-8')
  print(json.dumps({'passed':len(checks),'checks':checks}))
 finally:browser.close()
