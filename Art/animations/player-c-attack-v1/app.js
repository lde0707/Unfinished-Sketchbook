'use strict';
const $=id=>document.getElementById(id),ctx=$('stage').getContext('2d');
const titles=['1타 · 짧은 베기','2타 · 올려치기','3타 · 정면 찌르기'];
const phaseNames=['준비','뒤로 감기','빠른 휘두름','타격 자세','따라가기','회복'];
const thrustPhases=['2타 휘두름 끝','팔 거두기','앞으로 찌르기','찌르기 타격','팔 회수','복귀'];
const s=window.previewState={ready:false,playing:true,mode:'idle',clip:0,frame:0,time:0,clock:0,facing:1,queuedUntil:-1,lastClip:-1,comboUntil:-1,cooldownUntil:-1,auto:true,autoWait:500,attacks:[],frameGlobal:0};
let reg,art,idles,last=0;
function image(src){return new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>resolve(im);im.onerror=()=>reject(Error(src));im.src=src})}
function reset(auto=false){Object.assign(s,{mode:'idle',clip:0,frame:0,time:0,clock:0,queuedUntil:-1,lastClip:-1,comboUntil:-1,cooldownUntil:-1,auto,autoWait:500,attacks:[]});s.playing=true;$('pause').textContent='일시정지';render()}
function start(clip){s.mode='attack';s.clip=clip;s.frame=reg.clips[clip].entryFrame;s.time=0;s.queuedUntil=-1;s.attacks.push(clip+1)}
function attack(){if(!s.ready)return false;s.auto=false;if(s.mode==='attack'){if(s.clip===2)return false;s.queuedUntil=s.clock+reg.inputBufferMs;$('message').textContent='다음 타격 입력 예약';return true}if(s.clock<s.cooldownUntil)return false;start(s.clock<=s.comboUntil&&s.lastClip>=0&&s.lastClip<2?s.lastClip+1:0);render();return true}
function advance(ms){for(let remaining=ms;remaining>0;){const dt=Math.min(remaining,1);remaining-=dt;s.clock+=dt;s.time+=dt;if(s.mode==='attack'){
 const clip=reg.clips[s.clip];if(s.auto&&s.clip<2&&s.time>=clip.durationMs-100)s.queuedUntil=s.clock+reg.inputBufferMs;
 if(s.time>=clip.durationMs){const prev=s.clip;s.lastClip=prev;s.comboUntil=s.clock+reg.comboWindowMs;
  if(prev<2&&s.queuedUntil>=s.clock)start(prev+1);else{s.mode='idle';s.time=0;s.queuedUntil=-1;if(prev===2){s.cooldownUntil=s.clock+reg.finisherRecoveryMs;s.comboUntil=-1;s.autoWait=1100}else{s.auto=false}}
 }else{s.frame=clip.frames.findIndex(f=>s.time<f.startMs+f.durationMs);}
 }else{if(s.auto&&$('repeat').checked){s.autoWait-=dt;if(s.autoWait<=0&&s.clock>=s.cooldownUntil)start(0)}}}
 render();}
function select(n){s.auto=false;s.playing=false;s.mode='attack';s.clip=Math.floor(n/6);s.frame=n%6;s.time=reg.clips[s.clip].frames[s.frame].startMs;$('pause').textContent='재생';s.queuedUntil=-1;render()}
function drawSprite(im,isIdle){ctx.save();ctx.translate(600,0);ctx.scale(s.facing,1);if(isIdle)ctx.drawImage(im,-364,112);else{ThrustVfx.draw(ctx,s,'standing');ctx.drawImage(im,-384,0)}ctx.restore()}
function render(){if(!s.ready)return;ctx.fillStyle='#202629';ctx.fillRect(0,0,1200,640);ctx.fillStyle='#1c2225';ctx.fillRect(0,560,1200,80);ctx.strokeStyle='#52605d';ctx.beginPath();ctx.moveTo(40,560.5);ctx.lineTo(1160,560.5);ctx.stroke();
 const active=s.mode==='attack',cooling=!active&&s.clock<s.cooldownUntil;
 if(active){drawSprite(art[s.clip][s.frame],false);s.frameGlobal=s.clip*6+s.frame;$('scrub').value=s.frameGlobal;$('position').textContent=`${s.frameGlobal+1} / 18`;const m=reg.clips[s.clip].frames[s.frame];$('phase').textContent=`${s.frameGlobal+1}번 · ${titles[s.clip]} · ${(s.clip===2?thrustPhases:phaseNames)[s.frame]} · ${m.durationMs}ms`;
  $('detail').textContent=`${s.frame+1} / 6 프레임 · ${(s.clip===2?thrustPhases:phaseNames)[s.frame]}${s.queuedUntil>=s.clock?' · 다음 입력 예약':''}`;
 }else{drawSprite(idles[Math.floor(s.time/300)%6],true);$('detail').textContent=s.auto?'다음 시연 대기':'화면 클릭 또는 J로 공격';}
 $('state').textContent=active?titles[s.clip]:cooling?'회복 → Idle':'Idle';
 document.querySelectorAll('[data-clip]').forEach(b=>b.classList.toggle('active',active&&+b.dataset.clip===s.clip));document.querySelectorAll('[data-frame]').forEach(b=>b.classList.toggle('active',active&&+b.dataset.frame===s.frameGlobal));
}
$('combo').onclick=()=>{reset(true);s.autoWait=0;start(0);$('message').textContent='1타 → 2타 → 3타 → Idle 연결 시연';render()};
function trigger(){s.playing=true;$('pause').textContent='일시정지';attack()}
for(const id of ['stage','attack'])$(id).addEventListener('pointerdown',e=>{if(e.button!==0)return;e.preventDefault();trigger()});
$('attack').onclick=e=>{if(e.detail===0)trigger()};$('idle').onclick=()=>{reset(false);$('message').textContent='화면 클릭 또는 J · 준비 동작부터 공격을 재생합니다.'};
$('pause').onclick=()=>{s.playing=!s.playing;$('pause').textContent=s.playing?'일시정지':'재생'};
$('flip').onchange=()=>{s.facing=$('flip').checked?-1:1;render()};$('scrub').oninput=()=>select(+$('scrub').value);
$('prev').onclick=()=>select((s.frameGlobal+17)%18);$('next').onclick=()=>select((s.frameGlobal+1)%18);
document.querySelectorAll('[data-clip]').forEach(b=>b.onclick=()=>{reset(false);start(+b.dataset.clip);render()});
document.addEventListener('keydown',e=>{if(['INPUT','SELECT'].includes(e.target.tagName))return;if(e.repeat)return;if(e.code==='KeyJ'){e.preventDefault();trigger()}if(e.code==='KeyP')$('pause').click();if(['KeyA','ArrowLeft','KeyD','ArrowRight'].includes(e.code)){e.preventDefault();s.facing=['KeyA','ArrowLeft'].includes(e.code)?-1:1;$('flip').checked=s.facing<0;render()}});
window.previewDebug={reset,attack,advance,select,start,render};
function tick(t){const dt=last?Math.min(t-last,80):0;last=t;if(s.ready&&s.playing)advance(dt*+$('speed').value);requestAnimationFrame(tick)}
(async()=>{try{reg=await fetch('registration.json').then(r=>{if(!r.ok)throw Error('프레임 정보를 읽지 못했습니다.');return r.json()});[art,idles]=await Promise.all([Promise.all([1,2,3].map(a=>Promise.all(Array.from({length:6},(_,i)=>image(`attack${a}/frames/${String(i).padStart(2,'0')}.png`))))),Promise.all(Array.from({length:6},(_,i)=>image(`../player-c-idle-v1/frames/${String(i).padStart(2,'0')}.png`)))]);for(let n=0;n<18;n++){const b=document.createElement('button');b.textContent=n+1;b.dataset.frame=n;b.onclick=()=>select(n);$('frameButtons').appendChild(b)}await ThrustVfx.load();s.ready=true;render();requestAnimationFrame(tick)}catch(e){$('state').textContent='불러오기 실패';$('message').textContent=e.message;console.error(e)}})();
