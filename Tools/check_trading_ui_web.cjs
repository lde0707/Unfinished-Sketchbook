const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const lib=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(lib,'playwright'));
(async()=>{
 const out='Book/Validation/trading-ui';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.goto('http://127.0.0.1:8080/?v=trading-choices-20261026&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(12000);
  async function key(k){await page.keyboard.press(k,{delay:180});await page.waitForTimeout(600);}
  async function click(x,y){const p=await page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.left+(r.width-1920*s)/2+x*s,y:r.top+(r.height-1080*s)/2+y*s};},{x,y});await page.mouse.click(p.x,p.y);await page.waitForTimeout(650);}
  async function state(method='ReportEconomy',prefix='ECONOMY_STATE '){const result=page.waitForEvent('console',{predicate:m=>m.text().includes(prefix),timeout:15000});await page.evaluate(method=>unityInstance.SendMessage('World Prototype',method),method);const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  async function shot(name){const data=await page.evaluate(async()=>{const c=document.querySelector('canvas'),p=document.createElement('canvas');p.width=64;p.height=36;const ctx=p.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(c,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return c.toDataURL();}throw Error('blank');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  await page.keyboard.down('KeyE');await page.waitForTimeout(1100);await page.keyboard.up('KeyE');await page.waitForTimeout(500);
  let s=await state();assert(s.choice&&!s.shop&&s.paused&&s.choiceKind==='save');await shot('shop-entry');
  const at=await state('ReportJournalMap','JOURNAL_MAP ');await key('KeyD');const frozen=await state('ReportJournalMap','JOURNAL_MAP ');assert.equal(at.playerX,frozen.playerX);
  await click(1110,695);s=await state();assert(!s.choice&&!s.shop&&!s.paused);
  await key('KeyE');await click(810,695);s=await state();assert(s.shop&&!s.choice&&s.paused);await shot('counter');
  await click(1455,914);s=await state();assert(!s.choice&&s.dust===0,'unaffordable purchase disabled');
  await click(960,450);await key('Enter');s=await state();assert(!s.choice&&s.dust===0,'locked healing upgrade blocked');await shot('locked-product');
  await key('Escape');s=await state();assert(s.choice&&!s.shop&&s.paused);await key('Escape');s=await state();assert(!s.choice&&!s.shop&&!s.paused);
  await key('KeyE');await key('Enter');assert((await state()).shop);await key('KeyE');assert((await state()).choice);await key('Escape');
  async function spawn(pos){await page.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await page.waitForTimeout(1800);}
  await spawn('190,132');await key('KeyE');s=await state('ReportPickupNotice','PICKUP_NOTICE ');assert(s.open&&s.healing&&!s.choice&&!s.paused);await shot('pickup-acquired');await page.waitForTimeout(6500);s=await state('ReportPickupNotice','PICKUP_NOTICE ');assert(!s.open&&s.healing&&!s.paused);
  await spawn('10,138');await key('KeyE');s=await state();assert(s.choice&&s.choiceKind==='station');await shot('portal-confirm');await key('Escape');assert((await state('ReportJournalMap','JOURNAL_MAP ')).playerX<280);
  await key('KeyE');await key('Enter');let dest;for(let i=0;i<12;i++){await page.waitForTimeout(500);dest=await state('ReportJournalMap','JOURNAL_MAP ');if(dest.playerX>280)break;}assert(dest.playerX>280,'confirmed portal travel');
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/trading-ui-web.json',JSON.stringify({passed:true,checks:['held E opens only choice','world paused while choosing','mouse cancel and confirm','generated counter layout','unaffordable and locked purchase','Escape back hierarchy','keyboard entry','item immediately acquired then explained','portal preview and cancel','confirmed travel'],errors},null,2));console.log('TRADING_UI_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
