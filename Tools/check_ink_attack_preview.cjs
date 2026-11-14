const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1280,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:8093/player-ink-attack-v1/preview.html');await page.waitForFunction(()=>window.previewReady);
 for(let i=0;i<3;i++){await page.evaluate(n=>{auto=false;paused=true;strike(n);age=.055;draw()},i);assert.equal(await page.evaluate(()=>previewState.particles),62);await page.locator('#stage').screenshot({path:`Book/Validation/ink-attack-${i+1}.png`});}
 await page.locator('#flip').click();assert.equal(await page.evaluate(()=>facing),-1);
 await page.locator('#toggle').click();assert.equal(await page.evaluate(()=>enabled),false);
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/ink-attack-preview.json',JSON.stringify({passed:true,stages:3,particlesPerStrike:62,errors,scope:'Design preview only; existing source drawings and Unity runtime unchanged.'},null,2));console.log('INK_ATTACK_PREVIEW_OK');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
