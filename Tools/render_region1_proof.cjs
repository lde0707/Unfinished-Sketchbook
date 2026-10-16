// Contact sheet of actual Unity camera captures, not generated concept artwork.
const fs=require('node:fs'),path=require('node:path');
let sharp;try{sharp=require('sharp')}catch{sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'))}
const dir=path.resolve(__dirname,'../Book/Validation');
async function main(){
 const labels=['A · 구겨진 종이굴 · 회갈색','B · 흑연 광산 · 청회색','C · 스케치 나무 속 · 목재 갈색','D · 엉킨 뿌리 심부 · 회녹색'];
 const layers=[];
 for(let i=0;i<4;i++){
  const label=Buffer.from(`<svg width="960" height="44" xmlns="http://www.w3.org/2000/svg"><rect width="960" height="44" fill="#111318"/><text x="16" y="30" font-family="Malgun Gothic" font-size="23" fill="#dedfe1">${labels[i]}</text></svg>`);
  const crop=await sharp(path.join(dir,`region1-v8-zone-${'ABCD'[i]}.png`)).extract({left:0,top:540,width:1920,height:540}).resize(960,270).toBuffer();
  layers.push({input:label,left:i%2*960,top:Math.floor(i/2)*314},{input:crop,left:i%2*960,top:Math.floor(i/2)*314+44});
 }
 await sharp({create:{width:1920,height:628,channels:4,background:'#111318'}}).composite(layers).png().toFile(path.join(dir,'region1-v8-unity-comparison.png'));
}
main().catch(e=>{console.error(e);process.exitCode=1});
