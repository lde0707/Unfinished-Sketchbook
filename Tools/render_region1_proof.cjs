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
 // Optional same-camera before/after crops at native 1:1 pixel scale.
 if(process.argv[2]){
  const comparison=[];
  for(const [i,file,label] of [[0,process.argv[2],'수정 전 · 거의 평평한 윤곽'],[1,path.join(dir,'world1-1920x1080.png'),'수정 후 · 원화 기반 불규칙 윤곽']]){
   const title=Buffer.from(`<svg width="960" height="44" xmlns="http://www.w3.org/2000/svg"><rect width="960" height="44" fill="#111318"/><text x="16" y="30" font-family="Malgun Gothic" font-size="23" fill="#dedfe1">${label}</text></svg>`);
   comparison.push({input:title,left:i*960,top:0},{input:await sharp(file).extract({left:0,top:700,width:960,height:240}).toBuffer(),left:i*960,top:44});
  }
  await sharp({create:{width:1920,height:284,channels:4,background:'#111318'}}).composite(comparison).png().toFile(path.join(dir,'terrain-relief-before-after.png'));
 }
}
main().catch(e=>{console.error(e);process.exitCode=1});
