'use strict';
// Attacks replace the drawing only. The locomotion clock and trajectory keep running.
window.AirAttack=(()=>{
 const base=new URL('.',document.currentScript.src);let reg,images;
 async function load(){
  const response=await fetch(new URL('registration.json',base));if(!response.ok)throw Error('공중 공격 프레임 정보');reg=await response.json();
  images=Object.fromEntries(await Promise.all(['rise','fall'].map(async kind=>[kind,await Promise.all(reg.clips[kind].frames.map((_,i)=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>resolve(im);im.onerror=()=>reject(Error('공중 공격 원화'));im.src=new URL(`${kind}/frames/${String(i).padStart(2,'0')}.png`,base)})))])));
 }
 function trigger(state,context){
  if(!reg||!context.airborne||state.airAttack)return false;
  state.airAttack={kind:context.kind,startedAt:state.clock,facing:state.facing,frame:0};
  state.airAttackCount++;state.airAttackHistory.push({kind:context.kind,clock:state.clock,x:state.x,y:state.y});
  if(state.airAttackHistory.length>40)state.airAttackHistory.shift();return true;
 }
 function update(state,context){
  const attack=state.airAttack;if(!attack)return;
  const clip=reg.clips[attack.kind],elapsed=state.clock-attack.startedAt;
  if(!context.airborne||elapsed>=clip.durationMs){state.airAttack=null;return;}
  attack.frame=clip.frames.findIndex(f=>elapsed<f.startMs+f.durationMs);
 }
 function draw(ctx,state,headX,headY){
  const attack=state.airAttack;if(!attack)return false;
  const frame=reg.clips[attack.kind].frames[attack.frame];
  ctx.drawImage(images[attack.kind][attack.frame],headX-frame.headCenter[0],headY-frame.headCenter[1]);return true;
 }
 return {load,trigger,update,draw};
})();
