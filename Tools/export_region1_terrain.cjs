// Mechanical production export: approved v8 painted modules -> actual world Tilemap atlas.
// Artwork is sampled, never procedurally redrawn. The HTML collision grid is read-only.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
let sharp;try{sharp=require('sharp')}catch{sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'))}
const root=path.resolve(__dirname,'..'),out=path.join(root,'Book/Assets/Unfin/Art/Region1V8');
const d=JSON.parse(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Data/world12.json'))),W=d.regionX,H=d.height,N=128,DEPTH=384;
const cells=[];for(const run of d.mapRle.split(',')){const [v,n]=run.split('.').map(s=>parseInt(s,36));for(let i=0;i<n;i++)cells.push(v)}
assert.equal(cells.length,d.width*H);
assert.equal(crypto.createHash('sha256').update(Buffer.from(cells)).digest('hex'),d.gridSha256);
const ids=['01-paper-cavern','02-graphite-mine','03-sketch-tree','04-tangled-root-depths'];
const get=(x,y)=>x<0||y<0||x>=d.width||y>=H?1:cells[y*d.width+x];
const smooth=t=>(t=Math.max(0,Math.min(1,t)),t*t*(3-2*t)),mod=(a,b)=>(a%b+b)%b;
function mask(x,y){let m=(get(x,y-1)!==1?1:0)|(get(x+1,y)!==1?2:0)|(get(x,y+1)!==1?4:0)|(get(x-1,y)!==1?8:0);for(const [bit,a,b,dx,dy]of [[16,1,8,-1,-1],[32,1,2,1,-1],[64,4,2,1,1],[128,4,8,-1,1]])if(!(m&(a|b))&&get(x+dx,y+dy)!==1)m|=bit;return m}
function fillet(x,y){let m=0;for(const [bit,dx,dy]of [[1,-1,-1],[2,1,-1],[4,1,1],[8,-1,1]])if(get(x+dx,y)===1&&get(x,y+dy)===1&&get(x+dx,y+dy)===1)m|=bit;return m}
// Trace oriented solid/air boundaries. Solid lies to the right of every edge.
const edges=[],starts=new Map();
function edge(x,y,dx,dy){const e={x:x*N,y:y*N,dx,dy};edges.push(e);const k=`${x},${y}`;if(!starts.has(k))starts.set(k,[]);starts.get(k).push(e)}
for(let y=0;y<H;y++)for(let x=0;x<Math.min(d.width,W+4);x++)if(get(x,y)===1){
 if(get(x,y-1)!==1)edge(x,y,1,0);if(get(x+1,y)!==1)edge(x+1,y,0,1);
 if(get(x,y+1)!==1)edge(x+1,y+1,-1,0);if(get(x-1,y)!==1)edge(x,y+1,0,-1);
}
for(const e of edges){const next=starts.get(`${e.x/N+e.dx},${e.y/N+e.dy}`)||[];e.next=next.slice().sort((a,b)=>(e.dx*b.dy-e.dy*b.dx)-(e.dx*a.dy-e.dy*a.dx))[0];if(e.next)e.next.prev=e}
const primitives=[];
for(const e of edges){
 const turn=e.next?e.dx*e.next.dy-e.dy*e.next.dx:0,prev=e.prev?e.prev.dx*e.dy-e.prev.dy*e.dx:0;
 const r=turn>0?28:turn<0?24:0,r0=prev>0?28:prev<0?24:0;
 const ax=e.x+e.dx*r0,ay=e.y+e.dy*r0,bx=e.x+e.dx*(N-r),by=e.y+e.dy*(N-r);
 primitives.push({kind:0,ax,ay,bx,by,dx:e.dx,dy:e.dy,len:N-r-r0});
 if(r){const cx=bx+e.next.dx*r,cy=by+e.next.dy*r;primitives.push({kind:1,ax:bx,ay:by,bx:e.x+e.dx*N+e.next.dx*r,by:e.y+e.dy*N+e.next.dy*r,cx,cy,r,angle:Math.atan2(by-cy,bx-cx),turn})}
}
// Bucket nearby boundary pieces once, then evaluate a shared 4px distance field.
const buckets=Array.from({length:W*H},()=>[]);
for(const p of primitives){const x0=Math.max(0,Math.floor((Math.min(p.ax,p.bx)-DEPTH)/N)),x1=Math.min(W-1,Math.floor((Math.max(p.ax,p.bx)+DEPTH)/N));const y0=Math.max(0,Math.floor((Math.min(p.ay,p.by)-DEPTH)/N)),y1=Math.min(H-1,Math.floor((Math.max(p.ay,p.by)+DEPTH)/N));for(let y=y0;y<=y1;y++)for(let x=x0;x<=x1;x++)buckets[y*W+x].push(p)}
function nearest(x,y,list){let best=DEPTH*DEPTH,weight=1;for(const p of list){let qx,qy;if(p.kind===0){const t=Math.max(0,Math.min(p.len,(x-p.ax)*p.dx+(y-p.ay)*p.dy));qx=p.ax+p.dx*t;qy=p.ay+p.dy*t}else{let a=Math.atan2(y-p.cy,x-p.cx)-p.angle;while(a>Math.PI)a-=2*Math.PI;while(a< -Math.PI)a+=2*Math.PI;a=p.angle+Math.max(0,Math.min(Math.PI/2,a*p.turn))*p.turn;qx=p.cx+Math.cos(a)*p.r;qy=p.cy+Math.sin(a)*p.r}const dx=x-qx,dy=y-qy,dd=dx*dx+dy*dy;if(dd<best){best=dd;weight=Math.abs(dy)/Math.max(.00001,Math.abs(dx)+Math.abs(dy))}}return [Math.sqrt(best),weight]}
const rooms=d.rooms.filter(r=>r.r===1),zoneVertices=[];
for(let y=0;y<=H;y++)for(let x=0;x<=W;x++){const dist=[Infinity,Infinity,Infinity,Infinity];for(const r of rooms){const k='ABCD'.indexOf(r.z);if(k<0)continue;dist[k]=Math.min(dist[k],Math.hypot(Math.max(r.x-x,0,x-(r.x+r.w)),Math.max(r.y-y,0,y-(r.y+r.h))))}const min=Math.min(...dist),weights=dist.map(v=>smooth(1-(v-min)/4)),sum=weights.reduce((a,b)=>a+b,0);zoneVertices.push(weights.map(v=>v/sum))}
const order=[0,1,2,0,2,1,2,1,0,2,0,1];
const samples=[];
const relief=require('./terrain_relief.cjs'),profiles=[];
function sample(zone,t,depth,c){const q=mod(Math.floor(t),512*order.length),k=order[Math.floor(q/512)],x=q%512,y=Math.min(383,Math.max(0,Math.floor(depth)));return samples[zone][k][(y*512+x)*4+c]}
function inside(x,y,px,py,solid,m){for(let corner=0;corner<4;corner++){const cx=corner===1||corner===2?N-px:px,cy=corner<2?py:N-py;const faces=[9,3,6,12][corner];if(solid&&(m&faces)===faces&&cx<28&&cy<28&&Math.hypot(28-cx,28-cy)>28)return false;if(!solid&&(m&(1<<corner))&&cx<24&&cy<24&&Math.hypot(24-cx,24-cy)>=24)return true}return solid}
const PAD=2,PITCH=N+PAD*2,COLS=15,ATLAS=COLS*PITCH,PER=COLS*COLS;
async function main(){
 fs.mkdirSync(out,{recursive:true});
 for(const id of ids){const list=[];for(const v of 'ABC')list.push(await sharp(path.join(root,'Art/samples/region1-tiles-v8',id,`module-${v}.png`)).ensureAlpha().raw().toBuffer());samples.push(list);profiles.push(JSON.parse(fs.readFileSync(path.join(root,'Art/samples/region1-tiles-v8',id,'surface-profile.json'))).profiles)}
 const pool=[],hashes=new Map(),entries=[],zoneCounts=[0,0,0,0];let visible=0,fillets=0;
 const blank=Buffer.alloc(N*N*4);for(let i=0;i<blank.length;i+=4){blank[i]=5;blank[i+1]=6;blank[i+2]=8;blank[i+3]=255}
 function add(b){const h=crypto.createHash('sha256').update(b).digest('hex');if(!hashes.has(h)){hashes.set(h,pool.length);pool.push(b)}return hashes.get(h)}
 const dark=add(blank);
 for(let y=0;y<H;y++){
 for(let x=0;x<W;x++){
  const solid=get(x,y)===1,m=solid?mask(x,y):fillet(x,y);if(!solid&&!m)continue;
  const list=buckets[y*W+x];let id=dark;
  if(list.length){
   const field=new Float32Array(33*33*2);for(let j=0;j<=32;j++)for(let i=0;i<=32;i++){const v=nearest(x*N+i*4,y*N+j*4,list),o=(j*33+i)*2;field[o]=v[0];field[o+1]=v[1]}
   const z=[zoneVertices[y*(W+1)+x],zoneVertices[y*(W+1)+x+1],zoneVertices[(y+1)*(W+1)+x],zoneVertices[(y+1)*(W+1)+x+1]];
   const b=Buffer.alloc(N*N*4);let nonblack=false;
   for(let py=0;py<N;py++)for(let px=0;px<N;px++){
    const fx=(px+.5)/4,fy=(py+.5)/4,ix=Math.floor(fx),iy=Math.floor(fy),u=fx-ix,v=fy-iy,o=(py*N+px)*4;
    const f=(iy*33+ix)*2;let depth=(field[f]*(1-u)+field[f+2]*u)*(1-v)+(field[f+66]*(1-u)+field[f+68]*u)*v;
    let hw=(field[f+1]*(1-u)+field[f+3]*u)*(1-v)+(field[f+67]*(1-u)+field[f+69]*u)*v;hw=smooth(hw);
    const gx=x*N+px+.5,gy=y*N+py+.5;
    const zx=(px+.5)/N,zy=(py+.5)/N,weights=z[0].map((a,k)=>(a*(1-zx)+z[1][k]*zx)*(1-zy)+(z[2][k]*(1-zx)+z[3][k]*zx)*zy);
    let rim=0;for(let k=0;k<4;k++)if(weights[k]>.00001)rim+=weights[k]*(relief.sample(profiles[k],gx)*hw+relief.sample(profiles[k],gy)*(1-hw));
    const sign=inside(x,y,px+.5,py+.5,solid,m)?1:-1,alpha=Math.max(0,Math.min(1,sign*depth-rim+.5));
    if(!alpha)continue;depth=Math.max(0,depth-rim);
    for(let c=0;c<3;c++){let value=0;for(let k=0;k<4;k++)if(weights[k]>.00001)value+=weights[k]*(sample(k,gx,depth,c)*hw+sample(k,gy,depth,c)*(1-hw));b[o+c]=Math.round(value)}b[o+3]=Math.round(alpha*255);
    if(depth<DEPTH-1)nonblack=true;
   }
   id=solid&&!nonblack?dark:add(b);if(solid&&nonblack)visible++;
  }
  const weights=zoneVertices[y*(W+1)+x],zone=weights.indexOf(Math.max(...weights));if(solid)zoneCounts[zone]++;else fillets++;
  entries.push({x,y,id,mask:m,overlay:!solid,zone});
 }
 if(y%20===0)console.log(`row ${y}/${H}, unique ${pool.length}`);
 }
 const atlases=[];
 for(let start=0;start<pool.length;start+=PER){const raw=Buffer.alloc(ATLAS*ATLAS*4),count=Math.min(PER,pool.length-start);for(let i=0;i<count;i++){const b=pool[start+i],ox=i%COLS*PITCH,oy=Math.floor(i/COLS)*PITCH;for(let y=-PAD;y<N+PAD;y++)for(let x=-PAD;x<N+PAD;x++){const src=(Math.max(0,Math.min(N-1,y))*N+Math.max(0,Math.min(N-1,x)))*4,dst=((oy+y+PAD)*ATLAS+ox+x+PAD)*4;b.copy(raw,dst,src,src+4)}}const name=`atlas-${String(atlases.length).padStart(2,'0')}.png`;await sharp(raw,{raw:{width:ATLAS,height:ATLAS,channels:4}}).png().toFile(path.join(out,name));atlases.push(name)}
 // Only remove obsolete numbered outputs owned by this exporter.
 for(const name of fs.readdirSync(out))if(/^atlas-\d+\.png(?:\.meta)?$/.test(name)&&!atlases.includes(name.replace(/\.meta$/,'')))fs.unlinkSync(path.join(out,name));
 const manifest={version:8,gridSha256:d.gridSha256,tileSize:N,width:W,height:H,atlasSize:ATLAS,columns:COLS,pitch:PITCH,padding:PAD,tilesPerAtlas:PER,tileCount:pool.length,atlases,zones:ids,entries};
 fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify(manifest));
 const report={status:'passed',gridSha256:d.gridSha256,sourceGridChanged:false,region1Rooms:rooms.length,zoneCounts,uniqueTiles:pool.length,atlasCount:atlases.length,visibleTerrainCells:visible,terrainCells:entries.filter(e=>!e.overlay).length,possibleFilletCells:fillets,tilePixels:N,pixelsPerUnit:N,materialDepthPixels:DEPTH,outerRadiusPixels:28,innerRadiusPixels:24,transitionWidthCells:4,atlasPaddingPixels:PAD,scope:'All World 1 ordinary terrain, including ceiling, walls, steps, narrow columns and room transitions. World 2 and special gameplay tiles retained.'};
 assert.equal(report.terrainCells,cells.filter((v,i)=>i%d.width<W&&v===1).length);assert(zoneCounts.every(n=>n>0));
 fs.writeFileSync(path.join(root,'Book/Validation/region1-v8-export.json'),JSON.stringify(report,null,2)+'\n');console.log(report);
}
main().catch(e=>{console.error(e);process.exitCode=1});
