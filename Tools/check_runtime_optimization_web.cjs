const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const out='Book/Validation/runtime-optimization';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:800}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',m=>{if(/Exception|Missing.*script|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')});});
  await page.goto('http://127.0.0.1:8080/?v=fsm-cleanup-20261025&spawn=78,123');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});
  await page.locator('#unity-canvas').click();await page.waitForTimeout(4500);
  async function shot(name){const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  await shot('idle');
  await page.keyboard.down('KeyD');await page.waitForTimeout(500);await shot('run');
  for(let i=0;i<6;i++){
   await page.keyboard.press('KeyJ');
   if(i===1)await page.keyboard.press('Space');
   if(i===3){await page.keyboard.up('KeyD');await page.keyboard.down('KeyA');}
   await page.waitForTimeout(140);
  }
  await shot('moving-air-combat');await page.keyboard.up('KeyA');
  await page.keyboard.press('Space');await page.waitForTimeout(120);await page.keyboard.press('ShiftLeft');await page.waitForTimeout(170);await shot('dash-fall');
  await page.waitForTimeout(1200);await page.keyboard.press('Space');await page.waitForTimeout(140);await shot('jump-again');
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','490,36'));await page.waitForTimeout(1500);
  await page.keyboard.press('KeyJ');await page.waitForTimeout(150);await shot('region2');
  await page.keyboard.press('Escape');await page.waitForTimeout(250);await shot('pause');await page.keyboard.press('Escape');
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','248,131'));await page.waitForTimeout(1500);await shot('portal');
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/runtime-optimization-web.json',JSON.stringify({passed:true,realWebGL:true,actions:['idle/run','moving attack','jump attack','reverse during combo','dash/fall','re-jump','region2','pause/resume','portal proximity'],errors},null,2));
  console.log('RUNTIME_OPTIMIZATION_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
