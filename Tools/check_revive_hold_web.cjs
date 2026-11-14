const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1200,height:800}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|SendMessage.*not found|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
 await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')});});
 await page.goto('http://127.0.0.1:8080/?v=no-ranges-revive-hold-20261024&spawn=255,34');
 await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('#unity-canvas').click();await page.waitForTimeout(6000);
 async function capture(name){const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png');});const file='Book/Validation/'+name+'.png';fs.writeFileSync(file,Buffer.from(data.split(',')[1],'base64'));assert.ok((await sharp(file).stats()).entropy>1,'rendered game '+name);}
 await page.evaluate(()=>{unityInstance.SendMessage('World Prototype','PreviewPosition','255,34');});await page.waitForTimeout(1000);
 await page.evaluate(()=>unityInstance.SendMessage('047 boss (258, 34)','BeginPattern',2));await page.waitForTimeout(600);await capture('boss-no-range-web');
 await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','78,123'));await page.waitForTimeout(1000);
 await page.evaluate(()=>unityInstance.SendMessage('World Prototype','BeginDeath'));await page.waitForTimeout(1200);await capture('revive-lying-web');await page.waitForTimeout(2800);await capture('revive-finished-web');
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/revive-hold-ranges-web.json',JSON.stringify({passed:true,realWebGL:true,errors},null,2));console.log('REVIVE_HOLD_WEB_OK');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});
