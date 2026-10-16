// Mechanical atlas extraction, rim registration, seam and depth normalization.
// Artwork is drawn by image_gen; original atlases and prompts are preserved.
const fs = require('node:fs');
const path = require('node:path');
let sharp;
try { sharp = require('sharp'); } catch {
  sharp = require(process.env.UNFIN_SHARP_PATH || path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
}
const root = __dirname, n = 256;
const spec = JSON.parse(fs.readFileSync(path.join(root,'prompts.json'),'utf8'));
const smooth = t => (t=Math.max(0,Math.min(1,t)),t*t*(3-2*t));
const luma = (p,i) => p[i]*.2126+p[i+1]*.7152+p[i+2]*.0722;
async function main() {
  const checks=[], overview=[];
  for (let r=0;r<2;r++) {
    const region=spec.regions[r], source=path.join(root,'sources',region.file);
    const meta=await sharp(source).metadata();
    const w=Math.floor(meta.width/3), h=Math.floor(meta.height/2);
    for (let side=0;side<2;side++) {
      const outputs=[];
      for (let v=0;v<3;v++) {
        // Remove the thin generated panel dividers before producing 256px masters.
        const raw=await sharp(source).extract({left:v*w+2,top:side*h+2,width:w-4,height:h-4}).resize(n,n).removeAlpha().raw().toBuffer();
        const at=(t,d)=> side===0?(d*n+t)*3:(t*n+n-1-d)*3;
        const edge=Array.from({length:n},(_,t)=>{
          for(let d=0;d<24;d++)if(luma(raw,at(t,d))>45)return d;
          return 0;
        });
        const max=Math.max(1,...edge), body=Buffer.alloc(n*n*4);
        for(let t=0;t<n;t++)for(let d=0;d<n;d++) {
          const rim=edge[t]/max*6*smooth(Math.min(t,n-1-t)/24);
          const depth=Math.max(0,(d-rim)/(n-1-rim));
          const originalDepth=Math.min(n-1,Math.round(edge[t]+depth*(n-1-edge[t])));
          const i=at(t,originalDepth), o=(side===0?(d*n+t):(t*n+n-1-d))*4;
          for(let c=0;c<3;c++)body[o+c]=r===0?Math.round(luma(raw,i)*[.96,.99,1.05][c]):raw[i+c];
          body[o+3]=Math.round(255*Math.max(0,Math.min(1,d+.5-rim)));
        }
        // Consistent smooth full-depth value curve; preserve local painted variation.
        for(let d=0;d<n;d++) {
          let sum=0,count=0;
          for(let t=0;t<n;t++) {const i=(side===0?d*n+t:t*n+n-1-d)*4;if(body[i+3]>200){sum+=luma(body,i);count++;}}
          const target=(.018+.37*Math.pow(1-d/(n-1),1.15))*255;
          const gain=target/Math.max(1,sum/Math.max(1,count));
          for(let t=0;t<n;t++) {const i=(side===0?d*n+t:t*n+n-1-d)*4;for(let c=0;c<3;c++)body[i+c]=Math.min(255,Math.round(body[i+c]*gain));}
        }
        outputs.push(body);
      }
      // A shared narrow strip of PAINTED earth bridges A/B/C boundaries. A flat
      // average color here makes visible vertical/horizontal bars in long terrain.
      const bridge=Buffer.from(outputs[0]);
      for(let d=0;d<n;d++) {
        for(let v=0;v<3;v++)for(let t=0;t<n;t++) {
          const seam=Math.min(t,n-1-t), weight=smooth((seam-6)/18);
          if(seam>=24)continue;
          const i=(side===0?d*n+t:t*n+n-1-d)*4;
          const bt=seam<2?128:t<128?128+t:128+t-255;
          const bi=(side===0?d*n+bt:bt*n+n-1-d)*4;
          for(let c=0;c<3;c++)outputs[v][i+c]=Math.round(bridge[bi+c]*(1-weight)+outputs[v][i+c]*weight);
          // Preserve bumps inside each tile; the two endpoints share opaque height zero.
          if(seam<2)outputs[v][i+3]=255;
        }
      }
      for(let v=0;v<3;v++) {
        const file=region.id+'-'+String.fromCharCode(97+v)+(side===0?'-floor.png':'-wall.png');
        const png=await sharp(outputs[v],{raw:{width:n,height:n,channels:4}}).png().toBuffer();
        fs.writeFileSync(path.join(root,file),png);
        overview.push({input:png,left:v*n,top:(r*2+side)*n});
        const bands=[];
        for(let b=0;b<8;b++){let sum=0;for(let d=b*32;d<(b+1)*32;d++)for(let t=24;t<232;t++)sum+=luma(outputs[v],(side===0?d*n+t:t*n+n-1-d)*4);bands.push(+(sum/(32*208)).toFixed(2));}
        if(bands.some((value,i)=>i>0&&value>=bands[i-1]))throw Error(file+' depth is not decreasing');
        checks.push({file,width:n,height:n,depthBandLuma:bands,maximumRimRecessAt128:3});
      }
    }
  }
  await sharp({create:{width:768,height:1024,channels:4,background:'#1b1d24'}}).composite(overview).png().toFile(path.join(root,'active-patterns-abc.png'));
  fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify(checks,null,2)+'\n');
  console.log('Exported 12 masters; full-depth shading validated.');
}
main().catch(error=>{console.error(error);process.exitCode=1;});
