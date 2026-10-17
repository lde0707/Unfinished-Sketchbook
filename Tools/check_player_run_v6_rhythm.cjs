const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:8092/player-c-v6-rhythm/preview.html');await page.waitForFunction(()=>window.previewState?.ready);
 const folder='Art/animations/player-c-v6-rhythm',meta=JSON.parse(fs.readFileSync(folder+'/registration.json','utf8'));
 assert.equal(meta.frames.length,8);assert.equal(meta.frames.reduce((s,m)=>s+m.durationMs,0),640);assert.deepEqual(meta.frames.map(m=>m.durationMs),Array(8).fill(80));
 assert.deepEqual(meta.removedPreviousFrames,[1,4,7,10]);
 for(let n=0;n<8;n++){await page.locator(`[data-frame="${n}"]`).click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),n);assert.equal(await page.evaluate(()=>previewState.referenceFrame),meta.frames[n].referenceFrame);}
 await page.locator('#next').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),0);await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),7);
 for(const m of meta.frames){if(m.reusedExact){const a=fs.readFileSync(`${folder}/frames/${String(m.frame-1).padStart(2,'0')}.png`),b=fs.readFileSync(`Art/animations/player-c-v6-sprint/frames/${String(m.previousFrame-1).padStart(2,'0')}.png`);assert(a.equals(b))}else assert.equal(m.footGap,0)}
 await page.locator('#speed').selectOption('0.25');await page.locator('#flip').check();await page.locator('#headGuide').check();await page.locator('#floor').uncheck();await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#speed').selectOption('1');
 await page.locator('[data-frame="1"]').click();await page.screenshot({path:folder+'/preview-check.png',fullPage:true});await page.locator('#play').click();await page.waitForFunction(()=>previewState.frame!==1);
 assert.equal(await page.evaluate(()=>previewState.referenceAvailable),true);assert.deepEqual(errors,[]);
 const report={passed:true,frameCount:8,uniformFrameMs:80,loopMs:640,exactReusedFrames:[1,4,5,8],checked:'8 selectors, matched paused reference, wrap, playback, speed, flip, guides, 20 loaded comparison images, exact reused files and grounded edited contacts.',consoleErrors:errors};fs.writeFileSync(folder+'/browser-validation.json',JSON.stringify(report,null,2));console.log(report);
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
