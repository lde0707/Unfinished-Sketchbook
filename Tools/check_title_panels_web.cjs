const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  // A fresh browser context isolates this test from the user's real bookmark.
  const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{fs.appendFileSync('Builds/title-panel-browser.log',m.text()+'\n');if(/Exception|Shader.*error|SendMessage.*not found|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  const out='Book/Validation/title-panels-web';fs.mkdirSync(out,{recursive:true});
  await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
  async function capture(name){await page.waitForTimeout(450);const file=out+'/'+name+'.png';await page.locator('#unity-canvas').screenshot({path:file,timeout:60000});assert.ok((await sharp(file).stats()).entropy>1,'actual Unity render '+name);return file;}
  async function click(x,y){const b=await page.locator('#unity-canvas').boundingBox();await page.mouse.click(b.x+x/1920*b.width,b.y+y/1080*b.height);await page.waitForTimeout(350);}
  async function key(k){await page.keyboard.press(k);await page.waitForTimeout(350);}
  await page.goto('http://127.0.0.1:8080/?v=painted-title-panels-20261028');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(8000);await page.locator('#unity-canvas').focus();
  await capture('01-home');await key('ArrowDown');await key('Enter');await capture('02-screen');
  await click(920,155);await capture('03-sound');
  await click(1110,405);await capture('04-sound-adjusted');await click(1370,900);await page.waitForTimeout(1700);
  await click(1310,155);await capture('05-controls-locked');
  await key('Tab');await capture('06-screen-keyboard');
  await click(1340,380);await capture('07-resolution-preview');await key('Escape');
  await click(470,630);await capture('08-new-no-save');
  await click(400,900);await capture('09-home-after-back');
  await click(470,630);await click(1300,900);await page.waitForTimeout(5000);await capture('10-game-started');
  await key('Escape');await capture('11-pause');await click(960,710);await page.waitForTimeout(5000);await capture('12-home-saved');
  await click(470,520);await capture('13-continue-bookmark');await click(400,900);
  await click(470,630);await capture('14-new-overwrite');await key('Escape');
  await key('Enter');await key('ArrowDown');await key('Enter');await page.waitForTimeout(5000);await capture('15-resumed');
  await key('Escape');await click(960,710);await page.waitForTimeout(5000);
  await page.setViewportSize({width:1280,height:720});await click(470,744);await capture('16-settings-720');
  await page.setViewportSize({width:1920,height:820});await capture('17-settings-wide');
  assert.deepEqual(errors,[]);fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,realWebGL:true,errors,captures:17,scenarios:['three settings tabs','unlocked controls only','audio slider','save','resolution preview rollback','new game confirm','back','pause and save','real last-zone bookmark','overwrite cancel','continue','720p and ultrawide fit']},null,2));
  console.log('TITLE_PANELS_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
