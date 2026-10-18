'use strict';
const $=id=>document.getElementById(id), canvas=$('stage'), ctx=canvas.getContext('2d');
const clips={}, keys=new Set();
let jumpReg,tuckReg,stopReg,dashReg,fallReg,last=0,scale=1;
const JUMP_BUFFER_MS=120;
const S={ready:false,mode:'idle',jumpVariant:'tuck',time:0,frame:0,x:0,y:0,facing:1,dir:0,playing:true,history:[],demo:null,clock:0,jumpBufferedUntil:null,jumpCount:0,dashDir:1,dashReadyAt:0,dashCount:0,dashReturn:null,airDashUsed:false,fallVelocity:0,fallLandingMs:null,jumpOriginY:0,fallPoseFrom:null,ledge:null,airAttack:null,airAttackCount:0,airAttackHistory:[]};
const activeJump=()=>S.jumpVariant==='tuck'?tuckReg:jumpReg;
window.previewState=S;
const names={idle:'Idle · 대기',start:'출발',run:'Run · 달리기',stop:'정지',jump:'Jump · 점프',dash:'Dash · 대시',fall:'Fall · 낙하'};
function enter(mode,time=0){
  if(S.mode!==mode){S.history.push(`${S.mode} → ${mode}`);if(S.history.length>40)S.history.shift();}
  S.mode=mode;S.time=time;S.y=0;pose();
}
function direction(dir){if(!S.ready)return;S.dir=dir;if(dir&&S.mode!=='dash')S.facing=dir;reconcile();}
function reconcile(){
  if(['jump','dash','fall'].includes(S.mode))return;
  if(S.dir&&(S.mode==='idle'||S.mode==='stop'))enter('start');
  else if(!S.dir&&(S.mode==='run'||S.mode==='start'))enter('stop');
}
// Ground contact and the end of the landing drawing are separate events.
function canJump(){if(S.mode==='fall')return S.fallLandingMs!==null;if(S.mode==='dash'&&S.dashReturn)return false;return S.mode!=='jump'||(S.jumpOriginY===0&&S.time>=activeJump().flightEndMs);}
function startJump(originY=0){
  S.jumpOriginY=originY;S.fallPoseFrom=null;
  S.airAttack=null;
  if(S.dir)S.facing=S.dir;
  S.jumpBufferedUntil=null;S.jumpCount++;S.airDashUsed=false;S.dashReturn=null;
  if(S.mode==='jump'){S.history.push('jump → jump');if(S.history.length>40)S.history.shift();}
  S.jumpVariant=S.dir?'jump':'tuck';enter('jump');
}
function jump(){if(!S.ready)return;if(canJump())startJump();else S.jumpBufferedUntil=S.clock+JUMP_BUFFER_MS;}
function jumpFromLedge(height=180){
  if(!S.ready)return;
  const h=Math.max(1,Math.min(1000,Number(height)||180));
  S.ledge={edge:S.x-S.facing*24,height:h,direction:S.facing};
  startJump(-h);draw();
}
function jumpFallBoundary(){
  const reg=activeJump();return S.jumpOriginY<0?(reg.flightStartMs+reg.flightEndMs)/2+80:Infinity;
}
function fallVisualOffset(){
  if(!S.fallPoseFrom)return {x:0,y:0};
  const weight=Math.max(0,1-(S.clock-S.fallPoseFrom.clock)/80);
  return {x:S.fallPoseFrom.x*weight,y:S.fallPoseFrom.y*weight};
}
function continueJumpAsFall(){
  const reg=activeJump(),seconds=(reg.flightEndMs-reg.flightStartMs)/1000;
  const u=(S.time-reg.flightStartMs)/(reg.flightEndMs-reg.flightStartMs);
  pose();const height=S.y,head=reg.frames[S.frame].headCenter,anchor=S.jumpVariant==='tuck'?368:340;
  // Carry the actual downward velocity into free fall, without a midair landing.
  S.fallVelocity=4*reg.jumpHeightPx*(2*u-1)/seconds;S.fallLandingMs=null;
  S.fallPoseFrom={x:head[0]-anchor-(fallReg.frames[1].headCenter[0]-368),y:head[1]-fallReg.frames[1].headCenter[1],clock:S.clock};
  enter('fall',fallReg.entryMs);S.y=height;
}
function drop(height=160){
  S.airAttack=null;
  S.jumpOriginY=0;S.fallPoseFrom=null;
  if(!S.ready)return;
  const h=Math.max(1,Math.min(1000,Number(height)||160));
  S.fallVelocity=0;S.fallLandingMs=null;S.airDashUsed=false;S.dashReturn=null;S.jumpBufferedUntil=null;
  S.ledge={edge:S.x-S.facing*32,height:h,direction:S.facing};
  enter('fall');S.y=-h;draw();
}
function fallContactInMs(){
  const g=fallReg.gravityPxPerSecondSquared,v=S.fallVelocity,cap=fallReg.terminalSpeedPxPerSecond,dist=Math.max(0,-S.y);
  const accelTime=Math.max(0,(cap-v)/g),accelDist=v*accelTime+g*accelTime*accelTime/2;
  return 1000*(dist<=accelDist?(Math.sqrt(v*v+2*g*dist)-v)/g:accelTime+(dist-accelDist)/cap);
}
function advanceFall(dt){
  if(S.fallLandingMs!==null){S.fallLandingMs+=dt;return;}
  const seconds=dt/1000,g=fallReg.gravityPxPerSecondSquared,cap=fallReg.terminalSpeedPxPerSecond;
  const accel=Math.min(seconds,Math.max(0,(cap-S.fallVelocity)/g));
  S.y+=S.fallVelocity*accel+g*accel*accel/2+cap*(seconds-accel);
  S.fallVelocity=Math.min(cap,S.fallVelocity+g*seconds);
  if(S.y>=-1e-6){S.y=0;S.fallVelocity=0;S.fallLandingMs=0;S.airDashUsed=false;}
}
function dash(){
  if(!S.ready||S.mode==='dash'||S.clock<S.dashReadyAt)return false;
  const falling=S.mode==='fall'&&S.fallLandingMs===null;
  const airborne=falling||(S.mode==='jump'&&S.time>activeJump().flightStartMs&&S.time<activeJump().flightEndMs);
  if(airborne&&S.airDashUsed)return false;
  if(!airborne&&!canJump())return false;
  const offset=falling?fallVisualOffset():{x:0,y:0};
  S.dashReturn=airborne?{kind:falling?'fall':'jump',time:S.time,variant:S.jumpVariant,y:S.y,velocity:S.fallVelocity,headY:(falling?fallReg:activeJump()).frames[S.frame].headCenter[1]+offset.y,anchor:(falling||S.jumpVariant==='tuck'?368:340)-offset.x}:null;
  if(airborne)S.airDashUsed=true;
  S.dashDir=S.dir||S.facing;S.facing=S.dashDir;S.dashCount++;S.jumpBufferedUntil=null;
  S.airAttack=null;S.dashReadyAt=S.clock+dashReg.durationMs+dashReg.cooldownAfterEndMs;enter('dash');return true;
}
function finishDash(){
  S.facing=S.dir||S.dashDir;
  if(S.dashReturn){
    const resume=S.dashReturn;S.dashReturn=null;
    // A jump dash consumes the remaining jump arc; gravity takes over here.
    S.fallVelocity=resume.kind==='fall'?resume.velocity:0;S.fallLandingMs=null;
    enter('fall',resume.kind==='fall'?resume.time:fallReg.entryMs);S.y=resume.y;
  }
  else enter(S.dir?'run':'stop',S.dir?80:0);
}
function consumeBufferedJump(){
  if(S.jumpBufferedUntil===null)return;
  if(S.clock>S.jumpBufferedUntil+1e-6){S.jumpBufferedUntil=null;return;}
  if(canJump())startJump();
}
function airContext(){
  if(S.mode==='jump'&&S.time<activeJump().flightEndMs)return {airborne:true,kind:S.time<(activeJump().flightStartMs+activeJump().flightEndMs)/2?'rise':'fall'};
  return {airborne:S.mode==='fall'&&S.fallLandingMs===null,kind:'fall'};
}
function attack(){if(!S.ready)return false;const started=AirAttack.trigger(S,airContext());draw();return started;}
function duration(){return S.mode==='dash'?dashReg.durationMs:S.mode==='jump'?activeJump().durationMs:(S.mode==='start'||S.mode==='stop'?stopReg.durationMs:Infinity);}
function pose(){
  if(S.mode==='fall'){
    if(S.fallLandingMs!==null)S.frame=S.fallLandingMs<fallReg.landingMs?3:4;
    else if(S.time<fallReg.entryMs)S.frame=0;
    else if(S.time<fallReg.entryMs+fallReg.earlyFallMs)S.frame=1;
    else S.frame=2-Math.floor((S.time-fallReg.entryMs-fallReg.earlyFallMs)/fallReg.loopPoseMs)%2;
    return;
  }
  if(S.mode==='idle')S.frame=Math.floor(S.time/300)%6;
  else if(S.mode==='run')S.frame=Math.floor(S.time/80)%8;
  else if(S.mode==='dash')S.frame=Math.max(0,dashReg.frames.findIndex(f=>S.time<f.startMs+f.durationMs));
  else if(S.mode==='jump'){
    S.frame=activeJump().frames.findIndex(f=>S.time<f.startMs+f.durationMs);
    if(S.frame<0)S.frame=activeJump().frameCount-1;
  }else{
    const order=S.mode==='start'?[3,2,1,0]:[0,1,2,3];let t=S.time;S.frame=order.at(-1);
    for(const i of order){if(t<stopReg.frames[i].durationMs){S.frame=i;break;}t-=stopReg.frames[i].durationMs;}
  }
  S.y=0;
  if(S.mode==='dash'&&S.dashReturn)S.y=S.dashReturn.y;
  if(S.mode==='jump'){const reg=activeJump();S.y=S.jumpOriginY;if(S.time>reg.flightStartMs&&S.time<reg.flightEndMs){const u=(S.time-reg.flightStartMs)/(reg.flightEndMs-reg.flightStartMs);S.y-=4*reg.jumpHeightPx*u*(1-u);}}
}
function reset(){keys.clear();Object.assign(S,{mode:'idle',jumpVariant:'tuck',time:0,frame:0,x:0,y:0,facing:1,dir:0,history:[],demo:null,clock:0,jumpBufferedUntil:null,jumpCount:0,dashDir:1,dashReadyAt:0,dashCount:0,dashReturn:null,airDashUsed:false,fallVelocity:0,fallLandingMs:null,jumpOriginY:0,fallPoseFrom:null,ledge:null,airAttack:null,airAttackCount:0,airAttackHistory:[]});pose();draw();}
const demos={
  longFall:{name:'높은 곳에서 점프 → 하강 → 낙하 → 실제 착지',end:2400,events:[[0,'move',1],[150,'ledgeJump',180],[1750,'move',0]]},
  longFallAttack:{name:'높은 곳 점프 → 낙하 중 정면 공격',end:2600,events:[[0,'move',1],[150,'ledgeJump',220],[620,'attack'],[1750,'move',0]]},
  riseAttack:{name:'점프 상승 중 공격 · 좌우',end:2800,events:[[100,'move',1],[400,'jump'],[490,'attack'],[1000,'move',0],[1400,'move',-1],[1700,'jump'],[1790,'attack'],[2400,'move',0]]},
  fallAttack:{name:'발판 이탈 → 낙하 공격',end:1700,events:[[0,'move',1],[0,'drop',240],[160,'attack'],[1050,'move',0]]},
  jumpFallAttack:{name:'점프 상승 공격 → 하강 공격',end:1800,events:[[150,'jump'],[205,'attack'],[450,'attack']]},
  run:{name:'대기 → 달리기 → 정지 → 대기',end:2600,events:[[500,'move',1],[1900,'move',0]]},
  jump:{name:'제자리 점프 → 대기',end:2100,events:[[500,'jump']]},
  moving:{name:'달리며 점프 → 달리기',end:3300,events:[[300,'move',1],[1300,'jump'],[2800,'move',0]]},
  stopAir:{name:'달리며 점프 → 공중에서 키 놓기 → 대기',end:2800,events:[[300,'move',1],[1300,'jump'],[1600,'move',0]]},
  reJump:{name:'착지 직전 입력 → 연속 점프 3회',end:2500,events:[[500,'jump'],[925,'jump'],[1420,'jump']]},
  dash:{name:'달리기 → 대시 → 달리기 · 좌우',end:3400,events:[[200,'move',1],[800,'dash'],[1450,'move',0],[1950,'move',-1],[2450,'dash'],[3000,'move',0]]},
  dashJump:{name:'대시 → 점프 → 달리기',end:2600,events:[[200,'move',1],[800,'dash'],[920,'jump'],[2100,'move',0]]},
  airDash:{name:'점프 → 공중 대시 → 착지 · 좌우',end:3400,events:[[200,'move',1],[500,'jump'],[700,'dash'],[1350,'move',0],[1800,'move',-1],[2100,'jump'],[2300,'dash'],[3000,'move',0]]},
  fall:{name:'발판 이탈 → 낙하 → 착지',end:1800,events:[[0,'drop',160]]},
  movingFall:{name:'이동하며 발판 이탈 → 낙하 → 달리기',end:1800,events:[[0,'move',1],[0,'drop',160],[1200,'move',0]]},
  fallDash:{name:'낙하 → 공중 대시 → 낙하 → 착지',end:1800,events:[[0,'move',1],[0,'drop',160],[180,'dash'],[1200,'move',0]]},
  all:{name:'전체 연결 · 오른쪽 / 왼쪽',end:9000,events:[[500,'move',1],[1800,'move',0],[2300,'jump'],[3500,'move',1],[4500,'jump'],[5500,'move',0],[6200,'move',-1],[7200,'jump'],[7500,'move',0]]}
};
function demo(name){if(!S.ready)return;reset();S.playing=true;S.demo={...demos[name],events:demos[name].events.map(e=>[...e]),index:0};$('pause').textContent='일시정지';}
function events(){if(!S.demo)return;while(S.demo.index<S.demo.events.length&&S.demo.events[S.demo.index][0]<=S.clock+1e-6){const [,type,val]=S.demo.events[S.demo.index++];if(type==='move')direction(val);else if(type==='drop')drop(val);else if(type==='ledgeJump')jumpFromLedge(val);else if(type==='dash')dash();else if(type==='attack')attack();else jump();}if(S.clock>=S.demo.end-1e-6)S.demo=null;}
function advance(ms){
  if(!S.ready)return;
  // Bounded steps make keyboard, demo events and clip boundaries deterministic.
  while(ms>1e-6){events();consumeBufferedJump();reconcile();const remaining=duration()-S.time;let dt=Math.min(ms,8,remaining);
    // Resolve buffered input at exact contact, even if a render step crosses it.
    if(S.mode==='jump'&&S.time<activeJump().flightEndMs)dt=Math.min(dt,activeJump().flightEndMs-S.time);
    if(S.mode==='jump'&&S.time<jumpFallBoundary())dt=Math.min(dt,jumpFallBoundary()-S.time);
    if(S.mode==='fall')dt=Math.min(dt,S.fallLandingMs===null?fallContactInMs():fallReg.landingMs+fallReg.recoveryMs-S.fallLandingMs);
    if(S.demo&&S.demo.index<S.demo.events.length)dt=Math.min(dt,S.demo.events[S.demo.index][0]-S.clock);
    if(dt<1e-6){events();break;}
    if(S.mode==='dash')S.x+=S.dashDir*dashReg.speedPxPerSecond*dt/1000;
    else if(S.mode==='run')S.x+=S.dir*200*dt/1000;
    else if(S.mode==='jump'||S.mode==='fall'){
      // Keep directional motion through takeoff and landing, just like running.
      S.x+=S.dir*200*dt/1000;
    }
    S.time+=dt;S.clock+=dt;ms-=dt;
    if(S.mode==='fall')advanceFall(dt);
    if(S.mode==='jump'&&S.time>=jumpFallBoundary()-1e-6)continueJumpAsFall();
    if(S.mode==='jump'&&S.time>=activeJump().flightEndMs)S.airDashUsed=false;
    consumeBufferedJump();
    if(S.mode==='fall'&&S.fallLandingMs!==null&&S.fallLandingMs>=fallReg.landingMs+fallReg.recoveryMs-1e-6)enter(S.dir?'run':'idle',S.dir?80:0);
    if(S.time>=duration()-1e-6){if(S.mode==='dash')finishDash();else if(S.mode==='start')enter(S.dir?'run':'idle',S.dir?160:0);else if(S.mode==='stop')enter(S.dir?'start':'idle');else if(S.mode==='jump')enter(S.dir?'run':'idle',S.dir?80:0);}
    pose();AirAttack.update(S,airContext());
  }events();draw();
}
function draw(){
  if(!S.ready)return;
  const w=canvas.width,h=canvas.height,ground=520+Math.max(0,-S.y-150);
  ctx.clearRect(0,0,w,h);const bg=ctx.createLinearGradient(0,0,0,h);bg.addColorStop(0,'#20272b');bg.addColorStop(1,'#303a3b');ctx.fillStyle=bg;ctx.fillRect(0,0,w,h);
  // Ground marks scroll with world position, while the character stays in view.
  const camera=S.x, root=w/2;
  ctx.strokeStyle='#465350';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(0,ground);ctx.lineTo(w,ground);ctx.stroke();
  ctx.fillStyle='#1b2224';ctx.fillRect(0,ground+1,w,h-ground);
  if(S.ledge){const edge=S.ledge.edge-camera+root,top=ground-S.ledge.height;ctx.fillStyle='#3e4e50';ctx.fillRect(S.ledge.direction>0?edge-210:edge,top,210,14);ctx.fillStyle='#9baea6';ctx.fillRect(S.ledge.direction>0?edge-210:edge,top,210,2);}
  for(let n=Math.floor((camera-root)/100);n<(camera+root)/100;n++){const x=n*100-camera+root;ctx.strokeStyle='#414e4b';ctx.beginPath();ctx.moveTo(x,ground+1);ctx.lineTo(x,ground+9);ctx.stroke();ctx.fillStyle='#73817d';ctx.font='11px system-ui';ctx.fillText(`${n*100}px`,x+5,ground+27);}
  ctx.fillStyle=`rgba(0,0,0,${Math.max(.06,0.2+0.12*(1-Math.abs(S.y)/130))})`;ctx.beginPath();ctx.ellipse(root,ground-1,Math.max(10,42-Math.abs(S.y)*0.12),6,0,0,Math.PI*2);ctx.fill();
  const group=S.mode==='start'||S.mode==='stop'?'stop':S.mode==='jump'?S.jumpVariant:S.mode;
  const airPose=S.mode==='dash'?S.dashReturn:null;
  const fallOffset=group==='fall'?fallVisualOffset():{x:0,y:0};
  const anchor=(airPose?airPose.anchor+dashReg.frames[S.frame].headCenter[0]-365:group==='dash'?dashReg.pivot[0]*dashReg.cellSize:['run','stop','jump'].includes(group)?340:368)-fallOffset.x;
  // Align the head when switching from an airborne pose to the lower dash pose.
  const poseY=airPose?airPose.headY-dashReg.frames[S.frame].headCenter[1]:fallOffset.y;
  ctx.save();ctx.translate(root,ground+S.y);ctx.scale(S.airAttack?S.airAttack.facing:S.facing,1);
  if(S.mode==='dash'){for(const offset of [48,24]){ctx.globalAlpha=(offset===48?0.06:0.12)*Math.min(1,S.time/30);ctx.drawImage(clips.dash[S.frame],-anchor-offset,-448+poseY);}ctx.globalAlpha=1;}
  const motion=group==='jump'||group==='tuck'?activeJump():group==='fall'?fallReg:null;
  const head=motion?motion.frames[S.frame].headCenter:null;
  if(!S.airAttack||!head||!AirAttack.draw(ctx,S,head[0]-anchor,head[1]-448+poseY))ctx.drawImage(clips[group][S.frame],-anchor,-448+poseY);ctx.restore();
  $('state').textContent=airPose?'Dash · 공중 대시':names[S.mode]+(S.mode==='jump'&&S.jumpVariant==='tuck'?' · 웅크리기':'');$('detail').textContent=`프레임 ${S.frame+1} / ${clips[group].length} · ${S.facing===1?'오른쪽':'왼쪽'} · 높이 ${Math.round(-S.y)}px`;
  if(S.airAttack){$('state').textContent=S.airAttack.kind==='rise'?'Air Attack · 상승 공격':'Air Attack · 낙하 공격';$('detail').textContent+=` · 공격 원화 ${S.airAttack.frame+1}/4`; }
  $('attack').classList.toggle('active',!!S.airAttack);
  $('scenario').textContent=S.demo?`자동 시연: ${S.demo.name}`:'직접 조작 · A / D / ← / → · Space 점프 · Shift 대시 · J 공중 공격';
  $('dash').classList.toggle('active',S.mode==='dash');
  $('log').textContent=S.history.slice(-6).map(s=>s.replaceAll('start','출발').replaceAll('stop','정지')).join('　·　');
  $('left').classList.toggle('active',S.dir<0);$('right').classList.toggle('active',S.dir>0);
}
function manual(){S.demo=null;}
function fromKeys(){direction((keys.has('ArrowRight')||keys.has('KeyD')?1:0)-(keys.has('ArrowLeft')||keys.has('KeyA')?1:0));}
window.addEventListener('keydown',e=>{if(!S.ready||/INPUT|SELECT|TEXTAREA/.test(e.target.tagName))return;if(['ArrowLeft','ArrowRight','KeyA','KeyD','Space','ShiftLeft','ShiftRight','KeyJ'].includes(e.code)){e.preventDefault();manual();if(e.code==='KeyJ'){if(!e.repeat)attack();}else if(e.code==='Space'){if(!e.repeat)jump();}else if(e.code.startsWith('Shift')){if(!e.repeat)dash();}else{keys.add(e.code);fromKeys();}}});
window.addEventListener('keyup',e=>{if(keys.delete(e.code))fromKeys();});
window.addEventListener('blur',()=>{keys.clear();S.jumpBufferedUntil=null;direction(0);});
for(const [id,key] of [['left','ArrowLeft'],['right','ArrowRight']]){
  const b=$(id);b.addEventListener('pointerdown',e=>{if(!S.ready)return;e.preventDefault();manual();b.setPointerCapture(e.pointerId);keys.add(key);fromKeys();});
  for(const event of ['pointerup','pointercancel','lostpointercapture'])b.addEventListener(event,()=>{keys.delete(key);fromKeys();});
}
for(const target of [canvas,$('attack')])target.addEventListener('pointerdown',e=>{if(e.button!==0)return;e.preventDefault();manual();attack();});
$('attack').onclick=e=>{if(e.detail===0){manual();attack();}};
$('jump').onclick=()=>{manual();jump();};$('dash').onclick=()=>{manual();dash();};$('reset').onclick=reset;
$('pause').onclick=()=>{S.playing=!S.playing;$('pause').textContent=S.playing?'일시정지':'계속 재생';};
$('speed').onchange=e=>{scale=Number(e.target.value);};
document.querySelectorAll('[data-demo]').forEach(b=>b.onclick=()=>demo(b.dataset.demo));
window.previewDebug={advance,reset,direction,jump,dash,drop,demo,draw,attack,jumpFromLedge};
async function init(){
  [jumpReg,tuckReg,stopReg,dashReg,fallReg]=await Promise.all(['../player-c-moving-jump-v1/registration.json','../player-c-jump-v2/registration.json','../player-c-run-idle-v1/registration.json','../player-c-dash-v2/registration.json','../player-c-fall-v1/registration.json'].map(async p=>{const r=await fetch(p);if(!r.ok)throw Error(p);return r.json();}));
  await Promise.all(Object.entries({run:['player-c-v6-rhythm',8],idle:['player-c-idle-v1',6],jump:['player-c-moving-jump-v1',jumpReg.frameCount],tuck:['player-c-jump-v2',tuckReg.frameCount],dash:['player-c-dash-v2',dashReg.frameCount],fall:['player-c-fall-v1',fallReg.frameCount],stop:['player-c-run-idle-v1',4]}).map(async([name,[folder,count]])=>{clips[name]=await Promise.all(Array.from({length:count},(_,i)=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>resolve(img);img.onerror=()=>reject(Error(`이미지 로드 실패: ${img.src}`));img.src=`../${folder}/frames/${String(i).padStart(2,'0')}.png`;})));}));
  await AirAttack.load();
  S.ready=true;const requested=new URLSearchParams(location.search).get('demo');demo(Object.hasOwn(demos,requested)?requested:'all');draw();
  requestAnimationFrame(function tick(t){const dt=last?Math.min(t-last,50):0;last=t;if(S.playing)advance(dt*scale);requestAnimationFrame(tick);});
}
init().catch(e=>{$('state').textContent='불러오기 실패';$('detail').textContent=e.message;console.error(e);});
