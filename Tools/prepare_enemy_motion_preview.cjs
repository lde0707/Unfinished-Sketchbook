const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const world=JSON.parse(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Data/world12.json'),'utf8').replace(/^\uFEFF/,''));
const art=JSON.parse(fs.readFileSync(path.join(root,'Art/animations/enemies-region1-v2/manifest.json'),'utf8'));
const placements=JSON.parse(fs.readFileSync(path.join(root,'Art/animations/enemies-region1-v1/placements.json'),'utf8'));
const cells=[];for(const run of world.mapRle.split(',')){const [v,n]=run.split('.').map(s=>parseInt(s,36));for(let i=0;i<n;i++)cells.push(v);}
const get=(x,y)=>cells[y*world.width+x]??1;
const wall=t=>t>0&&![9,4,6,15].includes(t);
const profiles={COMMON01:'전방 구르기',COMMON02:'플레이어 위치를 향한 돌진',R101:'짧은 들이받기',R103:'전진 밀치기',A01:'전진 물기',A02:'낮은 도약과 착지',B01:'짧은 전진과 솟구치기',B02:'낮은 전방 돌진',B03:'플레이어 방향 돌진 후 복귀',C01:'플레이어 위치를 향한 급습',C02:'전방 도약',C03:'플레이어 방향 활공',D01:'접근 후 물기'};
const examples=Object.entries(profiles).map(([id,motion])=>{
 const preferred={COMMON02:9,C01:31};
 const p=placements.find(p=>p.id===id&&(preferred[id]===undefined||p.sourceIndex===preferred[id])),e=world.enemies[p.sourceIndex],a=art.enemies.find(e=>e.id===id),candidates=[];
 for(let x=e.tx-3;x<=e.tx+3;x++)for(let y=e.floor-2;y<=e.floor+7;y++){
  const t=get(x,y);if(t===0||t===3||t===9||wall(get(x,y-1))||wall(get(x,y-2)))continue;
  if(['COMMON02','B03','C01','C03'].includes(id)&&(y<e.floor+1||y>e.floor+5))continue;
  candidates.push({x,y,score:Math.abs(x-(e.tx-2))*2+Math.abs(y-e.floor)});
 }
 candidates.sort((a,b)=>a.score-b.score);if(!candidates.length)throw Error('No safe spawn '+id);
 return {id,name:a.name,motion,spawn:`${candidates[0].x},${candidates[0].y}`,sourceIndex:p.sourceIndex};
});
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><title>몬스터 공격 이동 · Unity 테스트</title><style>body{margin:0;background:#171b20;color:#eee7db;font:15px system-ui}header{padding:16px 24px}h1{font-size:24px;margin:0 0 8px}p{margin:6px 0;color:#b8c0c5}nav{display:flex;gap:6px;flex-wrap:wrap;padding:0 24px 12px}button{background:#2c343c;color:#eee7db;border:1px solid #657078;border-radius:5px;padding:8px;cursor:pointer}button.active{border-color:#e7bd75;color:#ffe2b2}iframe{border:0;width:100%;height:calc(100vh - 210px);min-height:620px}#status{color:#ead0a0}</style><header><h1>1지역 몬스터 · 실제 공격 이동 / 4종 프레임 보강</h1><p>종류를 누르면 실제 Unity 맵의 해당 몬스터 앞으로 이동합니다. A/D 이동 · Space 점프 · J 공격 · R 초기화</p><p id="status">Unity 불러오는 중…</p><p>피격 동작 없음 · 지형 충돌 적용 · 몬스터 공격 피해와 투사체는 아직 미구현</p></header><nav></nav><iframe id="game" src="/?v=enemy-aim-fade-20261019" allow="autoplay; fullscreen"></iframe><script>
const examples=${JSON.stringify(examples)};let selected=examples[0],ready=false;
function show(e){selected=e;document.querySelectorAll('nav button').forEach(b=>b.classList.toggle('active',b.dataset.id===e.id));if(!ready)return;const f=document.querySelector('#game');f.contentWindow.unityInstance.SendMessage('World Prototype','PreviewPosition',e.spawn);f.contentWindow.focus();document.querySelector('#status').textContent=e.name+' — '+e.motion+' / 예고 후 이동, 회복 뒤 반복';}
for(const e of examples){const b=document.createElement('button');b.textContent=e.name;b.dataset.id=e.id;b.onclick=()=>show(e);document.querySelector('nav').append(b);}
const timer=setInterval(()=>{const w=document.querySelector('#game').contentWindow;if(w.unityInstance){clearInterval(timer);ready=true;setTimeout(()=>show(selected),5000);}},300);
</script></html>`;
fs.writeFileSync(path.join(root,'Builds/WebGLSite/dist/enemy-motion-preview.html'),html);
fs.writeFileSync(path.join(root,'Book/Validation/enemy-motion-examples.json'),JSON.stringify(examples,null,2));
console.log('Prepared 13 actual Unity attack-motion examples.');
