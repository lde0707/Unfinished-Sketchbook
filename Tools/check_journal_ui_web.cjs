const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const out='Book/Validation/journal-ui';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const context=await browser.newContext({viewport:{width:1280,height:800}}),page=await context.newPage(),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('console',m=>{if(/Exception|Missing.*script|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
  page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url());});
  async function ready(){await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('#unity-canvas').click();await page.waitForTimeout(4000);}
  async function click(x,y){const p=await page.evaluate(({x,y})=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);return{x:r.left+(r.width-1920*s)/2+x*s,y:r.top+(r.height-1080*s)/2+y*s};},{x,y});await page.mouse.click(p.x,p.y);await page.waitForTimeout(450);}
  async function shot(name){const data=await page.evaluate(async()=>{const canvas=document.querySelector('#unity-canvas'),probe=document.createElement('canvas');probe.width=64;probe.height=36;const ctx=probe.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(canvas,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return canvas.toDataURL('image/png');}throw new Error('No rendered frame for capture');});fs.writeFileSync(path.join(out,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
  async function dimensions(w,h){await page.waitForFunction(({w,h})=>{const c=document.querySelector('canvas');return c.width===w&&c.height===h;},{w,h},{timeout:15000});}
  await page.goto('http://127.0.0.1:8080/?v=journal-settings-20261025&spawn=236,131');await ready();for(const pos of ['78,123','143,105','185,132','236,131']){await page.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await page.waitForTimeout(600);}await shot('game');await page.locator('#fullscreen').click();await page.waitForFunction(()=>!!document.fullscreenElement);
  await page.keyboard.press('KeyM');await page.waitForTimeout(900);await click(750,670);await click(1370,390);
  const editor=page.locator('#stilldrawn-map-note');await editor.waitFor();assert(await editor.evaluate(e=>document.fullscreenElement.contains(e)));
  const note='나중에 다시 돌아올 길\n새 능력을 얻으면 높은 발판을 확인하자.';await editor.fill(note);await page.keyboard.press('KeyM');assert.equal(await editor.inputValue(),note+'m');await editor.fill(note);
  await page.keyboard.press('Control+Enter');await editor.waitFor({state:'detached'});await page.waitForTimeout(800);await shot('map-note');
  await page.keyboard.press('KeyM');await page.waitForTimeout(400);await page.keyboard.press('Escape');await page.waitForTimeout(700);await shot('pause');
  await click(400,604);await shot('controls');await page.keyboard.press('Escape');await page.waitForTimeout(500);await click(400,500);await shot('audio-settings');
  await page.keyboard.press('Tab');await page.waitForTimeout(400);await shot('graphics-settings');
  const expected=[[1920,1200],[2560,1080],[2560,1440],[2560,1600],[3440,1440],[3840,2160],[1366,768],[1920,1080]];
  for(const [w,h] of expected){await click(1035,354);await dimensions(w,h);await click(1400,490);if(w===1920&&h===1200||w===3440)await shot('settings-'+w+'x'+h);}
  if(process.argv.includes('--visual-only')){assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/journal-ui-layout.json',JSON.stringify({passed:true,renderedCaptures:true,fullscreenNoteEditor:true,resolutions:8,errors},null,2));console.log('JOURNAL_UI_LAYOUT_OK');return;}
  await click(1035,354);await dimensions(1920,1200);await page.waitForTimeout(13000);await dimensions(1920,1080);
  await click(1035,447);await click(1035,540);await click(1035,633);await shot('graphics-changed');
  await page.keyboard.press('Escape');await page.waitForTimeout(400);await page.keyboard.press('Escape');await page.waitForTimeout(400);
  await page.reload();await ready();await dimensions(1920,1080);
  await page.keyboard.press('KeyM');await page.waitForTimeout(700);await click(1360,740);await click(1370,390);await editor.waitFor();assert.equal(await editor.inputValue(),note);
  await page.keyboard.press('Escape');await editor.waitFor({state:'detached'});await click(1520,607);await shot('map-after-delete');
  await page.keyboard.press('KeyM');await page.waitForTimeout(500);await page.reload();await ready();await page.keyboard.press('KeyM');await page.waitForTimeout(700);await shot('map-deletion-persisted');
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/journal-ui-web.json',JSON.stringify({passed:true,realWebGL:true,eightRenderResolutions:expected,timeoutRevert:true,koreanNotesCreateReloadDelete:true,typingMDoesNotCloseMap:true,pauseSettingsControls:true,keyboardSettingsTab:true,fullscreenNotes:true,errors},null,2));console.log('JOURNAL_UI_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
