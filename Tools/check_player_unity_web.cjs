const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const output=name=>'Book/Validation/'+(process.env.UNFIN_CAPTURE_PREFIX||'')+name;
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1080}});const errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text()+" "+JSON.stringify(m.location()));});
  page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  if(process.env.UNFIN_LOW_RES)await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="960" height="540"')});});
  await page.goto(process.env.UNFIN_TEST_URL||'http://127.0.0.1:8080/?v=attack-range-20261018');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  await page.waitForTimeout(5000); // Unity splash outlives createUnityInstance.
  await page.locator('#unity-canvas').click();await page.waitForTimeout(1000);
  assert(await page.locator('#loading').isHidden());
  await page.screenshot({path:output('player-sprites-web-idle.png')});
  await page.keyboard.press('KeyJ');await page.waitForTimeout(100);await page.screenshot({path:output('player-combat-web.png')});
  await page.waitForTimeout(280);await page.keyboard.press('KeyJ');await page.waitForTimeout(340);await page.keyboard.press('KeyJ');await page.waitForTimeout(190);
  await page.screenshot({path:output('player-thrust-web.png')});await page.waitForTimeout(900);
  await page.keyboard.down('KeyA');await page.waitForTimeout(650);await page.keyboard.up('KeyA');await page.waitForTimeout(400);
  await page.keyboard.down('Space');await page.waitForTimeout(110);await page.keyboard.press('KeyJ');await page.waitForTimeout(50);await page.keyboard.press('ShiftLeft');await page.waitForTimeout(70);await page.keyboard.up('Space');await page.waitForTimeout(190);await page.keyboard.press('KeyJ');
  await page.waitForTimeout(700);await page.keyboard.press('Digit2');await page.waitForTimeout(700);await page.screenshot({path:output('player-sprites-web-world2.png')});
  await page.keyboard.press('Digit1');await page.waitForTimeout(500);await page.keyboard.press('Space');await page.waitForTimeout(700);
  if(process.env.UNFIN_VERIFY_TEMPO){
   await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','78,123'));await page.waitForTimeout(500);
   await page.keyboard.down('KeyD');await page.waitForTimeout(350);await page.keyboard.down('Space');await page.waitForTimeout(250);
   await page.screenshot({path:output('tempo-moving-jump.png')});await page.waitForTimeout(650);
   await page.keyboard.up('Space');await page.keyboard.up('KeyD');await page.waitForTimeout(600);
  }
  if(process.env.UNFIN_VERIFY_ATTACK_CONTINUITY){
   await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','78,123'));await page.waitForTimeout(300);
   for(let i=0;i<32;i++){
    const direction=i%2?'KeyA':'KeyD';await page.keyboard.down(direction);
    if(i%8===0)await page.keyboard.press('Space');
    await page.keyboard.press('KeyJ');await page.waitForTimeout(60);
    if(i===18)await page.screenshot({path:output('rapid-steering-attack.png')});
    await page.keyboard.up(direction);
   }
   await page.waitForTimeout(700);
  }
  if(process.env.UNFIN_VERIFY_WALL){
   await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','133,60'));await page.waitForTimeout(500);
   await page.keyboard.down('KeyD');await page.keyboard.press('Space');await page.keyboard.down('KeyW');await page.waitForTimeout(500);
   await page.screenshot({path:output('wall-climb.png')});await page.keyboard.up('KeyW');await page.waitForTimeout(300);
   await page.screenshot({path:output('wall-hold.png')});await page.keyboard.press('Space');await page.keyboard.up('KeyD');await page.waitForTimeout(120);
   await page.screenshot({path:output('wall-jump.png')});await page.waitForTimeout(900);
   await page.keyboard.down('KeyD');await page.keyboard.press('Space');await page.keyboard.down('KeyW');await page.waitForTimeout(350);await page.keyboard.up('KeyW');await page.keyboard.up('KeyD');await page.keyboard.down('KeyA');await page.waitForTimeout(180);
   await page.screenshot({path:output('wall-release.png')});await page.keyboard.up('KeyA');
  }
  if(process.env.UNFIN_VERIFY_WALL_HOPS){
   await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','133,60'));await page.waitForTimeout(500);
   await page.keyboard.down('KeyD');await page.keyboard.press('Space');await page.waitForTimeout(450);
   await page.screenshot({path:output('wall-hops-start.png')});
   for(let i=0;i<5;i++){await page.keyboard.down('Space');await page.waitForTimeout(30);await page.keyboard.up('Space');await page.waitForTimeout(180);}
   await page.screenshot({path:output('wall-hops-end.png')});await page.keyboard.up('KeyD');
  }
  assert.deepEqual(errors,[]);fs.writeFileSync(output('player-sprites-web.json'),JSON.stringify({passed:true,loadedUnity:true,keyboardActions:['click attack','J ground combo','left run','stationary jump','J rising attack','air dash cancels attack','fall attack','region2','region1','re-jump'],errors},null,2)+'\n');console.log('PASS: Unity WebGL loaded; keyboard smoke sequence; no browser errors. Screenshots saved for visual review.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
