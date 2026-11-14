const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const R='Book/Assets/Unfin/Resources',backup='Builds/art-slice-backup-20261025',report=[];
function save(p,b){if(fs.existsSync(p)&&fs.readFileSync(p).equals(b))return;const q=path.join(backup,p);if(fs.existsSync(p)&&!fs.existsSync(q)){fs.mkdirSync(path.dirname(q),{recursive:true});fs.copyFileSync(p,q);}fs.writeFileSync(p+'.tmp',b);fs.renameSync(p+'.tmp',p);}
async function box(buf,threshold=180){const {data,info}=await sharp(buf).ensureAlpha().raw().toBuffer({resolveWithObject:true});let l=info.width,t=info.height,r=-1,b=-1;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>threshold){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}assert(r>=l);return {l,t,r,b};}
async function extract(source,rect,dest,size){const crop=await sharp(source).extract(rect).png().toBuffer();const b=await box(crop,32);const left=Math.max(0,b.l-3),top=Math.max(0,b.t-3),right=Math.min(rect.width,b.r+4),bottom=Math.min(rect.height,b.b+4);const out=await sharp(crop).extract({left,top,width:right-left,height:bottom-top}).resize(size-16,size-16,{fit:'inside'}).extend({top:8,bottom:8,left:8,right:8,background:'#00000000'}).png().toBuffer();save(dest,out);report.push({source,rect,dest});}
(async()=>{
// Layout follows the actual artwork, not an assumed 4x3 uniform grid.
const bx=[[0,400,800,1220,1448],[0,395,790,1120,1448],[0,375,690,1020,1448]],by=[0,440,746,1086];
for(let i=0;i<12;i++){let row=i/4|0,col=i%4;await extract('Art/samples/boss-region1-v2/effects-sheet.png',{left:bx[row][col],top:by[row],width:bx[row][col+1]-bx[row][col],height:by[row+1]-by[row]},R+'/BossRegion1/Effects/'+String(i).padStart(2,'0')+'.png',512);}
// Each animated prop row has its own vertical spacing. Preserve a shared
// crop and scale across its four frames so the animation does not pulse in size.
for(const [sheet,names,ys] of [
 ['Props',['save','station','sw_r','sw_b','sw_y','items'],[0,270,550,775,985,1210,1536]],
 ['Devices',['brazier','bar','target','diary','gate','bubble'],[0,248,485,735,975,1310,1536]]]){
 const source='Art/samples/region2-v1/sources/'+sheet+'.png';
 for(let row=0;row<6;row++){
  const crops=[];for(let col=0;col<4;col++)crops.push(await sharp(source).extract({left:col*256,top:ys[row],width:256,height:ys[row+1]-ys[row]}).png().toBuffer());
  const boxes=await Promise.all(crops.map(c=>box(c,190)));let l=Math.max(0,Math.min(...boxes.map(b=>b.l))-6),t=Math.max(0,Math.min(...boxes.map(b=>b.t))-6),r=Math.min(256,Math.max(...boxes.map(b=>b.r))+7),b=Math.min(ys[row+1]-ys[row],Math.max(...boxes.map(b=>b.b))+7);
  for(let col=0;col<4;col++){
   const name=names[row]==='items'?['heart','shard','healup','ability'][col]:names[row],frame=names[row]==='items'?0:col,dest=R+'/Region2/Props/'+name+'/'+String(frame).padStart(2,'0')+'.png';
   let rect={left:l,top:t,width:r-l,height:b-t};if(names[row]==='items'){const z=boxes[col];rect={left:Math.max(0,z.l-6),top:Math.max(0,z.t-6),width:Math.min(256,z.r+7)-Math.max(0,z.l-6),height:Math.min(ys[row+1]-ys[row],z.b+7)-Math.max(0,z.t-6)};}
   const out=await sharp(crops[col]).extract(rect).resize(248,248,{fit:'inside'}).extend({top:4,bottom:4,left:4,right:4,background:'#00000000'}).png().toBuffer();save(dest,out);report.push({source,row,col,yRange:[ys[row],ys[row+1]],rect,dest});
  }
 }
}
// The spear effect is deliberately wider than one nominal column.
const ex=[[0,400,740,1080,1448],[0,395,745,1100,1448],[0,400,905,1170,1448]],ey=[0,375,737,1086];
for(let i=0;i<12;i++){const y=i/4|0,x=i%4;await extract('Art/samples/region2-v1/sources/Effects.png',{left:ex[y][x],top:ey[y],width:ex[y][x+1]-ex[y][x],height:ey[y+1]-ey[y]},R+'/Region2/Effects/'+String(i).padStart(2,'0')+'.png',256);}
fs.mkdirSync('Book/Validation',{recursive:true});fs.writeFileSync('Book/Validation/art-slice-repair.json',JSON.stringify({sourceArtPreserved:true,backup,assets:report},null,2));console.log('SLICE_REPAIR_OK '+report.length);
})().catch(e=>{console.error(e);process.exitCode=1});
