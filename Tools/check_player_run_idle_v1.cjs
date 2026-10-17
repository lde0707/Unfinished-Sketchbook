const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1100,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:8092/player-c-run-idle-v1/preview.html');await page.waitForFunction(()=>previewState.ready);
 // Step in small increments, identical to normal frame clock updates.
 let result=await page.evaluate(()=>{previewDebug.restart(true);previewState.playing=false;for(let i=0;i<72;i++)previewDebug.advance(20);return {mode:previewState.mode,history:previewState.history}});
 assert.equal(result.mode,'stop');assert.deepEqual(result.history,['run','stop']);
 result=await page.evaluate(()=>{for(let i=0;i<16;i++)previewDebug.advance(20);return {mode:previewState.mode,history:previewState.history,frame:previewState.frame}});
 assert.equal(result.mode,'idle');assert.deepEqual(result.history,['run','stop','idle']);assert.equal(result.frame,0);
 await page.evaluate(()=>{for(let i=0;i<90;i++)previewDebug.advance(20)});assert.equal(await page.evaluate(()=>previewState.frame),0);
 // Stop can be requested at any phase; transition starts at the matching contact.
 for(let f=0;f<8;f++){
  await page.evaluate(f=>{previewDebug.restart(false);previewState.playing=false;previewState.frame=f;previewState.stopRequested=true},f);
  await page.evaluate(()=>{for(let i=0;i<32;i++){if(previewState.mode==='run')previewDebug.advance(20)}});
  assert.equal(await page.evaluate(()=>previewState.mode),'stop');
 }
 for(let i=0;i<4;i++){await page.locator(`[data-stop="${i}"]`).click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),i)}
 await page.locator('#contact').click();assert.equal(await page.evaluate(()=>previewState.mode),'run');await page.locator('#idle').click();assert.equal(await page.evaluate(()=>previewState.mode),'idle');
 await page.locator('#speed').selectOption('0.5');await page.locator('#flip').check();await page.locator('#floor').uncheck();await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#speed').selectOption('1');
 await page.locator('[data-stop="1"]').click();await page.screenshot({path:'Art/animations/player-c-run-idle-v1/preview-check.png',fullPage:true});
 await page.locator('#run').click();await page.locator('#stop').click();await page.waitForFunction(()=>previewState.mode==='idle');
 const reg=JSON.parse(fs.readFileSync('Art/animations/player-c-run-idle-v1/registration.json','utf8'));assert.equal(reg.frames.reduce((s,m)=>s+m.durationMs,0),320);assert(reg.frames.every(m=>Math.abs(m.soleCenterX-reg.plantedSoleX)<=.5));
 const lock=JSON.parse(fs.readFileSync('Art/animations/player-animation-selection.json','utf8'));for(const [name,hash] of Object.entries(lock.run.files))assert.equal(crypto.createHash('sha256').update(fs.readFileSync('Art/animations/'+name)).digest('hex'),hash);
 assert.deepEqual(errors,[]);const report={passed:true,transitionDurationMs:320,approvedRunUnchanged:true,checked:'Automatic run-stop-idle sequence, idle repeat, stop requested in all 8 run phases, manual stop button, 4 stop selectors, controls, planted sole and approved hashes.',consoleErrors:errors};fs.writeFileSync('Art/animations/player-c-run-idle-v1/browser-validation.json',JSON.stringify(report,null,2));console.log(report);
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
