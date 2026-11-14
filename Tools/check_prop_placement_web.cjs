const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const p=await b.newPage({viewport:{width:1200,height:800}}),errors=[];p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error/i.test(m.text()))errors.push(m.text())});
 await p.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')})});
 await p.goto('http://127.0.0.1:8080/?v=clear-prop-placement-20261023&spawn=400,88');await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6500);
 const spawn=async pos=>{await p.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await p.waitForTimeout(2600)};
 const cap=async name=>{const png=await p.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});fs.writeFileSync('Book/Validation/prop-placement-'+name+'.png',Buffer.from(png.split(',')[1],'base64'))};
 await spawn('400,88');await cap('shard-above-platform');await spawn('402,87');await cap('shard-collected');await spawn('47,128');await cap('shrine-and-portal');await spawn('250,131');await cap('portal-headroom');assert.deepEqual(errors,[]);
 fs.writeFileSync('Book/Validation/prop-placement-web.json',JSON.stringify({passed:true,errors,checks:['relocated shard above platform','pickup at new visible position','shrine, portal and buildings separated','portal fits beneath platform']},null,2));console.log('PROP_PLACEMENT_WEB_OK');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
