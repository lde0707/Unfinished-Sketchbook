from pathlib import Path
P=Path(__file__).resolve().parent
s=P.with_name('enemies-region1-v1').joinpath('preview.html').read_text(encoding='utf-8')
s=s.replace('if(stepIndex!==null)return stepIndex;', 'if(stepIndex!==null)return Math.min(stepIndex,selected.frameCounts[row]-1);')
s=s.replace('let i=0;i<4;i++','let i=0;i<ds.length;i++').replace('return 3;}','return ds.length-1;}')
s=s.replace('ctx.drawImage(img,f*384,row*384,384,384,','const index=selected.starts[row]+f;ctx.drawImage(img,(index%4)*384,Math.floor(index/4)*384,384,384,')
s=s.replace('length:4','length:selected.frameCounts[chosen]').replace('chosen*4+i','selected.starts[chosen]+i')
s=s.replace('${f+1}/4','${f+1}/${selected.frameCounts[i]}').replace('stepIndex+1)%4','stepIndex+1)%selected.frameCounts[chosen]')
s=s.replace('선택 동작의 네 프레임','선택 동작의 전체 프레임').replace('repeat(4,1fr);gap:6px','repeat(4,1fr);gap:6px')
s=s.replace('${d.enemies.length*12}개 프레임 / 원화 프레임 애니메이션 v1','${d.totalFrames}개 프레임 / 4종 이동·공격 8프레임 보강 v2')
s=s.replace('select(d.enemies[0]);','select(d.enemies[0]);')
s=s.replace('이미지 생성으로 만든 자세 프레임을 순서대로 재생합니다.','종이공·나방·귀뚜라미·도마뱀의 이동과 공격에 새 원화 32장을 추가했습니다. 기존 216장은 그대로 유지하며 공격 총시간도 같습니다.')
(P/'preview.html').write_text(s,encoding='utf-8')
