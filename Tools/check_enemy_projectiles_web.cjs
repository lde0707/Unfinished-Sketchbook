const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1200,height:850}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|Missing enemy|NullReference/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="960" height="540"')});});
  await page.goto('http://127.0.0.1:8080/?v=enemy-projectiles-20261019&spawn=210,128');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await page.locator('#unity-canvas').click();await page.waitForTimeout(6000);
  const examples=JSON.parse(fs.readFileSync('Book/Validation/enemy-projectile-examples.json','utf8'));
  for(const example of examples){
   await page.evaluate(spawn=>unityInstance.SendMessage('World Prototype','PreviewPosition',spawn),example.spawn);
   await page.waitForTimeout(2000);
   await page.locator('#unity-canvas').screenshot({path:`Book/Validation/projectile-${example.id}-flight.png`,timeout:60000});
   await page.waitForTimeout(400);
   await page.locator('#unity-canvas').screenshot({path:`Book/Validation/projectile-${example.id}-impact.png`,timeout:60000});
   await page.keyboard.press('KeyJ');await page.keyboard.press('Space');await page.waitForTimeout(150);await page.keyboard.press('ShiftLeft');await page.waitForTimeout(300);
  }
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/enemy-projectiles-web.json',JSON.stringify({passed:true,errors,checks:'Actual Unity map: 3 shooter flight/impact captures, attack/jump/dash input smoke. Numerical physics and emission assertions in EnemyProjectileValidation.'},null,2));
  console.log('ENEMY_PROJECTILES_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
