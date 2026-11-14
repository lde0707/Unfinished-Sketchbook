const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const b=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
const p=await b.newPage({viewport:{width:1400,height:900}}),errors=[];
p.on('pageerror',e=>errors.push(e.message));p.on('console',m=>{if(/Exception|Shader.*error|SendMessage.*not found/i.test(m.text()))errors.push(m.text())});
await p.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1920;canvas.height=1080;return;')})});
await p.goto('http://127.0.0.1:8080/?v=player-paper-surface-20261024&spawn=187,132');
await p.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await p.locator('#unity-canvas').click();await p.waitForTimeout(6500);
const cap=async name=>{const data=await p.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});fs.writeFileSync('Book/Validation/player-surface-web-'+name+'.png',Buffer.from(data.split(',')[1],'base64'))};
await cap('idle');await p.keyboard.down('d');await p.waitForTimeout(160);await cap('run');await p.keyboard.press('j');await p.waitForTimeout(80);await cap('attack');await p.keyboard.press('a');await p.keyboard.up('d');await p.keyboard.up('a');
await p.keyboard.press('Space');await p.waitForTimeout(130);await cap('jump');await p.keyboard.down('d');await p.keyboard.press('Shift');await p.waitForTimeout(60);await cap('dash');await p.keyboard.up('d');await p.waitForTimeout(1000);
await p.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','465,124'));await p.waitForTimeout(1200);await cap('npc-map');assert.deepEqual(errors,[]);
fs.writeFileSync('Book/Validation/player-surface-web.json',JSON.stringify({passed:true,errors,captures:['idle','run','attack','jump','dash','npc-map'],exactFrameCoverageAndAlpha:'Verified by PlayerSurfaceIntegration, all distinct active drawings'},null,2));console.log('PLAYER_SURFACE_WEB_OK');
}finally{await b.close()}})().catch(e=>{console.error(e);process.exit(1)});
