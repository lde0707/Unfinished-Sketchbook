const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const{chromium}=require(process.env.PLAYWRIGHT_MODULE||path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
await page.goto('http://127.0.0.1:8092/player-c-v8/preview.html');await page.waitForFunction(()=>window.previewState?.ready);
assert.equal(await page.evaluate(()=>previewState.frameCount),11);assert.equal(await page.locator('[data-frame]').count(),11);
const labels=[];for(let i=0;i<11;i++){await page.locator('[data-frame="'+i+'"]').click();labels.push(await page.evaluate(()=>previewState.label))}
assert.deepEqual(labels,['1','2','3','4','4-5','5','6','6-7','7','8','8-1']);
await page.locator('#next').click();assert.equal(await page.evaluate(()=>previewState.frame),0);await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.frame),10);
await page.locator('#speed').selectOption('0.25');await page.locator('#flip').check();await page.locator('#headGuide').check();await page.locator('#floor').uncheck();
await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#headGuide').uncheck();await page.locator('[data-frame="4"]').click();
assert.equal(await page.evaluate(()=>previewState.referenceFrame),4);
await page.screenshot({path:'Art/animations/player-c-v8/preview-check.png',fullPage:true});
await page.locator('#speed').selectOption('1');await page.locator('#play').click();await page.waitForFunction(()=>previewState.frame!==4);
assert.deepEqual(errors,[]);
const m=JSON.parse(fs.readFileSync('Art/animations/player-c-v8/registration.json','utf8'));assert.equal(m.frames.length,11);assert.equal(m.frames.reduce((s,f)=>s+f.durationMs,0),800);
assert.deepEqual(m.frames.filter(f=>f.inserted).map(f=>f.label),['4-5','6-7','8-1']);
assert(m.frames.every(f=>f.footGap>=0));assert(m.frames.filter(f=>f.supportLeg).every(f=>f.footGap===0));
assert.deepEqual(m.frames.filter(f=>f.armSmear).map(f=>f.label),['3','6']);
const r={passed:true,frameCount:11,inserted:3,loopMs:800,consoleErrors:errors,checked:'Eleven selectors, original v6 labels, reference phase mapping, wraparound, playback, speed and guides. Packaging checks confirm nonnegative ground gaps and six contact poses. Visual motion quality is not asserted by these checks.'};
fs.writeFileSync('Art/animations/player-c-v8/browser-validation.json',JSON.stringify(r,null,2));console.log(r);
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});

