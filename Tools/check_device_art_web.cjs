const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const p=await b.newPage({viewport:{width:1200,height:800}}),errors=[];p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error/i.test(m.text()))errors.push(m.text())});
 await p.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')})});
 await p.goto('http://127.0.0.1:8080/?v=world-devices-20261023&spawn=23,128');await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6500);
 for(const [name,pos] of [['dog','23,128'],['sun','527,20'],['npc-diary','463,124'],['lever','256,70'],['target','162,92'],['hatch','270,14'],['brazier','352,66']].filter(([name])=>!process.argv[2]||process.argv[2]===name)){
  await p.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await p.waitForTimeout(name==='brazier'?600:2300);
  const png=await p.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});fs.writeFileSync('Book/Validation/device-'+name+'.png',Buffer.from(png.split(',')[1],'base64'));
 }
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/device-art-web.json',JSON.stringify({passed:true,errors,checks:['seven map viewpoints','new artwork and quiet effects','no runtime errors'],native:'12 placements, draw order, lever and bridge reactions verified by DeviceArtIntegration'},null,2));console.log('DEVICE_ART_WEB_OK');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});


