// Preserve the painted upper-edge height signal separately from normalized color.
// Profiles span four tiles; cell boundaries never reset their height or phase.
const smooth=t=>(t=Math.max(0,Math.min(1,t)),t*t*(3-2*t));
const order=[0,1,2,0,2,1,2,1,0,2,0,1];
function extract(input,width=1536){
 const luminance=i=>input[i]*.2126+input[i+1]*.7152+input[i+2]*.0722;
 const edge=[];
 for(let x=0;x<width;x++){
  let peak=0,y=0;for(let j=0;j<40;j++)peak=Math.max(peak,luminance((j*width+x)*3));
  while(y<39&&luminance((y*width+x)*3)<peak*.76)y++;
  edge.push(y);
 }
 const blur=(values,r)=>values.map((_,x)=>{let sum=0,weight=0;for(let j=-r;j<=r;j++){const w=r+1-Math.abs(j);sum+=values[Math.max(0,Math.min(width-1,x+j))]*w;weight+=w}return sum/weight});
 // Round tiny spikes while keeping uneven, differently sized earth clumps.
 const fine=blur(edge,3),broad=blur(edge,13),signal=fine.map((v,i)=>v*.7+broad[i]*.3);
 const sorted=signal.slice().sort((a,b)=>a-b),lo=sorted[Math.floor(width*.05)],hi=sorted[Math.floor(width*.95)];
 if(hi-lo<.05)throw new Error('Painted edge has insufficient height variation');
 const heights=signal.map(v=>1.5+10.5*Math.max(0,Math.min(1,(v-lo)/(hi-lo))));
 const profiles=[];
 for(let k=0;k<3;k++)profiles.push(heights.slice(k*512,(k+1)*512).map((v,x)=>{
  const edge=Math.min(x,511-x);if(edge>=32)return v;
  const shared=heights[x<256?256+x:256+x-511],weight=smooth(edge/31);
  return shared*(1-weight)+v*weight;
 }));
 return {source:'Painted top-edge registration before color normalization',minimumInsetPixels:1.5,maximumInsetPixels:12,profiles};
}
function sample(profiles,t){const p=((t%(512*order.length))+512*order.length)%(512*order.length),slot=Math.floor(p/512),x=p%512,i=Math.floor(x),a=profiles[order[slot]][i],b=i<511?profiles[order[slot]][i+1]:profiles[order[(slot+1)%order.length]][0];return a+(b-a)*(x-i)}
module.exports={extract,sample};
