import fs from 'node:fs/promises';
const ROOT='C:/Users/doun0/OneDrive/바탕 화면/File/GitHub/Unfinished-Sketchbook/';
const FONT='프리젠테이션 5 Medium',BOLD='프리젠테이션 6 SemiBold';
function text(s,t,x,y,w,h,size=17,color='#191919',bold=false,align='left'){
 const a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.insets=0;a.text.style={typeface:bold?BOLD:FONT,fontSize:size,color,alignment:align,verticalAlignment:'middle'};return a;
}
function rect(s,x,y,w,h,fill='#FFFFFF',line='#333333'){return s.shapes.add({geometry:'rect',position:{left:x,top:y,width:w,height:h},fill,line:{fill:line,width:1}});}
function badge(s,n,x,y){const a=s.shapes.add({geometry:'ellipse',position:{left:x-14,top:y-14,width:28,height:28},fill:'#FFFFFF',line:{fill:'#D71920',width:1.1}});a.text=String(n);a.text.insets=0;a.text.style={typeface:FONT,fontSize:18,color:'#D71920',alignment:'center',verticalAlignment:'middle'};}
function table(s,values,x,y,w,h,widths,header=true,font=17){
 const a=s.tables.add({rows:values.length,columns:values[0].length,left:x,top:y,width:w,height:h,columnWidths:widths,values});
 a.borders.assign({fill:'#222222',width:0.75,style:'solid'});
 a.cells.block({row:0,column:0,rowCount:values.length,columnCount:values[0].length}).assign({fill:'#FFFFFF',textStyle:{typeface:FONT,fontSize:font,color:'#191919'},margins:{left:12,right:10,top:8,bottom:8},anchor:'center'});
 if(header){a.rows[0].height=42;a.cells.block({row:0,column:0,rowCount:1,columnCount:values[0].length}).assign({fill:'#E7E7E7',textStyle:{typeface:BOLD,fontSize:16,bold:true,alignment:'center'}});}
 return a;
}
function base(p,d,no,existing){const s=existing??p.slides.add({width:1280,height:720});s.background.fill='#FFFFFF';
 const head=table(s,[['Scene','Type'],[d.scene,d.type]],48,16,315,58,[160,155],false,17);head.cells.block({row:0,column:0,rowCount:2,columnCount:2}).assign({margins:{left:5,right:5,top:1,bottom:1}});head.rows[0].height=26;head.rows[1].height=32;head.cells.block({row:0,column:0,rowCount:1,columnCount:2}).assign({fill:'#D4D4D4',textStyle:{alignment:'center'}});head.cells.block({row:1,column:0,rowCount:1,columnCount:2}).textStyle.alignment='center';rect(s,16,16,32,58,'#D4D4D4');text(s,String(no),16,16,32,58,16,'#111111',false,'center');
 text(s,`UI-${String(no).padStart(2,'0')}   /   2026.10.31`,570,35,245,35,15,'#555555',false,'right');
 text(s,d.title,32,101,785,42,27);rect(s,22,673,1238,1,'#BBBBBB','#BBBBBB');text(s,d.footer||d.title,32,684,1220,24,15,'#666666');
 const rows=table(s,[['No','Description'],...d.desc.map((t,i)=>[String(i+1),t])],850,16,410,638,[50,360]);
 for(let i=1;i<=d.desc.length;i++){rows.rows[i].height=596/d.desc.length;rows.getCell(i,0).text.style={typeface:BOLD,fontSize:17,color:'#D71920',alignment:'center'};}
 if(d.flow)text(s,d.flow,32,610,780,48,17,'#444444');
 s.speakerNotes.text='현재 구현 기준 2026-10-31.\n'+(d.source||'Book/Assets/Unfin/Runtime')+'\n'+(d.note||'화면의 빨간 번호는 설명용이며 게임 UI에는 표시하지 않습니다.');return s;
}
async function draw(s,d){
 if(d.image){s.images.add({path:ROOT+d.image,position:{left:22,top:147,width:794,height:446.625},fit:'contain'});for(const [i,pt]of(d.points||[]).entries())badge(s,i+1,22+pt[0]/1920*794,147+pt[1]/1080*446.625);}
 if(d.kind==='flow'){
 const lanes=[['타이틀\nUI-03','新 / 이어하기 확인\nUI-04~06','게임 HUD\nUI-12'],['게임 HUD\nESC','일시정지\nUI-19','설정 / 조작\nUI-08~11'],['게임 HUD\nM','지도\nUI-15~16','메모\nUI-17~18'],['게임 HUD\nE','상호작용\nUI-20~25','즉시 획득 알림\nUI-26~27']];
 for(let r=0;r<4;r++){const y=167+r*108;badge(s,r+1,43,y+37);for(let c=0;c<3;c++){const x=66+c*250;rect(s,x,y,215,76,'#F3F3F3');text(s,lanes[r][c].replace('新','새 이야기'),x+8,y+10,199,56,18,'#222222',false,'center');if(c<2)text(s,r===3&&c===1?'또는':'→',x+218,y+25,32,28,r===3&&c===1?14:25);}}
 }
 if(d.kind==='zones'){
 const files=['A-paper-cave','B-graphite-mine','C-sketch-tree','D-root-depths','E-sunset-cavern','F-wax-workshop','G-painted-canyon','H-knight-tower'];
 const names=['구겨진 종이굴','흑연 광산','스케치 나무 속','엉킨 뿌리 심부','노을 크레파스 굴','밀랍 공방','색칠 지하 협곡','기사의 탑'];
 for(let i=0;i<8;i++){let x=32+i%4*198,y=175+Math.floor(i/4)*208;s.images.add({path:ROOT+'Book/Assets/Unfin/Resources/Title/Panels/'+files[i]+'.png',position:{left:x,top:y,width:188,height:127},fit:'contain'});text(s,names[i],x,y+132,188,50,18,'#222222',false,'center');}badge(s,1,46,161);badge(s,2,46,370);badge(s,3,795,161);badge(s,4,795,370);
 }
 if(d.kind==='purchase'){
 s.images.add({path:ROOT+'Docs/UI-Spec/build/screens/shop.png',position:{left:22,top:147,width:794,height:446.625},fit:'contain'});
 s.images.add({path:ROOT+'Book/Assets/Unfin/Resources/TradingUI/choice-panel.png',position:{left:160,top:205,width:510,height:340},fit:'contain'});
 text(s,'책등 보강 교환',225,277,380,40,23,'#E5DECA',true,'center');text(s,'최대 체력 5 → 6\n먹빛 가루 40개를 사용합니다.  보유 100개',224,336,380,70,18,'#B9BCB6',false,'center');text(s,'교환하기',240,441,160,35,19,'#E5DECA',false,'center');text(s,'뒤로가기',420,441,160,35,19,'#BBB9AB',false,'center');
 badge(s,1,225,286);badge(s,2,226,357);badge(s,3,242,429);badge(s,4,589,429);text(s,'구매 가능 상태 화면 도식 · 예시 보유량 100',40,575,740,26,15,'#555555');
 }
 if(d.kind==='choices'){
 for(let i=0;i<2;i++){let x=25+i*397;s.images.add({path:ROOT+'Book/Assets/Unfin/Resources/TradingUI/choice-panel.png',position:{left:x,top:180,width:390,height:260},fit:'contain'});text(s,i===0?'지름길 레버':'그림 친구 · 강아지',x+40,235,310,35,22,'#E5DECA',false,'center');text(s,i===0?'잠긴 뿌리 지름길을 엽니다.\n레버를 작동하시겠어요?':'미완성 그림을 완성하고 친구를 기억합니다.\n이 친구를 기록에 남기시겠어요?',x+45,290,300,65,17,'#B9BCB6',false,'center');text(s,i===0?'작동하기':'기억하기',x+65,365,105,35,17,'#E5DECA',false,'center');text(s,'뒤로가기',x+210,365,105,35,17,'#B9BCB6',false,'center');badge(s,i+1,x+30,226);}
 rect(s,40,472,740,108,'#F4F4F4','#AAAAAA');text(s,'NPC 대화 / 일지\nE로 내용 표시. 일지는 기록 후 구석 알림으로 안내합니다.',76,488,680,70,20);badge(s,3,58,485);badge(s,4,760,544);text(s,'현재 공통 패널과 문구를 재구성한 화면 도식',40,585,740,25,15,'#666666');
 }
 if(d.kind==='life'){
 const arr=[['Book/Validation/pickup-notice/death-denser.png','사망'],['Book/Validation/pickup-notice/revive-denser.png','부활']];for(let i=0;i<2;i++){let x=25+i*400;text(s,arr[i][1],x,155,350,30,22);s.images.add({path:ROOT+arr[i][0],position:{left:x,top:192,width:390,height:219.375},fit:'contain'});badge(s,i===0?1:4,x+25,215);badge(s,i===0?2:3,x+195,365);}
 table(s,[['단계','전환 조건 / 입력'],['사망','사망 연출 중 행동 제한'],['복귀','저장 지점으로 위치 이동'],['부활','약 1초 누움 후 일어남'],['완료','이동과 전투 입력 재개']],40,437,740,154,[160,580],true,17);
 }
 if(d.kind==='states'){
 table(s,[['현재 UI','게임 진행','ESC / 종료'],['HUD / 획득 알림','진행','일시정지 열기'],['지도 / 메모','정지','편집 종료 후 지도 닫기'],['교환 확인','정지','교환 취소, 상점 유지'],['갈피 교환소','정지','상점 진입 선택으로'],['일시정지 / 설정','정지','상위 패널 또는 게임'],['타이틀 패널','게임 미시작','타이틀로']],32,167,775,405,[235,145,395],true,18);for(let i=0;i<4;i++)badge(s,i+1,45,235+i*80);
 }
}
export async function addBatch({presentation,input}){
 const data=JSON.parse(await fs.readFile(ROOT+'Docs/UI-Spec/build/content.json','utf8'));
 for(let i=input.start;i<input.end;i++){const d=data[i],s=base(presentation,d,i+2);await draw(s,d);}
 return{slides:presentation.slides.items.length};
}
export async function repairAndRender({presentation}){
 const data=JSON.parse(await fs.readFile(ROOT+'Docs/UI-Spec/build/content.json','utf8'));
 const flow=presentation.slides.getItem(1);
 const old=await presentation.inspect({select:'$.slides[1].elements[*]',include:['id']});
 presentation.delete(old.records.map(r=>r.id));
 await draw(base(presentation,data[0],2,flow),data[0]);
 const headers=await presentation.inspect({select:"$.slides[*].elements[?(@.kind == 'table')]",include:['id','position']});
 for(const h of headers.records){if(h.position.left!==48)continue;const t=presentation.resolve(h.id);t.cells.block({row:0,column:0,rowCount:2,columnCount:2}).assign({margins:{left:5,right:5,top:1,bottom:1}});t.rows[0].height=26;t.rows[1].height=32;}
 await presentation.inspect({select:'$.slides[*]',include:['id','index','elements.id','elements.kind','elements.text','elements.position','notes.text'],fileName:'ui-spec-inspect.ndjson'});
 await presentation.render();
 return {slides:presentation.slides.items.length};
}
