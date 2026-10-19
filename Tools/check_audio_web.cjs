const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:800}}),errors=[],reports=[];
 const sample=async(ms)=>{const end=Date.now()+ms;do{await page.evaluate(()=>{for(const c of audioQA.contexts){const a=new Float32Array(c.qaAnalyser.fftSize);c.qaAnalyser.getFloatTimeDomainData(a);let p=0;for(const v of a)p+=v*v;audioQA.samples.push({t:performance.now(),rms:Math.sqrt(p/a.length),state:c.state});}});await new Promise(r=>setTimeout(r,80));}while(Date.now()<end);};
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/UNFIN_AUDIO_RUNTIME/.test(m.text()))reports.push(m.text());if(/Exception|audio.*failed/i.test(m.text()))errors.push(m.text());});
 await page.addInitScript(()=>{
  window.audioQA={starts:0,contexts:[],samples:[]};
  const connect=AudioNode.prototype.connect;
  AudioNode.prototype.connect=function(destination,...args){
   if(destination===this.context.destination){
    if(!this.context.qaAnalyser){const a=this.context.createAnalyser();a.fftSize=2048;this.context.qaAnalyser=a;connect.call(a,destination);window.audioQA.contexts.push(this.context);}
    return connect.call(this,this.context.qaAnalyser,...args);
   }return connect.call(this,destination,...args);
  };
  const start=AudioBufferSourceNode.prototype.start;AudioBufferSourceNode.prototype.start=function(...args){window.audioQA.starts++;return start.apply(this,args);};
  setInterval(()=>{for(const c of window.audioQA.contexts){const a=new Float32Array(c.qaAnalyser.fftSize);c.qaAnalyser.getFloatTimeDomainData(a);let p=0;for(const v of a)p+=v*v;window.audioQA.samples.push({t:performance.now(),rms:Math.sqrt(p/a.length),state:c.state});}},100);
 });
 await page.route(url=>url.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="640" height="360"')});});
 await page.goto('http://127.0.0.1:8080/?v=audio-20261019&spawn=252,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});
 await page.locator('#unity-canvas').click();await sample(4000);
 const music=await page.evaluate(()=>({starts:audioQA.starts,energy:Math.max(0,...audioQA.samples.map(x=>x.rms).filter(Number.isFinite)),states:audioQA.contexts.map(x=>x.state),sampleCount:audioQA.samples.length,last:audioQA.samples.slice(-3)}));
 await page.evaluate(()=>unityInstance.SendMessage('Game Audio','ReportAudio',''));await sample(200);assert(music.energy>0.00001,'music produces nonzero output after user gesture');
 await page.evaluate(()=>{unityInstance.SendMessage('Game Audio','SetMusic',0);audioQA.samples=[];});await sample(500);
 await page.keyboard.down('d');await sample(600);await page.keyboard.up('d');
 await page.keyboard.press('j');await sample(300);await page.keyboard.down('Space');await sample(200);await page.keyboard.press('ShiftLeft');await page.keyboard.up('Space');await sample(1500);
 const effects=await page.evaluate(()=>({starts:audioQA.starts,energy:Math.max(0,...audioQA.samples.map(x=>x.rms).filter(Number.isFinite))}));assert(effects.energy>0.00001,'effects produce audio without music');
 await page.evaluate(()=>{unityInstance.SendMessage('Game Audio','SetMusic',.32);unityInstance.SendMessage('Game Audio','ReportAudio','');});
 await page.keyboard.press('2');await sample(4000);await page.evaluate(()=>unityInstance.SendMessage('Game Audio','ReportAudio',''));await sample(300);
 assert(reports.some(s=>s.includes('region=1')&&s.includes('music=playing')),'region 2 theme active');
 await page.evaluate(()=>{unityInstance.SendMessage('Game Audio','SetMaster',0);audioQA.samples=[];});await sample(800);
 const muted=await page.evaluate(()=>Math.max(0,...audioQA.samples.slice(-3).map(x=>x.rms).filter(Number.isFinite)));assert(muted<.00001,'master mute');
 await page.evaluate(()=>unityInstance.SendMessage('Game Audio','SetMaster',.7));
 assert.equal(await page.locator('details input[type=range]').count(),3);assert.deepEqual(errors,[]);
 fs.writeFileSync('Book/Validation/audio-web.json',JSON.stringify({passed:true,music,effects,muted,reports,errors},null,2));console.log('AUDIO_WEB_OK',JSON.stringify({music,effects,muted,reports}));
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
