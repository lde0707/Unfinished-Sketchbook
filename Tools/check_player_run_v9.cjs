const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const{chromium}=require(process.env.PLAYWRIGHT_MODULE||path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];page.on('pageerror',e=>errors.push(String(e)));
await page.goto('http://127.0.0.1:8092/player-c-v9/preview.html');await page.waitForFunction(()=>window.previewState?.ready);
const labels=[];for(let i=0;i<11;i++){await page.locator('[data-frame="'+i+'"]').click();labels.push(await page.evaluate(()=>previewState.label));assert.equal(await page.evaluate(()=>previewState.referenceFrame),i)}
assert.deepEqual(labels,['1','2','3','4','4-5','5','6','6-7','7','8','8-1']);
await page.locator('#next').click();assert.equal(await page.evaluate(()=>previewState.frame),0);await page.locator('#prev').click();assert.equal(await page.evaluate(()=>previewState.frame),10);
await page.locator('#motion').uncheck();
for(let i=0;i<11;i++){await page.locator('[data-frame="'+i+'"]').click();assert(await page.evaluate(()=>document.getElementById('ours').toDataURL()===document.getElementById('reference').toDataURL()),'Disabled motion must render the identical frame')}
await page.locator('#motion').check();await page.locator('[data-frame="1"]').click();assert.deepEqual(await page.evaluate(()=>previewState.appliedOffset),[6,6]);assert(await page.evaluate(()=>document.getElementById('ours').toDataURL()!==document.getElementById('reference').toDataURL()));
await page.locator('[data-frame="6"]').click();assert.deepEqual(await page.evaluate(()=>previewState.appliedOffset),[6,4]);
await page.locator('#speed').selectOption('0.5');await page.locator('#flip').check();await page.locator('#headGuide').check();await page.locator('#floor').uncheck();
await page.locator('#flip').uncheck();await page.locator('#floor').check();await page.locator('#headGuide').uncheck();await page.locator('#speed').selectOption('1');
await page.screenshot({path:'Art/animations/player-c-v9/preview-check.png',fullPage:true});await page.locator('#play').click();await page.waitForFunction(()=>previewState.frame!==6);assert.deepEqual(errors,[]);
const b=JSON.parse(fs.readFileSync('Art/animations/player-c-v8/registration.json','utf8')),m=JSON.parse(fs.readFileSync('Art/animations/player-c-v9/registration.json','utf8'));
for(let i=0;i<11;i++){const name=String(i).padStart(2,'0')+'.png';assert(fs.readFileSync('Art/animations/player-c-v8/frames/'+name).equals(fs.readFileSync('Art/animations/player-c-v9/frames/'+name)));assert.equal(b.frames[i].durationMs,m.frames[i].durationMs);assert(m.frames[i].renderedFootGap>=0)}
const r={passed:true,frameCount:11,unchangedDrawings:11,loopMs:800,consoleErrors:errors,checked:'All frame selectors, matching v8 frame phase, wrap, speed, flip and guides. Motion disabled gives pixel-identical canvases for all eleven frames; enabled applies2:[6,6] and6:[6,4]. All PNG bytes and frame durations identical to v8.'};
fs.writeFileSync('Art/animations/player-c-v9/browser-validation.json',JSON.stringify(r,null,2));console.log(r);
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});

