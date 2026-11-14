const fs=require('fs'),path=require('path'),sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const source=path.join(process.env.USERPROFILE,'.codex/generated_images/01a13ce9-93a4-7d33-a322-461ed4f36c99'),dest='Book/Assets/Unfin/Resources/DeviceArt',art='Art/samples/world-devices-v1';
(async()=>{fs.mkdirSync(dest,{recursive:true});fs.mkdirSync(art,{recursive:true});
 const sheets=[['exec-e1822558-ee82-4078-ac32-048d12e827ca.png',['lever','target','hatch']],['exec-8e4bf675-2f8a-419a-8935-17ca8bd0bf95.png',['dog','sun','npc']],['exec-23c3fa37-f764-480b-ae1b-3356ffb4a518.png',['diary','brazier','bar']]];
 for(let index=0;index<sheets.length;index++){const [name,ids]=sheets[index],file=path.join(source,name),meta=await sharp(file).metadata();assertAlpha=meta.hasAlpha;if(!assertAlpha)throw Error('Missing transparency '+name);fs.copyFileSync(file,path.join(art,'sheet-'+(index+1)+'.png'));
  for(let i=0;i<3;i++){let left=Math.round(meta.width*i/3),right=Math.round(meta.width*(i+1)/3);const crop=await sharp(file).extract({left,top:0,width:right-left,height:meta.height}).toBuffer();await sharp(crop).trim({threshold:24}).resize({height:ids[i]==='bar'?512:384}).png().toFile(path.join(dest,ids[i]+'.png'));}
 }console.log('DEVICE_ART_READY 9 images / 12 placements');
})().catch(e=>{console.error(e);process.exit(1)});

