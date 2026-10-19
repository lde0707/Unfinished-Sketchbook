const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try {
  const page=await browser.newPage({viewport:{width:1600,height:1080}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  await page.goto('http://127.0.0.1:8080/?v=attack-range-20261018&spawn=252,131');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  // createUnityInstance resolves before Unity's splash finishes displaying.
  await page.waitForTimeout(5000);
  await page.locator('#unity-canvas').click();await page.waitForTimeout(1100);
  const box=await page.locator('#unity-canvas').boundingBox(),scale=box.height/8.4375;
  // Enemy at254,131: sample its upper strip, above the sweep at this distance.
  const roi={x:Math.round(box.x+box.width/2+1.65*scale),y:Math.round(box.y+box.height/2+.84*scale),w:Math.round(.65*scale),h:Math.round(.10*scale)};
  const prefix='Book/Validation/'+(process.env.UNFIN_CAPTURE_PREFIX||'attack-range-');
  async function shot(name){
   const png=await page.screenshot({path:prefix+name+'.png'});
   return page.evaluate(async({png,roi})=>{
    const im=new Image();im.src='data:image/png;base64,'+png;await im.decode();
    const c=document.createElement('canvas');c.width=roi.w;c.height=roi.h;const ctx=c.getContext('2d');
    ctx.drawImage(im,roi.x,roi.y,roi.w,roi.h,0,0,roi.w,roi.h);const pixels=ctx.getImageData(0,0,roi.w,roi.h).data;
    let green=0;for(let i=1;i<pixels.length;i+=4)green+=pixels[i];return green/(pixels.length/4);
   },{png:png.toString('base64'),roi});
  }
  const baseline=await shot('target-before'),samples=[];
  await page.keyboard.press('KeyJ');await page.waitForTimeout(125);
  for(let i=0;i<3;i++){samples.push(await shot('target-hit-'+i));await page.waitForTimeout(20);}
  await page.waitForTimeout(600);const restored=await shot('target-after');
  assert(Math.max(...samples)>baseline+35,`Expected target flash: ${JSON.stringify({baseline,samples,roi})}`);
  assert(Math.abs(restored-baseline)<12,`Target color restores after hit: ${baseline} -> ${restored}`);assert.deepEqual(errors,[]);
  fs.writeFileSync(prefix+'target-web.json',JSON.stringify({passed:true,spawn:'252,131',enemy:'254,131',baselineGreen:baseline,impactGreen:samples,restoredGreen:restored,errors},null,2)+'\n');
  console.log('PASS: real map enemy marker flashes on extended-range attack and recovers; no browser errors.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
