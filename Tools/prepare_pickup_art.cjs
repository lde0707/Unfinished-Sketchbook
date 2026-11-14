const fs=require('fs'),path=require('path');
const sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const root=path.resolve(__dirname,'..'), source=path.join(process.env.USERPROFILE,'.codex/generated_images/01a13ce9-93a4-7d33-a322-461ed4f36c99');
const art=path.join(root,'Art/samples/pickups-runtime-v1'),out=path.join(root,'Book/Assets/Unfin/Resources/PickupArt');
async function run(){fs.mkdirSync(art,{recursive:true});fs.mkdirSync(out,{recursive:true});
 const atlas=path.join(source,'exec-cb3ffea3-e25b-4eb2-b798-93ca1c9fd377.png');fs.copyFileSync(atlas,path.join(art,'items-source.png'));
 const xs=[0,410,740,1070,1448],ys=[0,402,733,1086],names=['heart','shard','item_heal','healup','item_shield','item_paint','pigment','wj','cut','dash','parry','climb'];
 for(let i=0;i<12;i++){const x=i%4,y=i/4|0;let image=sharp(atlas).extract({left:xs[x],top:ys[y],width:xs[x+1]-xs[x],height:ys[y+1]-ys[y]});
  const {data,info}=await image.raw().toBuffer({resolveWithObject:true});for(let p=3;p<data.length;p+=4)if(data[p]<32)data[p]=0;
  await sharp(data,{raw:info}).trim({threshold:12}).resize(256,256,{fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).extend({top:8,bottom:8,left:8,right:8,background:{r:0,g:0,b:0,alpha:0}}).png().toFile(path.join(out,names[i]+'.png'));
 }
 const portal=path.join(source,'exec-a4c21d29-ddf7-4c7e-bac5-dadb5d51f174.png');fs.copyFileSync(portal,path.join(art,'portal-source.png'));await sharp(portal).trim({threshold:16}).resize({height:768}).png().toFile(path.join(out,'station.png'));
 console.log('PICKUP_ART_READY 12 collectibles + portal');
}run().catch(e=>{console.error(e);process.exit(1)});
