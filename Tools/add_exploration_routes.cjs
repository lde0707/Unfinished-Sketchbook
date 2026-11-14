// Author actual cells once, synchronizing HTML, standalone data and Unity export.
const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),crypto=require('node:crypto'),cp=require('node:child_process');
const sourcePath='Prototype/world_map_data.js',source=fs.readFileSync(sourcePath,'utf8');
const d=JSON.parse(vm.runInNewContext(source+'\nJSON.stringify({W,H,ROOMS,PASS,SHAFTS,PICKS,grid:Array.from(buildMap())})'));
const ledger='Book/Assets/Unfin/Resources/ExplorationLayout.json';
if(fs.existsSync(ledger)){console.log('Exploration layout already authored; run validation instead of applying twice.');process.exit(0);}
const before=[...d.grid],grid=[...before];
function rect(x,y,w,h,t){for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++){assert(xx>0&&xx<d.W-1&&yy>0&&yy<d.H-1);const i=yy*d.W+xx;assert([0,1,4,6].includes(grid[i]),`Protected cell ${xx},${yy} (${grid[i]})`);grid[i]=t;}}
function shaft(x,y,w,h){rect(x,y,w,h,0);for(let yy=y+3,n=0;yy<y+h;yy+=3,n++)rect(n%2?x+w-3:x,yy,3,1,4);}
// Open choice before the mine: left shaft or the existing east mine lift.
shaft(142,105,5,25);rect(141,128,8,4,0);rect(144,129,3,1,4);
// A small reward room is visible through a scribble seal on the new descent.
rect(122,111,16,9,0);rect(137,115,6,4,0);rect(138,115,1,4,2);
// Starting room's right-hand detour becomes a genuine return loop after Dash.
rect(261,127,1,4,2);shaft(218,107,5,9);rect(218,112,47,4,0);rect(220,113,3,1,4);
// Two exits from the workshop: upper free circuit and lower Parry shortcut.
rect(381,66,24,4,0);shaft(400,66,5,16);
rect(400,84,3,1,4);rect(402,87,3,1,4);
rect(398,90,15,4,0);rect(400,90,1,4,13);
// Preserve visible upper ledges where new passages meet the original rooms.
rect(142,105,3,1,4);rect(253,113,2,1,4);
const rooms=[{r:1,n:31,z:'B',name:'연필밥 수장고',x:122,y:111,w:16,h:9,info:'갈림굴 서쪽 승강로에서 먼저 보이는 낙서 봉인 · 지우개 대시 후 체력 조각 회수'}];
d.ROOMS.push(...rooms);
const passages=[
 {r:1,z:'A',name:'되돌아보는 종이 회랑',x:218,y:112,w:47,h:4},
 {r:1,z:'B',name:'수장고 창문길',x:137,y:115,w:6,h:4},
 {r:2,z:'F',name:'공방 동쪽 우회랑',x:381,y:66,w:24,h:4},
 {r:2,z:'G',name:'튕김의 낮은 회랑',x:398,y:90,w:15,h:4}
];d.PASS.push(...passages);
const shafts=[{r:1,z:'B',name:'갈림굴 서쪽 승강로',x:142,y:105,w:5,h:25,base:132,kind:'up'},
 {r:1,z:'A',name:'종이 회랑 돌아오는 굴',x:218,y:107,w:5,h:9,base:116,kind:'up'},
 {r:2,z:'F',name:'공방 동쪽 승강로',x:400,y:66,w:5,h:16,base:88,kind:'up'}];d.SHAFTS.push(...shafts);
const rewards=[['heart',127,120,{exploration:'연필밥 수장고'}],['healup',87,45,{exploration:'밀랍 둥지 재방문'}]];d.PICKS.push(...rewards);
const frontiers=[
 {id:'start-return',name:'시작길 너머 종이 회랑',ability:'dash',x:261,y:127,w:1,h:4,reward:'2개 방을 잇는 귀환 순환로'},
 {id:'archive',name:'연필밥 수장고',ability:'dash',x:138,y:115,w:1,h:4,reward:'체력 조각'},
 {id:'mine-cache',name:'덩굴 뒤 창고',ability:'cut',x:181,y:128,w:2,h:4,reward:'회복 물감'},
 {id:'tree-road',name:'덩굴 길',ability:'cut',x:130,y:96,w:2,h:4,reward:'나무 구역으로 가는 길'},
 {id:'shield',name:'과녁 뒤 서재',ability:'parry',x:153,y:86,w:2,h:6,reward:'종이 방패 · 첫 피격 흡수'},
 {id:'wax-nest',name:'밀랍 둥지',ability:'climb',x:92,y:55,w:5,h:1,reward:'회복 물감 최대 횟수 +1',kind:'climb'},
 {id:'mine-water',name:'잠긴 수로 끝',ability:'swim',x:102,y:132,w:4,h:2,reward:'흑연 조각',kind:'future'},
 {id:'workshop-loop',name:'튕김의 낮은 회랑',ability:'parry',x:400,y:90,w:1,h:4,reward:'공방–협곡 순환로'},
 {id:'canyon-heart',name:'협곡 과녁 방',ability:'parry',x:404,y:118,w:4,h:6,reward:'체력 조각'},
 {id:'waterfall',name:'폭포 뒤 틈',ability:'climb',x:550,y:119,w:2,h:3,reward:'회복 물감 최대 횟수 +1',kind:'climb'},
 {id:'workshop-water',name:'잠긴 물웅덩이 동굴',ability:'swim',x:293,y:96,w:3,h:3,reward:'흑연 조각',kind:'future'},
 {id:'wind-cave',name:'바람 굴',ability:'glide',x:500,y:85,w:4,h:2,reward:'흑연 조각',kind:'future'},
 {id:'pigment-bridge',name:'색 막대 다리',ability:'parry',x:458,y:86,w:1,h:2,reward:'협곡 동쪽 길',kind:'bridge'}
];
const changes=[];for(let i=0;i<grid.length;i++)if(grid[i]!==before[i])changes.push({x:i%d.W,y:i/d.W|0,before:before[i],after:grid[i]});
// No original enemy/pickup is buried; hazardous terrain and existing locks survive.
for(const p of d.PICKS){const[_,x,y]=p;assert(![1,2,5,13].includes(grid[(y-1)*d.W+x]),`Buried pickup ${p}`);}
const runs=[];let v=grid[0],n=0;for(const t of grid){if(t===v)n++;else{runs.push(v.toString(36)+'.'+n.toString(36));v=t;n=1;}}runs.push(v.toString(36)+'.'+n.toString(36));
let updated=source;for(const[k,val]of Object.entries({ROOMS:d.ROOMS,PASS:d.PASS,SHAFTS:d.SHAFTS,PICKS:d.PICKS}))updated=updated.replace(new RegExp('^const '+k+'=.*;$','m'),'const '+k+'='+JSON.stringify(val)+';');
updated=updated.replace(/const MAPRLE='[^']*';/,"const MAPRLE='"+runs.join(',')+"';");
const hp='Prototype/unfin_world.html',html=fs.readFileSync(hp,'utf8'),start=html.indexOf('const T=16,'),end=html.indexOf('/* ---------- 플레이 맵: 게임 ---------- */',start);assert(start>=0&&end>start);
fs.writeFileSync(sourcePath,updated);fs.writeFileSync(hp,html.slice(0,start)+updated.slice(updated.indexOf('const T=16,'))+'\n'+html.slice(end));
const hash=a=>crypto.createHash('sha256').update(Buffer.from(a)).digest('hex');
fs.writeFileSync(ledger,JSON.stringify({schema:1,date:'2026-10-21',baseGridSha256:hash(before),gridSha256:hash(grid),frontiers,rooms,passages,shafts,changes,rewards,loops:[{name:'갈림굴–광산 양갈래',via:'서쪽 승강로 / 기존 동쪽 승강로',requires:'walljump'},{name:'시작길–비탈 귀환',via:'풀 그림자 굴 / 새 종이 회랑',requires:'dash'},{name:'공방–협곡 상하 순환',via:'동쪽 우회랑 / 낮은 회랑',requires:'parry'}]},null,2)+'\n');
cp.execFileSync(process.execPath,['Tools/export_unity_world.cjs'],{stdio:'inherit'});
// Existing mezzanine checks remain valid; topology hash now includes the new routes.
const mp='Book/Assets/Unfin/Data/room-mezzanines.json',m=JSON.parse(fs.readFileSync(mp));m.gridSha256=hash(grid);fs.writeFileSync(mp,JSON.stringify(m,null,2)+'\n');
console.log(JSON.stringify({changedCells:changes.length,frontiers:frontiers.length,loops:3,newRooms:rooms.length}));
