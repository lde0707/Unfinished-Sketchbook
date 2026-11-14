export async function finalize({presentation}) {
 const q=await presentation.inspect({select:'$.slides[*].elements[*]',include:['id','kind','text','position']});
 const convert=t=>String(t).replace(/UI-(\d{2})(?:~(\d{2}))?/g,(_,a,b)=>'UI-'+String(+a+1).padStart(2,'0')+(b?'~'+String(+b+1).padStart(2,'0'):'')).replace('UI-22 / 24 / 25','UI-22 / 25 / 26').replace('UI-05 / 06 / 08','UI-05 / 07 / 09');
 for(const e of q.records){
  if(!e.text)continue;
  if(e.kind==='table'){
   const t=presentation.resolve(e.id), rows=e.position.left===48?2:e.position.left===32?7:5, cols=e.position.left===32?3:2;
   for(let a=0;a<rows;a++)for(let b=0;b<cols;b++){const c=t.getCell(a,b),v=String(c.value??''),n=convert(v);if(n!==v)c.value=n;}
  }else{let n=convert(e.text);if(e.position.left===16&&e.position.top===16&&/^\d+$/.test(e.text))n=String(+e.text+1);if(n!==e.text)presentation.resolve(e.id).text=n;}
 }
 const move=(id,left,top)=>{const e=presentation.resolve(id);e.position={...e.position,left,top};};
 move('sh/0bu9obih',329,431);move('sh/1c3axwj2',452,350);
 const cap=presentation.resolve('sh/8bi9g32l');cap.position={left:40,top:597,width:730,height:22};cap.text.style={fontSize:14,color:'#666666'};
 const life=presentation.resolve('tb/ux4j6p0v');life.cells.block({row:0,column:0,rowCount:5,columnCount:2}).assign({margins:{left:12,right:12,top:3,bottom:3}});for(let i=0;i<5;i++)life.rows[i].height=32;
 const state=presentation.resolve('tb/h0bmxk3m');state.cells.block({row:0,column:0,rowCount:7,columnCount:1}).assign({margins:{left:42,right:10,top:8,bottom:8}});
 move('sh/zmd4ba54',31,214);move('sh/el43i5oj',31,252);move('sh/9cfmhkn6',31,308);move('sh/obm5of61',31,364);
 const {slide:s}=presentation.slides.insert({after:0,width:1280,height:720,background:{fill:'#FFFFFF'}});
 function tx(t,x,y,w,h,size=20,color='#191919',bold=false,align='left'){const a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.insets=0;a.text.style={typeface:bold?'프리젠테이션 6 SemiBold':'프리젠테이션 5 Medium',fontSize:size,color,alignment:align,verticalAlignment:'middle'};return a;}
 function rect(x,y,w,h,fill,line='#AAAAAA'){return s.shapes.add({geometry:'rect',position:{left:x,top:y,width:w,height:h},fill,line:{fill:line,width:0.75}});}
 rect(16,16,32,58,'#D4D4D4');tx('2',16,16,32,58,16,'#111111',false,'center');
 const head=s.tables.add({rows:2,columns:2,left:48,top:16,width:315,height:58,columnWidths:[160,155],values:[['Scene','Type'],['Overview','Contents']]});
 head.borders.assign({fill:'#222222',width:0.75,style:'solid'});head.cells.block({row:0,column:0,rowCount:2,columnCount:2}).assign({fill:'#FFFFFF',textStyle:{typeface:'프리젠테이션 5 Medium',fontSize:17,alignment:'center'},margins:{left:5,right:5,top:1,bottom:1},anchor:'center'});head.rows[0].height=26;head.rows[1].height=32;head.cells.block({row:0,column:0,rowCount:1,columnCount:2}).fill='#D4D4D4';
 tx('UI-02   /   2026.10.31',950,35,290,35,15,'#555555',false,'right');tx('목차',32,101,1000,42,30);
 const values=[['구성','주요 화면','페이지'],['01  전체 화면 흐름','진입 · 전환 · 복귀 경로','03'],['02  타이틀과 저장 기록','타이틀 / 새 이야기 / 이어하기 / 구역 이미지','04–08'],['03  설정과 조작','화면 / 해상도 미리보기·저장 / 소리 / 조작 안내','09–12'],['04  게임 화면','체력·회복 HUD / 미니맵 / 구역 이름','13–15'],['05  지도와 메모','지도 확대 / 범례 / 메모 작성·저장 / 위치 찾기','16–19'],['06  일시정지와 상호작용','ESC 메뉴 / 접근 안내 / 교환소 / 포탈 / 레버·이야기','20–26'],['07  획득과 플레이 복귀','획득 알림 / 능력 안내 / 사망·부활 / 공통 입력 규칙','27–30']];
 const t=s.tables.add({rows:8,columns:3,left:32,top:164,width:1216,height:420,columnWidths:[290,790,136],values});
 t.borders.assign({fill:'#777777',width:0.75,style:'solid'});t.cells.block({row:0,column:0,rowCount:8,columnCount:3}).assign({fill:'#FFFFFF',textStyle:{typeface:'프리젠테이션 5 Medium',fontSize:20,color:'#222222'},margins:{left:18,right:14,top:8,bottom:8},anchor:'center'});t.cells.block({row:0,column:0,rowCount:1,columnCount:3}).assign({fill:'#E7E7E7',textStyle:{typeface:'프리젠테이션 6 SemiBold',fontSize:18,bold:true}});t.cells.block({row:1,column:2,rowCount:7,columnCount:1}).textStyle.alignment='center';for(let i=0;i<8;i++)t.rows[i].height=i===0?42:54;
 tx('각 화면의 빨간 번호는 오른쪽 설명 표와 연결됩니다. 페이지 번호는 실제 슬라이드 순서입니다.',32,610,1216,45,17,'#555555');rect(22,673,1238,1,'#BBBBBB','#BBBBBB');tx('STILLDRAWN UI 기획서 · 목차',32,684,1216,24,15,'#666666');
 s.speakerNotes.text='2026-10-31. 표지 다음 목차 추가. 모든 후속 UI 번호와 화면 연결 참조를 1씩 갱신.';
 await presentation.render();return {slides:presentation.slides.items.length};
}
