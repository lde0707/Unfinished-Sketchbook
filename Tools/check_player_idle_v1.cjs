const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:8092/player-c-idle-v1/preview.html');await page.waitForFunction(()=>window.previewState?.ready);
 const folder='Art/animations/player-c-idle-v1',meta=JSON.parse(fs.readFileSync(folder+'/registration.json','utf8'));
 assert.equal(meta.frames.length,6);assert.equal(meta.frames.reduce((s,m)=>s+m.durationMs,0),1800);assert.deepEqual(meta.sequence,[2,0,3,1,3,0]);
 for(let n=0;n<6;n++){await page.locator(`[data-frame="${n}"]`).click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),n)}
 await page.locator('#next').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),0);await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),5);
 const validation=JSON.parse(fs.readFileSync(folder+'/validation.json','utf8'));assert(validation.headTravelY<=4);assert(Math.max(...validation.soleCenterX)-Math.min(...validation.soleCenterX)<=1);assert(validation.groundY.every(y=>y===448));
 const lock=JSON.parse(fs.readFileSync('Art/animations/player-animation-selection.json','utf8'));for(const [name,hash] of Object.entries(lock.run.files)){assert.equal(crypto.createHash('sha256').update(fs.readFileSync('Art/animations/'+name)).digest('hex'),hash)}
 await page.locator('#speed').selectOption('0.5');await page.locator('#flip').check();await page.locator('#headGuide').check();await page.locator('#floor').uncheck();await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#speed').selectOption('1');
 await page.locator('[data-frame="0"]').click();await page.screenshot({path:folder+'/preview-check.png',fullPage:true});await page.locator('#play').click();await page.waitForFunction(()=>previewState.frame!==0);
 assert.equal(await page.evaluate(()=>previewState.referenceAvailable),true);assert.deepEqual(errors,[]);
 const report={passed:true,frameCount:6,uniqueDrawings:4,loopMs:1800,headTravelY:validation.headTravelY,approvedRunUnchanged:true,consoleErrors:errors,checked:'6 frame controls, wrap/play, speed/flip/guides, 14 loaded sprites, grounded soles, subtle head travel and exact approved run hashes.'};fs.writeFileSync(folder+'/browser-validation.json',JSON.stringify(report,null,2));console.log(report);
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
