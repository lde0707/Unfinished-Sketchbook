const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const lib=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(lib,'playwright'));
(async()=>{
 const out='Book/Validation/pickup-toast';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[],checks=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
  await page.goto('http://127.0.0.1:8080/?v=corner-pickup-toast-20261031&spawn=190,132');
  await page.waitForFunction(()=>!!window.unityInstance,{}, {timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(9000);
  async function send(method,value){await page.evaluate(({method,value})=>value===undefined?unityInstance.SendMessage('World Prototype',method):unityInstance.SendMessage('World Prototype',method,value),{method,value});}
  async function state(method='ReportPickupNotice',prefix='PICKUP_NOTICE '){const result=page.waitForEvent('console',{predicate:m=>m.text().includes(prefix),timeout:15000});await send(method);const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  async function key(k){await page.keyboard.press(k,{delay:120});await page.waitForTimeout(600);}
  async function spawn(pos){await send('PreviewPosition',pos);await page.waitForTimeout(2000);}
  await spawn('190,132');await page.keyboard.down('KeyE');await page.waitForTimeout(1300);await page.keyboard.up('KeyE');let s=await state();
  assert(s.open&&s.healing&&!s.choice&&!s.paused&&!s.blocksInput,JSON.stringify(s));checks.push('held E acquires once without blocking gameplay');
  await page.screenshot({path:path.join(out,'heal-acquired.png')});
  const a=await state('ReportJournalMap','JOURNAL_MAP ');await key('KeyD');const b=await state('ReportJournalMap','JOURNAL_MAP ');assert.notEqual(a.playerX,b.playerX);checks.push('movement continues during corner notice');
  await key('Escape');s=await state();assert(s.open&&s.paused);const heldAge=s.age;await page.waitForTimeout(1000);assert.equal((await state()).age,heldAge);await key('Escape');s=await state();assert(!s.paused&&s.healing);checks.push('Escape opens normal pause and freezes notice timer');
  for(let i=0;i<60&&(await state()).open;i++)await page.waitForTimeout(250);s=await state();assert(!s.open&&s.healing&&!s.paused);checks.push('notice expires automatically and keeps acquired item');
  await spawn('198,128');await key('KeyE');s=await state();assert(!s.open&&!s.walljump);checks.push('guardian seal remains enforced');
  // Exercise the real enemy hit receiver to prepare a cleared encounter in this
  // isolated test session; this is not a combat difficulty/playthrough test.
  await page.evaluate(()=>{for(let i=0;i<9;i++)unityInstance.SendMessage('001 walker (202, 128)','Hit');});
  await page.waitForTimeout(1800);await key('KeyE');s=await state();assert(s.open&&s.walljump&&!s.choice&&!s.paused&&!s.blocksInput&&s.description.includes('미끄러')&&s.description.includes('Space'),JSON.stringify(s));checks.push('earned wall ability is committed before explanation');
  for(const [width,height] of [[1280,720],[1366,768],[1024,768]]){await page.setViewportSize({width,height});await page.waitForTimeout(500);await page.screenshot({path:path.join(out,`wall-${width}x${height}.png`)});}
  for(let i=0;i<60&&(await state()).open;i++)await page.waitForTimeout(250);s=await state();assert(!s.open&&s.walljump&&!s.paused);checks.push('wall notice also expires automatically');
  await spawn('236,131');await key('KeyE');let shop=await state('ReportEconomy','ECONOMY_STATE ');assert(shop.choice&&shop.choiceKind==='save'&&!shop.shop);await key('Enter');assert((await state('ReportEconomy','ECONOMY_STATE ')).shop);checks.push('shop retains separate confirmation');await key('Escape');await key('Escape');
  assert.deepEqual(errors,[]);fs.writeFileSync(path.join(out,'result.json'),JSON.stringify({passed:true,checks,errors},null,2));console.log('PICKUP_TOAST_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
