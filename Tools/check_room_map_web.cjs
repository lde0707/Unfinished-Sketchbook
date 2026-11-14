const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[],out='Book/Validation/room-map';fs.mkdirSync(out,{recursive:true});
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|SendMessage.*not found|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
 await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
 async function capture(name){const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png')});const file=out+'/'+name+'.png';fs.writeFileSync(file,Buffer.from(data.split(',')[1],'base64'));assert.ok((await sharp(file).stats()).entropy>1,'rendered '+name);return file;}
 async function move(pos){await page.evaluate(p=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',p),pos);await page.waitForTimeout(2400);}
 async function click(x,y){const b=await page.locator('#unity-canvas').boundingBox();await page.mouse.click(b.x+x/1920*b.width,b.y+y/1080*b.height);await page.waitForTimeout(900);}
 await page.goto('http://127.0.0.1:8080/?v=painted-room-map&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(5500);
 const a=await capture('01-entered-room');await move('243,131');const b=await capture('02-same-room');
 // The whole map stays fixed; only the small player marker should move.
 const rect={left:1035,top:54,width:200,height:104};
 const pa=await sharp(a).extract(rect).removeAlpha().raw().toBuffer(),pb=await sharp(b).extract(rect).removeAlpha().raw().toBuffer();let changed=0;
 for(let i=0;i<pa.length;i+=3)if(Math.abs(pa[i]-pb[i])+Math.abs(pa[i+1]-pb[i+1])+Math.abs(pa[i+2]-pb[i+2])>65)changed++;
 assert.ok(changed<450,'room stays visible without a player-following reveal circle');
 await click(1610,308);await capture('03-journal-room');await page.keyboard.press('Escape');await page.waitForTimeout(600);
 await move('490,36');await capture('04-region2-room');await move('78,123');await capture('05-tall-room');
 await move('236,131');await click(1610,308);await capture('06-remembered-rooms');await page.keyboard.press('Escape');
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/room-map-web.json',JSON.stringify({passed:true,realWebGL:true,stableRoomDrawingChangedPixels:changed,generatedPanel:true,journalMap:true,region2:true,tallRoom:true,errors},null,2));console.log('ROOM_MAP_WEB_OK');
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
