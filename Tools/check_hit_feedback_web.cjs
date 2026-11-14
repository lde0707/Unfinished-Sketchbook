const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
const p=await b.newPage({viewport:{width:1200,height:850}}),errors=[];
p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error|SendMessage.*(not found|no receiver)/i.test(m.text()))errors.push(m.text());});
await p.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')});});
await p.goto('http://127.0.0.1:8080/?v=hit-feedback-20261020&spawn=210,128');await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6500);
const cap=async name=>{const data=await p.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png');});fs.writeFileSync('Book/Validation/hit-feedback-'+name+'.png',Buffer.from(data.split(',')[1],'base64'));};
await p.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','210,128'));await p.waitForTimeout(800);
await p.evaluate(()=>unityInstance.SendMessage('003 range (214, 128)','ReceiveHit',1));await cap('enemy');
await p.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','211,128'));await p.waitForTimeout(200);
for(let i=0;i<24;i++){await p.waitForTimeout(50);await cap('player-'+i);}
assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/hit-feedback-web.json',JSON.stringify({passed:true,errors,note:'Actual enemy ReceiveHit callback and natural turret contact; shader runtime smoke.'},null,2));console.log('HIT_FEEDBACK_WEB_OK');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
