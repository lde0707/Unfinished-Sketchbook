// Package generated whole sprites: crop, uniformly scale, and arrange for review.
// No material painting, recoloring, silhouette warping, or pixel synthesis.
const fs=require('node:fs'),path=require('node:path');
const sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const root=__dirname,spec=JSON.parse(fs.readFileSync(path.join(root,'design.json'),'utf8'));
const label=(text,width,height,size=20)=>Buffer.from(`<svg width="${width}" height="${height}"><text x="16" y="${size+9}" font-family="Malgun Gothic, sans-serif" font-size="${size}" fill="#d6d6d0">${text}</text></svg>`);
(async()=>{
 const panels=[],records=[];
 for(let i=0;i<spec.regions.length;i++){
  const region=spec.regions[i],src=path.join(root,'sources',region.id+'.png');
  const {data,info}=await sharp(src).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  if(data[3]>0)throw Error(region.id+' background is not transparent');
  // Generated row spacing varies. Find transparent gaps instead of cutting equal thirds.
  const occupied=[];for(let y=0;y<info.height;y++){let count=0;for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=8)count++;if(count>2)occupied.push(y)}
  const bands=[];for(const y of occupied){const last=bands[bands.length-1];if(!last||y-last[1]>24)bands.push([y,y]);else last[1]=y;}
  if(bands.length!==3)throw Error('Expected three separated sprites: '+region.id+' '+JSON.stringify(bands));
  fs.mkdirSync(path.join(root,region.id),{recursive:true});
  panels.push({input:label(`${i+1}. ${region.name} · 128px / 1칸 · A / B / C`,1536,55,24),left:0,top:i*180});
  for(let v=0;v<3;v++){
   const y0=Math.max(0,bands[v][0]-2),y1=Math.min(info.height,bands[v][1]+3);let left=info.width,top=y1,right=-1,bottom=-1;
   for(let y=y0;y<y1;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=8){left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y)}
   if(right<left)throw Error('Empty sprite '+region.id+'/'+v);
   const crop={left,top,width:right-left+1,height:bottom-top+1};
   const sprite=await sharp(src).extract(crop).png().toBuffer();
   const name='ABC'[v],sizes={};
   for(const size of [128,256]){
    const height=Math.round(crop.height/crop.width*size);if(height>size*.4)throw Error('Not a thin foothold: '+region.id);
    const scaled=await sharp(sprite).resize(size,height).png().toBuffer();
    await sharp({create:{width:size,height:size,channels:4,background:'#00000000'}}).composite([{input:scaled,left:0,top:0}]).png().toFile(path.join(root,region.id,`${name}-${size}.png`));
    sizes[size]={width:size,height:size,visibleHeight:height};
   }
   const enlarged=await sharp(sprite).resize({width:448}).png().toBuffer();
   panels.push({input:enlarged,left:v*512+32,top:i*180+74});
   records.push({region:region.id,variant:name,crop,sizes});
  }
 }
 await sharp({create:{width:1536,height:900,channels:3,background:'#191d22'}}).composite(panels).png().toFile(path.join(root,'all-platforms-preview.png'));
 fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify({source:'generated raster sprites',cropOnly:true,regionCount:5,variantCount:15,records},null,2)+'\n');
 const cards=spec.regions.map(r=>`<section><h2>${r.name}</h2><p>실제 한 칸 크기 · 128px</p><div class="tiles">${'ABC'.split('').map(v=>`<figure><div class="tile"><img src="${r.id}/${v}-128.png" width="128" height="128"></div><figcaption>${v}</figcaption></figure>`).join('')}</div><details><summary>원화 세 가지 보기</summary><img class="sheet" src="sources/${r.id}.png"></details></section>`).join('');
 fs.writeFileSync(path.join(root,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>지역별 한 칸 발판</title><style>body{margin:0;background:#16191d;color:#dfdfd5;font:16px system-ui}main{max-width:1100px;margin:40px auto;padding:20px}h1{font-size:30px}p{color:#aebbbd;line-height:1.7}.overview{width:100%}section{padding:20px;border:1px solid #394246;border-radius:12px;margin:24px 0}.tiles{display:flex;gap:24px;flex-wrap:wrap}figure{margin:0}.tile{width:128px;height:128px;border:1px dashed #657073;background:#22272c}.tile img{display:block}figcaption{padding:8px;color:#b9c1c3}summary{cursor:pointer;padding:16px 0}.sheet{width:min(100%,760px);background:#252a30}</style><main><h1>지역별 한 칸 발판</h1><p>전체 5지역 · 지역당 3가지 원화 · 폭 128px / 1유닛<br>현재 맵의 얇은 통과형 발판을 위한 디자인 시안입니다. 둥근 끝과 작은 윗면 요철, 어두운 밑면으로 구성했습니다. Unity 적용 전입니다.</p><img class="overview" src="all-platforms-preview.png" alt="5지역의 발판15종 비교">${cards}<p><a href="design.json">제작 기준과 생성 프롬프트</a></p></main></html>`);
 console.log(JSON.stringify({regions:5,variants:records.length,heights:records.map(r=>r.sizes[128].visibleHeight)}));
})().catch(e=>{console.error(e);process.exit(1)});
