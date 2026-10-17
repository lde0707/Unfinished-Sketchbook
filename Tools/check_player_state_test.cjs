const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const root=path.resolve(__dirname,'../Art/animations');
 const selection=JSON.parse(fs.readFileSync(path.join(root,'player-animation-selection.json')));
 let hashCount=0;for(const kind of ['run','idle','jump'])for(const [file,hash] of Object.entries(selection[kind].files||{})){
   assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex'),hash,file);hashCount++;
 }
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1200,height:1000}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
 await page.goto('http://127.0.0.1:8092/player-state-test/preview.html');await page.waitForFunction(()=>previewState.ready);
 const results=await page.evaluate(()=>{
   const d=previewDebug,s=previewState;s.playing=false;
   const capture=()=>JSON.parse(JSON.stringify(s));
   d.reset();d.jump();d.advance(265);const apex=capture();d.advance(410);const stationary=capture();
   d.reset();d.direction(1);d.advance(160);const start=capture();d.advance(640);const run=capture();d.direction(0);d.advance(160);const stop=capture();
   d.reset();d.direction(1);d.advance(800);d.jump();d.advance(520);const moving=capture();
   d.jump();d.advance(300);d.direction(0);d.advance(220);const released=capture();
   d.reset();d.direction(-1);d.advance(800);d.jump();d.advance(520);const left=capture();
   d.reset();d.jump();d.advance(100);d.direction(-1);d.advance(575);const idleJumpRun=capture();
   const motion=[];
   for(const dir of [1,-1]){d.reset();d.direction(dir);d.advance(800);d.jump();const startX=s.x;let maxError=0;const seen=new Set();
     for(let t=1;t<=600;t++){d.advance(1);maxError=Math.max(maxError,Math.abs(s.x-(startX+dir*0.2*t)));if(s.mode==='jump')seen.add(s.frame);if(t===1)motion.push({dir,immediateY:s.y});}
     motion.push({dir,maxError,frames:[...seen]});}
   d.reset();d.jump();d.advance(36);const fastStationary=capture();
   for(let i=0;i<40;i++){d.direction(i%3-1);d.jump();d.advance(37);}d.direction(0);d.advance(2000);const stress=capture();
   d.demo('all');s.playing=false;d.advance(9000);const all=capture();
   return {apex,stationary,start,run,stop,moving,released,left,idleJumpRun,motion,fastStationary,stress,all};
 });
 assert.equal(results.apex.y,-130);assert.equal(results.apex.x,0);assert.equal(results.apex.frame,4);assert.equal(results.apex.jumpVariant,'tuck');
 assert.equal(results.stationary.mode,'idle');assert.equal(results.stationary.x,0);
 assert.equal(results.start.mode,'run');assert(Math.abs(results.run.x-128)<1e-8);
 assert.equal(results.stop.mode,'idle');assert.equal(results.stop.x,results.run.x);
 assert.equal(results.moving.mode,'run');assert.equal(results.moving.jumpVariant,'jump');assert(results.moving.x>results.run.x);
 assert.equal(results.released.mode,'idle');assert.equal(results.released.y,0);
 assert.equal(results.left.mode,'run');assert.equal(results.left.facing,-1);assert(results.left.x<0);
 assert.equal(results.idleJumpRun.mode,'run');assert.equal(results.idleJumpRun.facing,-1);
 for(const m of results.motion){if('immediateY' in m)assert(m.immediateY<0);else{assert(m.maxError<1e-8);assert.deepEqual(m.frames,[0,1,2,3,4,5]);}}
 assert(results.fastStationary.y<0);assert.equal(results.fastStationary.x,0);
 assert.equal(results.stress.mode,'idle');assert.equal(results.all.mode,'idle');assert.equal(results.all.demo,null);
 for(const edge of ['idle → start','start → run','run → stop','stop → idle','idle → jump','run → jump','jump → run','jump → idle'])assert(results.all.history.includes(edge),edge);
 await page.click('#reset');await page.keyboard.down('KeyD');await page.evaluate(()=>previewDebug.advance(400));
 assert.equal(await page.evaluate(()=>previewState.mode),'run');
 await page.keyboard.press('Space');assert.equal(await page.evaluate(()=>previewState.mode),'jump');
 await page.keyboard.up('KeyD');await page.evaluate(()=>previewDebug.advance(740));assert.equal(await page.evaluate(()=>previewState.mode),'idle');
 await page.keyboard.down('ArrowLeft');await page.evaluate(()=>previewDebug.advance(400));assert.equal(await page.evaluate(()=>previewState.facing),-1);await page.keyboard.up('ArrowLeft');
 await page.evaluate(()=>{previewDebug.reset();previewDebug.direction(1);previewDebug.advance(800);previewDebug.jump();previewDebug.advance(230);});
 await page.screenshot({path:path.join(root,'player-state-test/preview-check.png')});
 await page.setViewportSize({width:390,height:844});
 assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
 assert.deepEqual(errors,[]);
 const report={passed:true,approvedFileHashesVerified:hashCount,checks:['all six directed Idle/Run/Jump connections via start/stop bridges','stationary jump leaves ground after 35ms; height130px total675ms','moving jump leaves ground immediately; all6 poses total520ms','1ms samples show uninterrupted 200px/s through moving takeoff, flight, landing and return to run in both directions','release during flight lands in idle','rapid input settles without stuck state','keyboard after button focus','mobile width without overflow','no browser/network errors'],results};
 fs.writeFileSync(path.join(root,'player-state-test/browser-validation.json'),JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({passed:true,approvedFileHashesVerified:hashCount,checks:report.checks}));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
