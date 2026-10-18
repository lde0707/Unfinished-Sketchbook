const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const plan=JSON.parse(fs.readFileSync('Art/room-backdrops-v1/room-layouts.json'));
  const page=await browser.newPage({viewport:{width:1600,height:1080}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  await page.goto('http://127.0.0.1:8093/room-backdrops-v1/preview.html');
  await page.waitForFunction(()=>document.querySelectorAll('#rooms button').length===56);
  assert.equal(await page.locator('#title').textContent(),'무너진 갱도');
  await page.selectOption('#zone','C');await page.locator('button[data-id="1-14"]').click();
  await page.waitForFunction(()=>document.querySelector('#shot').complete&&document.querySelector('#shot').naturalWidth===1920);
  await page.screenshot({path:'Book/Validation/room-backdrops-gallery.png'});
  await page.selectOption('#zone','G');await page.fill('#search','폭포');
  assert.equal(await page.locator('#rooms button').count(),2);
  await page.locator('button[data-id="2-18"]').click();
  assert.match(await page.locator('#meta').textContent(),/34 × 28칸/);
  // All 56 saved compositions must be available, including the small optional rooms.
  for(const room of plan.rooms){const response=await page.request.get('http://127.0.0.1:8093/room-backdrops-v1/rooms/'+room.id+'.png');assert.equal(response.status(),200);}
  if(process.argv.includes('--gallery-only')){assert.deepEqual(errors,[]);console.log('PASS: gallery navigation and all 56 images.');return;}
  await page.goto('http://127.0.0.1:8080/?v=room-backdrops-20261018&spawn=78,123');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});
  await page.locator('#unity-canvas').focus();await page.waitForTimeout(6000);
  const rooms=['1-01','1-09','1-14','1-22','2-03','2-07','2-18','2-20'];
  for(const id of rooms){
   const room=plan.rooms.find(r=>r.id===id);assert(room.previewSpawn);
   await page.evaluate(spawn=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',spawn),room.previewSpawn);
   await page.waitForTimeout(400);await page.screenshot({path:'Book/Validation/room-backdrops/web-'+id+'.png'});
   await page.keyboard.down('KeyD');await page.keyboard.down('Space');await page.waitForTimeout(250);
   await page.keyboard.up('Space');await page.keyboard.up('KeyD');await page.waitForTimeout(500);
  }
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/room-backdrops-web.json',JSON.stringify({passed:true,gridSha256:plan.gridSha256,galleryRooms:56,webglRooms:rooms,errors,scope:'Gallery interaction / all images load / WebGL shader and movement smoke test. Native validation separately verifies scene layers and collision geometry.'},null,2)+'\n');
  console.log('PASS: 56 room previews, gallery navigation, 8 WebGL room renders and movement smoke test; zero browser errors.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
