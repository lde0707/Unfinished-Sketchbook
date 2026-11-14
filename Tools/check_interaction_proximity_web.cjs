const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[],reports=[],out='Book/Validation/interaction-proximity';fs.mkdirSync(out,{recursive:true});
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.text().includes('UNFIN_INTERACTION_GLOW'))reports.push(m.text());if(/Exception|Shader.*error|SendMessage.*not found|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
 await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
 async function send(method,value){await page.evaluate(({method,value})=>unityInstance.SendMessage('World Prototype',method,value),{method,value});}
 async function capture(name){const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});const file=out+'/'+name+'.png';fs.writeFileSync(file,Buffer.from(data.split(',')[1],'base64'));assert.ok((await sharp(file).stats()).entropy>1,'rendered '+name);return file;}
 async function move(p){await send('PreviewPosition',p);await page.waitForTimeout(2300);await send('ReportInteractionGlow','');await page.waitForTimeout(200);}
 await page.goto('http://127.0.0.1:8080/?v=interaction-proximity&spawn=24,128');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(4500);
 await send('ReportInteractionGlow','');await page.waitForTimeout(300);
 assert(reports.some(x=>/lockedFriends=[1-9]/.test(x)),'locked drawing friend glows before ability unlock');await capture('01-locked-friend-near');
 await send('PreviewInteractionGlow','off');await page.waitForTimeout(400);await capture('02-friend-feedback-off');await send('PreviewInteractionGlow','on');
 await move('20,128');const far=reports.at(-1);assert(/lockedFriends=0/.test(far),'friend glimmer fades when leaving');await capture('03-friend-far');
 for(const [name,pos] of [['save','235,131'],['portal','248,131'],['locked-gate','259,131'],['lever','257,70'],['npc','465,124'],['target','413,124'],['wax-wall','553,106']]){
  await move(pos);await capture('04-'+name);if(name==='locked-gate')assert(/tiles=[1-9]/.test(reports.at(-1)),'special locked gate proximity cue');
 }
 await send('PreviewInteractionGlow','on');assert.deepEqual(errors,[]);
 fs.writeFileSync('Book/Validation/interaction-proximity-web.json',JSON.stringify({passed:true,realWebGL:true,lockedFriend:true,distanceFade:true,lockedGate:true,reports,errors},null,2));console.log('INTERACTION_PROXIMITY_WEB_OK');
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
