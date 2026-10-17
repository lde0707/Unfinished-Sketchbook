const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:8092/player-c-v6-sprint/preview.html');await page.waitForFunction(()=>window.previewState?.ready);
 assert.equal(await page.evaluate(()=>previewState.referenceAvailable),true);
 for(let n=0;n<12;n++){await page.locator(`[data-frame="${n}"]`).click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),n);}
 await page.locator('#next').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),0);
 await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),11);
 await page.locator('#speed').selectOption('0.25');await page.locator('#flip').check();await page.locator('#headGuide').check();await page.locator('#floor').uncheck();
 await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#speed').selectOption('1');
 await page.locator('#bridgeA').click();assert.deepEqual(await page.evaluate(()=>previewState.sequence),[3,4,5,6]);
 await page.locator('#bridgeB').click();assert.deepEqual(await page.evaluate(()=>previewState.sequence),[9,10,11,0]);
 await page.locator('#all').click();assert.equal(await page.evaluate(()=>previewState.sequence.length),12);
 await page.locator('[data-frame="7"]').click();await page.screenshot({path:'Art/animations/player-c-v6-sprint/preview-check.png',fullPage:true});
 await page.locator('#play').click();await page.waitForFunction(()=>previewState.frame!==7);
 const meta=JSON.parse(fs.readFileSync('Art/animations/player-c-v6-sprint/registration.json','utf8'));
 assert.equal(meta.frames.length,12);assert.equal(meta.frames.reduce((s,f)=>s+f.durationMs,0),800);
 assert.deepEqual(meta.frames.filter(f=>f.inserted).map(f=>f.frame),[5,6,11,12]);
 assert.equal(meta.frames[7].oldPose,'6');assert.equal(meta.frames[7].footGap,6);
 for(const i of [0,3,4,5,6,9,10,11])assert.equal(meta.frames[i].footGap,0);
 assert.deepEqual(errors,[]);
 const report={passed:true,frameCount:12,inbetweens:4,loopMs:800,checked:'All 12 selectors, playback, wrap, two transition loops, flip, speed and guides; all 20 comparison images loaded. Feet contact and old-frame6 low clearance checked.',consoleErrors:errors,scope:'Anatomical progression and appearance are reviewed visually, not proven by these checks.'};fs.writeFileSync('Art/animations/player-c-v6-sprint/browser-validation.json',JSON.stringify(report,null,2));console.log(report);
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
