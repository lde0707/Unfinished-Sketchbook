const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1600,height:1080}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  await page.goto('http://127.0.0.1:8080/?v=responsive-camera-brightness-20261018&spawn=78,123');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  await page.locator('#unity-canvas').focus();await page.waitForTimeout(3000);
  for(const [name,spawn] of [['mine','78,123'],['waterfall','527,115']]){
   await page.evaluate(s=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',s),spawn);
   await page.waitForTimeout(600);
   await page.screenshot({path:`Book/Validation/camera-${name}-ground.png`});
   await page.keyboard.down('Space');await page.waitForTimeout(290);
   await page.screenshot({path:`Book/Validation/camera-${name}-jump.png`});
   await page.keyboard.up('Space');await page.waitForTimeout(800);
   for(let i=0;i<2;i++) {await page.keyboard.down('Space');await page.waitForTimeout(350);await page.keyboard.up('Space');await page.waitForTimeout(500);}
   await page.keyboard.down('KeyD');await page.waitForTimeout(400);
   await page.keyboard.down('Space');await page.waitForTimeout(200);
   await page.keyboard.press('Shift');await page.waitForTimeout(300);
   await page.keyboard.up('Space');await page.keyboard.up('KeyD');await page.waitForTimeout(1600);
   await page.screenshot({path:`Book/Validation/camera-${name}-after.png`});
  }
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/camera-follow-web.json',JSON.stringify({passed:true,rooms:['1-09','2-18'],errors,scope:'WebGL load, repeated jumping, run/jump/air dash and visual captures. Camera displacement and framing assertions are in camera-follow.json.'},null,2)+'\n');
  console.log('PASS: WebGL camera movement smoke test in two tall rooms, no browser errors.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
