from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
base=ROOT/'Art/animations/player-c-v6-sprint/preview.html';out=ROOT/'Art/animations/player-c-v6-rhythm/preview.html'
s=base.read_text(encoding='utf-8').split('<script>')[0]
s=s.replace('앞으로 달리기 12프레임','일정한 박자의 8프레임').replace('V6 SPRINT','V6 RHYTHM').replace('앞으로 숙인 달리기 · 12프레임','굽힌 무릎으로 착지 · 8프레임')
s=s.replace('양팔은 망토 안에 유지합니다. 기존 4→5와 8→1 사이에 연결 자세를 두 장씩 그렸습니다.<br>밝은 다리 = 가까운 쪽 · 어두운 다리 = 먼 쪽. 새 번호는 1부터 12까지입니다.','비슷한 기존 1·4·7·10번을 덜어내고, 착지와 통과 자세의 무릎을 굽혔습니다.<br>밝은 다리 = 가까운 쪽 · 어두운 다리 = 먼 쪽. 모든 프레임을 80ms로 재생합니다.')
s=s.replace('<button id="bridgeA">기존 4→5 확인</button><button id="bridgeB">기존 8→1 확인</button><button id="all">전체 반복</button>','')
s=s.replace('앞으로 달리는 수정안','무릎 착지 · 일정한 박자').replace('12장 · 전체 0.8초 · 직접 그린 연결 자세 4장','8장 · 80ms씩 · 전체 0.64초')
s=s.replace('수정 전 · 양팔을 감춘 v6','수정 전 · 12프레임').replace('원래 자세 비교 · 추가 구간은 직전 원본 자세 표시','재생 중 기존 0.8초 주기 · 정지 시 대응 자세 비교')
s=s.replace('max="11"','max="7"').replace('1 / 12','1 / 8').replace('12프레임 전체','8프레임 전체').replace('12프레임 달리기','8프레임 달리기')
s=s.replace('전신 원화 수정과 추가 프레임은 내장 imagegen으로 제작했습니다.','착지·통과 원화 4장은 내장 imagegen으로 수정하고, 나머지 4장은 그대로 사용했습니다.')
s+='''<script>
const $=id=>document.getElementById(id),oc=$('ours').getContext('2d'),rc=$('reference').getContext('2d');
const notes=['가까운 무릎 앞으로 · 낮은 전진','가까운 다리: 무릎 굽혀 몸 가까이 착지','가까운 다리: 체중 지지 · 먼 무릎 회수','가까운 발 뒤로 밀기 · 먼 무릎 앞으로','먼 무릎 앞으로 · 낮은 전진','먼 다리: 무릎 굽혀 몸 가까이 착지','먼 다리: 체중 지지 · 가까운 무릎 회수','먼 발 뒤로 밀기 · 가까운 무릎 앞으로'];
const state={frame:0,referenceFrame:0,playing:true,ready:false,frameCount:8,inbetweenCount:0,elapsed:0,referenceElapsed:0,last:0,referenceAvailable:false};window.previewState=state;let art=[],raw=[],registration,previous;
function img(src){return new Promise((resolve,reject)=>{const i=new Image();i.onload=()=>resolve(i);i.onerror=()=>reject(Error(src));i.src=src})}
function draw(c,im,head){c.fillStyle='#242a30';c.fillRect(0,0,512,512);if($('floor').checked){c.strokeStyle='#758591';c.beginPath();c.moveTo(20,448.5);c.lineTo(492,448.5);c.stroke()}c.save();if($('flip').checked){c.translate(512,0);c.scale(-1,1)}c.drawImage(im,0,0);if($('headGuide').checked){c.strokeStyle='#9ebcba';c.setLineDash([4,4]);c.beginPath();c.moveTo(head[0]-90,head[1]);c.lineTo(head[0]+90,head[1]);c.stroke()}c.restore()}
function render(){if(!state.ready)return;const n=state.frame,m=registration.frames[n],r=state.referenceFrame;draw(oc,art[n],m.headCenter);draw(rc,raw[r],previous.frames[r].headCenter);$('scrub').value=n;$('position').textContent=`${n+1} / 8`;$('role').textContent=`${n+1}번 · 이전 ${m.previousFrame}번 · 80ms — ${notes[n]}`;document.querySelectorAll('[data-frame]').forEach(b=>b.classList.toggle('active',+b.dataset.frame===n));state.displayedFrame=n;state.headY=m.headCenter[1]}
function pause(){state.playing=false;$('play').textContent='재생'}
function select(n){pause();state.frame=(n+8)%8;state.elapsed=state.frame*80;state.referenceFrame=registration.frames[state.frame].referenceFrame;state.referenceElapsed=previous.frames.slice(0,state.referenceFrame).reduce((s,m)=>s+m.durationMs,0);render()}
window.setPreviewFrame=select;$('prev').onclick=()=>select(state.frame-1);$('next').onclick=()=>select(state.frame+1);$('scrub').oninput=()=>select(+$('scrub').value);$('play').onclick=()=>{state.playing=!state.playing;$('play').textContent=state.playing?'일시정지':'재생'};['flip','floor','headGuide'].forEach(id=>$(id).onchange=render);
function tick(t){const dt=state.last?Math.min(t-state.last,100):0;state.last=t;if(state.playing&&state.ready){state.elapsed=(state.elapsed+dt*+$('speed').value)%640;state.frame=Math.floor(state.elapsed/80);state.referenceElapsed=(state.referenceElapsed+dt*+$('speed').value)%800;let e=state.referenceElapsed;for(let i=0;i<12;i++){state.referenceFrame=i;if(e<previous.frames[i].durationMs)break;e-=previous.frames[i].durationMs}render()}requestAnimationFrame(tick)}
(async()=>{try{[registration,previous]=await Promise.all([fetch('registration.json').then(r=>r.json()),fetch('../player-c-v6-sprint/registration.json').then(r=>r.json())]);[art,raw]=await Promise.all([Promise.all(Array.from({length:8},(_,i)=>img(`frames/${String(i).padStart(2,'0')}.png`))),Promise.all(Array.from({length:12},(_,i)=>img(`../player-c-v6-sprint/frames/${String(i).padStart(2,'0')}.png`)))]);for(let i=0;i<8;i++){const b=document.createElement('button');b.textContent=i+1;b.dataset.frame=i;b.onclick=()=>select(i);$('keys').appendChild(b)}state.ready=true;state.referenceAvailable=true;render();requestAnimationFrame(tick)}catch(e){$('error').textContent=e.message}})();
</script></html>'''
out.write_text(s,encoding='utf-8')
