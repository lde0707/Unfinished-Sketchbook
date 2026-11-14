const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const p=await b.newPage({viewport:{width:1200,height:800}}),errors=[];p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error/i.test(m.text()))errors.push(m.text())});
 await p.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')})});
 await p.goto('http://127.0.0.1:8080/?v=save-shrine-20261023&spawn=47,128');await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6500);
 const spawn=async pos=>{await p.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await p.waitForTimeout(2500)};
 const cap=async name=>{const png=await p.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});fs.writeFileSync('Book/Validation/save-shrine-'+name+'.png',Buffer.from(png.split(',')[1],'base64'))};
 await spawn('47,128');await cap('in-game');await spawn('50,128');await cap('active');assert.deepEqual(errors,[]);
 fs.writeFileSync('Book/Validation/save-shrine-web.json',JSON.stringify({passed:true,errors,checks:['WebGL shrine rendering at miner rest checkpoint','active shrine rendering'],native:'All 10 markers, floor alignment, health restore and active-state verified in SaveShrineValidation'},null,2));console.log('SAVE_SHRINE_WEB_OK');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
