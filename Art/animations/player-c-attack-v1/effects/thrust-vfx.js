'use strict';
// Shared generated VFX layer: the body sprites and their animation timing stay intact.
window.ThrustVfx=(()=>{
 const assetUrl=new URL('thrust-spike.png',document.currentScript.src).href;
 let sprite;
 const anchors={standing:{2:[528,420],3:[562,408]},move:{2:[486,420],3:[530,418]}};
 const visible=state=>state.mode==='attack'&&state.clip===2&&(state.frame===2||state.frame===3);
 async function load(){sprite=await new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>resolve(im);im.onerror=()=>reject(Error('찌르기 이펙트를 읽지 못했습니다.'));im.src=assetUrl});}
 // Called inside the character's facing transform, before drawing the original body.
 function draw(ctx,state,variant){
  if(!sprite||!visible(state))return;
  const [x,y]=anchors[variant][state.frame],width=state.frame===2?140:200;
  // Crop transparent outer padding only; the generated alpha is preserved.
  ctx.drawImage(sprite,140,490,1000,300,x-384-width*.28,y-width*.16,width,width*.3);
 }
 return {load,draw,visible};
})();
