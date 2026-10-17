const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1080}});const errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text()+" "+JSON.stringify(m.location()));});
  page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  await page.goto('http://127.0.0.1:8080/?v=player-sprites');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  await page.locator('#unity-canvas').click();await page.waitForTimeout(1000);
  assert(await page.locator('#loading').isHidden());
  await page.screenshot({path:'Book/Validation/player-sprites-web-idle.png'});
  await page.keyboard.down('KeyA');await page.waitForTimeout(650);await page.keyboard.up('KeyA');await page.waitForTimeout(400);
  await page.keyboard.down('Space');await page.waitForTimeout(110);await page.keyboard.press('ShiftLeft');await page.waitForTimeout(70);await page.keyboard.up('Space');await page.waitForTimeout(190);
  await page.waitForTimeout(700);await page.keyboard.press('Digit2');await page.waitForTimeout(700);await page.screenshot({path:'Book/Validation/player-sprites-web-world2.png'});
  await page.keyboard.press('Digit1');await page.waitForTimeout(500);await page.keyboard.press('Space');await page.waitForTimeout(700);
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/player-sprites-web.json',JSON.stringify({passed:true,loadedUnity:true,keyboardActions:['left run','stationary jump','air dash','fall','region2','region1','re-jump'],errors},null,2)+'\n');console.log('PASS: Unity WebGL loaded; keyboard smoke sequence; no browser errors. Screenshots saved for visual review.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
