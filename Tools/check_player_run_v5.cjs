const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1220,height:1050}}),errors=[];
 page.on('pageerror',e=>errors.push(String(e)));
 await page.goto(process.env.RUN_PREVIEW_URL||'http://127.0.0.1:8092/player-c-v5/preview.html');
 await page.waitForFunction(()=>window.previewState?.ready);
 assert.equal(await page.evaluate(()=>previewState.referenceAvailable),true);
 await page.evaluate(()=>setPreviewFrame(15));await page.locator('#next').click();
 assert.equal(await page.evaluate(()=>previewState.displayedFrame),0);
 await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),15);
 await page.locator('#speed').selectOption('0.25');await page.locator('#period').selectOption('800');
 await page.locator('#keys').check();assert.equal(await page.evaluate(()=>previewState.displayedFrame),14);
 await page.locator('#flip').check();await page.locator('#floor').uncheck();
 await page.locator('#keys').uncheck();await page.locator('#flip').uncheck();await page.locator('#floor').check();
 await page.locator('#period').selectOption('640');await page.locator('#speed').selectOption('1');
 await page.evaluate(()=>setPreviewFrame(4));
 await page.screenshot({path:'Art/animations/player-c-v5/preview-check.png',fullPage:true});
 await page.locator('#play').click();const before=await page.evaluate(()=>previewState.frame);
 await page.waitForFunction(n=>previewState.frame!==n,before);
 assert.deepEqual(errors,[]);
 // Public checkout must remain usable without the non-redistributable originals.
 await page.route('**/reference/*.png',r=>r.abort());await page.reload();
 await page.waitForFunction(()=>window.previewState?.ready);
 assert.equal(await page.evaluate(()=>previewState.referenceAvailable),false);
 assert.deepEqual(errors,[]);
 const report={passed:true,controls:['pause/play','previous/next loop wrap','slow speed','period','keys only','flip','floor'],originalsPresent:true,missingOriginalFallback:true,consoleErrors:errors};
 fs.writeFileSync('Art/animations/player-c-v5/browser-validation.json',JSON.stringify(report,null,2));console.log(report);
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1)});
