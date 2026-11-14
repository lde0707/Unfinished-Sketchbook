const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1280,height:800}}),errors=[],reports=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.text().includes('UNFIN_AUDIO_RUNTIME'))reports.push(m.text());if(/Exception|audio.*failed/i.test(m.text()))errors.push(m.text());});
 await page.addInitScript(()=>{
  window.ambientQA={contexts:[],samples:[],starts:0};const connect=AudioNode.prototype.connect;
  AudioNode.prototype.connect=function(destination,...args){if(destination===this.context.destination){if(!this.context.qaAnalyser){const a=this.context.createAnalyser();a.fftSize=4096;this.context.qaAnalyser=a;connect.call(a,destination);ambientQA.contexts.push(this.context);}return connect.call(this,this.context.qaAnalyser,...args);}return connect.call(this,destination,...args);};
  const start=AudioBufferSourceNode.prototype.start;AudioBufferSourceNode.prototype.start=function(...args){ambientQA.starts++;return start.apply(this,args);};
 });
 async function sample(ms){const end=Date.now()+ms;do{await page.evaluate(()=>{for(const c of ambientQA.contexts){const a=new Float32Array(c.qaAnalyser.fftSize);c.qaAnalyser.getFloatTimeDomainData(a);let p=0,peak=0;for(const v of a){p+=v*v;peak=Math.max(peak,Math.abs(v));}ambientQA.samples.push({rms:Math.sqrt(p/a.length),peak,state:c.state});}});await page.waitForTimeout(90);}while(Date.now()<end);}
 async function send(method,value){await page.evaluate(({method,value})=>unityInstance.SendMessage('Game Audio',method,value),{method,value});}
 async function move(p){await page.evaluate(p=>unityInstance.SendMessage('World Prototype','PreviewPosition',p),p);}
 async function energy(){return page.evaluate(()=>Math.max(0,...ambientQA.samples.slice(-12).map(x=>x.rms)));}
 await page.route(url=>url.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=960;canvas.height=540;return;')});});
 await page.goto('http://127.0.0.1:8080/?v=quiet-cavern-20261026&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('#unity-canvas').click();
 await send('SetEffects',0);await send('SetMaster',.7);await send('SetMusic',.32);
 const start1=await page.evaluate(()=>ambientQA.samples.length);await sample(22000);await send('ReportAudio','');
 async function stats(start){return page.evaluate(start=>{const s=ambientQA.samples.slice(start);return {peak:Math.max(0,...s.map(x=>x.peak)),rms:Math.max(0,...s.map(x=>x.rms)),silentFraction:s.filter(x=>x.rms<.000001).length/s.length};},start);}
 const region1=await stats(start1);assert(region1.rms>.000001,'occasional graphite cue reaches actual WebAudio output');assert(region1.silentFraction>.5,'mostly silence in first region');assert(region1.peak<.015,'quiet graphite cue ceiling');
 assert(reports.some(x=>/AmbienceGraphite_0/.test(x)&&/loop=True/.test(x)),'graphite sparse loop loaded');
 await move('490,36');const start2=await page.evaluate(()=>ambientQA.samples.length);await sample(31000);await send('ReportAudio','');const region2=await stats(start2);
 assert(region2.rms>.000001,'occasional crayon cave cue audible');assert(region2.silentFraction>.5&&region2.peak<.015,'second region mostly silent and quiet');assert(reports.some(x=>/region=1.*AmbienceCrayon_0.*level1=1.000/.test(x)),'second region fully crossfaded');
 await send('SetMusic',0);await sample(1500);const muted=await energy();assert(muted<.00001,'environment slider mutes both layers');
 await send('SetEffects',.65);await page.keyboard.press('j');await sample(1000);const effects=await energy();assert(effects>.00001,'combat sounds remain independent of environment volume');
 await send('SetMusic',.32);await send('SetMaster',0);await sample(1600);assert(await energy()<.00001,'master mute');
 await send('SetMaster',.7);await send('ReportAudio','');
 const signal=await page.evaluate(()=>({peak:Math.max(0,...ambientQA.samples.map(x=>x.peak)),states:ambientQA.contexts.map(x=>x.state)}));assert(signal.peak<.98,'WebAudio mix does not clip');assert.deepEqual(errors,[]);
 fs.writeFileSync('Book/Validation/ambience-web.json',JSON.stringify({passed:true,realWebAudio:true,region1,region2,environmentMuted:muted,effects,sparseCaveCues:true,reports,signal,errors},null,2));console.log('AMBIENCE_WEB_OK');
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
