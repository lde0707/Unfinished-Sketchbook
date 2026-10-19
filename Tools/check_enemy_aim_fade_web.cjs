const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const world=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/world12.json','utf8').replace(/^\uFEFF/,''));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1200,height:850}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|SendMessage.*no receiver/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="960" height="540"')});});
  await page.goto('http://127.0.0.1:8080/?v=enemy-aim-fade-20261019&spawn=266,118');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await page.locator('#unity-canvas').click();await page.waitForTimeout(6000);
  const shot=async name=>page.locator('#unity-canvas').screenshot({path:'Book/Validation/enemy-aim-'+name+'.png',timeout:60000});
  await shot('moth');
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','110,48'));await page.waitForTimeout(2200);await shot('owl');
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','252,131'));await page.waitForTimeout(1000);await shot('alive');
  const e=world.enemies[0],name=`000 ${e.type} (${e.tx}, ${e.floor})`;
  // Exercise the real runtime death path without relying on attack positioning.
  await page.evaluate(n=>{for(let i=0;i<3;i++)unityInstance.SendMessage(n,'OnHit');},name);
  await page.waitForTimeout(950);await shot('fading');await page.waitForTimeout(1300);await shot('gone');
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/enemy-aim-fade-web.json',JSON.stringify({passed:true,errors,checks:'Actual WebGL flight captures and forced lethal-hit fade captures; numerical trajectory and fade assertions in EnemyAnimationValidation.'},null,2));console.log('ENEMY_AIM_FADE_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
