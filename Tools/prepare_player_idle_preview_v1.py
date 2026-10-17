from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'Art/animations/player-c-v6-rhythm/preview.html').read_text(encoding='utf-8').split('<script>')[0]
s=s.replace('일정한 박자의 8프레임','조용한 호흡 Idle').replace('V6 RHYTHM','IDLE / V1').replace('굽힌 무릎으로 착지 · 8프레임','가만히 서서 숨 쉬기')
s=s.replace('비슷한 기존 1·4·7·10번을 덜어내고, 착지와 통과 자세의 무릎을 굽혔습니다.<br>밝은 다리 = 가까운 쪽 · 어두운 다리 = 먼 쪽. 모든 프레임을 80ms로 재생합니다.','발은 바닥에 둔 채 몸과 망토만 작게 들썩이는 Idle 시안입니다.<br>오른쪽 달리기는 확정한 버전 그대로입니다.')
s=s.replace('무릎 착지 · 일정한 박자','Idle · 검토용 시안').replace('8장 · 80ms씩 · 전체 0.64초','4개 원화 · 왕복 6프레임 · 1.8초 호흡')
s=s.replace('수정 전 · 12프레임','달리기 · 사용자 확정').replace('재생 중 기존 0.8초 주기 · 정지 시 대응 자세 비교','8프레임 · 80ms씩 · 원본 그대로')
s=s.replace('max="7"','max="5"').replace('1 / 8','1 / 6').replace('8프레임 전체 보기','Idle 원화 4장').replace('run-contact-sheet.jpg','idle-contact-sheet.jpg').replace('8프레임 달리기','Idle 원화')
s=s.replace('착지·통과 원화 4장은 내장 imagegen으로 수정하고, 나머지 4장은 그대로 사용했습니다.','Idle 원화는 내장 imagegen으로 제작했습니다. 네 자세를 순방향·역방향으로 이어 호흡이 반복됩니다.')
s=s.replace('run-atlas.png','idle-atlas.png')
s+='''<script>
const $=id=>document.getElementById(id),oc=$('ours').getContext('2d'),rc=$('reference').getContext('2d');
const notes=['편안히 내쉼','천천히 들이마심','가볍게 올라옴','들이마신 끝','천천히 내려옴','편안히 내쉬는 중'];
const state={frame:0,referenceFrame:0,playing:true,ready:false,frameCount:6,elapsed:0,referenceElapsed:0,last:0,referenceAvailable:false};window.previewState=state;let art=[],raw=[],registration,run;
function img(src){return new Promise((resolve,reject)=>{const i=new Image();i.onload=()=>resolve(i);i.onerror=()=>reject(Error(src));i.src=src})}
function draw(c,im,head){c.fillStyle='#242a30';c.fillRect(0,0,512,512);if($('floor').checked){c.strokeStyle='#758591';c.beginPath();c.moveTo(20,448.5);c.lineTo(492,448.5);c.stroke()}c.save();if($('flip').checked){c.translate(512,0);c.scale(-1,1)}c.drawImage(im,0,0);if($('headGuide').checked){c.strokeStyle='#9ebcba';c.setLineDash([4,4]);c.beginPath();c.moveTo(head[0]-90,head[1]);c.lineTo(head[0]+90,head[1]);c.stroke()}c.restore()}
function render(){if(!state.ready)return;const n=state.frame,m=registration.frames[n],r=state.referenceFrame;draw(oc,art[n],m.headCenter);draw(rc,raw[r],run.frames[r].headCenter);$('scrub').value=n;$('position').textContent=`${n+1} / 6`;$('role').textContent=`${n+1}번 · 원화 ${m.key+1} · ${notes[n]}`;document.querySelectorAll('[data-frame]').forEach(b=>b.classList.toggle('active',+b.dataset.frame===n));state.displayedFrame=n}
function pause(){state.playing=false;$('play').textContent='재생'}
function select(n){pause();state.frame=(n+6)%6;state.elapsed=state.frame*300;render()}
window.setPreviewFrame=select;$('prev').onclick=()=>select(state.frame-1);$('next').onclick=()=>select(state.frame+1);$('scrub').oninput=()=>select(+$('scrub').value);$('play').onclick=()=>{state.playing=!state.playing;$('play').textContent=state.playing?'일시정지':'재생'};['flip','floor','headGuide'].forEach(id=>$(id).onchange=render);
function tick(t){const dt=state.last?Math.min(t-state.last,100):0;state.last=t;if(state.playing&&state.ready){state.elapsed=(state.elapsed+dt*+$('speed').value)%1800;state.frame=Math.floor(state.elapsed/300);state.referenceElapsed=(state.referenceElapsed+dt*+$('speed').value)%640;state.referenceFrame=Math.floor(state.referenceElapsed/80);render()}requestAnimationFrame(tick)}
(async()=>{try{[registration,run]=await Promise.all([fetch('registration.json').then(r=>r.json()),fetch('../player-c-v6-rhythm/registration.json').then(r=>r.json())]);[art,raw]=await Promise.all([Promise.all(Array.from({length:6},(_,i)=>img(`frames/${String(i).padStart(2,'0')}.png`))),Promise.all(Array.from({length:8},(_,i)=>img(`../player-c-v6-rhythm/frames/${String(i).padStart(2,'0')}.png`)))]);for(let i=0;i<6;i++){const b=document.createElement('button');b.textContent=i+1;b.dataset.frame=i;b.onclick=()=>select(i);$('keys').appendChild(b)}state.ready=true;state.referenceAvailable=true;render();requestAnimationFrame(tick)}catch(e){$('error').textContent=e.message}})();
</script></html>'''
(ROOT/'Art/animations/player-c-idle-v1/preview.html').write_text(s,encoding='utf-8')
