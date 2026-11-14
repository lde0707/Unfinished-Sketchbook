const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
const p=await b.newPage({viewport:{width:1400,height:900}}),errors=[];
p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
await p.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')});});
await p.goto('http://127.0.0.1:8080/?v=world-darker20-20261025&spawn=236,131');
await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6000);
const data=await p.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});
fs.writeFileSync('Book/Validation/camera-grade-web.png',Buffer.from(data.split(',')[1],'base64'));
assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/camera-grade-web.json',JSON.stringify({passed:true,errors,publicBufferUnchanged:true},null,2));console.log('CAMERA_GRADE_WEB_OK');
}finally{await b.close();}})().catch(e=>{console.error(e);process.exit(1)});
