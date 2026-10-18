const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1050}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`)});
 await page.goto('http://127.0.0.1:8092/player-state-test/preview.html?demo=longFall&v=forward-fall2');await page.waitForFunction(()=>window.previewState?.ready);
 const result=await page.evaluate(()=>{
  const d=previewDebug,s=previewState,snap=()=>JSON.parse(JSON.stringify(s)),reset=()=>{d.reset();s.playing=false;};const cases=[];
  for(const dir of [-1,0,1])for(const height of [160,320]){
   reset();d.direction(dir);d.jumpFromLedge(height);const boundary=dir?310:345;d.advance(boundary-1);const before=snap();d.advance(1);const handoff=snap();let previousY=s.y,monotone=true,midairLanding=false,frames=new Set(),ticks=0;
   while(s.mode==='fall'&&s.fallLandingMs===null&&ticks++<2000){frames.add(s.frame);midairLanding||=s.frame>=3;d.advance(1);monotone&&=s.y>=previousY;previousY=s.y;}
   const contact=snap();d.advance(130);cases.push({dir,height,boundary,before,handoff,monotone,midairLanding,frames:[...frames],contact,end:snap()});
  }
  reset();d.direction(1);d.jumpFromLedge(220);d.advance(260);const attackStarted=d.attack();const attackBefore=snap();d.advance(50);const attackAfter=snap();d.advance(170);const attackEnd=snap();
  reset();d.jumpFromLedge(320);d.advance(330);s.airDashUsed=true;d.advance(15);const charge=snap();const extraDashRejected=!d.dash();
  reset();d.jumpFromLedge(320);d.advance(345);d.jump();d.advance(150);const noMidairJump=snap();while(s.mode==='fall'&&s.y<-35)d.advance(1);d.jump();d.advance(60);const buffered=snap();
  reset();d.direction(1);d.jumpFromLedge(220);d.advance(380);const beforeDash=snap();d.dash();d.advance(180);const afterDash=snap();
  const demos=[];for(const name of ['longFall','longFallAttack']){d.demo(name);s.playing=false;d.advance(3000);demos.push({name,end:snap()})}
  return {cases,attackStarted,attackBefore,attackAfter,attackEnd,charge,extraDashRejected,noMidairJump,buffered,beforeDash,afterDash,demos};
 });
 for(const r of result.cases){
  assert.equal(r.before.mode,'jump');assert.equal(r.handoff.mode,'fall');assert.equal(r.handoff.frame,1);assert.equal(r.handoff.fallLandingMs,null);
  const seconds=.46,u=.5+.08/seconds,expectedY=-r.height-4*130*u*(1-u),expectedV=4*130*(2*u-1)/seconds;
  assert(Math.abs(r.handoff.y-expectedY)<1e-7);assert(Math.abs(r.handoff.fallVelocity-expectedV)<1e-7);assert(Math.abs(r.handoff.x-r.dir*.2*r.boundary)<1e-7);
  assert(r.monotone);assert(!r.midairLanding);assert.deepEqual(r.frames,[1,2]);assert.equal(r.contact.y,0);assert.equal(r.contact.frame,3);assert.equal(r.contact.jumpCount,1);assert.equal(r.end.mode,r.dir?'run':'idle');
 }
 assert(result.attackStarted);assert.equal(result.attackAfter.mode,'fall');assert.equal(result.attackAfter.airAttack.startedAt,result.attackBefore.airAttack.startedAt);assert.equal(result.attackEnd.airAttack,null);
 assert(result.charge.airDashUsed);assert(result.extraDashRejected);assert.equal(result.noMidairJump.jumpCount,1);assert.equal(result.noMidairJump.mode,'fall');assert.equal(result.buffered.jumpCount,2);assert.equal(result.buffered.jumpOriginY,0);
 assert.equal(result.afterDash.mode,'fall');assert.equal(result.afterDash.y,result.beforeDash.y);assert.equal(result.afterDash.fallVelocity,result.beforeDash.fallVelocity);
 for(const r of result.demos){assert.equal(r.end.mode,'idle');assert.equal(r.end.demo,null);assert(r.end.history.includes('jump → fall'));}assert.equal(result.demos[1].end.airAttackHistory[0].kind,'fall');
 const base='Art/animations/player-c-air-attack-v1';for(const [name,digest] of Object.entries(JSON.parse(fs.readFileSync(`${base}/preserved-v1-hashes.json`))))assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(base,name))).digest('hex'),digest,name);
 for(const attack of [false,true]){await page.evaluate(attack=>{previewDebug.reset();previewState.playing=false;previewDebug.direction(1);previewDebug.jumpFromLedge(220);previewDebug.advance(440);if(attack){previewDebug.attack();previewDebug.advance(50)}},attack);await page.screenshot({path:`${base}/${attack?'forward-fall-attack':'jump-to-fall'}-preview.png`});}
 await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
 const report={passed:true,checks:['six high-jump cases preserve height, velocity and direction at jump-to-fall handoff','descending poses transition to fall loop without midair landing','landing only at actual lower ground','attack survives handoff and ends on schedule','no renewed dash charge or midair jump; buffered re-jump still works','fall dash preserves vertical state','both long-fall demos complete','rise4 frames and fall gather/recovery hashes preserved','no browser errors; mobile layout'],result};fs.writeFileSync(`${base}/jump-to-fall-validation.json`,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,checks:report.checks}));
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
