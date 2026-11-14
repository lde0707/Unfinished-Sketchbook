const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{
 const out='Book/Validation/atmosphere-v1';fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[],states=[];
  page.on('pageerror',e=>{errors.push(e.message);console.log('PAGE_ERROR',e.message);});page.on('console',m=>{if(/Exception|Shader.*error|memory access out of bounds|SendMessage.*not found/i.test(m.text()))errors.push(m.text());if(m.type()==='error'||/WORLD_ATMOSPHERE|UNFIN|Initialize engine/.test(m.text()))console.log(m.text().slice(0,1400));});
  page.on('requestfailed',r=>console.log('REQUEST_FAILED',r.url(),r.failure()));
  page.on('response',r=>{if(r.status()>=400)console.log('HTTP_ERROR',r.status(),r.url());});
  page.on('dialog',async d=>{console.log('DIALOG',d.message());await d.dismiss();});
  // Visual/behaviour check at a fixed 720p buffer; resolution coverage has a separate audit.
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
  const wait=ms=>page.waitForTimeout(ms);
  async function send(method,value=''){await page.evaluate(({method,value})=>unityInstance.SendMessage('World Prototype',method,value),{method,value});await wait(250);}
  async function state(){const msg=page.waitForEvent('console',{predicate:m=>m.text().includes('WORLD_ATMOSPHERE '),timeout:20000});await send('ReportAtmosphere');const str=(await msg).text();return JSON.parse(str.slice(str.indexOf('{')));}
  async function shot(name){const file=out+'/'+name+'.png';await page.locator('canvas').screenshot({path:file,timeout:60000});assert((await sharp(file).stats()).entropy>1);}
  async function key(k){await page.keyboard.press(k,{delay:200});await wait(500);}
  async function click(x,y){const b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+x/1920*b.width,b.y+y/1080*b.height,{delay:150});await wait(600);}
  await page.goto('http://127.0.0.1:8080/?v=immersive-atmosphere-20261028&spawn=78,123');try{await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});}catch(e){console.log(await page.locator('body').innerText());await page.screenshot({path:out+'/boot-failure.png'});throw e;}await wait(8000);await page.locator('canvas').focus();
  if(process.argv.includes('--motion-only')){
   await send('PreviewPosition','118,59');await wait(8000);
   const sequence=[];
   for(let i=0;i<4;i++){const s=await state();sequence.push(s);await shot('motion-'+i);await wait(1800);}
   assert(sequence[3].clock>sequence[0].clock+.5);assert(sequence.every(s=>s.visibleDecorations>0&&s.anchorChecksum===sequence[0].anchorChecksum));
   const a=await sharp(out+'/motion-0.png').extract({left:310,top:145,width:235,height:200}).removeAlpha().raw().toBuffer(),b=await sharp(out+'/motion-3.png').extract({left:310,top:145,width:235,height:200}).removeAlpha().raw().toBuffer();let changed=0;for(let i=0;i<a.length;i++)if(Math.abs(a[i]-b[i])>2)changed++;
   assert(changed>100,'hanging scenery / light changes in rendered background');assert.deepEqual(errors,[]);
   const result=JSON.parse(fs.readFileSync(out+'/result.json'));result.motion={sequence,changedChannels:changed,area:'background hanging roots, excluding player and UI'};result.checks=result.checks.filter(c=>!c.includes('recording'));result.checks.push('rendered background motion across four actual screenshots');fs.writeFileSync(out+'/result.json',JSON.stringify(result,null,2));console.log('ATMOSPHERE_MOTION_OK');return;
  }
  await shot('start-on');await send('SetAtmospherePreview','off');await shot('start-off');await send('SetAtmospherePreview','on');
  const first=await state();assert(first.decorations>15&&first.colliders===0,JSON.stringify(first));
  const points=JSON.parse(fs.readFileSync('Book/Validation/map-zone-positions.json','utf8'));
  for(let i=0;i<points.length;i++){
   const p=points[i];await send('PreviewPosition',p.position);await wait(3600);const s=await state();assert.equal(s.zone,i);assert(s.enabled);if(s.anchorChecksum!==undefined)assert.equal(s.anchorChecksum,first.anchorChecksum,'scenery remains world anchored');states.push(s);await shot(p.zone+'-atmosphere');
  }
  await key('KeyM');const paused=await state();await wait(1600);const held=await state();assert(paused.paused&&held.clock===paused.clock,'map freezes all atmosphere shader clocks');await key('KeyM');await wait(700);assert((await state()).clock>held.clock);
  await key('Escape');const menu=await state();await wait(1000);assert.equal((await state()).clock,menu.clock,'ESC freezes atmosphere');
  await click(960,475);await click(530,155);await click(1340,536);await click(1340,536);await key('Escape');await key('Escape');assert((await state()).lowQuality,'low quality atmosphere path');await shot('low-quality');
  // Return to high quality before recording and capturing the review scene.
  await key('Escape');await click(960,475);await click(530,155);await click(1340,536);await key('Escape');await key('Escape');assert(!(await state()).lowQuality);
  await send('PreviewPosition','78,123');await wait(2500);
  assert.deepEqual(errors,[]);fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,first,states,paused,held,checks:['all eight zone profiles','no decorative colliders','world anchored scenery','map and ESC freeze motion','low quality path','real Unity rendering'],errors},null,2));console.log('ATMOSPHERE_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
