// Room-specific composition, using the authoritative current map rather than the old art report.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const d=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/world12.json'));
const notes=[
['접힌 페이지 입구','큰 종이 아치는 왼쪽, 낮은 책 더미는 시작점 반대편에 둔다.',0,2],
['바람에 들린 종이 기둥','벽 점프 진입 방향에 세로 찢김을 두고 중앙은 비운다.',1,0],
['접힌 비탈의 단면','옆으로 넘어진 종이층과 작은 책 더미로 높이차를 강조한다.',2,1],
['풀 그림자 수직굴','세로 종이 버팀과 매달린 종이를 위아래로 엇갈리게 둔다.',1,0],
['세 갈래 페이지 아치','허브 중앙은 여백으로 남기고 출입구 사이에 서로 다른 종이 구조물을 둔다.',0,3],
['접힌 다락 천장','낮은 방이므로 작은 책 더미와 한쪽 종이 버팀만 둔다.',2,1],
['오래된 갱도 문틀','기울어진 입구 지지대와 뒤쪽 광석 수레로 광산 진입을 알린다.',0,2],
['낮고 긴 채굴 운반로','50칸 통로에 수레·무너진 골조·빈 구간을 불균등하게 배치한다.',2,3],
['끊어진 승강 채굴장','상층에는 윈치, 하층에는 무너진 골조를 두어 두 중간층의 높이를 읽게 한다.',1,3],
['작업이 멈춘 쉼터','작은 수레를 한쪽에 모으고 저장 지점 뒤에는 여백을 둔다.',2,0],
['감독의 거대한 채굴 골조','무너진 지지대가 전투 공간 양쪽을 감싸되 중앙 실루엣은 비운다.',3,1],
['묻힌 광석 창고','작은 방에 낮은 광석 더미 하나만 둔다.',2,3],
['커다란 옹이 빈터','단일 큰 옹이와 갈라진 줄기로 방의 정체성을 만든다.',3,0],
['속 빈 줄기의 세 층','46칸 높이에 하층 줄기·중층 가지·상층 둥지를 분리하고 각 중간층 뒤의 무게중심을 바꾼다.',1,3],
['옆으로 뻗은 가지','60칸 통로에서 서로 떨어진 큰 가지와 둥지를 번갈아 배치한다.',2,0],
['지워진 작은 둥지','낮고 작은 방에 비어 있는 둥지 하나를 배치한다.',2,3],
['가지에 얹힌 둥지','가로 가지와 큰 둥지를 좌우 비대칭으로 둔다.',2,1],
['공중에 걸린 뿌리 아치','길게 휘어진 뿌리가 다리 뒤를 감싸고 아래쪽은 어둡게 비운다.',0,1],
['갈라지는 뿌리 줄기','서로 다른 높이에 뿌리 기둥과 작은 둥지를 둔다.',1,2],
['사냥꾼의 은신 뿌리','양옆 뿌리와 후방 고치로 은신처를 만들고 전투 중앙을 비운다.',3,0],
['둥지 앞 고요한 빈터','낮은 뿌리 둔덕만 남겨 보스방의 밀도와 대비한다.',2,1],
['큰 낙서 둥지 외피','전투 공간 외곽의 거대한 열린 고치와 휜 뿌리를 다른 깊이로 배치한다.',3,0],
['기울어진 뿌리 내리막','내리막을 따라 크기가 다른 뿌리 둔덕을 엇갈리게 둔다.',2,1],
['고치 뒤 작은 틈','숨은 공간에 작은 고치 조각 하나만 둔다.',3,2],
['잊힌 책장','작은 서재를 뒤틀린 책장 하나로 구별한다.',3,2],
['밀랍이 밴 둥지','작은 가지 둥지를 방 폭에 맞춰 낮게 둔다.',2,3],
['잠긴 광산 잔해','물속 낮은 수레 잔해를 한쪽에 배치한다.',2,3],
['부드러운 뿌리 선반','작은 뿌리 둔덕과 매달린 고치를 떨어뜨려 둔다.',2,3],
['그리다 만 광산 구석','붕괴 골조 한쪽과 낮은 광석 더미로 작은 빈터를 감싼다.',3,2],
['찢긴 지층의 수로','긴 수로 위에 붕괴 골조와 매달린 윈치를 넓은 간격으로 둔다.',3,1],
['물감 번진 첫 문','색이 번진 크레파스 아치와 짧은 녹은 기둥으로 진입을 구별한다.',0,1],
['비탈 뒤 녹은 색층','큰 색층 아치와 낮은 크레파스 더미를 높이차에 맞춘다.',0,2],
['노을의 열린 큰 굴','중간층 뒤에 큰 아치, 양끝에는 높이가 다른 색 기둥을 둔다.',0,1],
['세 빛이 스민 광장','단일 아치와 기울어진 색판을 분리해 중앙의 빈 광장을 감싼다.',3,0],
['식은 화로와 배관','큰 화로 외피, 높이가 다른 굴뚝, 낮은 작업대를 겹친다.',0,1],
['공방의 비뚤어진 문','화로 입구 외피와 작업 잔해를 한쪽에 모은다.',0,2],
['겹층 밀랍 저장 선반','두 중간층 뒤에 서로 다른 높이의 저장 선반과 매달린 배관을 둔다.',3,1],
['지워진 작업 구석','작은 작업대 잔해 하나만 배치한다.',2,3],
['물감통 보관 벽감','선반과 낮은 용기 묶음을 한쪽으로 치우쳐 놓는다.',3,2],
['거북 굴의 식은 설비','큰 화로 잔해는 측면으로, 전투 중앙은 비워 둔다.',0,3],
['환기구의 색바람','길고 낮은 방에 기울어진 색판과 짧은 기둥을 띄엄띄엄 둔다.',3,1],
['협곡의 겹 절벽','길게 이어지는 절벽 실루엣과 끊어진 다리 잔해 사이에 큰 빈 공간을 둔다.',0,2],
['절벽 안쪽 저장 틈','작은 낮은 암반 덩어리 하나로 벽감을 표시한다.',2,3],
['끊어진 막대 다리 뒤','다리 잔해와 먼 암반 아치를 서로 다른 깊이에 둔다.',2,0],
['넓고 고요한 협곡 바닥','70칸 길이에 큰 절벽·낮은 퇴적층·빈 구간을 불균등하게 구성한다.',0,2],
['협곡의 닫힌 벽감','작은 퇴적층과 한쪽 절벽만 두어 목표물을 가리지 않는다.',3,2],
['녹아 흐른 색 광맥','굳은 퇴적층과 짧은 폭포 흔적을 비대칭으로 겹친다.',3,1],
['세 층을 관통하는 물감 폭포','28칸 높이에 긴 폭포를 후방으로, 앞쪽에는 끊긴 절벽을 층별로 둔다.',1,0],
['기사의 고요한 전실','낮은 깃발 잔해와 한쪽 부벽으로 전투 전 여백을 만든다.',2,1],
['기사 연병장의 부서진 아치','큰 찢긴 아치와 측면 부벽, 멀리 드리운 깃발로 전투 중앙을 감싼다.',0,1],
['탑 옆 작은 벽감','작은 부서진 장식물 하나만 남긴다.',2,3],
['지붕 없는 탑 회랑','긴 가로 방에 기울어진 탑 창틀과 낮은 잔해를 넓게 띄워 둔다.',3,2],
['폭포 너머의 작은 틈','작은 퇴적층 하나와 바깥 폭포의 흔적만 배치한다.',3,1],
['잠긴 공방 하부','낮은 설비 잔해만 두고 수면 위 여백을 유지한다.',2,3],
['협곡의 바람 통로','큰 절벽 사이에 넓은 빈 공간, 끝에 짧은 다리 잔해를 둔다.',0,2],
['해님이 머무는 다락','기울어진 창틀 하나와 낮은 잔해로 따뜻한 벽감을 만든다.',3,2]
];
assert.equal(notes.length,d.rooms.length);
const folders=['paper','mine','tree','root','sunset','workshop','canyon','tower'];
const cells=[];for(const run of d.mapRle.split(',')){const[t,n]=run.split('.').map(v=>parseInt(v,36));for(let i=0;i<n;i++)cells.push(t);}
const mezz=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/room-mezzanines.json'));
const rooms=d.rooms.map((r,i)=>{
 const [title,design,hero,secondary]=notes[i],id=`${r.r}-${String(r.n).padStart(2,'0')}`;
 const tall=r.h>=18,small=r.w<=14&&r.h<=7,wide=r.w>=40;
 const props=[];
 function add(part,nx,ny,w,h,layer,flip=false){
  props.push({part,x:+(r.x+r.w*nx).toFixed(3),y:+(r.y+r.h*ny).toFixed(3),w:+w.toFixed(3),h:+h.toFixed(3),layer,flip});
 }
 // Explicit hero choice per room; layout responds to aspect and gameplay use.
 if(small){const size=Math.min(r.w*.88,r.h*1.65);add(hero,.51,.58,size,size,1,i%2===0);}
 else if(tall){
  const size=Math.min(r.w*.70,19);
  add(hero,.28,.63,size,Math.min(r.h*.90,size*1.5),1,i%2===0);
  add(secondary,.78,.32,size*.82,Math.min(r.h*.62,size*1.28),0,true);
  add(2,.70,.84,Math.min(r.w*.58,17),Math.min(r.w*.58,17),2);
  if(r.h>25){add(1,.24,.18,size*.85,size*1.3,1,true);add(3,.73,.57,size*.72,size*.85,1);}
 }else{
  const size=Math.min(r.h*1.35,r.w*.48,19);
  add(hero,.23,.51,size,size,1,i%2===0);
  add(secondary,.77,.65,size*.88,size*.88,2,i%3===0);
  if(wide)add([0,1,2,3].find(p=>p!==hero&&p!==secondary),.52,.40,size*.78,size*.87,0,true);
 }
 // Long rooms use an asymmetric low accent, never a regular repeated texture grid.
 if(r.w>=55)add([0,1,2,3].find(p=>!props.some(q=>q.part===p)),.88,.40,Math.min(14,r.h*1.25),Math.min(14,r.h*1.25),0);
 let air=0,platforms=0;for(let y=r.y;y<r.y+r.h;y++)for(let x=r.x;x<r.x+r.w;x++){const t=cells[y*d.width+x];if(t===0)air++;if([4,6,15].includes(t))platforms++;}
 const floors=mezz.solidFloors.filter(f=>f.r===r.r&&f.n===r.n);
 // Thick suspended floors get rear structural connections, not a wallpaper behind them.
 for(const [j,f]of floors.entries()){
  const part=[0,1,2,3].find(p=>!props.some(q=>q.part===p));if(part===undefined)break;
  add(part,(f.x+f.w*.5-r.x)/r.w,(f.y+1-r.y)/r.h,Math.min(f.w*1.1,13),Math.min(r.h*.65,12),0,j%2===1);
 }
 // Three key tall rooms use a newly painted continuous structural spine.
 if(id==='1-09'){
  props.length=0;add(0,.49,.53,39,19,1);props[0].atlas='mine-scaffold.png';
  add(2,.78,.86,13,7,2);add(0,.15,.30,11,11,0,true);
 }
 if(id==='1-14'){
  props.length=0;add(0,.50,.51,23,47,1);props[0].atlas='tree-spine.png';
  add(2,.76,.19,7,5,2);add(3,.16,.58,7,9,0);add(0,.70,.84,10,11,0,true);
 }
 if(id==='2-18'){
  props.length=0;add(0,.34,.51,24,29,1);props[0].atlas='paint-fall.png';
  add(0,.79,.70,17,20,0,true);add(3,.75,.79,12,14,2);
 }
 for(const p of props)if(!p.atlas&&id!=='1-14'){
  if(r.z==='D'&&p.part===3)p.anchorCeiling=true;
  else p.anchorFloor=true;
 }
 return {id,r:r.r,n:r.n,z:r.z,name:r.name,x:r.x,y:r.y,w:r.w,h:r.h,title,design,
  shape:small?'alcove':tall?'vertical':wide?'wide':'chamber',screensX:+(r.w/15).toFixed(2),screensY:+(r.h/8.4375).toFixed(2),
  openCells:air,platformCells:platforms,mezzanines:floors.length,atlas:`${r.z}-${folders['ABCDEFGH'.indexOf(r.z)]}.png`,props};
});
const plan={version:1,gridSha256:d.gridSha256,referenceTilePixels:128,referenceViewport:[1920,1080],
 style:'기존 지형과 같은 무광 회화 질감. 가까운 배경 구조물, 비대칭 배치, 주행 지형보다 낮은 대비.',
 layers:[{order:-65,parallax:.035,brightness:.38},{order:-40,parallax:.065,brightness:.52},{order:-20,parallax:.10,brightness:.62}],
 rooms};
for(const r of rooms){
 const candidates=[];
 for(let y=r.y+2;y<=r.y+r.h;y++)for(let x=r.x+1;x<r.x+r.w-1;x++){
  const at=(xx,yy)=>cells[yy*d.width+xx];
  if(![1,4].includes(at(x,y))||at(x,y-1)!==0||at(x,y-2)!==0)continue;
  candidates.push({x,y,score:Math.abs(x+.5-(r.x+r.w*.5))+(r.y+r.h-y)*.3});
 }
 candidates.sort((a,b)=>a.score-b.score);if(candidates.length)r.previewSpawn=candidates[0].x+','+candidates[0].y;
}
for(const dir of ['Art/room-backdrops-v1','Book/Assets/Unfin/Art/RoomBackdropsV1'])fs.mkdirSync(dir,{recursive:true});
for(const file of ['Art/room-backdrops-v1/room-layouts.json','Book/Assets/Unfin/Art/RoomBackdropsV1/room-layouts.json'])fs.writeFileSync(file,JSON.stringify(plan,null,2)+'\n');
console.log(JSON.stringify({rooms:rooms.length,props:rooms.reduce((a,r)=>a+r.props.length,0),maxWidth:Math.max(...rooms.map(r=>r.w)),maxHeight:Math.max(...rooms.map(r=>r.h)),gridSha256:d.gridSha256}));
