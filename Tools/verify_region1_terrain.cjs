const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
let sharp;try{sharp=require('sharp')}catch{sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'))}
const root=path.resolve(__dirname,'..'),dir=path.join(root,'Book/Assets/Unfin/Art/Region1V8');
async function main(){
 const m=JSON.parse(fs.readFileSync(path.join(dir,'manifest.json'))),d=JSON.parse(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Data/world12.json')));
 assert.equal(m.gridSha256,d.gridSha256);assert.equal(m.tileSize,128);
 const cells=[];for(const s of d.mapRle.split(',')){const [v,n]=s.split('.').map(x=>parseInt(x,36));for(let i=0;i<n;i++)cells.push(v)}
 const ground=new Map(),overlay=new Map(),counts=new Map();
 for(const e of m.entries){assert(e.id>=0&&e.id<m.tileCount);const pos=e.y*m.width+e.x,key=e.overlay?overlay:ground;assert(!key.has(pos));key.set(pos,e);if(!e.overlay)assert.equal(cells[e.y*d.width+e.x],1);counts.set(e.id,(counts.get(e.id)||0)+1)}
 let compared=0;for(let y=0;y<m.height;y++)for(let x=0;x<m.width;x++){assert.equal(ground.has(y*m.width+x),cells[y*d.width+x]===1);compared++}
 let paddedPixels=0;
 const centerAlphas=[];
 for(let a=0;a<m.atlases.length;a++){
  const {data,info}=await sharp(path.join(dir,m.atlases[a])).ensureAlpha().raw().toBuffer({resolveWithObject:true});assert.equal(info.width,m.atlasSize);assert.equal(info.height,m.atlasSize);
  for(let slot=0;slot<m.tilesPerAtlas&&a*m.tilesPerAtlas+slot<m.tileCount;slot++){
   const ox=slot%m.columns*m.pitch+m.padding,oy=Math.floor(slot/m.columns)*m.pitch+m.padding;
   centerAlphas.push(data[((oy+64)*info.width+ox+64)*4+3]);
   for(let i=0;i<128;i++)for(let n=1;n<=m.padding;n++)for(const [x,y,xx,yy]of [[ox-n,oy+i,ox,oy+i],[ox+127+n,oy+i,ox+127,oy+i],[ox+i,oy-n,ox+i,oy],[ox+i,oy+127+n,ox+i,oy+127]]){
    const p=(y*info.width+x)*4,q=(yy*info.width+xx)*4;assert(data.subarray(p,p+4).equals(data.subarray(q,q+4)),'Atlas extrusion');paddedPixels++;
   }
  }
 }
 for(const e of m.entries)assert.equal(centerAlphas[e.id],e.overlay?0:255,'No holes in ground; no collision-cell-sized fillet');
 const report={status:'passed',cellsCompared:compared,groundPlacements:ground.size,possibleFilletPlacements:overlay.size,uniqueSprites:m.tileCount,actuallyReusedSpriteIds:[...counts.values()].filter(n=>n>1).length,reusedPlacements:m.entries.length-m.tileCount,paddedPixelsChecked:paddedPixels,gridSha256:m.gridSha256};
 fs.writeFileSync(path.join(root,'Book/Validation/region1-v8-atlas-check.json'),JSON.stringify(report,null,2)+'\n');console.log(report);
}
main().catch(e=>{console.error(e);process.exitCode=1});
