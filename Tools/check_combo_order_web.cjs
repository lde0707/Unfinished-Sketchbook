const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1200,height:800}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error/i.test(m.text()))errors.push(m.text())});
 await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')})});
 await page.goto('http://127.0.0.1:8080/?v=thrust-first-20261022&spawn=78,123');
 await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await page.locator('#unity-canvas').click();await page.waitForTimeout(6500);
 for(const dir of ['a','d']){await page.keyboard.down(dir);for(let i=0;i<8;i++){await page.keyboard.press('j');if(i===2)await page.keyboard.press('Space');if(i===6)await page.keyboard.press('Shift');await page.waitForTimeout(140)}await page.keyboard.up(dir)}
 await page.waitForTimeout(500);
 const png=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});fs.writeFileSync('Book/Validation/combo-order-web.png',Buffer.from(png.split(',')[1],'base64'));
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/combo-order-web.json',JSON.stringify({passed:true,errors,scope:'WebGL load and steering/jump/dash attack smoke. Exact 3-hit order, timings, transitions and hit volumes verified by Unity editor validation.'},null,2));console.log('COMBO_ORDER_WEB_OK');
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
