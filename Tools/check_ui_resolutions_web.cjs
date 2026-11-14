// Exercise actual WebGL render buffers, not a forced low-resolution test canvas.
const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
const resolutions=[[1366,768],[1920,1080],[1920,1200],[2560,1080],[2560,1440],[2560,1600],[3440,1440],[3840,2160]];
(async()=>{
 const out='Book/Validation/ui-resolutions';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const resume=process.argv.includes('--resume-4k'),windowOnly=process.argv.includes('--resume-window');
 const result=resume||windowOnly?JSON.parse(fs.readFileSync(out+'/result.json','utf8')):{passed:false,resolutions,captures:[],errors:[],checks:[]};
 if(resume)result.captures=result.captures.filter(c=>!c.file.startsWith('3840x2160-')||c.file.endsWith('-resolution-preview.png'));
 const save=()=>fs.writeFileSync(out+'/result.json',JSON.stringify(result,null,2));
 try{
  const page=await browser.newPage({viewport:{width:1366,height:768}});page.setDefaultTimeout(60000);
  page.on('pageerror',e=>result.errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds|out of memory|SendMessage.*not found/i.test(m.text()))result.errors.push(m.text());});
  const wait=ms=>page.waitForTimeout(ms);
  async function key(k){await page.keyboard.press(k,{delay:350});await wait(750);}
  async function click(x,y){const p=await page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.left+(r.width-1920*s)/2+x*s,y:r.top+(r.height-1080*s)/2+y*s};},{x,y});await page.mouse.click(p.x,p.y,{delay:350});await wait(750);}
  async function send(method,value='',target='World Prototype'){await page.evaluate(({method,value,target})=>unityInstance.SendMessage(target,method,value),{method,value,target});await wait(600);}
  async function geometry(){return page.evaluate(()=>{const c=document.querySelector('canvas'),r=c.getBoundingClientRect(),b=document.querySelector('#fullscreen').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{buffer:[c.width,c.height],viewport:[innerWidth,innerHeight],canvas:{x:r.x,y:r.y,w:r.width,h:r.height},button:{x:b.x,y:b.y,w:b.width,h:b.height},content:{x:r.x+(r.width-1920*s)/2,y:r.y+(r.height-1080*s)/2,w:1920*s,h:1080*s}};});}
  async function capture(size,name){await wait(500);const g=await geometry();assert.deepEqual(g.buffer,size,'actual render buffer');const r=g.canvas;assert(r.x>=-.5&&r.y>=-.5&&r.x+r.w<=g.viewport[0]+.5&&r.y+r.h<=g.viewport[1]+.5,'canvas fits viewport');const file=size.join('x')+'-'+name+'.png';await page.screenshot({path:out+'/'+file,timeout:60000});assert((await sharp(out+'/'+file).stats()).entropy>1,'nonblank '+file);result.captures.push({file,...g});save();console.log('CAPTURE '+file);}
  async function state(method,prefix){const event=page.waitForEvent('console',{predicate:m=>m.text().includes(prefix),timeout:20000});await send(method);const text=(await event).text();return JSON.parse(text.slice(text.indexOf('{')));}
  await page.goto('http://127.0.0.1:8080/?v=ui-resolution-audit-20261028');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await wait(8000);await page.locator('canvas').focus();
  if(!windowOnly){
  for(let i=0;i<resolutions.length;i++){
   const size=resolutions[i];await page.setViewportSize({width:size[0],height:size[1]});await wait(1200);
   await click(470,744);await click(530,155); // Home -> screen settings; first resolution is one left of default.
   await click(i===0?1050:1340,380);await wait(1500);
   if(!resume&&i<7)await capture(size,'resolution-preview');await click(1390,900);
   if(resume&&i<7){await key('Escape');continue;}
   await capture(size,'screen');await click(920,155);await capture(size,'sound');await click(1310,155);await capture(size,'controls');
   await key('Escape');await capture(size,'home');await click(470,630);await capture(size,'new-story');await key('Escape');
  }
  result.checks.push('All eight resolutions previewed and explicitly saved through the real settings panel');
  // Start a real isolated session to exercise both game menus and the saved bookmark.
  await click(470,630);await click(1300,900);await wait(6500);
  for(const size of resolutions){
   await page.setViewportSize({width:size[0],height:size[1]});await wait(1200);
   await page.evaluate(([width,height])=>{window.stilldrawnResolution={width,height};window.resizeStilldrawn();},size);await wait(900);
   await send('PreviewPosition','236,131');await capture(size,'hud');
   await key('KeyM');await capture(size,'map');await click(650,550);await click(1400,400);await wait(750);
   const input=page.locator('#stilldrawn-map-note');assert(await input.count(),'native note editor opens');await input.fill('다시 돌아올 길 · 구역별 메모 확인');
   const box=await input.boundingBox(),g=await geometry();assert(box.x>=g.content.x&&box.y>=g.content.y&&box.x+box.width<=g.content.x+g.content.w&&box.y+box.height<=g.content.y+g.content.h,'note fits content');
   await capture(size,'map-note');await key('Escape');await page.locator('canvas').focus();await key('Escape');
   await key('Escape');await capture(size,'pause');await click(960,590);await capture(size,'pause-controls');await key('Escape');await key('Escape');
   await send('PreviewPosition','236,131');await key('KeyE');let st=await state('ReportEconomy','ECONOMY_STATE ');assert(st.choice&&!st.shop&&st.paused,'interaction choice');await capture(size,'interaction');await click(810,695);st=await state('ReportEconomy','ECONOMY_STATE ');assert(st.shop&&st.paused,'shop opened');await capture(size,'shop');await key('Escape');await key('Escape');
  }
  await send('ReturnToTitle','','Stilldrawn Title');await wait(6500);
  for(const size of resolutions){
   await page.setViewportSize({width:size[0],height:size[1]});await wait(1200);await page.evaluate(([width,height])=>{window.stilldrawnResolution={width,height};window.resizeStilldrawn();},size);await wait(900);
   await click(470,520);await capture(size,'continue');await key('Escape');
  }
  }
  if(windowOnly){
   // Recheck the repaired CSS wrapper at every buffer size without re-running
   // the unchanged Unity panel interactions already recorded in this audit.
   for(const size of resolutions){await page.setViewportSize({width:size[0],height:size[1]});await page.evaluate(([width,height])=>{window.stilldrawnResolution={width,height};window.resizeStilldrawn();},size);await capture(size,'responsive-wrapper');}
  }
  // A 4K render buffer in a smaller window, plus fullscreen, must still fit.
  await page.setViewportSize({width:1366,height:768});await capture([3840,2160],'small-window');
  await page.locator('#fullscreen').click();await wait(1000);assert(await page.evaluate(()=>!!document.fullscreenElement));await capture([3840,2160],'fullscreen');await page.evaluate(()=>document.exitFullscreen());
  assert.deepEqual(result.errors,[]);result.passed=true;result.checks.push('Title, bookmark, all settings tabs, resolution preview, HUD, map, native note editor, pause, controls, interaction and shop at every resolution','4K buffer in 1366x768 window and fullscreen','No browser or Unity runtime exceptions');save();console.log('UI_RESOLUTIONS_WEB_OK');
 }finally{save();await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
