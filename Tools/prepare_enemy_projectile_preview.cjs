const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const world=JSON.parse(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Data/world12.json'),'utf8').replace(/^\uFEFF/,''));
const manifest=JSON.parse(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Resources/EnemyAnimations/manifest.json'),'utf8'));
const cells=[];for(const run of world.mapRle.split(',')){const [v,n]=run.split('.').map(v=>parseInt(v,36));for(let i=0;i<n;i++)cells.push(v);}
const get=(x,y)=>cells[y*world.width+x]??1;
const wall=t=>t>0&&![3,9,4,6,15].includes(t);
const examples=['R102','COMMON03','D02'].map(id=>{
 const placement=manifest.placements.find(p=>p.id===id),enemy=world.enemies[placement.sourceIndex],art=manifest.enemies.find(e=>e.id===id),choices=[];
 for(let x=enemy.tx-5;x<=enemy.tx+5;x++)for(let y=enemy.floor-2;y<=enemy.floor+3;y++){
  if(![1,4,16].includes(get(x,y))||get(x,y-1)!==0||get(x,y-2)!==0)continue;
  const side=x<enemy.tx?-1:1,ox=(enemy.x+enemy.w*.5)/16+side*art.bodyWidth*(id==='R102'?.55:id==='COMMON03'?.27:.45),oy=-enemy.floor+art.bodyHeight*(id==='R102'?.65:id==='COMMON03'?.8:.55);
  let clear=true;const steps=Math.ceil(Math.hypot(x+.5-ox,-y+.6875-oy)*32);
  for(let i=1;i<=steps;i++){const t=i/steps;if(wall(get(Math.floor(ox+(x+.5-ox)*t),Math.floor(-(oy+(-y+.6875-oy)*t))))){clear=false;break;}}
  if(!clear)continue;
  choices.push({x,y,score:Math.abs(x-(enemy.tx-4))+Math.abs(y-enemy.floor)*3});
 }
 choices.sort((a,b)=>a.score-b.score);if(!choices.length)throw Error('No safe projectile preview spawn: '+id);
 return {id,name:art.name,sourceIndex:placement.sourceIndex,spawn:`${choices[0].x},${choices[0].y}`,motion:id==='R102'?'수평 흑연탄':id==='COMMON03'?'포물선 잉크탄 · 착탄 웅덩이':'씨앗탄 · 한 번 튀기'};
});
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><title>원거리 몬스터 · 투사체</title><style>body{margin:0;background:#171b20;color:#eee7db;font:15px system-ui}header{padding:16px 24px}h1{font-size:23px;margin:0 0 8px}p{color:#c2c2b8;margin:5px 0}nav{display:flex;gap:8px;padding:0 24px 12px}button{background:#2c343c;color:#eee7db;border:1px solid #657078;border-radius:5px;padding:9px 14px;cursor:pointer}button.active{border-color:#dfc08b;color:#ffe1b4}iframe{border:0;width:100%;height:calc(100vh - 165px);min-height:620px}</style><header><h1>원거리 몬스터 · 실제 Unity 투사체</h1><p>A/D 이동 · Space 점프 · J/클릭으로 탄 베기 · Shift 대시 · R 초기화</p><p id="status">불러오는 중…</p></header><nav></nav><iframe id="game" src="/?v=enemy-projectiles-20261019" allow="autoplay; fullscreen"></iframe><script>
const examples=${JSON.stringify(examples)};let selected=examples[0],ready=false;
function show(e){selected=e;document.querySelectorAll('button').forEach(b=>b.classList.toggle('active',b.dataset.id===e.id));if(!ready)return;const f=document.querySelector('iframe');f.contentWindow.unityInstance.SendMessage('World Prototype','PreviewPosition',e.spawn);f.contentWindow.focus();document.querySelector('#status').textContent=e.name+' — '+e.motion;}
for(const e of examples){const b=document.createElement('button');b.dataset.id=e.id;b.textContent=e.name;b.onclick=()=>show(e);document.querySelector('nav').append(b);}
const timer=setInterval(()=>{const w=document.querySelector('iframe').contentWindow;if(w.unityInstance){clearInterval(timer);setTimeout(()=>{ready=true;show(selected)},5000)}},300);
</script></html>`;
fs.writeFileSync(path.join(root,'Builds/WebGLSite/dist/enemy-projectile-preview.html'),html);
fs.writeFileSync(path.join(root,'Book/Validation/enemy-projectile-examples.json'),JSON.stringify(examples,null,2));console.log(examples);
