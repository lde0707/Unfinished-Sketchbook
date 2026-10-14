// 2지역 · 크레파스 언덕 — 맵 데이터 (1지역과 같은 규격: 280 × 140 타일, 1타일 = 16px)
const T=16,W=280,H=140;
const ZONES={A:{name:'노을 능선'},B:{name:'밀랍 공방'},C:{name:'색칠 협곡'},D:{name:'기사의 탑'}};
// 방: y = 바닥(f) - 높이(h)
const R=(n,z,name,x,f,w,h,o={})=>Object.assign({n,z,name,x,y:f-h,w,h},o);
const ROOMS=[
 R(1,'A','물감 번진 관문',2,36,24,10,{info:'1지역에서 넘어오는 입구 · 세이브'}),
 R(2,'A','첫 능선',30,36,40,12,{stairs:'R',info:'녹는 발판 소개 · 크레용 병정 첫 등장 (혼자)'}),
 R(3,'A','노을 언덕',74,34,46,14,{info:'병정 + 밀랍 투척꾼 조합'}),
 R(4,'A','삼색 광장',124,36,44,14,{info:'허브 · 세이브 · 동쪽 물감 문 (물감통 3개로 열림)'}),
 R(5,'B','화로 작업장',58,70,44,14,{noHills:true,info:'불씨 화로 + 녹는 발판 퍼즐 · 동쪽 굴뚝으로 능선과 연결'}),
 R(6,'B','공방 입구',20,70,36,10,{info:'세이브 · 녹아내린 굴뚝 아래'}),
 R(7,'B','밀랍 저장고',10,94,40,16,{noHills:true,info:'밀랍 웅덩이(느려짐) · 녹은 방울 첫 등장'}),
 R(8,'B','지워진 그림 창고',2,94,6,4,{noHills:true,info:'낙서 벽 뒤 체력 조각 (지우개 대시)'}),
 R(9,'B','빨강 물감통 방',54,94,26,10,{noHills:true,info:'🔴 빨강 물감통 · 세이브 (엘리트 앞)'}),
 R(10,'B','거북의 굴',84,94,34,16,{noHills:true,info:'엘리트 깎인 크레파스 거북 → 밀랍 튕기기(패링)'}),
 R(11,'A','바람개비 언덕',80,12,36,8,{noHills:true,info:'🔵 파랑 물감통 · 녹는 발판 징검다리 끝'}),
 R(12,'C','협곡 중턱',128,88,50,10,{info:'세이브 · 색가루 박쥐 첫 등장 · 동쪽 11타일 낭떠러지 · 서쪽 흑연 덩굴'}),
 R(13,'C','덩굴 뒤 물감 창고',120,88,6,6,{noHills:true,info:'흑연 덩굴 뒤 흑연 조각 (연필 깎기)'}),
 R(14,'C','막대 다리 건너',189,88,30,10,{noHills:true,info:'🟡 노랑 물감통 · 투척꾼의 밀랍을 튕겨 색 막대를 쓰러뜨리면 다리'}),
 R(15,'C','협곡 바닥',128,124,70,12,{info:'세이브 · 덧칠에 굳은 주민 + 일기 조각 1 · 서쪽 과녁 문 · 동쪽 광맥으로'}),
 R(16,'C','협곡 과녁 방',104,124,20,8,{noHills:true,info:'과녁 문 뒤 체력 조각 (패링)'}),
 R(17,'C','녹은 크레파스 광맥',202,124,34,12,{noHills:true,info:'늪 위 녹는 발판 3개 + 불씨 화로 (꺼야 천천히 녹음)'}),
 R(18,'C','물감 폭포 홀',238,124,34,28,{noHills:true,stairs:'L',info:'높은 홀 · 서쪽 계단 위 흑연 조각 · 동쪽 벽은 밀랍'}),
 R(19,'D','기사 앞 쉼터',200,36,20,8,{noHills:true,info:'보스 전 세이브'}),
 R(20,'D','기사의 연병장',224,50,36,20,{noHills:true,arena:true,info:'보스 부러진 크레파스 기사 → 크레파스 벽타기'}),
 R(21,'D','탑 옆 틈새',272,24,6,4,{noHills:true,info:'벽타기 직후 바로 쓰는 비밀 · 흑연 조각'}),
 R(22,'D','탑 꼭대기',226,12,44,8,{noHills:true,info:'세이브 · 서쪽 노을 하늘길 → 3지역'}),
 R(23,'C','폭포 뒤 틈',272,106,6,4,{noHills:true,revisit:'벽타기 · 2장 보스 후',info:'🔁 홀 동쪽 밀랍 벽 22칸을 벽타기로 → 🎒 회복 물감 병 (+1회)'}),
 R(24,'B','잠긴 물웅덩이 동굴',12,108,30,6,{noHills:true,revisit:'물감 헤엄 · 3장',info:'🔁 밀랍 저장고 아래 물속 → 흑연 조각'}),
 R(25,'C','바람 언덕',234,88,36,8,{noHills:true,revisit:'활공 · 4장',info:'🔁 막대 다리 동쪽 14타일 늪 건너 → 흑연 조각'}),
 R(26,'D','해님 다락',236,20,24,6,{noHills:true,hidden:true,info:'🎨 숨은 그림 친구 · 크레파스 탑 서쪽 밀랍 벽 구멍 (벽타기) → 색칠 안 된 해님에게 W 길게로 크레파스 색칠'}),
];
const PASS=[
 {z:'A',x:26,y:32,w:4,h:4},{z:'A',x:70,y:30,w:4,h:4},{z:'A',x:120,y:32,w:4,h:4},
 {z:'C',x:126,y:84,w:2,h:4},
 {z:'B',x:56,y:66,w:2,h:4},{z:'B',x:50,y:90,w:4,h:4},{z:'B',x:80,y:90,w:4,h:4},
 {z:'C',x:178,y:78,w:11,h:34,name:'협곡 낭떠러지 (11타일)'},
 {z:'D',x:168,y:30,w:32,h:6,name:'물감 문 복도'},{z:'D',x:220,y:30,w:4,h:6},
 {z:'D',x:260,y:44,w:2,h:6},{z:'D',x:270,y:20,w:2,h:4},
 {z:'D',x:196,y:6,w:30,h:4,name:'노을 하늘길 → 3지역'},
 {z:'C',x:219,y:80,w:15,h:8,name:'녹은 크레파스 늪 (14타일)'},
 {z:'B',x:12,y:94,w:4,h:14},
 {z:'C',x:198,y:118,w:4,h:6},{z:'C',x:236,y:118,w:2,h:6},
];
const SHAFTS=[
 {z:'A',name:'바람개비 수직굴',x:110,y:12,w:5,h:22,base:34,kind:'up'},
 {z:'B',name:'녹아내린 굴뚝',x:44,y:36,w:5,h:24,base:70,kind:'up'},
 {z:'B',name:'저장고 승강로',x:30,y:70,w:5,h:8,base:94,kind:'up'},
 {z:'B',name:'공방 굴뚝 (능선 지름길)',x:96,y:34,w:5,h:22,base:70,kind:'up'},
 {z:'C',name:'협곡 낭떠러지',x:146,y:36,w:6,h:42,base:88,kind:'turn'},
 {z:'C',name:'협곡 사다리',x:132,y:88,w:5,h:24,base:124,kind:'up'},
 {z:'D',name:'크레파스 탑',x:262,y:12,w:7,h:38,base:50,kind:'climb',noLedges:true},
];
const SPAWNS=[
 // A 노을 능선
 ['soldier',50,36],['bat',58,28],
 ['soldier',84,34],['soldier',92,34],['thrower',104,34],['bat',96,22],
 ['bat',98,6],
 ['drip',112,34],
 // B 밀랍 공방
 ['fly',36,64],
 ['soldier',60,70],['drip',92,70],['thrower',98,70],['bat',76,58],
 ['soldier',40,70],['drip',24,94],['drip',34,94],['soldier',44,94],['bat',28,82],
 ['turtle',104,94,{name:'깎인 크레파스 거북',drop:'parry'}],
 // C 색칠 협곡
 ['bat',140,80],['bat',165,80],['soldier',170,88],['thrower',174,88],
 ['thrower',197,88,{keeper:'bar'}],
 ['range',140,124,{keeper:'target'}],['soldier',160,124],['drip',176,124],['bat',186,114],['soldier',150,124],['thrower',192,124,{red:true}],
 // D
 ['soldier',184,36,{red:true}],['bat',190,31],
 ['soldier',210,124],['bat',216,114],['drip',232,124],['thrower',234,124],
 ['soldier',252,124],['bat',256,104],['thrower',266,124],['drip',244,124],
 ['knight',242,50,{name:'부러진 크레파스 기사'}],
 ['bat',240,6],['ball',254,12],['soldier',246,12],
];
const PICKS=[
 ['save',6,36],['save',128,36],['save',24,70],['save',58,94],['save',196,124],['save',204,36],['save',230,12],['save',158,88],
 ['heart',4,94],['heart',108,124],
 ['shard',122,88],['shard',274,24],['shard',30,108],['shard',266,88],['shard',112,94],['shard',214,88],
 ['sw_r',66,94],['sw_b',84,12],['sw_y',210,88],
 ['shard',245,100],['healup',274,106],['npc',186,124],['diary',190,124],['brazier',219,124],
 ['bar',187,88],['target',134,124],['brazier',70,70],['brazier',85,70],
 ['sketch',250,20,{who:'sun',need:'climb',w:30,h:30}],
 ['station',14,35,{sid:'r2a',sname:'물감 번진 관문',w:16,h:30}],['station',136,34,{sid:'r2b',sname:'삼색 광장',w:16,h:30}],['station',193,124,{sid:'r2c',sname:'협곡 바닥',w:16,h:30}],['station',240,12,{sid:'r2d',sname:'탑 꼭대기',w:16,h:30}],['station',38,108,{sid:'r2e',sname:'잠긴 물웅덩이 동굴',w:16,h:30}],
];
const START={tx:8,floor:36};
const REGION_ID='r2',REGION_NAME='2장 크레파스 언덕';
const FILES={r1:'unfin_prototype.html',r2:'unfin_region2.html'};
const RIBBONS={
 r1a:{to:'r1b',col:'#8F909C'},r1b:{to:'r1a',col:'#8F909C'},
 r1d:{to:'r2b',col:'#B5585A'},r2b:{to:'r1d',col:'#B5585A'},
 r2a:{to:'r2c',col:'#D9823F'},r2c:{to:'r2a',col:'#D9823F'},
 r2d:{to:'r1s',col:'#5D7FA8',secret:true},r1s:{to:'r2d',col:'#5D7FA8',secret:true,hidden:true},
 r1c:{to:'r3a',col:'#7FA7B8'},r2e:{to:'r3b',col:'#7FA7B8'},
};
function buildMap(){
 const g=new Uint8Array(W*H).fill(1);
 const set=(x,y,v)=>{if(x>0&&y>0&&x<W-1&&y<H-1)g[y*W+x]=v;};
 const get=(x,y)=>(x<0||y<0||x>=W||y>=H)?1:g[y*W+x];
 const carve=r=>{for(let j=r.y;j<r.y+r.h;j++)for(let i=r.x;i<r.x+r.w;i++)set(i,j,0);};
 ROOMS.forEach(carve);PASS.forEach(carve);SHAFTS.forEach(carve);
 const hash=(x,y)=>((Math.imul(x,73856093)^Math.imul(y,19349663))>>>0)%100;
 const snap=g.slice();
 for(let j=2;j<H-2;j++)for(let i=2;i<W-2;i++){if(snap[j*W+i]!==1)continue;if(snap[(j-1)*W+i]===0)continue;
  const open=snap[j*W+i-1]===0||snap[j*W+i+1]===0||snap[(j-1)*W+i]===0||snap[(j+1)*W+i]===0;if(open&&hash(i,j)<0/*벽·천장 울퉁불퉁 끔 (평평하게)*/)g[j*W+i]=0;}
 ROOMS.filter(r=>r.stairs).forEach(r=>{const f=r.y+r.h;for(let k=1;;k++){const row=f-3*k;if(row<r.y+2)break;
  const xs=r.stairs==='L'?(k%2?r.x:r.x+5):(k%2?r.x+r.w-5:r.x+r.w-10);for(let i=xs;i<xs+5;i++)if(get(i,row)===0)set(i,row,4);}});
 SHAFTS.filter(s=>!s.noLedges).forEach(s=>{for(let k=1;;k++){const row=s.base-3*k;if(row<s.y+1)break;const xs=k%2?s.x:s.x+s.w-2;for(let i=xs;i<xs+2;i++)if(get(i,row)===0)set(i,row,4);}});
 // 계단식 언덕
 const keep=SPAWNS.concat(PICKS).filter(s=>!['bat','fly'].includes(s[0]));
 ROOMS.filter(r=>!r.arena&&!r.noHills&&r.w>=12).forEach((r,ri)=>{const f=r.y+r.h,maxH=r.h>=10?2:1;let prevH=0;
  for(let bx=r.x+3;bx<r.x+r.w-3;bx+=4){const roll=hash(bx*5+ri,f*11)%100;let h=roll<35?0:roll<75?1:2;h=Math.min(h,maxH,prevH+1);h=Math.max(h,prevH-1,0);
   for(let x=bx;x<Math.min(bx+4,r.x+r.w-3);x++){let ok=g[f*W+x]===1&&!keep.some(s=>Math.abs(s[1]-x)<=2&&Math.abs(s[2]-f)<=1);
    for(let y=f-1-h-3;y<f;y++)if(y>r.y&&get(x,y)!==0)ok=false;if(!ok){h=0;continue;}for(let y=f-h;y<f;y++)set(x,y,1);}prevH=h;}});
 // ---- 2지역 기믹
 // 녹는 크레파스 발판 (15): 첫 능선 소개, 바람개비 징검다리, 화로 작업장
 [[38,33,3],[46,30,3],[86,9,3],[93,9,3],[100,9,3],[65,67,3],[72,66,3],[79,67,3]].forEach(([x,y,w])=>{for(let i=x;i<x+w;i++)set(i,y,15);});
 // 징검다리 아래 늪 (3): 바람개비 언덕 바닥
 for(let i=86;i<=104;i++)set(i,12,3);
 // 화로 작업장 바닥 늪
 for(let i=64;i<=86;i++)set(i,70,3);
 // 밀랍 웅덩이 (16, 느려짐): 저장고 바닥 일부
 for(let i=20;i<=40;i++)set(i,94,16);
 // 흑연 덩굴 (5): 노을 언덕 → 덩굴 너머 다락
 for(let j=84;j<=87;j++){set(126,j,5);set(127,j,5);}
 // 낙서 벽 (2): 저장고 서쪽 → 지워진 그림 창고
 for(let j=90;j<=93;j++){set(8,j,2);set(9,j,2);}
 // 과녁 문 (13): 협곡 바닥 서쪽
 for(let j=118;j<=123;j++){set(124,j,13);set(125,j,13);set(126,j,13);set(127,j,13);}
 for(let j=114;j<=117;j++)for(let i=124;i<=127;i++)set(i,j,1);
 for(let i=128;i<=141;i++)if(get(i,123)===1)set(i,123,0); // 과녁 사선: 튕긴 탄이 막히지 않게 평평하게
 // 물감 문 (18): 삼색 광장 동쪽 → 보스
 for(let j=30;j<=35;j++){set(168,j,18);set(169,j,18);}
 // 크레파스 탑: 양쪽 밀랍 벽 (8)
 for(let j=12;j<=49;j++){if(get(261,j)!==0||j<44)set(261,j,8);if(!(j>=20&&j<=23))set(269,j,8);}
 for(let j=44;j<=49;j++)set(261,j,0);
 for(let j=20;j<=23;j++)set(269,j,0); // 21 탑 옆 틈새 입구
 for(let j=16;j<=19;j++){set(260,j,0);set(261,j,0);} // 해님 다락 입구 (탑 서쪽 벽 구멍)
 // 낭떠러지 아래·늪 바닥
 for(let i=219;i<=233;i++){set(i,88,3);set(i,89,1);}
 // 물속 동굴 (9): 저장고 아래 연결 통로 + 동굴
 for(let j=94;j<=107;j++)for(let i=12;i<=15;i++)set(i,j,9);
 for(let i=10;i<=17;i++)if(get(i,94)===16)set(i,94,9);
 // 광맥: 늪 + 녹는 발판
 for(let i=206;i<=229;i++)set(i,124,3);
 [[208,121,3],[215,121,3],[222,121,3]].forEach(([x,y,w])=>{for(let i=x;i<x+w;i++)set(i,y,15);});
 // 물감 폭포 홀: 동쪽 밀랍 벽 + 틈새 입구
 for(let j=96;j<=123;j++)set(271,j,j>=102&&j<=105?0:8);
 for(let j=96;j<=101;j++)set(271,j,8);
 // 연병장 발판
 [[230,44,5],[242,40,6],[253,44,5]].forEach(([x,y,w])=>{for(let i=x;i<x+w;i++)set(i,y,4);});
 SPAWNS.concat(PICKS).forEach(s=>{if(!['bat','fly','bar'].includes(s[0])){if(!g[s[2]*W+s[1]])set(s[1],s[2],1);set(s[1],s[2]-1,0);}});
 for(let i=0;i<W;i++){g[i]=1;g[(H-1)*W+i]=1;}for(let j=0;j<H;j++){g[j*W]=1;g[j*W+W-1]=1;}
 return g;
}
module.exports={T,W,H,ZONES,ROOMS,PASS,SHAFTS,SPAWNS,PICKS,START,buildMap};
