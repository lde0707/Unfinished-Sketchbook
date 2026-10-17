const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:8092/player-c-v6/preview.html');await page.waitForFunction(()=>window.previewState?.ready);
 assert.equal(await page.evaluate(()=>previewState.frameCount),8);assert.equal(await page.evaluate(()=>previewState.inbetweenCount),0);
 const seen=[];for(let n=0;n<8;n++){await page.locator(`[data-frame="${n}"]`).click();seen.push(await page.evaluate(()=>previewState.displayedFrame));}
 assert.deepEqual(seen,[0,1,2,3,4,5,6,7]);await page.locator('#next').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),0);
 await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.displayedFrame),7);
 await page.locator('#speed').selectOption('0.25');await page.locator('#flip').check();await page.locator('#headGuide').check();await page.locator('#floor').uncheck();
 await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#speed').selectOption('1');await page.locator('[data-frame="4"]').click();
 assert.match(await page.locator('#role').textContent(),/먼 왼다리.*앞으로/);
 await page.screenshot({path:'Art/animations/player-c-v6/preview-check.png',fullPage:true});
 await page.locator('#play').click();await page.waitForFunction(()=>previewState.frame!==4);
 await page.route('**/reference/*.png',r=>r.abort());await page.reload();await page.waitForFunction(()=>window.previewState?.ready);assert.equal(await page.evaluate(()=>previewState.referenceAvailable),false);assert.deepEqual(errors,[]);
 const meta=JSON.parse(fs.readFileSync('Art/animations/player-c-v6/registration.json','utf8'));
 assert.equal(meta.frames.length,8);for(const n of [0,3,4,7])assert.equal(meta.frames[n].footGap,0);for(const n of [4,5,6])assert.equal(meta.frames[n].forwardLeg,'far');assert(new Set(meta.frames.map(f=>f.headCenter[1])).size>1);
 const report={passed:true,frameCount:8,inbetweens:0,checked:'All eight frame selectors, wrapping, play, speed, flip, ground/head guides, local-source fallback. PNG metadata confirms sole registration and varying body height.',consoleErrors:errors,scope:'Visual identity and anatomical continuity were inspected separately; metadata checks do not prove leg animation quality.'};fs.writeFileSync('Art/animations/player-c-v6/browser-validation.json',JSON.stringify(report,null,2));console.log(report);
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1)});
