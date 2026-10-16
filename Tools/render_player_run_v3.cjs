const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
let playwright;
try { playwright = require('playwright'); }
catch { playwright = require(process.env.PLAYWRIGHT_MODULE || path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright')); }
const root = path.resolve(__dirname, '..');
const out = path.join(root, 'Art/animations/player-c-v3');
(async () => {
  const browser = await playwright.chromium.launch({
    executablePath: process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true
  });
  try {
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', e => errors.push(String(e)));
    await page.goto(process.env.RUN_PREVIEW_URL || 'http://127.0.0.1:8092/player-c-v3/preview.html');
    await page.waitForFunction(() => typeof window.exportRunFrame === 'function');
    fs.mkdirSync(path.join(out, 'frames'), {recursive: true});
    for (let n = 0; n < 36; n++) {
      const data = await page.evaluate(n => window.exportRunFrame(n), n);
      fs.writeFileSync(path.join(out, 'frames', `${String(n).padStart(2,'0')}.png`), Buffer.from(data.split(',')[1], 'base64'));
    }
    const report = await page.evaluate(async () => {
      const {footPath} = await import('./rig.js');
      const result = window.checkRun();
      const eps = 1e-6;
      const dist = (a,b) => Math.hypot(a.x-b.x,a.y-b.y);
      result.loopPositionError = dist(footPath(0),footPath(1));
      result.maxBoundaryVelocityDifference = Math.max(...[0,.38].map(p => {
        const a=footPath(p-eps), b=footPath(p), c=footPath(p+eps);
        return Math.hypot((b.x-a.x)/eps-(c.x-b.x)/eps,(b.y-a.y)/eps-(c.y-b.y)/eps);
      }));
      return result;
    });
    assert(report.maxBoneLengthError < 1e-6);
    assert.equal(report.groundError, 0);
    assert.equal(report.loopPositionError, 0);
    assert(report.maxBoundaryVelocityDifference < .1);
    await page.locator('#play').click();
    await page.locator('#speed').selectOption('0.25');
    await page.locator('#scrub').fill('35');
    await page.locator('#next').click();
    await page.waitForFunction(() => document.getElementById('status').textContent.includes('1 / 36'));
    await page.locator('#flip').click();
    await page.locator('#ground').uncheck();
    await page.locator('#sync').uncheck();
    assert.deepEqual(errors, []);
    report.controls = 'Pause, speed, scrub, wraparound next frame, flip, ground and comparison period checked.';
    report.scope = 'Numerical and browser checks only; not visual approval or Unity locomotion validation.';
    fs.writeFileSync(path.join(out, 'validation.json'), JSON.stringify(report, null, 2));
    console.log(report);
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
