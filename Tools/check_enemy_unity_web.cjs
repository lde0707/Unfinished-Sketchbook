const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1600,height:1080}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8080/?v=enemy-no-hurt-20261019&spawn=252,131');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  await page.waitForTimeout(6000);await page.locator('#unity-canvas').click();
  for(const [zone,spawn] of [['a','252,131'],['b','106,132'],['c','119,60'],['d','230,70']]){
   await page.evaluate(s=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',s),spawn);
   await page.waitForTimeout(1500);
   await page.screenshot({path:`Book/Validation/enemy-unity-${zone}.png`});
  }
  await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','252,131'));
  await page.keyboard.down('KeyD');await page.waitForTimeout(100);await page.keyboard.up('KeyD');
  for(let i=0;i<3;i++){await page.keyboard.press('KeyJ');await page.waitForTimeout(650);}
  await page.screenshot({path:'Book/Validation/enemy-unity-after-hits.png'});
  await page.waitForTimeout(1800);await page.screenshot({path:'Book/Validation/enemy-unity-death.png'});
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/enemy-unity-web.json',JSON.stringify({passed:true,errors,checks:'WebGL loads; four zone captures; attack input smoke check. State assertions are in EnemyAnimationValidation.'},null,2));
  console.log('PASS: local Unity WebGL loaded and four zone captures saved.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
