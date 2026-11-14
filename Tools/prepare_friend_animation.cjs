const fs=require('fs'),path=require('path');const sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const generated=path.join(process.env.USERPROFILE,'.codex/generated_images/01a13ce9-93a4-7d33-a322-461ed4f36c99'),art='Art/samples/friends-animation-v2',dest='Book/Assets/Unfin/Resources/FriendAnimations';
(async()=>{fs.mkdirSync(art,{recursive:true});fs.mkdirSync(dest,{recursive:true});
 const sources={dog:'exec-d922b0dd-a6d3-4681-b6d9-178c251aec75.png',sun:'exec-d593764c-6f30-4991-9586-f677e9d11bbc.png',npc:'exec-25d5df63-2e23-4500-993e-571deffcacd6.png'};
 for(const [name,file] of Object.entries(sources)){let input=path.join(generated,file);fs.copyFileSync(input,path.join(art,name+'-sheet.png'));let meta=await sharp(input).metadata(),cw=Math.floor(meta.width/4),ch=Math.floor(meta.height/3),cells=[];let x0=cw,y0=ch,x1=0,y1=0;
  for(let i=0;i<12;i++){const raw=await sharp(input).extract({left:(i%4)*cw,top:(i/4|0)*ch,width:cw,height:ch}).ensureAlpha().raw().toBuffer();cells.push(raw);if(i<8)for(let y=0;y<ch;y++)for(let x=0;x<cw;x++)if(raw[(y*cw+x)*4+3]>160){x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y)}}
  x0=Math.max(0,x0-3);y0=Math.max(0,y0-3);x1=Math.min(cw-1,x1+3);y1=Math.min(ch-1,y1+3);fs.mkdirSync(path.join(dest,name),{recursive:true});
  for(let i=0;i<12;i++){for(let p=3;p<cells[i].length;p+=4)if(cells[i][p]<24)cells[i][p]=0;await sharp(cells[i],{raw:{width:cw,height:ch,channels:4}}).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1}).resize({height:512}).png().toFile(path.join(dest,name,i.toString().padStart(2,'0')+'.png'))}
  fs.copyFileSync(path.join(dest,name,'00.png'),'Book/Assets/Unfin/Resources/DeviceArt/'+name+'.png');
 }
 fs.writeFileSync(path.join(art,'timings.json'),JSON.stringify({idle:[.52,.42,.16,.52],interaction:[.3,.3,.3,.3],depart:[.14,.14,.18,.2],npcHold:.65,friendHold:1.2},null,2));console.log('FRIEND_ANIMATION_ART_READY 3 characters / 36 complete drawings');
})().catch(e=>{console.error(e);process.exit(1)});

