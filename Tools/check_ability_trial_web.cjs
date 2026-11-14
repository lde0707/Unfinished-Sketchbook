const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const lib=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(lib,'playwright'));
(async()=>{
 const out='Book/Validation/ability-trial';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.goto('http://127.0.0.1:8080/?v=ability-trial-20261026&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(12000);
  async function state(){const result=page.waitForEvent('console',{predicate:m=>m.text().includes('ABILITY_TRIAL '),timeout:15000});await page.evaluate(()=>unityInstance.SendMessage('World Prototype','ReportAbilityTrial'));const m=(await result).text();return JSON.parse(m.slice(m.indexOf('{')));}
  async function shot(name){const data=await page.evaluate(async()=>{const c=document.querySelector('canvas'),p=document.createElement('canvas');p.width=64;p.height=36;const ctx=p.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(c,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return c.toDataURL();}throw Error('blank');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  const initial=await state();assert.equal(initial.health,9);assert(!initial.unlocked&&!initial.acquired);
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','198,128'));await page.waitForTimeout(450);
  await page.keyboard.press('KeyE',{delay:90});const locked=await state();assert(!locked.acquired&&!locked.collected&&!locked.unlocked);await shot('sealed-hand');
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','206,128'));await page.waitForTimeout(400);
  const samples=[];for(let i=0;i<24;i++){samples.push(await state());await page.waitForTimeout(700);if(i===12)await shot('guardian');if(samples.some(s=>s.state==='Attack')&&Math.max(...samples.map(s=>s.guardianX))-Math.min(...samples.map(s=>s.guardianX))>.4){await shot('guardian');break;}}fs.writeFileSync(path.join(out,'samples.json'),JSON.stringify(samples,null,2));
  assert(samples.some(s=>s.state==='Attack'),'guardian must attack in actual room');assert(Math.max(...samples.map(s=>s.guardianX))-Math.min(...samples.map(s=>s.guardianX))>.4,'guardian physically charges');
  await page.keyboard.press('Space',{delay:90});await page.keyboard.press('KeyJ',{delay:90});await page.waitForTimeout(250);await shot('jump-and-attack');
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/ability-trial-web.json',JSON.stringify({passed:true,initial,locked,samples,checks:['actual WebGL guardian replaces first walker','E cannot claim sealed hand','guardian attack animation and physical movement','base jump and attack capture'],errors},null,2));console.log('ABILITY_TRIAL_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
