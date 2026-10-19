const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1200,height:850}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',m=>{if(/Exception|Shader.*error/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="960" height="540"')});});
  await page.goto('http://127.0.0.1:8080/?v=surface-contact-20261019&spawn=78,123');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});
  await page.locator('#unity-canvas').click();await page.waitForTimeout(6000);
  const capture=async name=>page.locator('#unity-canvas').screenshot({path:'Book/Validation/contact-'+name+'.png',timeout:60000});
  await capture('floor');
  await page.evaluate(()=>unityInstance.SendMessage('World Prototype','PreviewPosition','133,60'));await page.waitForTimeout(500);
  await page.keyboard.down('d');await page.keyboard.press('Space');await page.keyboard.down('w');await page.waitForTimeout(500);
  await page.keyboard.up('w');await page.waitForTimeout(300);await capture('wall');
  for(let i=0;i<4;i++){await page.keyboard.down('Space');await page.waitForTimeout(30);await page.keyboard.up('Space');await page.waitForTimeout(200);}
  await capture('wall-hops');await page.keyboard.up('d');
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/player-contact-web.json',JSON.stringify({passed:true,errors,screenshots:['contact-floor.png','contact-wall.png','contact-wall-hops.png'],note:'Browser load/input smoke and visual captures; collision assertions run in PlayerContactValidation.'},null,2));
  console.log('PLAYER_CONTACT_WEB_OK');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
