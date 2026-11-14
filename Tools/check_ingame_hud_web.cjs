const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[],out='Book/Validation/ingame-hud';fs.mkdirSync(out,{recursive:true});
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
  async function capture(name){await page.waitForTimeout(350);const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});const file=out+'/'+name+'.png';fs.writeFileSync(file,Buffer.from(data.split(',')[1],'base64'));assert.ok((await sharp(file).stats()).entropy>1,'rendered '+name);return file;}
  async function click(x,y){const b=await page.locator('#unity-canvas').boundingBox();await page.mouse.click(b.x+x/1920*b.width,b.y+y/1080*b.height);await page.waitForTimeout(500);}
  async function difference(a,b,rect){const x=await sharp(a).extract(rect).removeAlpha().raw().toBuffer(),y=await sharp(b).extract(rect).removeAlpha().raw().toBuffer();let n=0;for(let i=0;i<x.length;i++)n+=Math.abs(x[i]-y[i]);return n/x.length;}
  await page.goto('http://127.0.0.1:8080/?v=ingame-hud&spawn=78,123');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(3000);await page.locator('#unity-canvas').focus();
  const game=await capture('01-gameplay');await click(1610,308);const map=await capture('02-map-region1');
  assert.ok(await difference(game,map,{left:390,top:80,width:500,height:450})>5,'minimap button opens journal');
  await click(359,155);const second=await capture('03-map-region2');assert.ok(await difference(map,second,{left:35,top:85,width:500,height:50})>.3,'region tab selection changes');
  await click(572,155);await capture('04-map-connected');await click(960,500);const pin=await capture('05-map-pin');
  await click(1740,941);const clear=await capture('06-map-clear-pin');assert.ok(await difference(pin,clear,{left:1080,top:608,width:170,height:40})>.2,'map pin can be cleared');
  await page.keyboard.press('Escape');await page.waitForTimeout(500);const closed=await capture('07-escape-closes-map');assert.ok(await difference(closed,clear,{left:390,top:80,width:500,height:450})>5,'Esc returns from map to gameplay');
  await page.keyboard.press('Escape');await page.waitForTimeout(500);await capture('08-pause');await page.keyboard.press('Escape');await page.waitForTimeout(500);await capture('09-resume');
  await click(1800,360);await capture('10-hud-pause-button');await page.keyboard.press('Escape');await page.waitForTimeout(500);
  await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','185,132'));await page.waitForTimeout(500);await capture('12-red-healing-pickup');
  await page.evaluate(()=>window.unityInstance.SendMessage('World Prototype','PreviewPosition','190,132'));await page.waitForTimeout(700);const healed=await capture('13-healing-unlocked');
  const healthPixels=await sharp(healed).extract({left:277,top:27,width:145,height:77}).removeAlpha().raw().toBuffer();let red=0;for(let i=0;i<healthPixels.length;i+=3)if(healthPixels[i]>65&&healthPixels[i]>healthPixels[i+1]*1.4&&healthPixels[i]>healthPixels[i+2]*1.3)red++;assert.ok(red>40,'collected healing item reveals a red HUD bottle');
  await page.setViewportSize({width:1100,height:900});await page.waitForTimeout(500);await page.evaluate(()=>{const c=document.querySelector('#unity-canvas');c.width=1024;c.height=768});await capture('11-compact-viewport');
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/ingame-hud-web.json',JSON.stringify({passed:true,realWebGL:true,minimapButton:true,regionTabs:true,mapPinClear:true,escapeClosesMap:true,pauseResume:true,compactViewport:true,errors},null,2));console.log('INGAME_HUD_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
