// Runtime canvas compositing: the authored sprite pixels remain unchanged.
const bodyCanvas=document.createElement('canvas');bodyCanvas.width=960;bodyCanvas.height=500;
const bodyContext=bodyCanvas.getContext('2d');
let feedbackTime=1,feedbackEnabled=true,impactX=525,impactFacing=1;
function startFeedback(){feedbackTime=0;impactX=x+facing*45;impactFacing=facing;}
function drawBody(draw){
 bodyContext.clearRect(0,0,960,500);draw(bodyContext);
 if(feedbackEnabled&&feedbackTime<.20){
  bodyContext.globalCompositeOperation='source-atop';
  // A muted warm first pulse followed by a softer rust tint; never hide the body.
  bodyContext.fillStyle=feedbackTime<.055?'rgba(219,150,129,.52)':`rgba(162,79,76,${.53*Math.max(0,1-(feedbackTime-.055)/.145)})`;
  bodyContext.fillRect(0,0,960,500);bodyContext.globalCompositeOperation='source-over';
 }
 ctx.drawImage(bodyCanvas,0,0);
}
function drawFeedback(){
 if(!feedbackEnabled||feedbackTime>.34)return;
 const t=feedbackTime;ctx.save();ctx.translate(impactX,292);ctx.scale(impactFacing,1);
 if(t<.09){
  ctx.globalAlpha=Math.max(0,1-t/.09);ctx.lineCap='round';
  const spread=1+Math.min(1,t/.025)*.28;ctx.scale(spread,spread);
  const impact=()=>{ctx.beginPath();ctx.moveTo(-43,19);ctx.lineTo(-8,-1);ctx.lineTo(-20,-32);ctx.lineTo(3,-8);ctx.lineTo(44,-27);ctx.lineTo(12,1);ctx.lineTo(25,29);ctx.lineTo(1,9);ctx.lineTo(-30,34);ctx.lineTo(-9,5);ctx.closePath()};
  impact();ctx.fillStyle='#34232d';ctx.fill();
  ctx.save();ctx.scale(.66,.66);impact();ctx.fillStyle='#d7b29a';ctx.fill();ctx.restore();
  ctx.scale(1/spread,1/spread);
 }
 for(let i=0;i<28;i++){
  const seed=Math.sin(i*71.83+12.4)*43758.5453,r=seed-Math.floor(seed),a=i*2.39996;
  const life=.19+r*.15;if(t>life)continue;
  const speed=85+r*190,dx=Math.cos(a)*speed*t-65*t,dy=Math.sin(a)*speed*t+110*t*t;
  ctx.globalAlpha=.85*Math.pow(1-t/life,.7);ctx.fillStyle=i%6===0?'#cbbda6':i%3===0?'#895663':'#2b2331';
  ctx.save();ctx.translate(dx,dy);ctx.rotate(a);ctx.beginPath();ctx.ellipse(0,0,(2+r*3)*(i<9?2.4:1),1.4+r*2.3,0,0,Math.PI*2);ctx.fill();ctx.restore();
 }
 ctx.restore();
}
