const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright'));
(async()=>{
 const out='Book/Validation/resolution-preview-web';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const errors=[],checks=[];let page;
 try{
  page=await browser.newPage({viewport:{width:1280,height:720}});page.setDefaultTimeout(60000);
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds|SendMessage.*not found/i.test(m.text()))errors.push(m.text());});
  page.on('dialog',async d=>{errors.push(d.message());await d.dismiss();});
  const wait=ms=>page.waitForTimeout(ms);
  async function click(x,y){const p=await page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.x+(r.width-1920*s)/2+x*s,y:r.y+(r.height-1080*s)/2+y*s};},{x,y});await page.mouse.click(p.x,p.y,{delay:250});await wait(900);}
  async function key(k){await page.keyboard.press(k,{delay:350});await wait(800);}
  async function buffer(w,h,label){await page.waitForFunction(({w,h})=>{const c=document.querySelector('canvas');return c.width===w&&c.height===h;},{w,h},{timeout:30000});checks.push(label);console.log('PASS '+label);}
  async function boot(reload=false){if(reload)await page.reload();else await page.goto('http://127.0.0.1:8080/?v=resolution-save-preview-20261029');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await wait(5000);await page.locator('canvas').focus();}
  async function openSettings(){await click(470,744);await click(530,155);}
  async function shot(name){await page.screenshot({path:out+'/'+name+'.png',timeout:60000});}
  await boot();await buffer(1920,1080,'initial saved resolution');await openSettings();
  await click(1050,380);await buffer(1366,768,'arrow immediately previews without confirmation');await wait(13500);await buffer(1366,768,'no automatic 12-second rollback');await shot('01-live-preview');
  await click(1340,380);await click(1340,380);await buffer(1920,1200,'successive arrows remain usable');
  await click(920,155);await buffer(1920,1200,'sound tab retains preview');await click(530,155);await click(1340,458);await buffer(1920,1200,'quality change retains preview');
  await click(420,900);await buffer(1920,1080,'back cancels all unsaved resolution changes');
  await openSettings();await click(1050,380);await key('Escape');await buffer(1920,1080,'ESC cancels preview');
  await openSettings();await key('ArrowLeft');await buffer(1366,768,'keyboard arrow previews immediately');
  for(let i=0;i<8;i++)await key('ArrowDown');await key('Enter');await key('Escape');await buffer(1366,768,'keyboard Save persists when leaving settings');
  await boot(true);await buffer(1366,768,'explicit save survives reload');
  await openSettings();await click(1340,380);await click(1390,900);await click(1340,380);await key('Escape');await buffer(1920,1080,'save then another edit rolls back to newly saved value');
  await openSettings();await click(1340,380);await click(920,155);await click(1390,900);await click(420,900);await buffer(1920,1200,'save from sound tab commits resolution');
  await openSettings();await click(900,900);await buffer(1920,1080,'defaults preview immediately');await key('Escape');await buffer(1920,1200,'unsaved defaults roll back');
  await page.evaluate(()=>{unityInstance.SendMessage('Stilldrawn Title','SkipForPreview');unityInstance.SendMessage('World Prototype','PreviewPosition','118,59');});await wait(2500);await key('Escape');await click(960,475);await click(530,155);
  await click(1050,380);await buffer(1920,1080,'in-game settings preview');await shot('02-pause-preview');await key('Escape');await buffer(1920,1200,'leaving in-game settings restores saved resolution');await key('Escape');
  await key('Escape');await click(960,475);await click(530,155);await click(1050,380);await click(1390,900);await shot('03-saved');await key('Escape');await key('Escape');await buffer(1920,1080,'in-game Save survives resume');
  await boot(true);await buffer(1920,1080,'in-game save survives reload');await openSettings();await click(1050,380);await boot(true);await buffer(1920,1080,'reload discards unsaved preview');await shot('04-reloaded');
  assert.deepEqual(errors,[]);fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('RESOLUTION_PREVIEW_WEB_OK');
 }catch(e){if(page){await page.screenshot({path:out+'/failure.png',timeout:20000}).catch(()=>{});console.log(await page.locator('body').innerText().catch(()=>''));}throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
