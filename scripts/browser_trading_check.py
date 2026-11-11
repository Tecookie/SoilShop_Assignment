"""Approved 5.3/5.4 workflows through React and real Compose gRPC.

Creates clearly labelled fictional fixtures; keeps their immutable history.
Reads ignored .env without printing credentials. Expected 409s are exercised.
"""
from pathlib import Path
import json
import os
import uuid
from playwright.sync_api import sync_playwright, expect

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'test-results'
OUT.mkdir(exist_ok=True)
settings = dict(line.split('=', 1) for line in (ROOT / '.env').read_text().splitlines()
                if '=' in line and not line.startswith('#'))
base = 'http://127.0.0.1:' + os.environ.get('SHOP_CHECK_PORT', settings.get('WEB_PORT', '8080'))
checks, errors = [], []

with sync_playwright() as p:
    browser = p.chromium.launch(channel='chrome', headless=True)
    try:
        req = p.request.new_context(base_url=base)
        login = req.post('/api/auth/login', data={'username': settings['BOOTSTRAP_USERNAME'], 'password': settings['BOOTSTRAP_PASSWORD']})
        assert login.ok, login.status
        headers = {'Authorization': 'Bearer ' + login.json()['token']}

        def call(path, body=None, method='GET'):
            r = req.fetch('/api' + path, method=method, headers=headers, data=body)
            assert r.ok, (path, r.status, r.text()[:300])
            return r.json() if r.status != 204 else None

        code = 'TRADING-DEMO-' + uuid.uuid4().hex[:8].upper()
        product = call('/products', {'code': code, 'name': 'Đất kiểm tra giao dịch [mẫu giả lập]', 'productType': 'GeneralPurpose'}, 'POST')
        sku = call('/skus', {'productId': product['id'], 'code': code, 'packageAmount': 5, 'packageUnit': 'kg', 'salePriceVnd': 50000}, 'POST')
        supplier = call('/suppliers', {'code': code, 'name': 'Nhà cung cấp giao dịch [mẫu giả lập]'}, 'POST')
        page = browser.new_page(viewport={'width': 1440, 'height': 1050})
        page.on('pageerror', lambda e: errors.append(str(e)))
        page.on('response', lambda r: errors.append(str(r.status) + ' ' + r.url) if r.status >= 500 else None)
        page.goto(base)
        page.get_by_label('Username', exact=True).fill(settings['BOOTSTRAP_USERNAME'])
        page.get_by_label('Password', exact=True).fill(settings['BOOTSTRAP_PASSWORD'])
        page.get_by_role('button', name='Sign in →', exact=True).click()
        expect(page.get_by_role('heading', name='Inventory', exact=True)).to_be_visible()

        def nav(label):
            page.get_by_role('button', name=label, exact=True).click()
            page.wait_for_load_state('networkidle')

        def row(trade):
            return page.get_by_role('row').filter(has_text=trade['code'][:12].upper())

        def stock():
            return call('/skus/' + str(sku['id']))['stockQty']

        def save(path, method='POST'):
            with page.expect_response(lambda r: r.url.endswith('/api' + path) and r.request.method == method) as saved:
                page.get_by_role('button', name='Save draft', exact=True).click()
            assert saved.value.ok, saved.value.status
            expect(page.locator('.notice[role="status"]')).to_contain_text('Draft saved.')
            page.wait_for_load_state('networkidle')
            return saved.value.json()

        nav('Purchase receipts')
        page.get_by_role('button', name='+ New receipt', exact=True).click()
        page.get_by_label('Search Supplier', exact=True).fill(code)
        page.get_by_label('Supplier', exact=True).select_option(str(supplier['id']))
        page.get_by_label('Search SKU 1', exact=True).fill(code)
        page.get_by_label('SKU 1', exact=True).select_option(str(sku['id']))
        page.get_by_label('Bags 1', exact=True).fill('4')
        receipt = save('/purchase-receipts')  # Unknown actual cost is valid in a draft.
        assert receipt['lines'][0]['unitPriceVnd'] is None and stock() == 0
        row(receipt).get_by_role('button', name='Edit draft', exact=True).click()
        page.get_by_label('Bags 1', exact=True).fill('6')
        page.get_by_label('Unit cost 1 (VND)', exact=True).fill('30000')
        receipt = save('/purchase-receipts/' + str(receipt['id']), 'PUT')
        row(receipt).get_by_role('button', name='Review total', exact=True).click()
        review = page.get_by_role('region', name='Review quote')
        expect(review).to_contain_text('180.000')
        review.get_by_role('button', name='Confirm receipt', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Confirmed.')
        assert stock() == 6
        checks.append('Receipt unknown-cost draft, versioned edit, real gRPC quote and confirm')

        nav('Sales')
        page.get_by_role('button', name='+ New sale', exact=True).click()
        page.get_by_label('Search SKU 1', exact=True).fill(code)
        page.get_by_label('SKU 1', exact=True).select_option(str(sku['id']))
        sale = save('/sales')
        row(sale).get_by_role('button', name='Edit draft', exact=True).click()
        page.get_by_label('Bags 1', exact=True).fill('2')
        sale = save('/sales/' + str(sale['id']), 'PUT')
        row(sale).get_by_role('button', name='Review total', exact=True).click()
        review = page.get_by_role('region', name='Review quote')
        expect(review).to_contain_text('100.000')
        changed = {k: sku[k] for k in ['productId', 'code', 'packageAmount', 'packageUnit', 'lowStockThreshold', 'targetStock']}
        changed.update(salePriceVnd=60000, expectedVersion=sku['version'])
        call('/skus/' + str(sku['id']), changed, 'PUT')
        with page.expect_response(lambda r: r.url.endswith('/confirm') and r.request.method == 'POST') as conflict:
            review.get_by_role('button', name='Confirm sale', exact=True).click()
        assert conflict.value.status == 409 and stock() == 6
        expect(review).to_contain_text('120.000')
        review.get_by_role('button', name='Confirm sale', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Confirmed.')
        assert stock() == 4
        checks.append('Sale draft/edit; stale reviewed price returns 409 and refreshed quote; explicit retry freezes new prices')

        nav('Purchase receipts')
        row(receipt).get_by_role('button', name='Cancel document', exact=True).click()
        page.get_by_label('Cancellation reason', exact=True).fill('Mẫu thử: đảo phiếu nhập khi thiếu tồn')
        with page.expect_response(lambda r: r.url.endswith('/cancel')) as denied:
            page.get_by_role('button', name='Confirm cancellation', exact=True).click()
        assert denied.value.status == 409 and stock() == 4
        page.get_by_role('button', name='Keep document', exact=True).click()
        checks.append('Receipt cancellation shortage returns 409 with no partial stock reversal')

        nav('Sales')
        row(sale).get_by_role('button', name='Cancel document', exact=True).click()
        page.get_by_label('Cancellation reason', exact=True).fill('Mẫu thử: hủy toàn bộ phiếu bán')
        page.get_by_role('button', name='Confirm cancellation', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Cancelled in full.')
        expect(row(sale)).to_contain_text('Cancelled')
        expect(row(sale)).to_contain_text('120.000')
        assert stock() == 6
        for width in [390, 1440]:
            page.set_viewport_size({'width': width, 'height': 1000})
            assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), width
            page.screenshot(path=str(OUT / f'trading-{width}.png'), full_page=True)
        checks.append('Admin sale cancellation restores original bags, preserves price/reason/time and works at mobile/desktop widths')

        nav('Purchase receipts')
        row(receipt).get_by_role('button', name='Cancel document', exact=True).click()
        page.get_by_label('Cancellation reason', exact=True).fill('Mẫu thử: hủy phiếu nhập sau khi đã hoàn tồn')
        page.get_by_role('button', name='Confirm cancellation', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Cancelled in full.')
        assert stock() == 0
        checks.append('Admin receipt cancellation deducts all original quantities once stock is sufficient')

        nav('Sales')
        page.get_by_role('button', name='+ New sale', exact=True).click()
        page.get_by_label('Search SKU 1', exact=True).fill(code)
        page.get_by_label('SKU 1', exact=True).select_option(str(sku['id']))
        disposable = save('/sales')
        row(disposable).get_by_role('button', name='Delete', exact=True).click()
        page.get_by_role('region', name='Delete draft').get_by_role('button', name='Delete draft', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Draft deleted.')
        expect(row(disposable)).to_have_count(0)
        assert stock() == 0
        checks.append('Explicit draft deletion removes only the draft without changing stock')
        ledger = call('/stock-movements?skuId=' + str(sku['id']))
        assert sorted(x['kind'] for x in ledger['items']) == ['Purchase', 'PurchaseCancel', 'Sale', 'SaleCancel']
        assert not errors, errors
        result = {'checks': checks, 'serverOrPageErrors': errors, 'fixtureCode': code, 'finalStock': stock()}
        (OUT / 'trading-browser-checks.json').write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        print(json.dumps({'passed': len(checks), 'checks': checks}))
    finally:
        browser.close()
