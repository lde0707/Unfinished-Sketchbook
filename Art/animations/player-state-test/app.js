'use strict';
const $=id=>document.getElementById(id), canvas=$('stage'), ctx=canvas.getContext('2d');
const clips={}, keys=new Set();
let jumpReg,stopReg,last=0,scale=1;
const S={ready:false,mode:'idle',time:0,frame:0,x:0,y:0,facing:1,dir:0,playing:true,history:[],demo:null,clock:0};
window.previewState=S;
const names={idle:'Idle · 대기',start:'출발',run:'Run · 달리기',stop:'정지',jump:'Jump · 점프'};
function enter(mode,time=0){
  if(S.mode!==mode){S.history.push(`${S.mode} → ${mode}`);if(S.history.length>40)S.history.shift();}
  S.mode=mode;S.time=time;S.y=0;pose();
}
function direction(dir){if(!S.ready)return;S.dir=dir;if(dir)S.facing=dir;reconcile();}
function reconcile(){
  if(S.mode==='jump')return;
  if(S.dir&&(S.mode==='idle'||S.mode==='stop'))enter('start');
  else if(!S.dir&&(S.mode==='run'||S.mode==='start'))enter('stop');
}
function jump(){if(!S.ready||S.mode==='jump')return;enter('jump');}
function duration(){return S.mode==='jump'?jumpReg.durationMs:(S.mode==='start'||S.mode==='stop'?stopReg.durationMs:Infinity);}
function pose(){
  if(S.mode==='idle')S.frame=Math.floor(S.time/300)%6;
  else if(S.mode==='run')S.frame=Math.floor(S.time/80)%8;
  else if(S.mode==='jump'){
    S.frame=jumpReg.frames.findIndex(f=>S.time<f.startMs+f.durationMs);
    if(S.frame<0)S.frame=7;
  }else{
    const order=S.mode==='start'?[3,2,1,0]:[0,1,2,3];let t=S.time;S.frame=order.at(-1);
    for(const i of order){if(t<stopReg.frames[i].durationMs){S.frame=i;break;}t-=stopReg.frames[i].durationMs;}
  }
  S.y=0;
  if(S.mode==='jump'&&S.time>jumpReg.flightStartMs&&S.time<jumpReg.flightEndMs){const u=(S.time-jumpReg.flightStartMs)/(jumpReg.flightEndMs-jumpReg.flightStartMs);S.y=-4*jumpReg.jumpHeightPx*u*(1-u);}
}
function reset(){keys.clear();Object.assign(S,{mode:'idle',time:0,frame:0,x:0,y:0,facing:1,dir:0,history:[],demo:null,clock:0});pose();draw();}
const demos={
  run:{name:'대기 → 달리기 → 정지 → 대기',end:2600,events:[[500,'move',1],[1900,'move',0]]},
  jump:{name:'제자리 점프 → 대기',end:2100,events:[[500,'jump']]},
  moving:{name:'달리며 점프 → 달리기',end:3300,events:[[300,'move',1],[1300,'jump'],[2800,'move',0]]},
  stopAir:{name:'달리며 점프 → 공중에서 키 놓기 → 대기',end:2800,events:[[300,'move',1],[1300,'jump'],[1600,'move',0]]},
  all:{name:'전체 연결 · 오른쪽 / 왼쪽',end:9000,events:[[500,'move',1],[1800,'move',0],[2300,'jump'],[3500,'move',1],[4500,'jump'],[5500,'move',0],[6200,'move',-1],[7200,'jump'],[7500,'move',0]]}
};
function demo(name){if(!S.ready)return;reset();S.playing=true;S.demo={...demos[name],events:demos[name].events.map(e=>[...e]),index:0};$('pause').textContent='일시정지';}
function events(){if(!S.demo)return;while(S.demo.index<S.demo.events.length&&S.demo.events[S.demo.index][0]<=S.clock+1e-6){const [,type,val]=S.demo.events[S.demo.index++];if(type==='move')direction(val);else jump();}if(S.clock>=S.demo.end)S.demo=null;}
function advance(ms){
  if(!S.ready)return;
  // Bounded steps make keyboard, demo events and clip boundaries deterministic.
  while(ms>1e-6){events();reconcile();const remaining=duration()-S.time;let dt=Math.min(ms,8,remaining);
    if(S.demo&&S.demo.index<S.demo.events.length)dt=Math.min(dt,S.demo.events[S.demo.index][0]-S.clock);
    if(dt<1e-6){events();break;}
    if(S.mode==='run')S.x+=S.dir*200*dt/1000;
    else if(S.mode==='jump'){
      // Horizontal control only during flight; planted anticipation/landing stays fixed.
      const flight=Math.max(0,Math.min(S.time+dt,jumpReg.flightEndMs)-Math.max(S.time,jumpReg.flightStartMs));
      S.x+=S.dir*200*flight/1000;
    }
    S.time+=dt;S.clock+=dt;ms-=dt;
    if(S.time>=duration()-1e-6){if(S.mode==='start')enter(S.dir?'run':'idle',S.dir?160:0);else if(S.mode==='stop')enter(S.dir?'start':'idle');else if(S.mode==='jump')enter(S.dir?'run':'idle',S.dir?80:0);}
    pose();
  }events();draw();
}
function draw(){
  if(!S.ready)return;
  const w=canvas.width,h=canvas.height,ground=520;
  ctx.clearRect(0,0,w,h);const bg=ctx.createLinearGradient(0,0,0,h);bg.addColorStop(0,'#20272b');bg.addColorStop(1,'#303a3b');ctx.fillStyle=bg;ctx.fillRect(0,0,w,h);
  // Ground marks scroll with world position, while the character stays in view.
  const camera=S.x, root=w/2;
  ctx.strokeStyle='#465350';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(0,ground);ctx.lineTo(w,ground);ctx.stroke();
  ctx.fillStyle='#1b2224';ctx.fillRect(0,ground+1,w,h-ground);
  for(let n=Math.floor((camera-root)/100);n<(camera+root)/100;n++){const x=n*100-camera+root;ctx.strokeStyle='#414e4b';ctx.beginPath();ctx.moveTo(x,ground+1);ctx.lineTo(x,ground+9);ctx.stroke();ctx.fillStyle='#73817d';ctx.font='11px system-ui';ctx.fillText(`${n*100}px`,x+5,ground+27);}
  ctx.fillStyle=`rgba(0,0,0,${0.2+0.12*(1-Math.abs(S.y)/130)})`;ctx.beginPath();ctx.ellipse(root,ground-1,42-Math.abs(S.y)*0.12,6,0,0,Math.PI*2);ctx.fill();
  const group=S.mode==='start'||S.mode==='stop'?'stop':S.mode,anchor=group==='run'||group==='stop'?340:368;
  ctx.save();ctx.translate(root,ground+S.y);ctx.scale(S.facing,1);ctx.drawImage(clips[group][S.frame],-anchor,-448);ctx.restore();
  $('state').textContent=names[S.mode];$('detail').textContent=`프레임 ${S.frame+1} / ${clips[group].length} · ${S.facing===1?'오른쪽':'왼쪽'} · 높이 ${Math.round(-S.y)}px`;
  $('scenario').textContent=S.demo?`자동 시연: ${S.demo.name}`:'직접 조작 · A / D / ← / → + Space';
  $('log').textContent=S.history.slice(-6).map(s=>s.replaceAll('start','출발').replaceAll('stop','정지')).join('　·　');
  $('left').classList.toggle('active',S.dir<0);$('right').classList.toggle('active',S.dir>0);
}
function manual(){S.demo=null;}
function fromKeys(){direction((keys.has('ArrowRight')||keys.has('KeyD')?1:0)-(keys.has('ArrowLeft')||keys.has('KeyA')?1:0));}
window.addEventListener('keydown',e=>{if(!S.ready||/INPUT|SELECT|TEXTAREA/.test(e.target.tagName))return;if(['ArrowLeft','ArrowRight','KeyA','KeyD','Space'].includes(e.code)){e.preventDefault();manual();if(e.code==='Space'){if(!e.repeat)jump();}else{keys.add(e.code);fromKeys();}}});
window.addEventListener('keyup',e=>{if(keys.delete(e.code))fromKeys();});
window.addEventListener('blur',()=>{keys.clear();direction(0);});
for(const [id,key] of [['left','ArrowLeft'],['right','ArrowRight']]){
  const b=$(id);b.addEventListener('pointerdown',e=>{if(!S.ready)return;e.preventDefault();manual();b.setPointerCapture(e.pointerId);keys.add(key);fromKeys();});
  for(const event of ['pointerup','pointercancel','lostpointercapture'])b.addEventListener(event,()=>{keys.delete(key);fromKeys();});
}
$('jump').onclick=()=>{manual();jump();};$('reset').onclick=reset;
$('pause').onclick=()=>{S.playing=!S.playing;$('pause').textContent=S.playing?'일시정지':'계속 재생';};
$('speed').onchange=e=>{scale=Number(e.target.value);};
document.querySelectorAll('[data-demo]').forEach(b=>b.onclick=()=>demo(b.dataset.demo));
window.previewDebug={advance,reset,direction,jump,demo,draw};
async function init(){
  [jumpReg,stopReg]=await Promise.all(['../player-c-jump-v1/registration.json','../player-c-run-idle-v1/registration.json'].map(async p=>{const r=await fetch(p);if(!r.ok)throw Error(p);return r.json();}));
  await Promise.all(Object.entries({run:['player-c-v6-rhythm',8],idle:['player-c-idle-v1',6],jump:['player-c-jump-v1',8],stop:['player-c-run-idle-v1',4]}).map(async([name,[folder,count]])=>{clips[name]=await Promise.all(Array.from({length:count},(_,i)=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>resolve(img);img.onerror=()=>reject(Error(`이미지 로드 실패: ${img.src}`));img.src=`../${folder}/frames/${String(i).padStart(2,'0')}.png`;})));}));
  S.ready=true;demo('all');draw();
  requestAnimationFrame(function tick(t){const dt=last?Math.min(t-last,50):0;last=t;if(S.playing)advance(dt*scale);requestAnimationFrame(tick);});
}
init().catch(e=>{$('state').textContent='불러오기 실패';$('detail').textContent=e.message;console.error(e);});
