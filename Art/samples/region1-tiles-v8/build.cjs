// Mechanical tile export and tilemap assembly. All material artwork comes from image_gen.
// Pre-bake curved joins into reusable 128px cells; preview PNGs are rebuilt ONLY from the exported atlas.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
let sharp;try{sharp=require('sharp')}catch{sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'))}
const root=__dirname,N=128,MW=512,D=384,W=24*N,H=7*N;
const spec=JSON.parse(fs.readFileSync(path.join(root,'prompts.json'),'utf8'));
const smooth=t=>(t=Math.max(0,Math.min(1,t)),t*t*(3-2*t));
const mod=(a,b)=>(a%b+b)%b;
const lum=(a,i)=>a[i]*.2126+a[i+1]*.7152+a[i+2]*.0722;
const png=(raw,w,h)=>sharp(raw,{raw:{width:w,height:h,channels:4}}).png().toBuffer();

async function material(file){
  const input=await sharp(file).resize(1536,D,{fit:'fill'}).removeAlpha().raw().toBuffer();
  const normalized=Buffer.alloc(1536*D*4);
  // Register the generated surface to the same physical edge. Ignore dark air slivers.
  for(let x=0;x<1536;x++){
    let edge=0,peak=0;
    for(let y=0;y<40;y++)peak=Math.max(peak,lum(input,(y*1536+x)*3));
    for(let y=0;y<40;y++)if(lum(input,(y*1536+x)*3)>=peak*.76){edge=y;break;}
    for(let y=0;y<D;y++){
      const sy=Math.min(D-1,Math.round(edge+y*(D-1-edge)/(D-1))),i=(sy*1536+x)*3,o=(y*1536+x)*4;
      const end=smooth((y-(D-32))/31);
      for(let c=0;c<3;c++)normalized[o+c]=Math.round(input[i+c]*(1-end)+[5,6,8][c]*end);
      normalized[o+3]=255;
    }
  }
  const modules=[];
  for(let k=0;k<3;k++){
    const b=Buffer.alloc(MW*D*4);
    for(let y=0;y<D;y++)normalized.copy(b,y*MW*4,(y*1536+k*MW)*4,(y*1536+(k+1)*MW)*4);
    modules.push(b);
  }
  // Shared painted 32px transition only at 4-cell module endpoints, never at every cell.
  for(const b of modules)for(let y=0;y<D;y++)for(let x=0;x<MW;x++){
    const edge=Math.min(x,MW-1-x);if(edge>=32)continue;
    const mix=smooth(edge/31),sx=x<MW/2?256+x:256+x-(MW-1);
    const i=(y*MW+x)*4,j=(y*1536+sx)*4;
    for(let c=0;c<3;c++)b[i+c]=Math.round(b[i+c]*mix+normalized[j+c]*(1-mix));
  }
  for(const a of modules)for(const b of modules)for(let y=0;y<D;y++)for(let c=0;c<4;c++)
    assert.equal(a[(y*MW+MW-1)*4+c],b[y*MW*4+c],'Every module endpoint must match');
  return modules;
}

function profile(points){
  const primitives=[];let length=0;
  function line(a,b){const dx=b[0]-a[0],dy=b[1]-a[1],len=Math.hypot(dx,dy);if(len>0){primitives.push({a,b,len,start:length,kind:'line'});length+=len;}}
  let last=points[0];
  for(let i=1;i<points.length-1;i++){
    const prev=points[i-1],p=points[i],next=points[i+1];
    const l1=Math.hypot(p[0]-prev[0],p[1]-prev[1]),l2=Math.hypot(next[0]-p[0],next[1]-p[1]);
    const u=[(p[0]-prev[0])/l1,(p[1]-prev[1])/l1],v=[(next[0]-p[0])/l2,(next[1]-p[1])/l2];
    const r=Math.min(28,l1/2,l2/2),a=[p[0]-u[0]*r,p[1]-u[1]*r],b=[p[0]+v[0]*r,p[1]+v[1]*r];
    const center=[a[0]+v[0]*r,a[1]+v[1]*r],angle=Math.atan2(a[1]-center[1],a[0]-center[0]);
    const sweep=(u[0]*v[1]-u[1]*v[0])*Math.PI/2;
    line(last,a);primitives.push({kind:'arc',a,b,center,r,angle,sweep,len:r*Math.PI/2,start:length});length+=r*Math.PI/2;last=b;
  }
  line(last,points.at(-1));
  const tops=new Float64Array(W);
  for(let x=0;x<W;x++){
    const px=x+.5;let top=H;
    for(const s of primitives){
      if(s.kind==='line'&&s.a[0]!==s.b[0]&&px>=s.a[0]&&px<=s.b[0])top=s.a[1];
      if(s.kind==='arc'&&px>=s.a[0]&&px<=s.b[0]){
        const dy=Math.sqrt(Math.max(0,s.r*s.r-(px-s.center[0])**2));
        const sign=Math.sign(Math.sin(s.angle+s.sweep/2));top=s.center[1]+sign*dy;
      }
    }
    tops[x]=top;
  }
  return {primitives,tops};
}
function closest(x,y,primitives){
  let best=Infinity,t=0,nx=0,ny=1;
  for(const s of primitives){
    let qx,qy,f;
    if(s.kind==='line'){
      const dx=s.b[0]-s.a[0],dy=s.b[1]-s.a[1];f=Math.max(0,Math.min(1,((x-s.a[0])*dx+(y-s.a[1])*dy)/(s.len*s.len)));
      qx=s.a[0]+f*dx;qy=s.a[1]+f*dy;
    }else{
      let delta=Math.atan2(y-s.center[1],x-s.center[0])-s.angle;
      while(delta>Math.PI)delta-=Math.PI*2;while(delta< -Math.PI)delta+=Math.PI*2;
      f=Math.max(0,Math.min(1,delta/s.sweep));const angle=s.angle+f*s.sweep;
      qx=s.center[0]+s.r*Math.cos(angle);qy=s.center[1]+s.r*Math.sin(angle);
    }
    const d=(x-qx)**2+(y-qy)**2;if(d<best){best=d;t=s.start+f*s.len;nx=x-qx;ny=y-qy;}
  }
  return [Math.sqrt(best),t,Math.abs(ny)/Math.max(.0001,Math.abs(nx)+Math.abs(ny))];
}
const order=[0,1,2,0,2,1,2,1,0,2,0,1];
function sample(modules,t,d){
  const tx=Math.floor(Math.max(0,t)),k=order[Math.floor(tx/MW)%order.length],x=tx%MW;
  const y=Math.min(D-1,Math.max(0,Math.floor(d)));
  return {b:modules[k],i:(y*MW+x)*4};
}
function bake(modules,points){
  const {primitives,tops}=profile(points),raw=Buffer.alloc(W*H*4);
  for(let x=0;x<W;x++)for(let y=Math.max(0,Math.floor(tops[x])-1);y<H;y++){
    const [depth,t,horizontalWeight]=closest(x+.5,y+.5,primitives);
    // Sub-pixel contact variation is periodic over FOUR cells and shares endpoints.
    const q=mod(t,MW),rim=(1.3+.7*Math.sin(q*.061)+.6*Math.sin(q*.137))*smooth(Math.min(q,MW-q)/20);
    const inside=(y+.5>=tops[x]?depth:-depth)-rim,alpha=Math.max(0,Math.min(1,inside+.5));
    if(alpha===0)continue;
    // Blend horizontal/vertical projections around corners instead of stretching a
    // single arc-length texel into a radial streak through the whole ground depth.
    const a=sample(modules,x,Math.max(0,depth-rim)),b=sample(modules,y,Math.max(0,depth-rim)),o=(y*W+x)*4;
    const weight=smooth(horizontalWeight);
    for(let c=0;c<3;c++)raw[o+c]=Math.round(a.b[a.i+c]*weight+b.b[b.i+c]*(1-weight));raw[o+3]=Math.round(alpha*255);
  }
  return raw;
}
function split(raw,pool,ids){
  const cells=[];
  for(let ty=0;ty<H/N;ty++)for(let tx=0;tx<W/N;tx++){
    const b=Buffer.alloc(N*N*4);
    for(let y=0;y<N;y++)raw.copy(b,y*N*4,((ty*N+y)*W+tx*N)*4,((ty*N+y)*W+tx*N+N)*4);
    if(!b.some((v,i)=>i%4===3&&v)){cells.push(-1);continue;}
    const hash=crypto.createHash('sha256').update(b).digest('hex');
    if(!ids.has(hash)){ids.set(hash,pool.length);pool.push(b);}
    cells.push(ids.get(hash));
  }
  return cells;
}
function fromAtlas(atlas,aw,cells){
  const raw=Buffer.alloc(W*H*4);
  cells.forEach((id,pos)=>{
    if(id<0)return;
    const tx=pos%24,ty=Math.floor(pos/24),sx=id%12*N,sy=Math.floor(id/12)*N;
    for(let y=0;y<N;y++)atlas.copy(raw,((ty*N+y)*W+tx*N)*4,((sy+y)*aw+sx)*4,((sy+y)*aw+sx+N)*4);
  });return raw;
}
function gridSvg(cells){
  let s=`<svg width="${W}" height="${H}" xmlns="http://www.w3.org/2000/svg"><g stroke="#8dcad8" stroke-opacity=".42" stroke-width="1">`;
  for(let x=0;x<=W;x+=N)s+=`<path d="M${x} 0V${H}"/>`;
  for(let y=0;y<=H;y+=N)s+=`<path d="M0 ${y}H${W}"/>`;
  s+='</g><g fill="#bae6ee" font-family="Arial" font-size="18">';
  cells.forEach((id,pos)=>{if(id>=0)s+=`<text x="${pos%24*N+7}" y="${Math.floor(pos/24)*N+22}">${id}</text>`});
  return Buffer.from(s+'</g></svg>');
}
async function main(){
  const reports=[],overview=[],proof=[];
  const layouts={
    straight:[[0,3*N],[W,3*N]],
    steps:[[0,3*N],[12*N,3*N],[12*N,2*N],[16*N,2*N],[16*N,4*N],[20*N,4*N],[20*N,3*N],[W,3*N]]
  };
  for(let ri=0;ri<spec.regions.length;ri++){
    const region=spec.regions[ri],out=path.join(root,region.id);fs.mkdirSync(path.join(out,'tiles'),{recursive:true});
    // Remove only this exporter's numbered tile files before a fresh atlas export.
    for(const file of fs.readdirSync(path.join(out,'tiles')))if(/^\d{3}\.png$/.test(file))fs.unlinkSync(path.join(out,'tiles',file));
    const modules=await material(path.join(root,'sources',region.id+'.png'));
    for(let k=0;k<3;k++)await sharp(modules[k],{raw:{width:MW,height:D,channels:4}}).png().toFile(path.join(out,`module-${'ABC'[k]}.png`));
    const pool=[],ids=new Map(),maps={},baked={};
    for(const [name,points]of Object.entries(layouts)){baked[name]=bake(modules,points);maps[name]=split(baked[name],pool,ids);}
    const aw=12*N,ah=Math.ceil(pool.length/12)*N,atlas=Buffer.alloc(aw*ah*4);
    for(let id=0;id<pool.length;id++){
      const sx=id%12*N,sy=Math.floor(id/12)*N;
      for(let y=0;y<N;y++)pool[id].copy(atlas,((sy+y)*aw+sx)*4,y*N*4,(y+1)*N*4);
      fs.writeFileSync(path.join(out,'tiles',String(id).padStart(3,'0')+'.png'),await png(pool[id],N,N));
    }
    fs.writeFileSync(path.join(out,'atlas.png'),await png(atlas,aw,ah));
    // Reload the exported PNG. No full-scene concept artwork is used by the final assembly.
    const reread=await sharp(path.join(out,'atlas.png')).ensureAlpha().raw().toBuffer();
    const counts=new Map();for(const cells of Object.values(maps))for(const id of cells)if(id>=0)counts.set(id,(counts.get(id)||0)+1);
    // Repeated terrain must be truly identical PNG tiles, not just reused dark fill.
    assert.deepEqual(maps.straight.slice(3*24,3*24+4),maps.straight.slice(3*24+12,3*24+16),'A surface module reuses the same four tile IDs');
    for(const [name,cells]of Object.entries(maps)){
      const assembled=fromAtlas(reread,aw,cells);assert(assembled.equals(baked[name]),'Atlas reconstruction must be pixel-exact');
      const fg=await png(assembled,W,H);
      const clean=await sharp({create:{width:W,height:H,channels:4,background:'#202227'}}).composite([{input:fg}]).png().toBuffer();
      fs.writeFileSync(path.join(out,`${name}-connected.png`),clean);
      const grid=await sharp(clean).composite([{input:gridSvg(cells)}]).png().toBuffer();fs.writeFileSync(path.join(out,`${name}-grid.png`),grid);
      fs.writeFileSync(path.join(out,`${name}-tilemap.json`),JSON.stringify({tileSize:N,width:24,height:7,atlas:'atlas.png',atlasColumns:12,emptyTile:-1,cells},null,2)+'\n');
      if(name==='steps'){
        const label=Buffer.from(`<svg width="1536" height="46" xmlns="http://www.w3.org/2000/svg"><rect width="1536" height="46" fill="#111318"/><text x="20" y="31" fill="#dce0e4" font-family="Malgun Gothic,Arial" font-size="24">${ri+1}. ${region.name} · 128px 타일 · 바닥 / 벽 / 둥근 단차</text></svg>`);
        overview.push({input:label,left:0,top:ri*494},{input:await sharp(clean).resize(1536,448).toBuffer(),left:0,top:ri*494+46});
        proof.push({input:label,left:0,top:ri*494},{input:await sharp(grid).resize(1536,448).toBuffer(),left:0,top:ri*494+46});
      }
    }
    const placements=[...counts.values()].reduce((a,b)=>a+b,0),reused=[...counts.values()].filter(n=>n>1).length;
    assert(reused>=24,'Demo must actually reuse many identical tile images');
    const report={id:region.id,name:region.name,tileSize:N,uniqueTiles:pool.length,placementsAcrossTwoMaps:placements,reusedTileIds:reused,repeatedPlacements:placements-pool.length,moduleCells:[4,3],modules:3,endpointSeams:'exact RGBA match for all 9 module pairings',assembly:'PNG atlas reloaded; both layouts reconstructed pixel-exactly from tile IDs',limitations:'Demonstration tiles baked for these two layouts; not a complete arbitrary-layout autotile set. Macro modules must keep their internal tile order.'};
    reports.push(report);console.log(JSON.stringify(report));
  }
  for(const [name,layers]of [['all-regions-connected',overview],['all-regions-grid',proof]])await sharp({create:{width:1536,height:1976,channels:4,background:'#111318'}}).composite(layers).png().toFile(path.join(root,name+'.png'));
  fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify(reports,null,2)+'\n');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
