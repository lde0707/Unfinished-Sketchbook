const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
const p=await b.newPage({viewport:{width:1200,height:850},deviceScaleFactor:2}),errors=[];
p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error/i.test(m.text()))errors.push(m.text());});
await p.goto('http://127.0.0.1:8080/?v=player-resolution-20261020&spawn=76,120');await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6500);
const measure=()=>p.locator('#unity-canvas').evaluate(c=>{const gl=c.getContext('webgl2')||c.getContext('webgl');return{width:c.width,height:c.height,bufferWidth:gl.drawingBufferWidth,bufferHeight:gl.drawingBufferHeight,cssWidth:c.getBoundingClientRect().width,dpr:devicePixelRatio};});
const normal=await measure();assert(normal.width>=Math.ceil(normal.cssWidth*normal.dpr));assert.equal(normal.width,normal.bufferWidth);assert.equal(normal.width/normal.height,16/9);
await p.locator('#unity-canvas').screenshot({path:'Book/Validation/player-resolution-hidpi.png',timeout:60000});
await p.keyboard.press('j');await p.waitForTimeout(40);await p.locator('#unity-canvas').screenshot({path:'Book/Validation/player-resolution-attack.png',timeout:60000});
await p.setViewportSize({width:2100,height:1250});await p.waitForFunction(()=>{const c=document.querySelector('#unity-canvas');return c.width===Math.min(3840,Math.max(1920,Math.ceil(c.getBoundingClientRect().width*devicePixelRatio/16)*16));},{},{timeout:45000});await p.waitForTimeout(1000);const large=await measure();assert.equal(large.width,Math.min(3840,Math.max(1920,Math.ceil(large.cssWidth*large.dpr/16)*16)));assert.equal(large.bufferWidth,large.width);
await p.setViewportSize({width:1000,height:720});await p.waitForFunction(()=>document.querySelector('#unity-canvas').width===1920,{},{timeout:45000});await p.waitForTimeout(1000);const small=await measure();assert.equal(small.width,1920);assert.equal(small.bufferWidth,1920);assert.deepEqual(errors,[]);
fs.writeFileSync('Book/Validation/player-resolution-web.json',JSON.stringify({passed:true,normal,large,small,errors},null,2));console.log('PLAYER_RESOLUTION_WEB_OK',JSON.stringify({normal,large,small}));
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});

