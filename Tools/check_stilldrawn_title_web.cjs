const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|SendMessage.*not found|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
 await page.route(u=>u.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
 async function key(k){await page.keyboard.press(k);await page.waitForTimeout(120);}
 async function clickUi(x,y){const b=await page.locator('#unity-canvas').boundingBox();await page.mouse.click(b.x+x/1920*b.width,b.y+y/1080*b.height);await page.waitForTimeout(250);}
 const out='Book/Validation/stilldrawn-title';fs.mkdirSync(out,{recursive:true});
 async function capture(name){await page.waitForTimeout(250);const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png');});const file=out+'/'+name+'.png';fs.writeFileSync(file,Buffer.from(data.split(',')[1],'base64'));assert.ok((await sharp(file).stats()).entropy>1,'rendered game '+name);return file;}
 async function boot(url){await page.goto(url);await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(2500);await page.locator('#unity-canvas').focus();}
 if((process.argv.includes('--static-logo')||process.argv.includes('--intro'))){
  await boot('http://127.0.0.1:8080/?v=title-intro-hover');
  await page.evaluate(()=>window.unityInstance.SendMessage('Stilldrawn Title','ReturnToTitle'));
  // Scene re-entry uses the same entrance as startup, with the browser already
  // connected so its first painted logo frames can be sampled reliably.
  const frames=[];
  for(let i=0;i<32;i++){
   await page.waitForTimeout(100);const data=await page.evaluate(async()=>{await new Promise(requestAnimationFrame);return document.querySelector('#unity-canvas').toDataURL('image/png');});
   const bytes=Buffer.from(data.split(',')[1],'base64');if((await sharp(bytes).stats()).entropy<1)continue;
   const stats=await sharp(bytes).extract({left:95,top:75,width:547,height:218}).stats();frames.push({bytes,brightness:stats.channels.slice(0,3).reduce((n,c)=>n+c.mean,0)});
  }
  assert.ok(frames.length>2,'title entrance frames captured');frames.sort((a,b)=>a.brightness-b.brightness);
  const intro=out+'/20-logo-intro.png';fs.writeFileSync(intro,frames[0].bytes);
  fs.writeFileSync(out+'/20b-logo-progress.png',frames[Math.floor(frames.length/2)].bytes);
  await page.waitForTimeout(1500);const complete=await capture('21-logo-complete');
  async function cropDifference(a,b,rect){const x=await sharp(a).extract(rect).removeAlpha().raw().toBuffer(),y=await sharp(b).extract(rect).removeAlpha().raw().toBuffer();let sum=0;for(let i=0;i<x.length;i++)sum+=Math.abs(x[i]-y[i]);return sum/x.length;}
  const reveal=await cropDifference(intro,complete,{left:95,top:75,width:547,height:218});assert.ok(reveal<3,'logo is fully visible without a reveal animation');
  const bounds=await page.locator('#unity-canvas').boundingBox();await page.mouse.move(bounds.x+310/1920*bounds.width,bounds.y+637/1080*bounds.height);const hover=await capture('22-menu-hover');
  await page.mouse.move(bounds.x+20/1920*bounds.width,bounds.y+20/1080*bounds.height);const leave=await capture('23-menu-leave');
  const area={left:107,top:410,width:220,height:34};const emphasis=await cropDifference(hover,leave,area);assert.ok(emphasis>.3,'mouse leave removes menu emphasis');
  // Ambient fog keeps changing behind the labels; compare its residual
  // difference against the much stronger real hover emphasis.
  const neutral=await cropDifference(complete,leave,area);assert.ok(neutral<emphasis*.25,'no persistent selected row after pointer leaves');assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/stilldrawn-title-static-logo-hover.json',JSON.stringify({passed:true,realWebGL:true,staticLogoDifference:reveal,hoverDifference:emphasis,mouseLeaveBaselineDifference:neutral,errors},null,2));console.log('STILLDRAWN_TITLE_STATIC_LOGO_HOVER_OK');return;
 }
 await boot('http://127.0.0.1:8080/?v=stilldrawn-title-20261024');assert.equal(await page.title(),'Stilldrawn');await page.evaluate(()=>{const c=document.querySelector('#unity-canvas');c.width=1920;c.height=1080});await page.waitForTimeout(600);await capture('01-title-new');
 if(process.argv.includes('--motion')||process.argv.includes('--record-only')){
  await page.evaluate(()=>{const c=document.querySelector('#unity-canvas');c.width=1280;c.height=720});await page.waitForTimeout(500);
  const first=await capture('14-motion-a');await page.waitForTimeout(3000);const second=await capture('15-motion-b');
  async function difference(a,b){const x=await sharp(a).removeAlpha().raw().toBuffer(),y=await sharp(b).removeAlpha().raw().toBuffer();let sum=0;for(let i=0;i<x.length;i++)sum+=Math.abs(x[i]-y[i]);return sum/x.length;}
  const changed=await difference(first,second);assert.ok(changed>.2,'title visibly animates while world is paused');
  const video=await page.evaluate(async()=>{const source=document.querySelector('#unity-canvas'),c=document.createElement('canvas');c.width=source.width;c.height=source.height;const context=c.getContext('2d');await new Promise(requestAnimationFrame);context.drawImage(source,0,0);let live=true,frame;const copy=()=>{if(!live)return;context.drawImage(source,0,0);frame=requestAnimationFrame(copy);};frame=requestAnimationFrame(copy);const stream=c.captureStream(20),chunks=[];const type=MediaRecorder.isTypeSupported('video/webm;codecs=vp8')?'video/webm;codecs=vp8':'video/webm';const recorder=new MediaRecorder(stream,{mimeType:type,videoBitsPerSecond:1800000});recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};const done=new Promise(resolve=>recorder.onstop=resolve);recorder.start(1000);await new Promise(resolve=>setTimeout(resolve,24000));recorder.stop();await done;live=false;cancelAnimationFrame(frame);stream.getTracks().forEach(t=>t.stop());return await new Promise(resolve=>{const reader=new FileReader();reader.onload=()=>resolve(reader.result);reader.readAsDataURL(new Blob(chunks,{type:'video/webm'}));});});
  fs.writeFileSync(out+'/title-loop.webm',Buffer.from(video.split(',')[1],'base64'));
  const videoFrame=await page.evaluate(async src=>{const v=document.createElement('video');v.muted=true;await new Promise((ok,no)=>{v.onloadeddata=ok;v.onerror=no;v.src=src;v.load();});await v.play();await new Promise(ok=>setTimeout(ok,800));v.pause();const c=document.createElement('canvas');c.width=v.videoWidth;c.height=v.videoHeight;c.getContext('2d').drawImage(v,0,0);return c.toDataURL('image/png');},video);const decoded=Buffer.from(videoFrame.split(',')[1],'base64');assert.ok((await sharp(decoded).stats()).entropy>1,'recording contains the real title');fs.writeFileSync(out+'/video-frame.png',decoded);if(process.argv.includes('--record-only')){console.log('TITLE_LOOP_VIDEO_DECODE_OK');return;}

  await clickUi(310,704);await page.waitForTimeout(800);await capture('18-motion-settings-before');await clickUi(500,666);await page.waitForTimeout(800);await capture('19-motion-settings-after');await clickUi(310,837);await page.mouse.move(20,20);await page.waitForTimeout(1200);
  const frozenA=await capture('16-motion-reduced-a');await page.waitForTimeout(2200);const frozenB=await capture('17-motion-reduced-b');const frozen=await difference(frozenA,frozenB);assert.ok(frozen<.01,'reduced motion produces a static title');assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/stilldrawn-title-motion-web.json',JSON.stringify({passed:true,realWebGL:true,animatedDifference:changed,reducedMotionDifference:frozen,loopVideoSeconds:24,errors},null,2));console.log('STILLDRAWN_TITLE_MOTION_WEB_OK');return;
 }
if(process.argv.includes('--branding')){await page.evaluate(()=>{const c=document.querySelector('#unity-canvas');c.width=1280;c.height=720});await page.waitForTimeout(300);await clickUi(310,704);await page.mouse.move(20,20);await capture('13-brand-settings');assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/stilldrawn-branding-web.json',JSON.stringify({passed:true,realWebGL:true,logo:'stilldrawn-logo-v1',menuFont:'MaruBuri-SemiBold',latinFont:'Marcellus-Regular',errors,screenshots:[out+'/01-title-new.png',out+'/13-brand-settings.png']},null,2));console.log('STILLDRAWN_BRANDING_WEB_OK');return;}if(process.argv.includes('--title-only')){assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/stilldrawn-title-no-player-web.json',JSON.stringify({passed:true,realWebGL:true,errors,screenshot:out+'/01-title-new.png'},null,2));console.log('STILLDRAWN_TITLE_NO_PLAYER_WEB_OK');return;}await page.evaluate(()=>{const c=document.querySelector('#unity-canvas');c.width=1280;c.height=720});await page.waitForTimeout(300);
 await clickUi(310,704);await page.mouse.move(20,20);await page.waitForTimeout(350);await capture('02-settings');
 await key('ArrowRight');for(let i=0;i<4;i++)await key('ArrowDown');await key('Enter');await capture('03-reduced-motion');
 await key('ArrowDown');await key('Enter');await capture('04-controls');await key('Escape');await key('Escape');
 await key('Enter');await page.waitForTimeout(3500);await capture('05-new-game');
 await page.keyboard.down('d');await page.waitForTimeout(450);await page.keyboard.up('d');await key('Space');await page.waitForTimeout(700);
 await key('Escape');await capture('06-pause');
 for(let i=0;i<3;i++)await key('ArrowDown');await key('Enter');await page.waitForTimeout(3000);await capture('07-real-continue');
 await key('ArrowDown');await key('Enter');await capture('08-overwrite-confirm');await key('Escape');
 await key('Enter');await page.waitForTimeout(3000);await capture('09-continued-game');
 await page.reload();await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(2500);await page.locator('#unity-canvas').focus();await capture('10-persisted-title');
 await key('ArrowDown');await key('ArrowDown');await key('ArrowDown');await key('Enter');await capture('11-credits');
 await boot('http://127.0.0.1:8080/?v=stilldrawn-title-20261024&spawn=255,34');await capture('12-preview-bypass');
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/stilldrawn-title-web.json',JSON.stringify({passed:true,realWebGL:true,captures:12,errors,scenarios:['initial title','settings','reduced motion','controls','new game','pause','save and title','overwrite confirmation cancel','continue across scene reload','browser reload persistence','credits','preview spawn bypass']},null,2));console.log('STILLDRAWN_TITLE_WEB_OK');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});
