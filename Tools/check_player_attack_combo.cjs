const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
async function main(only){const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
for(const moving of only==='standing'?[false]:only==='moving'?[true]:[false,true]){
 const dir=moving?'player-c-move-attack-v1':'player-c-attack-v1',base=`Art/animations/${dir}`,page=await browser.newPage({viewport:{width:1280,height:1100}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`)});
 await page.goto(`http://127.0.0.1:8092/${dir}/preview.html?v=thrust`);await page.waitForFunction(()=>previewState.ready);
 const durations=moving?[480,400,420]:[360,290,460];
 const result=await page.evaluate(({moving,durations})=>{const d=previewDebug,s=previewState,snap=()=>JSON.parse(JSON.stringify(s));
 function reset(dir=1){d.reset(false);s.playing=false;if(moving){d.direction(dir);d.advance(400)}}
 reset();d.start(1);d.advance(durations[1]-100);d.attack();d.advance(99);const before=snap();d.advance(1);const third=snap();d.advance(durations[2]);const ended=snap();const locked=!d.attack();d.advance(500);const readyAgain=d.attack();
 reset();d.start(2);d.advance((moving?180:150)-1);const chamber=s.frame;d.advance(1);const extension=s.frame;d.advance(12);const contact=s.frame;d.advance(48);const retract=s.frame;
 const singles=[];for(let i=0;i<3;i++){reset();d.start(i);const seen=new Set();for(let t=0;t<durations[i];t++){seen.add(s.frame);d.advance(1)}singles.push({frames:[...seen],mode:s.mode})}
 reset();d.attack();d.advance(1);d.attack();d.advance(durations[0]-1);const expired=snap();
 reset();d.start(2);const thirdQueueRejected=!d.attack();d.advance(durations[2]);const noWrap=snap();
 reset();d.attack();d.advance(durations[0]+251);d.attack();const expiredCombo=snap();
 reset();d.attack();d.advance(durations[0]+250);d.attack();const atComboBoundary=snap();
 const movement=[];if(moving){for(const facing of [-1,1]){reset(facing);const from=s.x;for(let stage=0;stage<3;stage++){if(stage===0)d.attack();d.advance(durations[stage]-100);if(stage<2)d.attack();d.advance(100)}movement.push({facing,distance:s.x-from,speed:s.vx,mode:s.mode})}
 reset();d.attack();d.direction(0);d.advance(130);const stopped=snap();d.direction(1);d.advance(10);movement.push({stopped,resumed:snap()});reset();d.attack();d.direction(-1);movement.push({cancelled:snap()});}
 reset();d.start(1);d.advance((moving?160:80)+(moving?100:70));d.attack();const holdStart=snap();d.advance(139);const holdEnd=snap();d.advance(1);const afterHold=snap();
 const refreshSamples=[];
 for(const hz of [60,120])for(const offset of [0,4,8,12,16]){reset();d.start(1);d.advance(offset);const seen=new Set();for(let t=offset;t<durations[1];t+=1000/hz){if(s.mode==='attack')seen.add(s.frame);d.advance(1000/hz)}refreshSamples.push({hz,offset,frames:[...seen]})}
 const frames=[];for(let i=0;i<18;i++){d.select(i);frames.push(s.frameGlobal)}
 return {atComboBoundary,before,third,ended,locked,readyAgain,singles,expired,thirdQueueRejected,noWrap,expiredCombo,movement,frames,refreshSamples,holdStart,holdEnd,afterHold,thrust:{chamber,extension,contact,retract}};},{moving,durations});
 for(const sample of result.refreshSamples){assert(sample.frames.includes(3),JSON.stringify(sample));assert(!sample.frames.includes(2)&&!sample.frames.includes(4),JSON.stringify(sample))}
 assert.equal(result.holdStart.clip,1);assert.equal(result.holdStart.frame,5);assert.equal(result.holdEnd.clip,1);assert.equal(result.holdEnd.frame,5);assert.equal(result.afterHold.clip,2);assert.equal(result.afterHold.time,0);
 assert.deepEqual(result.thrust,{chamber:1,extension:2,contact:3,retract:4});
 assert.equal(result.before.clip,1);assert.equal(result.before.frame,5);assert.equal(result.third.clip,2);assert.equal(result.third.time,0);assert.equal(result.third.frame,0);
 assert.equal(result.ended.mode,moving?'run':'idle');assert(result.locked&&result.readyAgain&&result.thirdQueueRejected);assert.notEqual(result.noWrap.mode,'attack');
 for(const [stage,x] of result.singles.entries()){assert.deepEqual(x.frames,stage===1?[0,1,3,5]:[0,1,2,3,4,5]);assert.equal(x.mode,moving?'run':'idle')}
 assert.notEqual(result.expired.mode,'attack');assert.equal(result.expiredCombo.clip,0);assert.equal(result.atComboBoundary.clip,1);assert.equal(result.frames.length,18);
 if(moving){for(const r of result.movement.slice(0,2)){assert(Math.abs(r.distance-r.facing*320*durations.reduce((a,b)=>a+b,0)/1000)<1e-7);assert.equal(r.speed,r.facing*320);assert.equal(r.mode,'run')}assert.equal(result.movement[2].stopped.variant,'standing');assert.equal(result.movement[2].resumed.variant,'move');assert.equal(result.movement[3].cancelled.mode,'run')}
 for(const selector of ['#stage','#attack']){
  await page.evaluate(selector=>{previewDebug.reset(false);previewState.playing=false;document.querySelector(selector).addEventListener('pointerdown',()=>{previewState.playing=false},{once:true})},selector);
  const b=await page.locator(selector).boundingBox();await page.mouse.move(b.x+b.width/2,b.y+b.height/2);await page.mouse.down();
  assert.deepEqual(await page.evaluate(()=>({frame:previewState.frame,time:previewState.time,count:previewState.attacks.length})),{frame:0,time:0,count:1});await page.mouse.up();assert.equal(await page.evaluate(()=>previewState.attacks.length),1);
 }
 const reg=JSON.parse(fs.readFileSync(`${base}/registration.json`));assert.deepEqual(reg.clips.map(c=>c.durationMs),durations);assert(reg.clips.every(c=>c.activeMs[0]>0));assert.equal(reg.finisherRecoveryMs,500);
 assert.deepEqual(reg.clips[1].frames.map(f=>f.durationMs),moving?[80,80,0,100,0,140]:[40,40,0,70,0,140]);
 assert.equal(reg.clips[1].frames[3].durationMs,reg.clips[0].frames[3].durationMs);
 assert.equal(reg.clips[1].activeMs[0],reg.clips[0].activeMs[0]);
 const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
 for(const [file,digest] of Object.entries(JSON.parse(fs.readFileSync(`${base}/pre-thrust-preserved-hashes.json`))))assert.equal(hash(`Art/animations/${file}`),digest,file);
 assert.equal(hash(`${base}/attack2/frames/05.png`),hash(`${base}/attack3/frames/00.png`));
 await page.evaluate(()=>previewDebug.select(15));await page.screenshot({path:`${base}/preview-check.png`});await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));assert.deepEqual(errors,[]);
 const report={passed:true,timingRevision:'combo-window250-v9',checks:['attack2 matches attack1 preparation and main contact timing; one70/100ms swing drawing then140ms hold; queued input survives','60/120Hz samples show only one swing drawing at five phase offsets','original thrust onset and recovery preserved; extension12ms, contact48ms, retract60ms','attack2 plays four poses; other attacks retain six','second swing completes before third thrust starts','identical full-body drawing at 2-to-3 boundary','150ms queue expiry; 250ms combo boundary;251ms restarts attack1','third finisher blocks restart for500ms','attack1/2 image hashes unchanged','pointerdown starts preparation once','all18 frames inspectable',...(moving?['constant movement in both directions','standing/moving phase continuity and reverse cancellation']:[]),'no browser errors; mobile layout'],result};
 fs.writeFileSync(`${base}/browser-validation.json`,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({directory:dir,passed:true,checks:report.checks}));await page.close();
}
}finally{await browser.close()}}
module.exports=main;if(require.main===module)main().catch(e=>{console.error(e);process.exit(1)});
