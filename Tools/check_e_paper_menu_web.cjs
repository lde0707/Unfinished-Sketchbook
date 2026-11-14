const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const lib=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(lib,'playwright'));
(async()=>{
 const out='Book/Validation/e-paper-menu';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.goto('http://127.0.0.1:8080/?v=e-paper-menu-20261026&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(12000);
  async function click(x,y){const p=await page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.left+(r.width-1920*s)/2+x*s,y:r.top+(r.height-1080*s)/2+y*s};},{x,y});await page.mouse.click(p.x,p.y);await page.waitForTimeout(500);}
  async function state(method='ReportJournalMap',prefix='JOURNAL_MAP '){const result=page.waitForEvent('console',{predicate:m=>m.text().includes(prefix),timeout:15000});await page.evaluate(method=>unityInstance.SendMessage('World Prototype',method),method);const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  async function shot(name){const data=await page.evaluate(async()=>{const c=document.querySelector('canvas'),p=document.createElement('canvas');p.width=64;p.height=36;const ctx=p.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(c,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return c.toDataURL();}throw Error('blank');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  await shot('health-hud');await page.keyboard.press('Escape',{delay:90});await page.waitForTimeout(500);assert((await state()).paused);await shot('pause');
  await click(960,590);await shot('controls-e');await page.keyboard.press('Escape',{delay:90});await page.waitForTimeout(400);
  await click(960,473);await shot('settings');await page.keyboard.press('Escape',{delay:90});await page.waitForTimeout(400);
  await click(960,356);assert(!(await state()).paused);
  await page.keyboard.press('KeyE',{delay:100});await page.waitForTimeout(600);await page.keyboard.press('Enter',{delay:100});await page.waitForTimeout(600);assert((await state('ReportEconomy','ECONOMY_STATE ')).shop);await shot('e-shop');
  await page.keyboard.press('KeyE',{delay:70});await page.waitForTimeout(350);assert(!(await state('ReportEconomy','ECONOMY_STATE ')).shop);await page.keyboard.press('Escape',{delay:100});await page.waitForTimeout(500);
  async function spawn(pos){await page.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await page.waitForTimeout(3500);}
  await spawn('190,132');await page.keyboard.down('KeyW');await page.waitForTimeout(1000);await page.keyboard.up('KeyW');await shot('item-before-e');
  await page.keyboard.press('KeyE',{delay:100});await page.waitForTimeout(600);await page.keyboard.press('Enter',{delay:100});await page.waitForTimeout(1800);await shot('item-after-e');
  const sharp=require(path.join(lib,'sharp'));async function red(name){const {data}=await sharp(path.join(out,name+'.png')).extract({left:420,top:48,width:75,height:92}).raw().toBuffer({resolveWithObject:true});let count=0;for(let i=0;i<data.length;i+=4)if(data[i]>60&&data[i]>data[i+1]*1.35&&data[i]>data[i+2]*1.35)count++;return count;}
  const beforeRed=await red('item-before-e'),afterRed=await red('item-after-e');assert(afterRed>beforeRed+60,JSON.stringify({beforeRed,afterRed}));
  await spawn('466,124');await page.keyboard.press('KeyE',{delay:60});await page.waitForTimeout(1100);await shot('npc-single-e');
  await spawn('10,138');const before=await state();await page.keyboard.down('KeyW');await page.waitForTimeout(900);await page.keyboard.up('KeyW');assert((await state()).playerX<280);
  await page.keyboard.press('KeyE',{delay:100});await page.waitForTimeout(600);await page.keyboard.press('Enter',{delay:100});await page.waitForTimeout(2800);const after=await state();assert(after.playerX>280,JSON.stringify({before,after}));await shot('portal-single-e');
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/e-paper-menu-web.json',JSON.stringify({passed:true,checks:['generated pause shown','pause freezes and resume works','controls and settings accessible','confirmed E enters shop','W does not collect healing item','confirmed E unlocks healing HUD','NPC single E capture','W does not travel','confirmed E travels through portal'],beforeRed,afterRed,errors},null,2));console.log('E_PAPER_MENU_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
