const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1200,height:1000}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`)});
 await page.goto('http://127.0.0.1:8092/player-state-test/preview.html?demo=riseAttack&v=air-attack1');await page.waitForFunction(()=>previewState.ready);
 const result=await page.evaluate(()=>{
  const d=previewDebug,s=previewState;const reset=()=>{d.reset();s.playing=false;};const snap=()=>JSON.parse(JSON.stringify(s));
  const paths=[];
  for(const type of ['rise','jump-fall','drop'])for(const dir of [-1,0,1]){
   const setup=()=>{reset();d.direction(dir);if(type==='drop'){d.drop(240);d.advance(100)}else{d.jump();d.advance(type==='rise'?80:330)}};
   const run=attack=>{setup();const started=attack?d.attack():false,kind=s.airAttack?.kind;const rows=[],frames=new Set();
    for(let t=0;t<650;t+=5){if(s.airAttack)frames.add(s.airAttack.frame);d.advance(5);rows.push([s.x,s.y,s.mode,s.time,s.fallVelocity])}
    return {started,kind,rows,frames:[...frames],end:snap()}};
   paths.push({type,dir,baseline:run(false),attacking:run(true)});
  }
  reset();const groundRejected=!d.attack();d.jump();d.advance(80);const start=d.attack(),busyRejected=!d.attack();d.advance(220);const expired=!s.airAttack;const repeat=d.attack(),repeatKind=s.airAttack?.kind;
  reset();d.jump();d.advance(480);d.attack();d.advance(15);const landing=snap();
  reset();d.drop(160);d.advance(400);d.attack();d.jump();d.advance(22);const bufferedLanding=snap();
  reset();d.jump();d.advance(80);d.attack();d.dash();const dash=snap();const dashRejected=!d.attack();d.advance(180);const afterDash=snap();const afterDashAttack=d.attack(),afterDashKind=s.airAttack?.kind;
  reset();d.direction(1);d.jump();d.advance(80);d.attack();d.direction(-1);const reverse=snap();d.advance(220);const afterReverse=snap();
  const demos=[];for(const name of ['riseAttack','fallAttack','jumpFallAttack']){d.demo(name);s.playing=false;d.advance(3000);demos.push({name,history:s.airAttackHistory,mode:s.mode})}
  return {paths,groundRejected,start,busyRejected,expired,repeat,repeatKind,landing,bufferedLanding,dash,dashRejected,afterDash,afterDashAttack,afterDashKind,reverse,afterReverse,demos};
 });
 for(const p of result.paths){assert(p.attacking.started);assert.equal(p.attacking.kind,p.type==='rise'?'rise':'fall');assert.deepEqual(p.attacking.rows,p.baseline.rows);assert.equal(p.attacking.end.airAttack,null);if(p.type!=='jump-fall')assert.deepEqual(p.attacking.frames,[0,1,2,3]);}
 assert(result.groundRejected&&result.start&&result.busyRejected&&result.expired&&result.repeat);assert.equal(result.repeatKind,'fall');
 assert.equal(result.landing.airAttack,null);assert.equal(result.landing.y,0);assert.equal(result.bufferedLanding.airAttack,null);assert.equal(result.bufferedLanding.jumpCount,1);
 assert.equal(result.dash.airAttack,null);assert(result.dashRejected);assert.equal(result.afterDash.mode,'fall');assert(result.afterDashAttack);assert.equal(result.afterDashKind,'fall');
 assert.equal(result.reverse.airAttack.facing,1);assert.equal(result.reverse.dir,-1);assert.equal(result.afterReverse.airAttack,null);
 assert.deepEqual(result.demos.map(d=>d.history.map(a=>a.kind)),[['rise','rise'],['fall'],['rise','fall']]);
 for(const id of ['stage','attack']){
  await page.evaluate(()=>{previewDebug.reset();previewState.playing=false;previewDebug.jump();previewDebug.advance(80)});
  const box=await page.locator('#'+id).boundingBox();await page.mouse.move(box.x+box.width/2,box.y+box.height/2);await page.mouse.down();assert.equal(await page.evaluate(()=>previewState.airAttackCount),1);await page.mouse.up();assert.equal(await page.evaluate(()=>previewState.airAttackCount),1);
 }
 await page.evaluate(()=>{previewDebug.reset();previewState.playing=false;previewDebug.drop(240);previewDebug.advance(100)});await page.keyboard.down('KeyJ');await page.keyboard.down('KeyJ');assert.equal(await page.evaluate(()=>previewState.airAttackCount),1);await page.keyboard.up('KeyJ');assert.equal(await page.evaluate(()=>previewState.airAttack.kind),'fall');
 for(const kind of ['rise','fall']){
  await page.evaluate(kind=>{previewDebug.reset();previewState.playing=false;if(kind==='rise'){previewDebug.jump();previewDebug.advance(80)}else{previewDebug.drop(160);previewDebug.advance(150)}previewDebug.attack();previewDebug.advance(50)},kind);
  await page.screenshot({path:`Art/animations/player-c-air-attack-v1/${kind}-preview.png`});
 }
 await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
 const report={passed:true,checks:['nine trajectory comparisons: identical position, height, clock and gravity with/without attack','ascent, jump descent and ledge fall choose correct drawings','four drawn frames; repeat blocked until clip ends','landing, buffered re-jump and dash cancel attack','post-dash falling attack','air direction changes preserve strike facing','real pointer and J input; no held-key repeat','three scenario demos','no load errors; mobile layout'],result};
 fs.writeFileSync('Art/animations/player-c-air-attack-v1/browser-validation.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({passed:true,checks:report.checks}));
 }finally{await browser.close()}})().catch(e=>{console.error(e);process.exit(1)});
