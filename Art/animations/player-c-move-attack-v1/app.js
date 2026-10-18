'use strict';
const $=id=>document.getElementById(id),ctx=$('stage').getContext('2d');
const titles=['1타 · 이동 베기','2타 · 이동 올려치기','3타 · 전진 찌르기'];
const phases=['디딤 · 준비','뒤로 밀기 · 감기','반대 무릎 앞으로 · 휘두름','반대발 디딤 · 타격','반대발 밀기 · 따라가기','다음 걸음 · 회복'];
const thrustPhases=['2타 휘두름 끝','팔 거두기','앞으로 찌르기','찌르기 타격','팔 회수','달리기 복귀'];
const s=window.previewState={ready:false,playing:true,mode:'idle',variant:'move',clip:0,frame:0,time:0,clock:0,x:0,vx:0,direction:0,facing:1,attackFacing:1,runTime:0,queuedUntil:-1,lastClip:-1,comboUntil:-1,cooldownUntil:-1,auto:true,demoCycle:-1,demoFired:false,attacks:[],cancellations:0,frameGlobal:0};
let reg,standingReg,art,standing,runs,idles,last=0;const held=new Set(),pointerDirections=new Map();
const image=src=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>resolve(im);im.onerror=()=>reject(Error(src));im.src=src});
const clipData=()=> (s.variant==='move'?reg:standingReg).clips[s.clip];
function reset(auto=false){held.clear();pointerDirections.clear();Object.assign(s,{mode:'idle',variant:'move',clip:0,frame:0,time:0,clock:0,x:0,vx:0,direction:0,facing:1,attackFacing:1,runTime:0,queuedUntil:-1,lastClip:-1,comboUntil:-1,cooldownUntil:-1,auto,demoCycle:-1,demoFired:false,attacks:[],cancellations:0,frameGlobal:0});s.playing=true;$('pause').textContent='일시정지';render()}
function direction(value,manual=true){if(manual)s.auto=false;s.direction=value;if(value&&s.mode==='attack'&&value!==s.attackFacing){s.mode='run';s.time=0;s.runTime=80;s.queuedUntil=-1;s.lastClip=-1;s.comboUntil=-1;s.cancellations++}if(value)s.facing=value;render()}
function start(clip){s.mode='attack';s.clip=clip;s.time=0;s.variant=Math.abs(s.vx)>20||s.direction?'move':'standing';s.frame=clipData().entryFrame;s.attackFacing=s.facing;s.queuedUntil=-1;s.attacks.push({stage:clip+1,variant:s.variant,x:s.x,clock:s.clock})}
function attack(manual=true){if(!s.ready)return false;if(manual)s.auto=false;if(s.mode==='attack'){if(s.clip===2)return false;s.queuedUntil=s.clock+reg.inputBufferMs;return true}if(s.clock<s.cooldownUntil)return false;start(s.clock<=s.comboUntil&&s.lastClip>=0&&s.lastClip<2?s.lastClip+1:0);render();return true}
function step(dt){s.clock+=dt;s.time+=dt;
 if(s.auto){const cycle=Math.floor(s.clock/4400),t=s.clock%4400;if(cycle!==s.demoCycle){s.demoCycle=cycle;s.demoFired=false;direction(cycle%2?-1:1,false)}if(t>=700&&!s.demoFired){s.demoFired=true;attack(false)}if(s.mode==='attack'&&s.clip<2&&s.time>=clipData().durationMs-100)s.queuedUntil=s.clock+reg.inputBufferMs;}
 const target=s.direction*320,delta=2400*dt/1000;s.vx+=Math.max(-delta,Math.min(delta,target-s.vx));s.x+=s.vx*dt/1000;
 if(s.mode==='attack'){
  const variant=Math.abs(s.vx)>20||s.direction?'move':'standing';
  if(variant!==s.variant){const progress=s.time/clipData().durationMs;s.variant=variant;s.time=progress*clipData().durationMs;}
  const clip=clipData();
  if(s.time>=clip.durationMs){const prev=s.clip;s.lastClip=prev;s.comboUntil=s.clock+reg.comboWindowMs;
   if(prev<2&&s.queuedUntil>=s.clock)start(prev+1);else{s.mode=Math.abs(s.vx)>20?'run':'idle';s.time=0;s.runTime=80;s.queuedUntil=-1;if(prev===2){s.cooldownUntil=s.clock+reg.finisherRecoveryMs;s.comboUntil=-1}}
  }else s.frame=clip.frames.findIndex(f=>s.time<f.startMs+f.durationMs);
 }else{s.mode=Math.abs(s.vx)>20?'run':'idle';s.runTime=(s.runTime+dt*Math.abs(s.vx)/320)%640;}
}
function advance(ms){for(let left=ms;left>0;){const dt=Math.min(1,left);left-=dt;step(dt)}render()}
function select(n){s.auto=false;s.playing=false;s.mode='attack';s.variant='move';s.clip=Math.floor(n/6);s.frame=n%6;s.time=reg.clips[s.clip].frames[s.frame].startMs;s.attackFacing=s.facing;s.queuedUntil=-1;s.frameGlobal=n;$('pause').textContent='재생';render()}
function render(){if(!s.ready)return;ctx.fillStyle='#202629';ctx.fillRect(0,0,1200,640);ctx.fillStyle='#1b2225';ctx.fillRect(0,560,1200,80);
 const camera=s.x-600;ctx.strokeStyle='#56655e';ctx.beginPath();ctx.moveTo(0,560.5);ctx.lineTo(1200,560.5);ctx.stroke();ctx.font='12px system-ui';ctx.fillStyle='#6f817a';
 for(let world=Math.floor(camera/100)*100;world<camera+1200;world+=100){const x=world-camera;ctx.beginPath();ctx.moveTo(x,560);ctx.lineTo(x,570);ctx.stroke();ctx.fillText(`${world}`,x+4,587)}
 const active=s.mode==='attack';let im,ox,oy;
 if(active){im=(s.variant==='move'?art:standing)[s.clip][s.frame];ox=-384;oy=0;s.frameGlobal=s.clip*6+s.frame;$('scrub').value=s.frameGlobal;$('position').textContent=`${s.frameGlobal+1} / 18`;const m=clipData().frames[s.frame];$('phase').textContent=`${s.frameGlobal+1}번 · ${s.clip===2?thrustPhases[s.frame]:s.variant==='move'?phases[s.frame]:m.phase} · ${m.durationMs}ms`;}
 else{im=s.mode==='run'?runs[Math.floor(s.runTime/80)%8]:idles[Math.floor(s.time/300)%6];ox=-364;oy=112;}
 ctx.save();ctx.translate(600,0);ctx.scale(active?s.attackFacing:s.facing,1);if(active)ThrustVfx.draw(ctx,s,s.variant);ctx.drawImage(im,ox,oy);ctx.restore();
 $('state').textContent=active?(s.variant==='move'?titles[s.clip]:`${s.clip+1}타 · 제자리`):s.mode==='run'?'Run':'Idle';
 $('detail').textContent=`이동 ${Math.round(Math.abs(s.vx))} px/s · 위치 ${Math.round(s.x)}${active?` · ${s.frame+1}/6 프레임`:''}${s.clock<s.cooldownUntil?' · 공격 회복 중 (이동 가능)':''}`;
 document.querySelectorAll('[data-clip]').forEach(b=>b.classList.toggle('active',active&&+b.dataset.clip===s.clip));document.querySelectorAll('[data-frame]').forEach(b=>b.classList.toggle('active',active&&+b.dataset.frame===s.frameGlobal));
}
function play(){s.playing=true;$('pause').textContent='일시정지'}
$('demo').onclick=()=>{reset(true);$('message').textContent='달리기 → 이동 1·2·3타 → 달리기 · 좌우 번갈아 시연'};
$('manual').onclick=()=>{reset(false);$('message').textContent='A/D를 누른 채 J로 공격하세요. 이동 버튼도 누르고 있으면 움직입니다.'};
for(const id of ['stage','attack'])$(id).addEventListener('pointerdown',e=>{if(e.button!==0)return;e.preventDefault();play();attack()});
$('attack').onclick=e=>{if(e.detail===0){play();attack()}};$('pause').onclick=()=>{s.playing=!s.playing;$('pause').textContent=s.playing?'일시정지':'재생'};
function updateInput(){const pointers=[...pointerDirections.values()];const right=held.has('KeyD')||held.has('ArrowRight'),left=held.has('KeyA')||held.has('ArrowLeft');direction(pointers.length?pointers[pointers.length-1]:(right?1:0)-(left?1:0))}
for(const [id,dir] of [['left',-1],['right',1]]){const b=$(id);b.style.touchAction='none';b.onpointerdown=e=>{e.preventDefault();b.setPointerCapture(e.pointerId);pointerDirections.set(e.pointerId,dir);play();updateInput()};for(const event of ['pointerup','pointercancel','lostpointercapture'])b.addEventListener(event,e=>{if(pointerDirections.delete(e.pointerId))updateInput()})}
document.addEventListener('keydown',e=>{if(['INPUT','SELECT'].includes(e.target.tagName))return;if(['KeyA','KeyD','ArrowLeft','ArrowRight'].includes(e.code)){e.preventDefault();held.add(e.code);play();updateInput()}if(e.repeat)return;if(e.code==='KeyJ'){e.preventDefault();play();attack()}if(e.code==='KeyP')$('pause').click()});
document.addEventListener('keyup',e=>{if(held.delete(e.code))updateInput()});window.addEventListener('blur',()=>{held.clear();pointerDirections.clear();if(!s.auto)direction(0)});
$('prev').onclick=()=>select((s.frameGlobal+17)%18);$('next').onclick=()=>select((s.frameGlobal+1)%18);$('scrub').oninput=()=>select(+$('scrub').value);
document.querySelectorAll('[data-clip]').forEach(b=>b.onclick=()=>{reset(false);direction(1);s.vx=320;start(+b.dataset.clip);render()});
window.previewDebug={reset,direction,attack,advance,select,start,render};
function tick(t){const dt=last?Math.min(t-last,80):0;last=t;if(s.ready&&s.playing)advance(dt*+$('speed').value);requestAnimationFrame(tick)}
(async()=>{try{[reg,standingReg]=await Promise.all([fetch('registration.json').then(r=>r.json()),fetch('../player-c-attack-v1/registration.json').then(r=>r.json())]);const load=(prefix,count)=>Promise.all(Array.from({length:count},(_,i)=>image(`${prefix}/frames/${String(i).padStart(2,'0')}.png`)));[art,standing,runs,idles]=await Promise.all([Promise.all([1,2,3].map(i=>load(`attack${i}`,6))),Promise.all([1,2,3].map(i=>load(`../player-c-attack-v1/attack${i}`,6))),load('../player-c-v6-rhythm',8),load('../player-c-idle-v1',6)]);for(let n=0;n<18;n++){const b=document.createElement('button');b.textContent=n+1;b.dataset.frame=n;b.onclick=()=>select(n);$('frameButtons').appendChild(b)}await ThrustVfx.load();s.ready=true;render();requestAnimationFrame(tick)}catch(e){$('state').textContent='불러오기 실패';$('message').textContent=e.message;console.error(e)}})();
