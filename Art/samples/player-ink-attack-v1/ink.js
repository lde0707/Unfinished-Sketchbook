const c=document.querySelector('canvas'),ctx=c.getContext('2d'),status=document.querySelector('#status');
let clips,images=[],idle,age=2,stage=0,facing=1,auto=true,paused=false,enabled=true,last=0,gap=0,particles=[];
const artStage=()=>[2,0,1][stage];
const palette=['#181b28','#242430','#37313e','#75626e','#cebea4'];
function random(seed){return()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296}}
function strike(n){stage=n;age=0;gap=0;particles=[];const rng=random(712+n*233),range=(a,b)=>a+(b-a)*rng();
 for(let i=0;i<62;i++){
  const kind=[2,0,1][n];const angle=kind===2?range(-.42,.42):kind===0?range(-1.1,1.15):range(-1.55,.3),speed=range(130,710);
  const streak=i<15,paper=i>52;particles.push({x:range(-10,95),y:range(-42,42),vx:Math.cos(angle)*speed,vy:Math.sin(angle)*speed-(kind===1?130:0),life:range(.18,.42),size:paper?range(3,7):range(2,9),stretch:streak?range(3,7):range(1,1.9),color:paper?'#d3c2a5':palette[Math.floor(rng()*4)],spin:range(-5,5),rot:angle});
 }
}
function strokePath(radius,width,a0,a1,phase){ctx.beginPath();for(let j=0;j<=34;j++){let t=j/34,a=a0+(a1-a0)*t,r=radius+(Math.sin(j*2.4+phase)*3);let x=Math.cos(a)*r,y=Math.sin(a)*r*.65;j?ctx.lineTo(x,y):ctx.moveTo(x,y)}for(let j=34;j>=0;j--){let t=j/34,a=a0+(a1-a0)*t,thick=width*Math.pow(Math.sin(Math.PI*t),.6),r=radius-thick+Math.sin(j*2.2)*2;ctx.lineTo(Math.cos(a)*r,Math.sin(a)*r*.65)}ctx.closePath()}
function ink(){if(!enabled||age<0||age>.44)return;ctx.save();ctx.translate(artStage()===2?565:560,artStage()===2?412:435);ctx.scale(facing,1);
 const burst=Math.min(1,age/.025),fade=Math.max(0,1-age/.14);ctx.globalAlpha=fade;
 if(artStage()<2){const a0=artStage()===0?-1.5:-.15,a1=artStage()===0?1.1:-2.2;
  ctx.save();ctx.scale(.85+.15*burst,.85+.15*burst);strokePath(170,58,a0,a1,stage);ctx.fillStyle='#d8c7a7';ctx.fill();strokePath(165,48,a0+.015,a1-.015,stage);ctx.fillStyle='#20202c';ctx.fill();
  strokePath(186,8,a0+.18,a1-.1,stage+2);ctx.fillStyle='#71606b';ctx.fill();strokePath(142,12,a0+.1,a1-.16,stage+3);ctx.fillStyle='#4c3e4d';ctx.fill();ctx.restore();
 }else{
  ctx.beginPath();ctx.moveTo(-65,-25);ctx.lineTo(80,-16);ctx.lineTo(300,0);ctx.lineTo(103,17);ctx.lineTo(-75,32);ctx.lineTo(-13,5);ctx.closePath();ctx.fillStyle='#d4c4a8';ctx.fill();
  ctx.beginPath();ctx.moveTo(-80,-18);ctx.lineTo(80,-11);ctx.lineTo(296,0);ctx.lineTo(80,12);ctx.lineTo(-85,25);ctx.lineTo(-8,3);ctx.closePath();ctx.fillStyle='#22222f';ctx.fill();
  ctx.strokeStyle='#71606b';ctx.lineWidth=3;ctx.beginPath();ctx.moveTo(8,-28);ctx.quadraticCurveTo(135,-18,235,-8);ctx.stroke();
 }
 for(const p of particles){if(age>p.life)continue;const t=age,alpha=Math.pow(1-t/p.life,.65),x=p.x+p.vx*t,y=p.y+p.vy*t+240*t*t;ctx.globalAlpha=alpha;ctx.save();ctx.translate(x,y);ctx.rotate(p.rot+p.spin*t*.15);ctx.fillStyle=p.color;
  ctx.strokeStyle='#a99583';ctx.lineWidth=.7;ctx.beginPath();ctx.ellipse(0,0,p.size*p.stretch*(1-t/p.life*.4),p.size*.55,0,0,Math.PI*2);ctx.fill();ctx.restore();
 }ctx.restore();ctx.globalAlpha=1;
}
function draw(){ctx.clearRect(0,0,1200,640);const bg=ctx.createLinearGradient(0,0,0,640);bg.addColorStop(0,'#28313b');bg.addColorStop(1,'#435058');ctx.fillStyle=bg;ctx.fillRect(0,0,1200,640);
 ctx.fillStyle='#252b30';ctx.fillRect(0,519,1200,121);ctx.strokeStyle='#88867b';ctx.lineWidth=3;ctx.beginPath();ctx.moveTo(0,519);ctx.lineTo(1200,519);ctx.stroke();
 ctx.save();ctx.translate(400,519);ctx.scale(facing,1);let frame=-1;
 if(age<.42){const clip=clips[stage];let t=age+clip.times.slice(0,2).reduce((a,b)=>a+b,0);frame=clip.times.length-1;for(let i=0;i<clip.times.length;i++){if(t<clip.times[i]){frame=i;break}t-=clip.times[i]}const im=images[stage][frame];ctx.drawImage(im,-384*.9,-560*.9,im.width*.9,im.height*.9)}else ctx.drawImage(idle,-358*.75,-448*.75,384,384);ctx.restore();ink();
 status.textContent=`${stage+1}타 · ${age<.44?'먹물 분출 '+Math.round(age*1000)+'ms':'대기'} · ${enabled?'먹 이펙트 켜짐':'기존 원화만'}`;window.previewState={stage,age,particles:particles.length,enabled,paused,facing};
}
function tick(now){const dt=Math.min(.05,(now-last)/1000);last=now;if(!paused){const delta=dt*Number(document.querySelector('#speed').value);age+=delta;gap+=delta;if(auto&&gap>.85)strike((stage+1)%3)}draw();requestAnimationFrame(tick)}
const load=src=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>resolve(im);im.onerror=reject;im.src=src});
document.querySelectorAll('[data-hit]').forEach(b=>b.onclick=()=>{auto=false;paused=false;strike(Number(b.dataset.hit))});
document.querySelector('#auto').onclick=e=>{auto=!auto;e.target.textContent='3단 자동 시연 '+(auto?'켜짐':'꺼짐')};document.querySelector('#flip').onclick=()=>facing*=-1;
document.querySelector('#pause').onclick=e=>{paused=!paused;e.target.textContent=paused?'재생':'일시정지'};
document.querySelector('#toggle').onclick=e=>{enabled=!enabled;e.target.textContent='먹 이펙트 '+(enabled?'켜짐':'꺼짐')};c.onclick=()=>{auto=false;strike((stage+1)%3)};window.addEventListener('keydown',e=>{if(e.key.toLowerCase()==='j'&&!e.repeat){auto=false;strike((stage+1)%3)}});
fetch('clips.json').then(r=>r.json()).then(async data=>{clips=data;images=await Promise.all(clips.map(cl=>Promise.all(cl.files.map(load))));idle=await load('idle.png');strike(0);window.previewReady=true;requestAnimationFrame(tick)}).catch(e=>status.textContent='로딩 오류: '+e);
