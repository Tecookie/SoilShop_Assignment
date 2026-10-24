"""Read-only UI checks for the explicit demo seed; no token/storage inspection."""
import json
from pathlib import Path
from playwright.sync_api import sync_playwright, expect

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'test-results'
settings = dict(line.split('=', 1) for line in (ROOT / '.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
base = 'http://localhost:' + settings.get('WEB_PORT', '8080')
groups = [('GeneralPurpose', 'Đất trồng đa dụng', 4), ('FoliageMix', 'Đất trồng kiểng lá', 4),
          ('FloweringMix', 'Đất trồng cây hoa', 2), ('SucculentMix', 'Đất trồng sen đá / xương rồng', 2),
          ('OrchidMix', 'Giá thể trồng lan', 2), ('SeedStartingMix', 'Giá thể ươm hạt', 2)]
errors = []
checks = []

with sync_playwright() as p:
    browser = p.chromium.launch(channel='chrome', headless=True)
    try:
        page = browser.new_page(viewport={'width': 1440, 'height': 1080})
        page.on('pageerror', lambda error: errors.append(str(error)))
        page.on('console', lambda message: errors.append(message.text) if message.type == 'error' else None)
        page.goto(base)
        page.wait_for_load_state('networkidle')
        page.get_by_label('Username', exact=True).fill(settings['BOOTSTRAP_USERNAME'])
        page.get_by_label('Password', exact=True).fill(settings['BOOTSTRAP_PASSWORD'])
        page.get_by_role('button', name='Sign in →', exact=True).click()
        expect(page.get_by_role('heading', name='Inventory', exact=True)).to_be_visible()
        page.get_by_role('searchbox', name='Search inventory').fill('SEED-V1-')
        expect(page.locator('tbody tr')).to_have_count(16)
        expect(page.get_by_role('row').filter(has_text='SEED-V1-P02-5L').locator('td').nth(3)).to_have_text('1')
        expect(page.get_by_role('row').filter(has_text='SEED-V1-P01-5KG')).to_contain_text('Out of stock')
        expect(page.get_by_role('row').filter(has_text='SEED-V1-P08-10L')).to_contain_text('Inactive')
        page.screenshot(path=str(OUT / 'seed-inventory.png'), full_page=True)
        checks.append('16 seeded SKUs display normal, low, last-bag, zero and inactive stock')
        for code, label, count in groups:
            page.get_by_role('combobox', name='Filter inventory by type').select_option(code)
            expect(page.locator('tbody tr')).to_have_count(count)
            expect(page.locator('tbody tr td:first-child small:last-child')).to_have_text([label] * count)
        checks.append('All six inventory product-type filters return the matching SKU counts')
        page.get_by_role('combobox', name='Filter inventory by type').select_option('Unclassified')
        expect(page.locator('tbody tr')).to_have_count(0)
        expect(page.get_by_role('status')).to_contain_text('No matching SKUs')
        checks.append('Unclassified filter has an explicit empty state for the classified seed')
        page.get_by_role('combobox', name='Filter inventory by type').select_option('OrchidMix')
        expect(page.locator('tbody tr')).to_have_count(2)
        for width in [320, 768, 1024, 1440]:
            page.set_viewport_size({'width': width, 'height': 900})
            assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), f'Page overflow at {width}px'
        page.set_viewport_size({'width': 1440, 'height': 1080})
        checks.append('Category control/table stay inside the page at four viewport sizes')
        page.get_by_role('button', name='Catalogue', exact=True).click()
        page.get_by_label('Filter catalogue by type', exact=True).select_option('OrchidMix')
        expect(page.locator('tbody tr')).to_have_count(1)
        expect(page.locator('tbody tr')).to_contain_text('Giá thể trồng lan [mẫu]')
        page.get_by_role('button', name='Edit', exact=True).click()
        expect(page.get_by_label('Product type', exact=True)).to_have_value('OrchidMix')
        page.get_by_role('button', name='Close form', exact=True).click()
        page.screenshot(path=str(OUT / 'seed-catalogue.png'), full_page=True)
        checks.append('Catalogue filtering and edit form preserve the selected product type')
        page.get_by_role('button', name='Sign out', exact=True).click()
        page.get_by_label('Username', exact=True).fill(settings['DEMO_EMPLOYEE_USERNAME'])
        page.get_by_label('Password', exact=True).fill(settings['DEMO_EMPLOYEE_PASSWORD'])
        page.get_by_role('button', name='Sign in →', exact=True).click()
        expect(page.get_by_role('heading', name='Inventory', exact=True)).to_be_visible()
        expect(page.get_by_role('button', name='Catalogue', exact=True)).to_have_count(0)
        expect(page.get_by_role('button', name='Purchase receipts', exact=True)).to_be_visible()
        expect(page.get_by_role('button', name='Sales', exact=True)).to_be_visible()
        checks.append('Seeded Employee logs in and has operational navigation without Admin catalogue')
        assert not errors, errors
        result = {'checks': checks, 'consoleErrors': errors, 'readOnly': True}
        (OUT / 'seed-browser-checks.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
        print(json.dumps({'passed': len(checks), 'checks': checks}))
    finally:
        browser.close()
