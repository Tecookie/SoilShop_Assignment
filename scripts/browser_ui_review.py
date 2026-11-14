"""Read-only UI review against the local Vite/API stack; never confirms trades."""
from pathlib import Path
import json, re
from playwright.sync_api import sync_playwright, expect

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'test-results' / 'ui-redesign'
OUT.mkdir(parents=True, exist_ok=True)
settings = dict(line.split('=', 1) for line in (ROOT / '.env').read_text().splitlines() if '=' in line and not line.startswith('#'))
names = ['Inventory', 'Dashboard', 'Tìm đất phù hợp', 'Purchase receipts', 'Sales', 'Stock movements', 'Replenishment', 'Stock counts', 'Daily summaries', 'Catalogue', 'Suppliers', 'SKU prices & stock rules', 'Staff accounts', 'Database backups']
checks = []

with sync_playwright() as p:
    browser = p.chromium.launch(channel='msedge')
    page = browser.new_page(viewport={'width':1440, 'height':1000})
    errors = []
    page.on('pageerror', lambda error: errors.append(str(error)))
    page.goto('http://127.0.0.1:5173')
    page.screenshot(path=str(OUT / 'login-desktop.png'), full_page=True)
    page.get_by_label('Username', exact=True).fill(settings['BOOTSTRAP_USERNAME'])
    password = page.get_by_label('Password', exact=True)
    password.fill(settings['BOOTSTRAP_PASSWORD'])
    page.get_by_role('button', name='Show password', exact=True).click()
    expect(password).to_have_attribute('type', 'text')
    page.get_by_role('button', name='Hide password', exact=True).click()
    expect(password).to_have_attribute('type', 'password')
    checks.append('Password visibility toggle keeps the entered value')
    page.get_by_role('button', name='Sign in', exact=True).click()
    expect(page.get_by_role('heading', name='Inventory', exact=True)).to_be_visible()

    def navigate(name):
        if page.viewport_size['width'] <= 900:
            page.get_by_role('button', name='Open menu', exact=True).click()
        page.get_by_role('navigation', name='Main navigation').get_by_role('button', name=name, exact=True).click()
        page.wait_for_load_state('networkidle')
        expect(page.locator('.page-heading h1')).to_be_visible()

    def no_overflow(label):
        assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth'), label

    for width in (1440, 768, 390):
        page.set_viewport_size({'width':width, 'height':1000 if width > 600 else 844})
        for i, name in enumerate(names):
            navigate(name)
            no_overflow(f'{name} at {width}')
            if name in ('Inventory','Dashboard','Purchase receipts','Staff accounts','Tìm đất phù hợp'):
                page.screenshot(path=str(OUT / f'{width}-{i}.png'), full_page=True)
        for name, button in [('Purchase receipts', '+ New receipt'), ('Sales', '+ New sale'), ('Catalogue', '+ Product'), ('Staff accounts', 'New account')]:
            navigate(name)
            page.get_by_role('button', name=button, exact=True).click()
            page.wait_for_load_state('networkidle')
            expect(page.locator('main form').first).to_be_visible()
            if name == 'Purchase receipts':
                expect(page.get_by_label('Supplier', exact=True).locator('option').nth(1)).to_be_attached()
                expect(page.get_by_label('SKU 1', exact=True).locator('option').nth(1)).to_be_attached()
            no_overflow(f'{name} form at {width}')
            page.screenshot(path=str(OUT / f'{width}-form-{names.index(name)}.png'), full_page=True)
        checks.append(f'All 14 screens render without page overflow at {width}px')
        checks.append(f'Receipt, sale, product and account forms fit at {width}px without saving')

    page.get_by_role('button', name='Open menu', exact=True).click()
    expect(page.get_by_role('button', name='Close menu', exact=True)).to_be_focused()
    page.screenshot(path=str(OUT / 'navigation-mobile.png'), full_page=True)
    page.keyboard.press('Shift+Tab')
    expect(page.get_by_role('navigation', name='Main navigation').get_by_role('button').last).to_be_focused()
    page.keyboard.press('Tab')
    expect(page.get_by_role('button', name='Close menu', exact=True)).to_be_focused()
    page.keyboard.press('Escape')
    expect(page.get_by_role('button', name='Open menu', exact=True)).to_be_focused()
    expect(page.locator('.sidebar')).not_to_be_visible()
    checks.append('Mobile drawer opens, traps keyboard focus and closes with Escape')

    navigate('Dashboard')
    page.get_by_role('button', name='Today', exact=True).click()
    page.wait_for_load_state('networkidle')
    expect(page.get_by_role('button', name='Today', exact=True)).to_have_attribute('aria-pressed','true')
    expect(page.locator('.dashboard-metrics')).to_be_visible()
    checks.append('Dashboard quick range loads API statistics')

    navigate('Inventory')
    page.route('**/api/skus?**', lambda route: route.fulfill(status=200, content_type='application/json', body=json.dumps({'items':[], 'total':0, 'page':1, 'pageSize':20})))
    page.get_by_role('button', name='Refresh stock', exact=True).click()
    expect(page.get_by_role('status').filter(has_text='No matching SKUs')).to_be_visible()
    page.screenshot(path=str(OUT / 'empty-mobile.png'), full_page=True)
    page.unroute('**/api/skus?**')
    page.route('**/api/skus?**', lambda route: route.fulfill(status=503, content_type='application/json', body='{"title":"Inventory temporarily unavailable. Please retry."}'))
    page.get_by_role('button', name='Refresh stock', exact=True).click()
    expect(page.get_by_role('alert')).to_contain_text('temporarily unavailable')
    page.screenshot(path=str(OUT / 'error-mobile.png'), full_page=True)
    page.unroute('**/api/skus?**')
    page.get_by_role('button', name='Refresh stock', exact=True).click()
    expect(page.get_by_role('alert')).to_have_count(0)
    checks.append('Empty and service-error states render; refresh recovers')
    page.get_by_role('button', name='Sign out', exact=True).click()
    expect(page.get_by_label('Username', exact=True)).to_be_visible()
    no_overflow('Mobile login')
    page.screenshot(path=str(OUT / 'login-mobile.png'), full_page=True)
    checks.append('Sign-out and mobile login remain usable')
    assert not errors, errors
    checks.append('No uncaught browser exceptions')
    browser.close()

(OUT / 'checks.json').write_text(json.dumps({'checks': checks, 'viewports':[1440,768,390], 'scope':'Real login and read-only navigation; empty/error responses intercepted only in test browser. No trading, count or administration mutations.'}, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(checks, ensure_ascii=False, indent=2))
