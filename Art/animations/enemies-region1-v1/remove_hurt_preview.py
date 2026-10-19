from pathlib import Path
p=Path(__file__).with_name('preview.html')
s=p.read_text(encoding='utf-8').replace('4종 애니메이션','3종 애니메이션').replace('이동 / 공격 / 피격 / 죽음','이동 / 공격 / 죽음').replace("['이동','공격','피격','죽음']","['이동','공격','죽음']").replace("${d.enemies.length*4}개 동작 · ${d.enemies.length*16}개 프레임","${d.enemies.length*3}개 동작 · ${d.enemies.length*12}개 프레임").replace('.states{display:grid;grid-template-columns:repeat(4,1fr)','.states{display:grid;grid-template-columns:repeat(3,1fr)').replace('직접 그린 자세 프레임을 순서대로 재생합니다.','이미지 생성으로 만든 자세 프레임을 순서대로 재생합니다. 피격 동작은 제외했고 Unity에서는 짧은 점멸만 표시합니다.')
p.write_text(s,encoding='utf-8')
