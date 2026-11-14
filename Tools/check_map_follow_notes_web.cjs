const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const out='Book/Validation/map-follow-notes';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.goto('http://127.0.0.1:8080/?v=map-follow-notes-20261026&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(12000);
  async function point(x,y){return page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.left+(r.width-1920*s)/2+x*s,y:r.top+(r.height-1080*s)/2+y*s};},{x,y});}
  async function click(x,y){const p=await point(x,y);await page.mouse.click(p.x,p.y);await page.waitForTimeout(300);}
  async function state(){const result=page.waitForEvent('console',{predicate:m=>m.text().includes('JOURNAL_MAP '),timeout:15000});await page.evaluate(()=>unityInstance.SendMessage('World Prototype','ReportJournalMap'));const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  async function shot(name){const data=await page.evaluate(async()=>{const c=document.querySelector('canvas'),p=document.createElement('canvas');p.width=64;p.height=36;const ctx=p.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(c,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return c.toDataURL();}throw Error('blank');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  const start=await state();await page.keyboard.down('KeyD');await page.waitForTimeout(1200);const moving=await state();await page.keyboard.up('KeyD');await page.waitForTimeout(900);let s=await state();
  assert(moving.playerX>start.playerX+.2&&moving.miniX>start.miniX,JSON.stringify({start,moving}));assert(Math.abs(s.miniX-s.playerX)<.08);await shot('minimap');
  const note1={x:s.playerX,y:s.playerY,text:'첫 지역의 다시 돌아올 길'},note2={x:490,y:35,text:'두 번째 지역의 숨겨진 통로'};
  await page.keyboard.press('KeyM');await page.waitForTimeout(500);
  function mapPoint(s,n){return{x:325+(n.x-s.x)/s.width*700,y:377+(n.y-s.y)/s.height*350};}
  async function clickNotePosition(n){const v=await state(),p=mapPoint(v,n);await click(p.x,p.y);}
  async function save(text){await page.evaluate(text=>{unityInstance.SendMessage('World Prototype','SetMapNoteDraft',text);unityInstance.SendMessage('World Prototype','SaveMapNote','');},text);await page.waitForTimeout(250);}
  await clickNotePosition(note1);await save(note1.text);assert.equal((await state()).notes,1);
  await click(650,288);await clickNotePosition(note2);await save(note2.text);assert.equal((await state()).notes,2);
  await click(1390,737);s=await state();assert.equal(s.selected,0);assert.equal(s.text,note1.text);assert(s.zoom>=3&&s.x<280&&s.x+s.width>=note1.x);await shot('list-focus-region1');
  await click(1390,778);s=await state();assert.equal(s.selected,1);assert.equal(s.text,note2.text);assert(s.x>=280&&s.x+s.width>=490);await shot('list-focus-region2');
  await click(575,343);await click(500,550);s=await state();assert.equal(s.selected,-1);await clickNotePosition(note2);s=await state();assert.equal(s.selected,1);assert.equal(s.text,note2.text);assert.equal(s.notes,2);await shot('marker-read');
  const down=await point(700,550),end=await point(760,580);await page.mouse.move(down.x,down.y);await page.mouse.down();await page.mouse.move(end.x,end.y,{steps:12});await page.mouse.up();await page.waitForTimeout(250);assert.equal((await state()).notes,2);
  await page.keyboard.press('KeyM');await page.waitForTimeout(500);s=await state();const miniW=30*314/165;
  await click(1548+(note1.x-(s.miniX-miniW/2))/miniW*314,76+(note1.y-(s.miniY-15))/30*165);s=await state();assert(s.paused);assert.equal(s.selected,0);assert.equal(s.text,note1.text);await shot('minimap-note-open');
  await page.keyboard.press('KeyM');await page.reload();await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(3500);await page.keyboard.press('KeyM');await page.waitForTimeout(500);await click(1390,778);s=await state();assert.equal(s.notes,2);assert.equal(s.selected,1);assert.equal(s.text,note2.text);assert(s.x>=280);
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/map-follow-notes-web.json',JSON.stringify({passed:true,checks:['minimap follows movement and settles','map marker displays saved text','list focuses across regions at 3x zoom','drag creates no note','minimap note opens journal and selects content','notes and navigation survive reload'],start,moving,errors},null,2));console.log('MAP_FOLLOW_NOTES_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
