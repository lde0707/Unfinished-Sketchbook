const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  await page.goto('http://127.0.0.1:8092/player-state-test/preview.html?demo=dash');await page.waitForFunction(()=>previewState.ready);
  const results=await page.evaluate(()=>{
   const d=previewDebug,s=previewState;s.playing=false;const snap=()=>JSON.parse(JSON.stringify(s)),directions=[];
   for(const dir of [1,-1]){
    d.reset();d.direction(dir);d.advance(400);const from=s.x;d.dash();const frames=new Set();let maxError=0;
    for(let t=0;t<180;t++){frames.add(s.frame);d.advance(1);maxError=Math.max(maxError,Math.abs(s.x-from-dir*.64*(t+1)));}
    directions.push({dir,frames:[...frames],distance:s.x-from,maxError,after:snap()});
   }
   d.reset();d.direction(1);d.advance(400);d.dash();d.advance(50);d.direction(-1);const locked=snap();d.advance(130);const reversed=snap();
   d.reset();d.dash();d.advance(180);const released=snap();d.advance(160);const idle=snap();
   d.reset();d.dash();const blockedActive=d.dash();d.advance(180);const blockedCooldown=d.dash();d.advance(219);const blockedBeforeReady=d.dash();d.advance(1);const readyAgain=d.dash();
   d.reset();d.direction(1);d.advance(400);d.dash();d.advance(90);d.jump();const cancel=snap();d.advance(520);const cancelAfter=snap();
   d.reset();d.jump();d.advance(100);const blockedAir=d.dash();d.advance(395);const acceptedLanding=d.dash();
   d.reset();d.dash();d.reset();const resetReady=d.dash();
   d.demo('dash');s.playing=false;d.advance(3400);const demo=snap();
   d.demo('dashJump');s.playing=false;d.advance(2600);const jumpDemo=snap();
   return {directions,locked,reversed,released,idle,blockedActive,blockedCooldown,blockedBeforeReady,readyAgain,cancel,cancelAfter,blockedAir,acceptedLanding,resetReady,demo,jumpDemo};
  });
  for(const r of results.directions){assert.deepEqual(r.frames,[0,1,2,3]);assert(Math.abs(r.distance-r.dir*115.2)<1e-8);assert(r.maxError<1e-8);assert.equal(r.after.mode,'run');assert.equal(r.after.facing,r.dir);assert.equal(r.after.y,0);}
  assert.equal(results.locked.mode,'dash');assert.equal(results.locked.facing,1);assert.equal(results.reversed.mode,'run');assert.equal(results.reversed.facing,-1);
  assert.equal(results.released.mode,'stop');assert.equal(results.idle.mode,'idle');
  for(const key of ['blockedActive','blockedCooldown','blockedBeforeReady','blockedAir'])assert.equal(results[key],false,key);
  for(const key of ['readyAgain','acceptedLanding','resetReady'])assert.equal(results[key],true,key);
  assert.equal(results.cancel.mode,'jump');assert.equal(results.cancel.jumpVariant,'jump');assert.equal(results.cancelAfter.mode,'run');
  assert.equal(results.demo.dashCount,2);assert.equal(results.demo.mode,'idle');assert.equal(results.jumpDemo.dashCount,1);assert.equal(results.jumpDemo.jumpCount,1);assert.equal(results.jumpDemo.mode,'idle');
  await page.click('#reset');await page.keyboard.down('KeyD');await page.evaluate(()=>previewDebug.advance(400));await page.keyboard.down('ShiftLeft');
  assert.equal(await page.evaluate(()=>previewState.mode),'dash');await page.evaluate(()=>previewDebug.advance(500));await page.keyboard.down('ShiftLeft');assert.equal(await page.evaluate(()=>previewState.dashCount),1);
  await page.keyboard.up('ShiftLeft');await page.keyboard.press('ShiftRight');assert.equal(await page.evaluate(()=>previewState.dashCount),2);await page.keyboard.up('KeyD');
  await page.click('#reset');await page.click('#dash');assert.equal(await page.evaluate(()=>previewState.mode),'dash');
  await page.evaluate(()=>previewDebug.advance(60));await page.screenshot({path:'Art/animations/player-c-dash-v1/preview-check.png'});
  await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
  const report={passed:true,checks:['4 drawings across 180ms; 115.2px travel at640px/s in both directions','start direction locked until dash ends','resume run or stop then idle','jump cancels ground dash; airborne dash rejected; landing accepts dash','active/cooldown reentry rejected; ready at400ms from start','reset clears cooldown','left/right Shift and on-screen button; no auto-repeat dash','both dash demos finish correctly','mobile width and no browser errors'],results};
  fs.writeFileSync('Art/animations/player-c-dash-v1/browser-validation.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,checks:report.checks}));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
