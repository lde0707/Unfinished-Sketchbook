// Fast wrapper regression: layout must fit even if WebGL delays the JS resize event.
// This is a CSS check; check_ui_resolutions_web.cjs separately runs the actual game.
const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 const cases=[];
 try{
  const page=await browser.newPage({viewport:{width:3840,height:2160}});
  await page.route('**/Build/**',route=>route.abort());
  await page.goto('http://127.0.0.1:8080/');
  await page.evaluate(()=>{document.querySelector('#loading').hidden=true;window.removeEventListener('resize',resizeGameBuffer);});
  for(const buffer of [[1366,768],[1920,1080],[1920,1200],[2560,1080],[2560,1440],[2560,1600],[3440,1440],[3840,2160]]){
   await page.evaluate(([width,height])=>{window.stilldrawnResolution={width,height};resizeStilldrawn();},buffer);
   for(const viewport of [[3840,2160],[1366,768],[1366,650],[1440,900],[768,1024]]){
    await page.setViewportSize({width:viewport[0],height:viewport[1]});
    const rect=await page.evaluate(()=>{const c=document.querySelector('canvas'),r=c.getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height,buffer:[c.width,c.height]};});
    assert.deepEqual(rect.buffer,buffer);assert(rect.x>=-.5&&rect.y>=-.5&&rect.x+rect.w<=viewport[0]+.5&&rect.y+rect.h<=viewport[1]+.5,JSON.stringify({buffer,viewport,rect}));
    assert(Math.abs(rect.w/rect.h-buffer[0]/buffer[1])<.002,'aspect preserved');cases.push({buffer,viewport,rect});
   }
  }
  fs.writeFileSync('Book/Validation/ui-resolutions/viewport-layout.json',JSON.stringify({passed:true,resizeHandlerDisabled:true,cases},null,2));console.log('WEBGL_VIEWPORT_OK '+cases.length);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
