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
   d.reset();d.jump();d.advance(100);const acceptedAir=d.dash();
   d.reset();d.jump();d.advance(495);const acceptedLanding=d.dash();
   const airCases=[];
   for(const dir of [0,1,-1])for(const at of [60,230,430]){
    d.reset();d.direction(dir);d.jump();d.advance(at);const before=snap();const accepted=d.dash();let maxHeightError=0;
    for(let t=0;t<179;t++){d.advance(1);maxHeightError=Math.max(maxHeightError,Math.abs(s.y-before.y));}
    const during=snap();d.advance(1);const resumed=snap();d.advance(1000);const landed=snap();
    airCases.push({dir,at,accepted,before,during,resumed,landed,maxHeightError});
   }
   d.reset();d.direction(1);d.jump();d.advance(60);d.dash();d.advance(400);const secondAir=d.dash();d.advance(180);d.jump();d.advance(60);const nextJumpAir=d.dash();
   d.reset();d.direction(1);d.jump();d.advance(100);d.dash();d.jump();const noAirJump=snap();d.direction(0);d.advance(180);const releaseAir=snap();d.advance(1000);const noExtraJump=snap();
   d.reset();d.direction(-1);d.jump();d.advance(430);d.dash();d.advance(150);d.jump();d.advance(60);const bufferedLanding=snap();
   d.demo('airDash');s.playing=false;d.advance(3400);const airDemo=snap();
   d.reset();d.dash();d.reset();const resetReady=d.dash();
   d.demo('dash');s.playing=false;d.advance(3400);const demo=snap();
   d.demo('dashJump');s.playing=false;d.advance(2600);const jumpDemo=snap();
   return {directions,locked,reversed,released,idle,blockedActive,blockedCooldown,blockedBeforeReady,readyAgain,cancel,cancelAfter,acceptedAir,acceptedLanding,airCases,secondAir,nextJumpAir,noAirJump,releaseAir,noExtraJump,bufferedLanding,airDemo,resetReady,demo,jumpDemo};
  });
  for(const r of results.directions){assert.deepEqual(r.frames,[0,1,2,3]);assert(Math.abs(r.distance-r.dir*115.2)<1e-8);assert(r.maxError<1e-8);assert.equal(r.after.mode,'run');assert.equal(r.after.facing,r.dir);assert.equal(r.after.y,0);}
  assert.equal(results.locked.mode,'dash');assert.equal(results.locked.facing,1);assert.equal(results.reversed.mode,'run');assert.equal(results.reversed.facing,-1);
  assert.equal(results.released.mode,'stop');assert.equal(results.idle.mode,'idle');
  for(const key of ['blockedActive','blockedCooldown','blockedBeforeReady','secondAir'])assert.equal(results[key],false,key);
  for(const key of ['readyAgain','acceptedAir','acceptedLanding','resetReady','nextJumpAir'])assert.equal(results[key],true,key);
  for(const r of results.airCases){assert(r.accepted);assert.equal(r.maxHeightError,0);assert.equal(r.during.mode,'dash');assert.equal(r.resumed.mode,'jump');assert.equal(r.resumed.time,r.before.time);assert.equal(r.resumed.jumpVariant,r.before.jumpVariant);assert.equal(r.resumed.y,r.before.y);assert(Math.abs(r.resumed.x-r.before.x-(r.dir||1)*115.2)<1e-8);assert.equal(r.landed.y,0);assert.equal(r.landed.mode,r.dir?'run':'idle');assert.equal(r.landed.airDashUsed,false);}
  assert.equal(results.noAirJump.mode,'dash');assert.equal(results.noAirJump.jumpCount,1);assert.equal(results.releaseAir.mode,'jump');assert(results.releaseAir.y<0);assert.equal(results.noExtraJump.jumpCount,1);assert.equal(results.noExtraJump.mode,'idle');
  assert.equal(results.bufferedLanding.jumpCount,2);assert.equal(results.bufferedLanding.mode,'jump');assert.equal(results.bufferedLanding.time,0);assert.equal(results.airDemo.dashCount,2);assert.equal(results.airDemo.mode,'idle');
  assert.equal(results.cancel.mode,'jump');assert.equal(results.cancel.jumpVariant,'jump');assert.equal(results.cancelAfter.mode,'run');
  assert.equal(results.demo.dashCount,2);assert.equal(results.demo.mode,'idle');assert.equal(results.jumpDemo.dashCount,1);assert.equal(results.jumpDemo.jumpCount,1);assert.equal(results.jumpDemo.mode,'idle');
  await page.click('#reset');await page.keyboard.down('KeyD');await page.evaluate(()=>previewDebug.advance(400));await page.keyboard.down('ShiftLeft');
  assert.equal(await page.evaluate(()=>previewState.mode),'dash');await page.evaluate(()=>previewDebug.advance(500));await page.keyboard.down('ShiftLeft');assert.equal(await page.evaluate(()=>previewState.dashCount),1);
  await page.keyboard.up('ShiftLeft');await page.keyboard.press('ShiftRight');assert.equal(await page.evaluate(()=>previewState.dashCount),2);await page.keyboard.up('KeyD');
  await page.click('#reset');await page.click('#dash');assert.equal(await page.evaluate(()=>previewState.mode),'dash');
  await page.click('#reset');await page.keyboard.press('Space');await page.evaluate(()=>previewDebug.advance(230));await page.keyboard.press('ShiftLeft');assert.equal(await page.evaluate(()=>previewState.mode),'dash');assert(await page.evaluate(()=>previewState.dashReturn!==null));
  await page.evaluate(()=>previewDebug.advance(60));await page.screenshot({path:'Art/animations/player-c-dash-v2/preview-check.png'});
  await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
  const reg=JSON.parse(fs.readFileSync('Art/animations/player-c-dash-v2/registration.json'));assert(reg.frames.every(f=>f.headCenter[1]===308&&f.bounds[3]<430));
  const report={passed:true,artReview:'All four drawings visually inspected: both legs tucked, no foot strike or running stride. Constant head registration verified.',checks:['4 drawings across 180ms; 115.2px travel at640px/s in both directions','start direction locked until dash ends','ground dash resumes run or stop then idle','air dash tested at rise/apex/descent in stationary/left/right jumps; height held; exact jump time and height resumed','one air dash per jump even after cooldown; landing restores charge','Space during air dash cannot create midair jump; late input buffers through landing','release movement during air dash resumes falling instead of snapping to ground','jump cancels ground dash; landing accepts dash','active/cooldown reentry rejected; ready at400ms from start','reset clears cooldown','left/right Shift and on-screen button; no auto-repeat dash; real keyboard air dash','all dash demos finish correctly','mobile width and no browser errors'],results};
  fs.writeFileSync('Art/animations/player-c-dash-v2/browser-validation.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,checks:report.checks}));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
