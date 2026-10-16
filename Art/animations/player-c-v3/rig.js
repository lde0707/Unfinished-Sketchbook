// Short-limbed run: controlled foot arcs and articulated painted limb pieces.
export const FRAME_COUNT=36, FPS=60, DURATION=FRAME_COUNT/FPS, GROUND=330;
const TAU=Math.PI*2, STANCE=.38, HIP_X=274;
const images={};
export const ready=Promise.all(['body','thigh','shin','foot'].map(name=>new Promise((resolve,reject)=>{
 const im=new Image();im.onload=()=>{images[name]=im;resolve()};im.onerror=reject;im.src=`${name}.png`;
})));
const wrap=p=>((p%1)+1)%1;
export function footPath(phase){
 const p=wrap(phase);
 if(p<STANCE)return {x:25-47*p/STANCE,y:0,angle:0,grounded:true};
 const u=(p-STANCE)/(1-STANCE),v=1-u;
 const tangent=47*(1-STANCE)/(3*STANCE);
 const x=v*v*v*(-22)+3*v*v*u*(-22-tangent)+3*v*u*u*(25+tangent)+u*u*u*25;
 const y=-34*Math.pow(Math.sin(Math.PI*u),2);
 return {x,y,angle:-.30*Math.sin(TAU*u)*Math.sin(Math.PI*u),grounded:false};
}
export function pose(phase){
 const p=wrap(phase),bob=2.5*Math.sin(2*TAU*p),hip={x:HIP_X,y:274+bob};
 return {phase:p,bob,hip,near:footPath(p),far:footPath(p+.5)};
}
function segment(ctx,image,a,b,width){
 const dx=b.x-a.x,dy=b.y-a.y,len=Math.hypot(dx,dy);
 ctx.save();ctx.translate(a.x,a.y);ctx.rotate(Math.atan2(dy,dx)-Math.PI/2);
 ctx.beginPath();ctx.roundRect(-width/2,-width*.35,width,len+width*.7,width/2);ctx.clip();
 // Use the painted middle of the part: overlapping end-cap outlines would look mechanical.
 ctx.drawImage(image,0,image.height*.22,image.width,image.height*.56,-width/2,-width*.35,width,len+width*.7);ctx.restore();
}
function joint(ctx,point,radius){
 const im=images.thigh;ctx.save();ctx.beginPath();ctx.arc(point.x,point.y,radius,0,TAU);ctx.clip();
 ctx.drawImage(im,im.width*.2,im.height*.35,im.width*.6,im.height*.3,point.x-radius,point.y-radius,radius*2,radius*2);ctx.restore();
}
export function solveLeg(hip,foot){
 const ankle={x:HIP_X+foot.x,y:GROUND-9+foot.y};
 const dx=ankle.x-hip.x,dy=ankle.y-hip.y,d=Math.hypot(dx,dy),l1=30,l2=29;
 if(d>=l1+l2)throw new Error(`Unreachable ankle: ${d}`);
 const a=(l1*l1-l2*l2+d*d)/(2*d),h=Math.sqrt(Math.max(0,l1*l1-a*a));
 return {hip,knee:{x:hip.x+dx/d*a+dy/d*h,y:hip.y+dy/d*a-dx/d*h},ankle};
}
function drawLeg(ctx,hip,foot,far){
 const {knee,ankle}=solveLeg(hip,foot);
 ctx.save();if(far)ctx.filter='brightness(0.68)';
 ctx.save();ctx.translate(ankle.x,ankle.y);ctx.rotate(foot.angle);ctx.drawImage(images.foot,-10,-12,32,21);ctx.restore();
 segment(ctx,images.thigh,hip,knee,17);segment(ctx,images.shin,knee,ankle,14);
 joint(ctx,knee,7.6);joint(ctx,ankle,5.8);ctx.restore();
}
function body(ctx,p){
 const im=images.body,s=.295,x=31,y=34+p.bob,w=im.width*s,h=im.height*s;
 // Only the trailing cloth receives a small delayed wave; head stays stable.
 for(let col=0;col<Math.ceil(w);col++){
  const influence=Math.pow(Math.max(0,1-col/240),1.6);
  const wave=4*influence*Math.sin(TAU*(p.phase*2-col/370)-.65),width=Math.min(1,w-col);
  ctx.drawImage(im,col/s,0,width/s,im.height,x+col,y+wave,width,h);
 }
}
export function render(ctx,phase,{flip=false,ground=false}={}){
 ctx.clearRect(0,0,420,420);const p=pose(phase);
 if(ground){ctx.strokeStyle='#343b43';ctx.beginPath();ctx.moveTo(22,GROUND);ctx.lineTo(398,GROUND);ctx.stroke()}
 ctx.save();if(flip){ctx.translate(420,0);ctx.scale(-1,1)}
 drawLeg(ctx,p.hip,p.far,true);drawLeg(ctx,p.hip,p.near,false);body(ctx,p);ctx.restore();
}
