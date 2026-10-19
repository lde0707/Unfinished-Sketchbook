const path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
const page=await b.newPage({viewport:{width:1200,height:850}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
await page.goto('http://127.0.0.1:8093/player-death-revive-v1/preview.html');await page.waitForFunction(()=>window.previewReady);
for(const phase of ['death','revive']){await page.locator('#'+phase).click();await page.locator('#pause').click();
for(let i=0;i<5;i++)await page.locator('#next').click();await page.waitForFunction(p=>previewState.phase===p&&previewState.frame===5,phase);
await page.locator('#stage').screenshot({path:`Art/samples/player-death-revive-v1/${phase}-preview.png`});}
await page.locator('#cycle').click();await page.waitForFunction(()=>previewState.phase==='death');await page.waitForFunction(()=>previewState.phase==='revive');
assert.deepEqual(errors,[]);console.log('DEATH_REVIVE_PREVIEW_OK');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
