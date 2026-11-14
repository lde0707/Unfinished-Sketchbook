const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const lib=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(lib,'playwright'));
(async()=>{
 const out='Book/Validation/player-life-vfx';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const context=await browser.newContext({viewport:{width:1280,height:720}});
  const page=await context.newPage(),errors=[],samples=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
  await page.goto('http://127.0.0.1:8080/?v=paper-life-effects-20261031&spawn=244,131');
  await page.waitForFunction(()=>!!window.unityInstance,{}, {timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(10000);
  async function send(method,value){await page.evaluate(({method,value})=>value===undefined?unityInstance.SendMessage('World Prototype',method):unityInstance.SendMessage('World Prototype',method,value),{method,value});}
  async function state(){const result=page.waitForEvent('console',{predicate:m=>m.text().includes('LIFE_EFFECTS '),timeout:15000});await send('ReportLifeEffects');const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  await send('PreviewPosition','244,131');await page.waitForTimeout(3000);await send('BeginDeath');
  const shots=new Set();
  for(let i=0;i<180;i++){
   const s=await state();samples.push(s);
   let name=s.death&&s.elapsed>.16&&s.elapsed<.7?'death':s.revive&&s.elapsed>.2&&s.elapsed<.95?'revive-gather':s.revive&&s.elapsed>=1&&s.elapsed<1.6?'revive-rise':null;
   if(name&&!shots.has(name)){await page.screenshot({path:path.join(out,name+'.png')});shots.add(name);}
   if(samples.some(s=>s.revive)&&s.phase===0)break;
   await page.waitForTimeout(40);
  }
  assert(samples.some(s=>s.death&&s.phase===1&&s.particles>0),'visible death burst');
  assert(samples.some(s=>s.revive&&s.phase===2&&s.particles>0),'visible revive gathering');
  assert(samples.at(-1).phase===0&&samples.at(-1).particles===0,'effect clears after revive');
  await send('BeginDeath');await page.waitForTimeout(200);await send('PreviewPosition','244,131');await page.waitForTimeout(500);assert.equal((await state()).particles,0,'manual teleport clears effects');
  assert.deepEqual(errors,[]);
  await context.close();
  fs.writeFileSync(path.join(out,'result.json'),JSON.stringify({passed:true,checks:['death burst','revive gathering','clears after revive','clears on manual reset'],shots:[...shots],samples,errors},null,2));console.log('PLAYER_LIFE_VFX_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
