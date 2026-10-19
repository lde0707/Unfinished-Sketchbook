const fs=require('node:fs'),path=require('node:path');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|VFX/.test(m.text()))console.log(m.text());});
 await page.route(url=>url.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="640" height="360"')});});
 await page.goto('http://127.0.0.1:8080/?v=cc0-vfx-20261019&spawn=252,131');
 await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await page.waitForTimeout(6000);
 await page.locator('#unity-canvas').click();
 await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','252,131'));await page.waitForTimeout(1000);
 await page.keyboard.down('d');await page.waitForTimeout(220);await page.keyboard.up('d');
 for(let i=0;i<3;i++){
 await page.keyboard.press('j');await page.waitForTimeout(100);await page.screenshot({path:`Book/Validation/vfx-hit-${i}.png`});await page.waitForTimeout(350);
 }
 await page.keyboard.down('Space');await page.waitForTimeout(180);await page.screenshot({path:'Book/Validation/vfx-jump.png'});
 await page.keyboard.press('ShiftLeft');await page.waitForTimeout(50);await page.screenshot({path:'Book/Validation/vfx-air-dash.png'});
 await page.keyboard.up('Space');await page.waitForTimeout(1300);await page.screenshot({path:'Book/Validation/vfx-rest.png'});
 if(errors.length)throw new Error(errors.join('\n'));
 fs.writeFileSync('Book/Validation/vfx-web.json',JSON.stringify({passed:true,errors,checks:'WebGL loads; actual keyboard attack, jump, air dash; screenshots captured'},null,2));
 console.log('VFX_WEB_OK');
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
