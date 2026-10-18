const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1600,height:1080}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  await page.goto('http://127.0.0.1:8080/?v=zone-tiles-20261018&spawn=78,123');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  // createUnityInstance resolves before the Unity splash has finished drawing.
  await page.locator('#unity-canvas').focus();await page.waitForTimeout(6000);
  assert(await page.locator('#loading').isHidden());
  await page.screenshot({path:'Book/Validation/mezzanine-web-query-start.png'});
  const rooms=[['1-9','78,123'],['1-14','108,48'],['2-3','372,28'],['2-4','430,30'],['2-7','300,88'],['2-18','527,115']];
  for(const [name,spawn]of rooms){
   await page.evaluate(cell=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',cell),spawn);
   await page.waitForTimeout(350);await page.screenshot({path:`Book/Validation/mezzanine-web-${name}.png`});
   await page.keyboard.down('KeyD');await page.keyboard.down('Space');await page.waitForTimeout(300);
   await page.keyboard.up('Space');await page.keyboard.up('KeyD');await page.waitForTimeout(600);
  }
  await page.keyboard.press('Digit1');await page.waitForTimeout(300);
  assert.deepEqual(errors,[]);
  const gridSha256=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/world12.json')).gridSha256;
  fs.writeFileSync('Book/Validation/mezzanines-web.json',JSON.stringify({passed:true,gridSha256,loadedUnity:true,rooms:rooms.map(([room,spawn])=>({room,spawn})),actions:['review spawn','right jump','return to starting region'],scope:'Browser load/input smoke test; route reachability is verified separately by the Unity GridMotor tests.',errors},null,2)+'\n');
  console.log('PASS: all six updated rooms rendered in WebGL, keyboard smoke test, zero browser errors.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
