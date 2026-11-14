const fs=require('fs'),path=require('path'),sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const generated=path.join(process.env.USERPROFILE,'.codex/generated_images/01a13ce9-93a4-7d33-a322-461ed4f36c99'),out='Art/samples/boss-region1-v2',dest='Book/Assets/Unfin/Resources/BossRegion1';
const files={Idle:'977eba77-473c-41aa-9d38-3ca783216ff3',Sweep:'b2001f94-fff8-46b7-9e85-af342b0bdf3d',Slam:'626f3eb8-169e-4aec-8507-2625738922c7',Rush:'ad2d7e06-dc65-45c2-bd36-650f7352174f',Roots:'f88337f9-a178-457a-9186-169ae5a21117',Fan:'4d2e2b9f-d586-4390-b6a3-8e2b17323512',Cross:'bfeea2dd-78ba-49f8-9e62-9bceb5749bed',Death:'e730c8ed-ec90-4be5-b953-77317c749106',Move:'c105a4e1-4510-491c-b0e0-066271fd6186',Stagger:'bd921305-1d18-4bbe-ba4e-8600ac5ec7e2'};
const times={Idle:Array(12).fill(.12),Move:Array(12).fill(.085),Stagger:[.04,.05,.06,.07,.08,.08,.08,.06,.05,.05,.04,.04],Death:[.09,.09,.1,.12,.12,.12,.13,.14,.15,.17,.2,.3],Sweep:[.1,.11,.11,.11,.11,.07,.05,.08,.08,.08,.08,.08],Slam:[.12,.15,.15,.1,.1,.1,.2,.08,.12,.1,.1,.1],Rush:[.1,.12,.13,.13,.14,.08,.17,.2,.08,.08,.08,.08],Roots:[.11,.12,.12,.14,.16,.08,.2,.28,.08,.08,.08,.08],Fan:[.1,.11,.12,.13,.14,.08,.06,.07,.08,.08,.08,.08],Cross:[.09,.1,.1,.1,.11,.07,.15,.12,.07,.08,.12,.12]};
const patterns=[
{name:'Sweep',label:'묵직한 가로 베기',activeStart:.54,activeEnd:.66,reach:3.4,response:'일반 점프 또는 뒤로 이동'},
{name:'Slam',label:'체중 내려찍기',leapStart:.42,activeStart:.92,activeEnd:1.12,reach:2.55,response:'고정된 착지 예고에서 이동, 충격파는 점프'},
{name:'Rush',label:'낮은 어깨 돌진',activeStart:.62,activeEnd:1.07,moveSpeed:8,response:'점프·벽 점프'},
{name:'Roots',label:'흑연 뿌리와 매듭',activeStart:.65,activeEnd:1.21,pulseGap:.2,pulseLife:.16,response:'점프·발판, 연필 깎기로 매듭을 끊어 경직'},
{name:'Fan',label:'흑연 파편 부채',activeStart:.6,activeEnd:.68,projectileSpeed:4.8,response:'뒤로 이동·점프·보스 뒤로 이동'},
{name:'Cross',label:'두 번의 교차 베기',activeStart:.5,activeEnd:.57,secondStart:.84,secondEnd:.91,response:'두 번째 베기까지 거리 확보·점프'}
];
(async()=>{fs.mkdirSync(out,{recursive:true});fs.mkdirSync(dest,{recursive:true});fs.copyFileSync(path.join(generated,'exec-442e285b-f982-48d8-afa9-0f1f0522e115.png'),path.join(out,'boss-master.png'));
let reference;const registration={};
for(const [name,id] of Object.entries(files)){
const src=path.join(generated,'exec-'+id+'.png');fs.copyFileSync(src,path.join(out,name+'-sheet.png'));const m=await sharp(src).metadata();if(!m.hasAlpha)throw Error('No alpha '+name);const cw=Math.floor(m.width/4),ch=Math.floor(m.height/3),frames=[];
for(let i=0;i<12;i++)frames.push(await sharp(src).extract({left:i%4*cw,top:(i/4|0)*ch,width:cw,height:ch}).resize(512,512).ensureAlpha().png().toBuffer());
const bounds=async frame=>{const d=await sharp(frame).raw().toBuffer();let top=512,bottom=0;for(let y=0;y<512;y++)for(let x=100;x<412;x++)if(d[(y*512+x)*4+3]>230){top=Math.min(top,y);bottom=Math.max(bottom,y);}return {top,bottom,height:bottom-top+1};};
const neutral=await bounds(frames[0]);if(name==='Idle')reference=neutral;const scale=reference.height/neutral.height*.82,canvas=640,ground=592;registration[name]={scale,neutral,ground};fs.mkdirSync(path.join(dest,name),{recursive:true});
for(let i=0;i<12;i++){
// One scale per entire clip; baseline registration does not rescale individual poses.
const b=await bounds(frames[i]);const airborne=name==='Slam'&&i>=3&&i<=6;const bottom=airborne?neutral.bottom:b.bottom;const size=Math.round(512*scale),dx=Math.round(320-256*scale),dy=Math.round(ground-(bottom+1)*scale);
const scaled=await sharp(frames[i]).resize(size,size).png().toBuffer(),l=Math.max(0,-dx),t=Math.max(0,-dy),w=Math.min(size-l,canvas-Math.max(0,dx)),h=Math.min(size-t,canvas-Math.max(0,dy));const insert=await sharp(scaled).extract({left:l,top:t,width:w,height:h}).png().toBuffer();const registered=await sharp({create:{width:canvas,height:canvas,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:insert,left:Math.max(0,dx),top:Math.max(0,dy)}]).png().toBuffer();await sharp(registered).resize(512,512).png().toFile(path.join(dest,name,String(i).padStart(2,'0')+'.png'));
}}
const fx=path.join(generated,'exec-9988c979-5989-4b16-93ea-edc815f2c4ce.png');fs.copyFileSync(fx,path.join(out,'effects-sheet.png'));const fm=await sharp(fx).metadata(),fw=Math.floor(fm.width/4),fh=Math.floor(fm.height/3);for(let i=0;i<12;i++){const cell=await sharp(fx).extract({left:i%4*fw,top:(i/4|0)*fh,width:fw,height:fh}).png().toBuffer();await sharp(cell).trim({threshold:14}).resize(512,512,{fit:'inside'}).png().toFile(path.join(dest,'Effects',String(i).padStart(2,'0')+'.png'));}
const manifest={name:'엉킨 낙서 덩어리',version:2,sourceIndex:47,health:30,bodyWidth:3.05,bodyHeight:2.8,canvas:512,anchorX:256,groundY:473.6,pixelsPerUnit:reference.height*.82*.8/2.8,effectCount:12,clips:Object.keys(files).map(name=>({name,durations:times[name]})),patterns,requiredAbilities:['walljump','cut'],unavailableBeforeDefeat:['dash','parry','climb'],timingNote:'Seconds are our tuning against current player physics, not measured values from reference games.'};
fs.writeFileSync(path.join(dest,'manifest.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify(manifest,null,2));fs.writeFileSync(path.join(out,'registration.json'),JSON.stringify(registration,null,2));if(!fs.existsSync(path.join(out,'background.png')))throw Error('Missing canonical boss v2 background');console.log('BOSS_V2_READY 120 body drawings + 12 effects; PPU',manifest.pixelsPerUnit);
})().catch(e=>{console.error(e);process.exit(1)});
