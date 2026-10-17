const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1100}}),errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
 await page.goto('http://127.0.0.1:8092/player-state-test/preview.html?demo=fall');await page.waitForFunction(()=>previewState.ready);
 const results=await page.evaluate(()=>{
  const d=previewDebug,s=previewState;s.playing=false;const snap=()=>JSON.parse(JSON.stringify(s));const contact=Math.sqrt(320/1800)*1000,falls=[];
  for(const dir of [0,1,-1]){d.reset();d.direction(dir);d.drop(160);let lastY=s.y,monotone=true,minV=Infinity,maxV=0;const frames=new Set();
   for(let t=0;t<Math.floor(contact);t++){frames.add(s.frame);d.advance(1);monotone&&=s.y>=lastY;lastY=s.y;minV=Math.min(minV,s.fallVelocity);maxV=Math.max(maxV,s.fallVelocity);}
   d.advance(contact-Math.floor(contact));const landed=snap();d.advance(65);const recover=snap();d.advance(65);const done=snap();falls.push({dir,frames:[...frames],monotone,minV,maxV,landed,recover,done});}
  d.reset();d.drop(1000);let maxSpeed=0;for(let t=0;t<1500;t++){d.advance(1);maxSpeed=Math.max(maxSpeed,s.fallVelocity);}const high=snap();
  d.reset();d.drop(160);d.advance(100);d.jump();d.advance(1000);const early=snap();
  const buffered=[];for(const offset of [120,121]){d.reset();d.drop(160);d.advance(contact-offset);d.jump();d.advance(offset);buffered.push({offset,state:snap()});}
  d.reset();d.drop(160);d.advance(contact+10);d.jump();const immediate=snap();
  d.reset();d.direction(-1);d.drop(160);d.advance(180);const beforeDash=snap();d.dash();d.advance(100);const midDash=snap();d.advance(80);const resumed=snap();d.advance(1000);const afterDash=snap();
  const demos=[];for(const name of ['fall','movingFall','fallDash']){d.demo(name);s.playing=false;d.advance(1800);demos.push({name,state:snap()});}
  return {contact,falls,high,maxSpeed,early,buffered,immediate,beforeDash,midDash,resumed,afterDash,demos};
 });
 for(const r of results.falls){assert(r.monotone);assert(r.minV>0);assert.deepEqual(r.frames,[0,1,2]);assert.equal(r.landed.y,0);assert.equal(r.landed.frame,3);assert.equal(r.landed.jumpCount,0);assert.equal(r.recover.frame,4);assert.equal(r.done.mode,r.dir?'run':'idle');assert(Math.abs(r.landed.x-r.dir*.2*results.contact)<1e-7);}
 assert.equal(results.maxSpeed,900);assert.equal(results.high.mode,'idle');assert.equal(results.high.y,0);
 assert.equal(results.early.jumpCount,0);assert.equal(results.early.mode,'idle');
 assert.equal(results.buffered[0].state.jumpCount,1);assert.equal(results.buffered[0].state.mode,'jump');assert.equal(results.buffered[1].state.jumpCount,0);
 assert.equal(results.immediate.mode,'jump');assert.equal(results.immediate.jumpCount,1);
 assert.equal(results.midDash.mode,'dash');assert.equal(results.midDash.y,results.beforeDash.y);assert.equal(results.resumed.mode,'fall');assert.equal(results.resumed.y,results.beforeDash.y);assert.equal(results.resumed.fallVelocity,results.beforeDash.fallVelocity);assert.equal(results.resumed.time,results.beforeDash.time);assert.equal(results.afterDash.mode,'run');assert.equal(results.afterDash.y,0);
 for(const r of results.demos){assert.equal(r.state.mode,'idle');assert.equal(r.state.jumpCount,0);assert.equal(r.state.demo,null);}
 await page.click('[data-demo="fall"]');await page.evaluate(()=>{previewState.playing=false;previewDebug.reset();previewDebug.drop(160);previewDebug.advance(220);});
 await page.screenshot({path:'Art/animations/player-c-fall-v1/preview-check.png'});await page.keyboard.press('ShiftLeft');assert.equal(await page.evaluate(()=>previewState.mode),'dash');assert.equal(await page.evaluate(()=>previewState.dashReturn.kind),'fall');
 await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
 const report={passed:true,checks:['fall begins without jump or upward velocity','stationary and left/right fall, 3 airborne poses and 2 landing poses','gravity contact time and continuous horizontal movement','long fall respects terminal900px/s and lands without overshoot','no airborne jump; 120ms buffered input and landing interrupt work','fall air dash preserves height, velocity and fall time','all fall demos complete without incrementing jump count','keyboard air dash, mobile width, no browser errors'],results};fs.writeFileSync('Art/animations/player-c-fall-v1/browser-validation.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,checks:report.checks}));
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1);});
