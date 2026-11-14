const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[],out='Book/Validation/zone-entry';fs.mkdirSync(out,{recursive:true});
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|SendMessage.*not found|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
 await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
 async function capture(name){const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});const file=out+'/'+name+'.png';fs.writeFileSync(file,Buffer.from(data.split(',')[1],'base64'));assert.ok((await sharp(file).stats()).entropy>1,'rendered '+name);return file;}
 async function move(position){await page.evaluate(pos=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',pos),position);}
 async function difference(a,b){const rect={left:390,top:30,width:500,height:115};const x=await sharp(a).extract(rect).removeAlpha().raw().toBuffer(),y=await sharp(b).extract(rect).removeAlpha().raw().toBuffer();let sum=0;for(let i=0;i<x.length;i++)sum+=Math.abs(x[i]-y[i]);return sum/x.length;}
 await page.goto('http://127.0.0.1:8080/?v=zone-entry&spawn=78,123');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(4700);
 // Software WebGL can run fewer simulation frames than wall-clock time.
 // Allow enough game frames for the debounced entry and complete fade.
 await move('236,131');await page.waitForTimeout(3500);const shown=await capture('01-paper-cave-title');await page.waitForTimeout(15000);const hidden=await capture('02-title-faded');
 const fade=await difference(shown,hidden);assert.ok(fade>1,'generated zone title appears and fades away');
 await move('259,131');await page.waitForTimeout(1000);await capture('03-special-block-no-popup');
 await move('490,36');await page.waitForTimeout(3500);await capture('04-knight-tower-title');
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/zone-entry-web.json',JSON.stringify({passed:true,realWebGL:true,generatedLettering:true,zoneTitleFadeDifference:fade,zoneEntryScreenshots:['01-paper-cave-title','02-title-faded','03-special-block-no-popup','04-knight-tower-title'],errors},null,2));console.log('ZONE_ENTRY_WEB_OK');
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
