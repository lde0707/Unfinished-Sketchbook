// Reproducible edits to the combined HTML/Unity map. All coordinates are logical cells.
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),file=p=>path.join(root,p);
const source=fs.readFileSync(file('Prototype/world_map_data.js'),'utf8');
const data=JSON.parse(vm.runInNewContext(source+'\nJSON.stringify({W,H,ROOMS,PASS,SHAFTS,SPAWNS,PICKS,START,grid:Array.from(buildMap())})'));
const hash=g=>crypto.createHash('sha256').update(Buffer.from(g)).digest('hex');
const spec=[
 {r:1,n:9,description:'잉크 위 중간층 → 반대편 상층 선반 → 기존 서쪽 계단으로 돌아오는 순환로',
  floors:[[76,123,13,2],[62,117,12,2]],clear:[[65,120,5,1]],steps:[[71,126,4],[75,120,4]],
  routes:[[[65,70,126],[71,75,126]],[[71,75,126],[76,89,123]],[[76,89,123],[75,79,120]],[[75,79,120],[62,74,117]],[[62,74,117],[65,70,114]]]},
 {r:1,n:14,description:'무너지는 계단 사이에 세 개의 두꺼운 휴식층 · 중앙층에서 좌우 둥지 출입구로 연결',
  floors:[[106,48,11,2],[101,36,11,2],[108,24,11,2]],clear:[[100,39,5,1]],steps:[[109,42,4],[112,39,4],[112,36,5],[105,27,3]],
  routes:[[[100,105,51],[106,117,48]],[[106,117,48],[100,105,45]],[[100,105,45],[105,113,42]],[[105,113,42],[112,116,39]],[[112,116,39],[101,117,36]],[[101,117,36],[100,105,33]],[[100,105,33],[105,110,30]],[[105,110,30],[100,108,27]],[[100,108,27],[108,119,24]],[[108,119,24],[100,105,21]]]},
 {r:2,n:3,description:'중앙 2층을 양쪽 발판으로 접근 · 상층 발판에서 바람개비 수직굴로 연결',
  floors:[[368,28,15,2]],clear:[],steps:[[363,31,4],[384,31,4],[384,25,4]],
  routes:[[[363,367,34],[363,367,31]],[[363,367,31],[368,383,28]],[[368,383,28],[384,388,25]],[[384,388,25],[390,392,25]],[[384,388,34],[384,388,31]],[[384,388,31],[368,383,28]]]},
 {r:2,n:4,description:'저장 지점 위 중앙 언덕 → 광장 상층 회랑 → 동쪽 물감 문 앞 하층으로 합류',
  floors:[[427,30,14,2]],clear:[],steps:[[424,33,3],[443,33,3]],
  routes:[[[423,426,35],[424,427,33]],[[424,427,33],[427,441,30]],[[427,441,30],[443,446,33]],[[443,446,36],[443,446,33]],[[443,446,33],[427,441,30]]]},
 {r:2,n:7,description:'밀랍 웅덩이 위 엇갈린 두 중간층 · 중앙 승강로에 합류하고 오른쪽 출입구로 내려오는 동선',
  floors:[[297,88,12,2],[316,85,11,2]],clear:[],steps:[[293,91,4],[310,85,3]],
  routes:[[[296,299,94],[293,297,91]],[[293,297,91],[297,309,88]],[[297,309,88],[310,313,85]],[[310,313,85],[316,327,85]],[[316,327,85],[313,315,82]],[[313,315,82],[310,312,79]]]},
 {r:2,n:18,description:'서쪽 발판 → 세 개의 엇갈린 중간층 → 기존 흑연 선반 · 동쪽 밀랍 벽의 벽타기 구간은 보존',
  floors:[[525,115,10,2],[530,109,7,2],[523,103,9,2]],clear:[[525,118,3,1],[523,106,5,1]],steps:[[520,118,5],[528,112,2],[533,106,4]],
  routes:[[[518,523,121],[520,525,118]],[[520,525,118],[525,535,115]],[[525,535,115],[523,530,112]],[[523,530,112],[530,537,109]],[[530,537,109],[533,537,106]],[[533,537,106],[523,532,103]],[[523,532,103],[523,528,100]]]}
];
const ledgerPath=file('Book/Assets/Unfin/Data/room-mezzanines.json');
const prior=fs.existsSync(ledgerPath)?JSON.parse(fs.readFileSync(ledgerPath)):null;
// Permit an intentional revision of this design by restoring only its previous edits.
const before=[...data.grid];
if(prior)for(const e of prior.changes){assert.equal(before[e.y*data.W+e.x],e.after,'Map changed since the mezzanine edit');before[e.y*data.W+e.x]=e.before;}
const grid=[...before];
for(const s of spec){
 const room=data.ROOMS.find(a=>a.r===s.r&&a.n===s.n);assert(room);s.name=room.name;
 const put=(rect,type)=>{const[x,y,w,h=1]=rect;for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++){
  assert(xx>=room.x&&xx<room.x+room.w&&yy>=room.y&&yy<room.y+room.h,'Edit outside selected room');
  const i=yy*data.W+xx;assert([0,1,4,6].includes(grid[i]),`Protected gameplay tile at ${xx},${yy}`);
  if(type===0)assert([0,4,6].includes(grid[i]),'Only remove obsolete thin platforms');
  grid[i]=type;
 }};
 s.clear.forEach(a=>put(a,0));s.floors.forEach(a=>put(a,1));s.steps.forEach(a=>put(a,4));
 room.info=room.info.replace(/ · 중간층:.*$/,'')+' · 중간층: '+s.description;
}
const changes=[];
for(let i=0;i<grid.length;i++)if(grid[i]!==before[i])changes.push({x:i%data.W,y:Math.floor(i/data.W),before:before[i],after:grid[i]});
// No original solid wall, exit, water, wax, progression gate, collectible, or enemy is deleted.
assert(changes.every(e=>[0,4,6].includes(e.before)));
const exported=JSON.parse(fs.readFileSync(file('Book/Assets/Unfin/Data/world12.json')));
for(const marker of [...exported.enemies,...exported.pickups]){
 for(let yy=Math.floor(marker.y/16);yy<Math.ceil((marker.y+marker.h)/16);yy++)for(let xx=Math.floor(marker.x/16);xx<Math.ceil((marker.x+marker.w)/16);xx++){
  const i=yy*data.W+xx;if(before[i]!==1)assert.notEqual(grid[i],1,`${marker.type} buried at ${marker.tx},${marker.floor}`);
 }
}
// Keep the right-hand climb-only secret approach in the waterfall hall untouched.
for(let y=96;y<124;y++)for(let x=540;x<560;x++)assert.equal(grid[y*data.W+x],before[y*data.W+x]);
const wall=t=>t>0&&t!==9&&![4,6,15].includes(t);
for(let i=data.W*2;i<grid.length;i++)if([4,6,15].includes(grid[i])&&!wall(before[i-data.W])&&!wall(before[i-data.W*2]))
 assert(!wall(grid[i-data.W])&&!wall(grid[i-data.W*2]),`Newly blocked platform headroom at ${i%data.W},${Math.floor(i/data.W)}`);
assert(!changes.some(e=>e.after===1&&[...data.PASS,...data.SHAFTS].some(r=>e.x>=r.x&&e.x<r.x+r.w&&e.y>=r.y&&e.y<r.y+r.h)),'New floor blocks an existing passage or shaft');
const runs=[];let v=grid[0],n=0;for(const t of grid){if(t===v)n++;else{runs.push(v.toString(36)+'.'+n.toString(36));v=t;n=1}}runs.push(v.toString(36)+'.'+n.toString(36));
const updated=source.replace(/^const ROOMS=.*;$/m,'const ROOMS='+JSON.stringify(data.ROOMS)+';').replace(/const MAPRLE='[^']*';/,"const MAPRLE='"+runs.join(',')+"';");
const htmlPath=file('Prototype/unfin_world.html'),html=fs.readFileSync(htmlPath,'utf8');
const start=html.indexOf('const T=16,'),end=html.indexOf('/* ---------- 플레이 맵: 게임 ---------- */',start);assert(start>=0&&end>start);
if(updated!==source)fs.writeFileSync(file('Prototype/world_map_data.js'),updated);
const updatedHtml=html.slice(0,start)+updated.slice(updated.indexOf('const T=16,'))+'\n'+html.slice(end);
if(updatedHtml!==html)fs.writeFileSync(htmlPath,updatedHtml);
const report={schema:1,date:'2026-10-18',source:'Tools/add_room_mezzanines.cjs',baseGridSha256:hash(before),gridSha256:hash(grid),rooms:spec,changes,
 routeChecks:spec.flatMap(s=>s.routes.map(([a,b],i)=>({room:`${s.r}-${s.n}`,label:`${s.name} / ${i+1}`,fromX:a[0],fromEnd:a[1],fromY:a[2],toX:b[0],toEnd:b[1],toY:b[2]}))),
 solidFloors:spec.flatMap(s=>s.floors.map(([x,y,w,h])=>({r:s.r,n:s.n,x,y,w,h}))),
 preserved:['room bounds and low ceilings','all original solid walls and progression tiles','all markers and start position','waterfall wall-climb approach'],
 checks:{newlyBlockedPlatforms:0,blockedPassageOrShaftCells:0,allMarkerBodiesClear:true},
 thickFloors:spec.reduce((n,s)=>n+s.floors.length,0)};
fs.writeFileSync(ledgerPath,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({rooms:spec.length,thickFloors:report.thickFloors,changedCells:changes.length,gridSha256:report.gridSha256},null,2));
