"""UI smoke against local Compose. Creates clearly labelled demo transactions.

Requires Python playwright and installed Google Chrome; reads ignored .env locally.
Never prints credentials, tokens or browser storage.
"""
from pathlib import Path
import json
import re
import uuid
from playwright.sync_api import sync_playwright, expect

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'test-results'
OUT.mkdir(exist_ok=True)
settings = dict(line.split('=', 1) for line in (ROOT / '.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
base = 'http://localhost:' + settings.get('WEB_PORT', '8080')
suffix = uuid.uuid4().hex[:8].upper()
code = 'DEMO-MIX-' + suffix
name = 'Đất trồng đa dụng (mẫu ' + suffix + ')'
errors = []
checks = []

with sync_playwright() as p:
    browser = p.chromium.launch(channel='chrome', headless=True)
    try:
        page = browser.new_page(viewport={'width': 1440, 'height': 1000})
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.on('console', lambda message: errors.append(message.text) if message.type == 'error' else None)
        page.goto(base)
        page.wait_for_load_state('networkidle')
        expect(page.get_by_label('Username', exact=True)).to_be_visible()
        page.screenshot(path=str(OUT / 'login.png'), full_page=True)
        page.get_by_label('Username', exact=True).fill(settings['BOOTSTRAP_USERNAME'])
        page.get_by_label('Password', exact=True).fill(settings['BOOTSTRAP_PASSWORD'])
        page.get_by_role('button', name=re.compile('^Sign in')).click()
        expect(page.get_by_role('heading', name='Inventory', exact=True)).to_be_visible()
        checks.append('JWT login through React/Nginx/API')
        page.get_by_role('button', name='Catalogue', exact=True).click()
        page.get_by_role('button', name='+ Product', exact=True).click()
        page.get_by_label('Code', exact=True).fill(code)
        page.get_by_label('Name', exact=True).fill(name)
        page.get_by_label('Brand (optional)', exact=True).fill('Demonstration catalogue')
        with page.expect_response(lambda r: r.request.method == 'POST' and r.url.endswith('/api/products')) as created:
            page.get_by_role('button', name='Save', exact=True).click()
        assert created.value.status == 201
        product = created.value.json()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Saved.')
        page.get_by_role('button', name='+ Supplier', exact=True).click()
        page.get_by_label('Code', exact=True).fill('DEMO-SUP-' + suffix)
        page.get_by_label('Name', exact=True).fill('Greenhouse Supply (demo ' + suffix + ')')
        with page.expect_response(lambda r: r.request.method == 'POST' and r.url.endswith('/api/suppliers')) as created:
            page.get_by_role('button', name='Save', exact=True).click()
        assert created.value.status == 201
        supplier = created.value.json()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Saved.')
        page.get_by_role('button', name='+ Packaging / SKU', exact=True).click()
        page.get_by_label('Code', exact=True).fill(code + '-5KG')
        page.get_by_label(re.compile('^Product')).select_option(str(product['id']))
        page.get_by_label('Package amount', exact=True).fill('5')
        page.get_by_label('Sale price (VND)', exact=True).fill('55000')
        page.get_by_label('Preferred supplier (optional)', exact=True).select_option(str(supplier['id']))
        with page.expect_response(lambda r: r.request.method == 'POST' and r.url.endswith('/api/skus')) as created:
            page.get_by_role('button', name='Save', exact=True).click()
        assert created.value.status == 201
        sku = created.value.json()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Saved.')
        checks.append('Admin product, supplier and sealed-package SKU creation')

        page.get_by_role('button', name='Purchase receipts', exact=True).click()
        page.get_by_role('button', name='+ New receipt', exact=True).click()
        page.get_by_label('Supplier', exact=True).select_option(str(supplier['id']))
        page.get_by_label('SKU 1', exact=True).select_option(str(sku['id']))
        page.get_by_label('Bags 1', exact=True).fill('6')
        page.get_by_label('Unit cost 1 (VND)', exact=True).fill('30000')
        page.get_by_role('button', name='Save draft', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Draft saved.')
        # Wait for the refreshed list and target this run's document, not an older draft.
        receipt_row = page.get_by_role('row').filter(has_text=code + '-5KG')
        receipt_row.get_by_role('button', name='Confirm receipt', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Confirmed.')
        checks.append('Receipt draft/confirm adds 6 bags using independent gRPC totals')
        page.get_by_role('button', name='Sales', exact=True).click()
        page.get_by_role('button', name='+ New sale', exact=True).click()
        page.get_by_label('SKU 1', exact=True).select_option(str(sku['id']))
        page.get_by_label('Bags 1', exact=True).fill('2')
        page.get_by_role('button', name='Save draft', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Draft saved.')
        sale_row = page.get_by_role('row').filter(has_text=code + '-5KG')
        sale_row.get_by_role('button', name='Confirm sale', exact=True).click()
        expect(page.locator('.notice[role="status"]')).to_contain_text('Confirmed.')
        expect(page.get_by_role('row').filter(has_text=code + '-5KG')).to_contain_text('110.000')
        checks.append('Sale draft/confirm deducts 2 bags and freezes 110000 VND total')

        page.get_by_role('button', name='Inventory', exact=True).click()
        page.get_by_role('searchbox', name='Search inventory').fill(code)
        row = page.get_by_role('row').filter(has_text=code + '-5KG')
        expect(row.locator('td').nth(3)).to_have_text('4')
        expect(row).to_contain_text('Low stock')
        page.screenshot(path=str(OUT / 'inventory-desktop.png'), full_page=True)
        checks.append('Live inventory shows 4 remaining bags')
        for width in [320, 768, 1024, 1440]:
            page.set_viewport_size({'width': width, 'height': 1000})
            assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), f'Page overflow at {width}px'
            expect(page.get_by_role('heading', name='Inventory', exact=True)).to_be_visible()
        page.set_viewport_size({'width': 390, 'height': 844})
        page.screenshot(path=str(OUT / 'inventory-mobile.png'), full_page=True)
        checks.append('Responsive viewport checks at 320/768/1024/1440px')

        page.set_viewport_size({'width': 1440, 'height': 1000})
        page.get_by_role('button', name='Replenishment', exact=True).click()
        suggestion = page.get_by_role('row').filter(has_text=f'SKU #{sku["id"]}')
        for attempt in range(20):
            if suggestion.count() == 1 and suggestion.locator('td').nth(2).inner_text() == '26':
                break
            page.get_by_role('button', name='Refresh', exact=True).click()
            page.wait_for_timeout(1000)
        expect(suggestion).to_have_count(1)
        expect(suggestion.locator('td').nth(2)).to_have_text('26')
        page.screenshot(path=str(OUT / 'replenishment.png'), full_page=True)
        checks.append('Redis/Worker creates one active suggestion for target30-current4=26 bags')
        page.get_by_role('button', name='Stock movements', exact=True).click()
        expect(page.get_by_role('row').filter(has_text=f'SKU #{sku["id"]}')).to_have_count(2)
        checks.append('Two stock movements are visible in the audit trail')
        page.get_by_role('button', name='Sign out', exact=True).click()
        expect(page.get_by_label('Password', exact=True)).to_be_visible()
        assert not errors, errors
        checks.append('Sign-out and zero browser console/page errors')
        (OUT / 'browser-checks.json').write_text(json.dumps({'checks': checks, 'demoSkuId': sku['id'], 'consoleErrors': errors}, indent=2), encoding='utf-8')
        print(json.dumps({'passed': len(checks), 'checks': checks}))
    finally:
        browser.close()
