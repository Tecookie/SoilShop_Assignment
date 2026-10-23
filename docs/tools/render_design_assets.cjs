// Render trusted local design HTML; no browser interaction with external sites.
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const dependencyRoot = process.env.CODEX_DESIGN_NODE_MODULES;
const { chromium } = require(dependencyRoot ? path.join(dependencyRoot, 'playwright') : 'playwright');

(async () => {
  const root = path.resolve(__dirname, '..', 'diagrams');
  const items = JSON.parse(fs.readFileSync(path.join(root, 'manifest.json'), 'utf8'));
  const browser = await chromium.launch({ headless: true, channel: process.env.DESIGN_BROWSER_CHANNEL || 'chrome' });
  const page = await browser.newPage({ viewport: { width: 1880, height: 1220 }, deviceScaleFactor: 2 });
  const checks = [];
  for (const item of items) {
    await page.setViewportSize({ width: item.width + 80, height: item.height + 100 });
    await page.goto(pathToFileURL(path.join(root, item.slug + '.html')).href);
    await page.locator('svg').screenshot({ path: path.join(root, 'images', item.slug + '.png') });
    const check = await page.evaluate(() => {
      const svg = document.querySelector('svg');
      const bounds = svg.viewBox.baseVal;
      const outside = [...svg.querySelectorAll('text')].filter(t => {
        const b = t.getBBox();
        return b.x < 0 || b.y < 0 || b.x + b.width > bounds.width || b.y + b.height > bounds.height;
      }).map(t => t.textContent);
      return { outside, accessible: !!svg.querySelector('title') && !!svg.querySelector('desc') };
    });
    await page.setViewportSize({ width: 390, height: 844 });
    check.mobile = await page.evaluate(() => ({
      pageOverflow: document.documentElement.scrollWidth > window.innerWidth,
      localScroll: document.querySelector('.diagram-container').scrollWidth > document.querySelector('.diagram-container').clientWidth
    }));
    await page.setViewportSize({ width: item.width + 80, height: item.height + 100 });
    checks.push({ slug: item.slug, ...check });
  }
  fs.writeFileSync(path.join(root, 'render-checks.json'), JSON.stringify(checks, null, 2) + '\n');
  await browser.close();
  const failures = checks.filter(c => c.outside.length || !c.accessible || c.mobile.pageOverflow || !c.mobile.localScroll);
  console.log(JSON.stringify({ rendered: checks.length, failures }, null, 2));
  if (failures.length) process.exitCode = 1;
})().catch(error => { console.error(error.message); process.exitCode = 1; });
