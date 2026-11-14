const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const out='Book/Validation/map-zoom';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:800}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.goto('http://127.0.0.1:8080/?v=map-zoom-20261025&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('#unity-canvas').click();await page.waitForTimeout(3500);
  async function point(x,y){return page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.left+(r.width-1920*s)/2+x*s,y:r.top+(r.height-1080*s)/2+y*s};},{x,y});}
  async function move(x,y){const p=await point(x,y);await page.mouse.move(p.x,p.y);}
  async function click(x,y){const p=await point(x,y);await page.mouse.click(p.x,p.y);await page.waitForTimeout(250);}
  async function state(){const result=page.waitForEvent('console',{predicate:m=>m.text().includes('JOURNAL_MAP '),timeout:15000});await page.evaluate(()=>unityInstance.SendMessage('World Prototype','ReportJournalMap'));const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  async function shot(name){const data=await page.evaluate(async()=>{const c=document.querySelector('canvas'),p=document.createElement('canvas');p.width=64;p.height=36;const ctx=p.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(c,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return c.toDataURL('image/png');}throw Error('No rendered capture');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  await page.keyboard.press('KeyM');await page.waitForTimeout(700);let s=await state();assert.equal(s.zoom,1);assert(s.paused);
  await click(745,343);s=await state();assert.equal(s.zoom,3);await shot('player-focus');
  const before=s,nx=(800-325)/700,ny=(610-377)/350;await move(800,610);await page.mouse.wheel(0,-120);await page.waitForTimeout(450);s=await state();assert(s.zoom>3&&s.zoom<=6);
  assert(Math.abs((before.x+before.width*nx)-(s.x+s.width*nx))<before.width/700*1.5,JSON.stringify({before,after:s,nx}));assert(Math.abs((before.y+before.height*ny)-(s.y+s.height*ny))<before.height/350*1.5,JSON.stringify({before,after:s,ny}));
  const beforeDrag=s;await move(800,610);await page.mouse.down();const end=await point(880,650);await page.mouse.move(end.x,end.y,{steps:12});await page.mouse.up();await page.waitForTimeout(300);s=await state();assert(s.x<beforeDrag.x&&s.y<beforeDrag.y);assert.equal(s.notes,0);
  await click(1370,390);assert.equal(await page.locator('#stilldrawn-map-note').count(),0,'drag must not create an empty note');
  const worldPoint={x:s.x+(750-325)/700*s.width,y:s.y+(640-377)/350*s.height};
  await click(750,640);await click(1370,390);const editor=page.locator('#stilldrawn-map-note');await editor.waitFor();await editor.fill('확대한 지도에서도 위치가 그대로인 메모');await page.keyboard.press('Control+Enter');await editor.waitFor({state:'detached'});assert.equal((await state()).notes,1);await shot('zoomed-note');
  await click(575,343);s=await state();assert.equal(s.zoom,1);await click(325+worldPoint.x/280*700,377+worldPoint.y/140*350);await click(1370,390);await editor.waitFor();assert.equal(await editor.inputValue(),'확대한 지도에서도 위치가 그대로인 메모');await page.keyboard.press('Control+Enter');await editor.waitFor({state:'detached'});
  await move(800,610);for(let i=0;i<20;i++){await page.mouse.wheel(0,-120);await page.waitForTimeout(80);}await page.waitForTimeout(350);assert.equal((await state()).zoom,6);await shot('maximum-zoom');for(let i=0;i<20;i++){await page.mouse.wheel(0,120);await page.waitForTimeout(80);}await page.waitForTimeout(350);assert.equal((await state()).zoom,1);
  await click(327,343);assert((await state()).zoom>1);await click(256,343);assert(Math.abs((await state()).zoom-1)<.001);await click(327,343);await click(700,288);s=await state();assert.equal(s.zoom,1);assert.equal(s.x,280);await click(350,288);assert.equal((await state()).x,0);
  await click(745,343);s=await state();await move(1400,400);await page.mouse.wheel(0,-120);await page.waitForTimeout(250);assert.equal((await state()).zoom,s.zoom);await shot('final-map');
  await page.keyboard.press('KeyM');await page.waitForTimeout(350);assert.equal((await state()).paused,false);await page.keyboard.press('KeyM');await page.waitForTimeout(250);assert.equal((await state()).zoom,s.zoom);await page.keyboard.press('KeyM');assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/map-zoom-web.json',JSON.stringify({passed:true,wheelCursorAnchor:true,zoomRange:[1,6],dragDoesNotCreateNote:true,notesKeepWorldCoordinates:true,regionReset:true,playerFocus:true,buttons:true,resume:true,reopenKeepsZoom:true,errors},null,2));console.log('MAP_ZOOM_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
