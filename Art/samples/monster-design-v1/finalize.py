import json
from pathlib import Path
from PIL import Image
P=Path(__file__).resolve().parent
rooms={r['group']:r for r in json.loads((P/'map-captures.json').read_text())}
positions={
 'common':['왼쪽 지상','중앙 오른쪽 공중','오른쪽 지상'],
 'r1':['왼쪽 지상','플레이어 오른쪽 포탑','맨 오른쪽'],
 'a':['왼쪽 위 발판','플레이어 왼쪽','오른쪽 건너편'],
 'b':['플레이어 왼쪽','오른쪽 지상','오른쪽 공중 주머니'],
 'c':['왼쪽 위 매달린 개체','왼쪽 발판 도마뱀','오른쪽 공중'],
 'd':['왼쪽','중앙 오른쪽 씨주머니','맨 오른쪽 그루터기'],
 'r2':['왼쪽 창 병정','맨 오른쪽 투척꾼','중앙 오른쪽 공중'],
 'e':['플레이어 오른쪽 등짐충','맨 오른쪽 위 달팽이','왼쪽 공중'],
 'f':['왼쪽 위 선반의 왼쪽','같은 선반 중앙','같은 선반 오른쪽'],
 'g':['왼쪽','중앙 오른쪽 방패벌레','맨 오른쪽 사수'],
 'h':['왼쪽 창 시종','플레이어 왼쪽 방패','오른쪽 위 공중']}
data=json.loads((P/'design.json').read_text(encoding='utf-8'))
page=(P/'preview.html').read_text(encoding='utf-8')
page=page.replace('그림은 각 묶음의 왼쪽부터 카드 순서와 같습니다. 클릭하면 원본을 엽니다.','현재 Unity 맵 캡처를 바탕으로 만든 AI 배치 시안 11장입니다. 각 카드에 그림 속 위치를 적었습니다. 클릭하면 큰 이미지를 엽니다.')
page=page.replace('기획·외형 시안 — Unity 미적용','Unity 캡처 기반 AI 합성 시안 — 몬스터는 Unity 미적용')
page=page.replace('원화 크기는 실제 키 비율이 아닙니다.','생성 과정에서 일부 배경·크기 표현이 달라질 수 있습니다. 실제 Unity 렌더와 구분해 보도록 원본 캡처를 제공합니다. 그림 속 배치는 외형 비교용으로, 확정 스폰 배치가 아닙니다.')
page=page.replace('href="prompts.json">생성 프롬프트','href="map-prompts.json">생성 프롬프트')
for g in data['groups']:
 key=g['id'];room=rooms[key]
 caption=f'<p class="muted">촬영 방: {room["room"]} {room["roomName"]} · <a href="map-originals/{key}.png" target="_blank">실제 Unity 원본 캡처 보기</a> · 아래 이미지는 AI 배치 시안입니다.</p>'
 page=page.replace(f'<a href="{key}.png"',caption+f'<a href="{key}.png"',1)
 for m in [m for m in data['monsters'] if m['group']==key]:
  position=positions[key][m['panel']-1];m['imagePosition']=position
  page=page.replace(f'<h3>{m["name"]}</h3>',f'<h3>{m["name"]}</h3><p class="muted">그림 위치: {position}</p>')
data['imageType']='AI mockups based on current local Unity screenshots; not rendered Unity enemies'
(P/'design.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
(P/'preview.html').write_text(page,encoding='utf-8')
doc=P.parents[2]/'Docs/22_일반_몬스터_설계.md'
text=doc.read_text(encoding='utf-8')
text=text.replace('## 33종 상세','## 맵 배치 이미지 읽는 법\n\n사용자 요청에 따라 별도 원화 시트 대신 현재 로컬 Unity WebGL 화면 11장을 직접 캡처하고, built-in image_gen으로 몬스터를 합성했다. 이 이미지는 Unity에 몬스터를 설치해 렌더한 결과가 아니다. 생성 과정에서 배경 일부와 크기 표현이 달라질 수 있으며 원본 캡처는 Art/samples/monster-design-v1/map-originals에 보존한다. 정확한 렌더 비교는 도감의 원본 링크를 사용한다. 한 장의 3종은 디자인 비교를 위해 모은 것으로 확정 전투 배치나 해금 순서를 뜻하지 않는다.\n\n- 실제 사용 프롬프트: Art/samples/monster-design-v1/map-prompts.json\n- 캡처 방과 좌표: map-captures.json\n- 생성 출처: generation.json\n- 최초 미실행 원화 프롬프트 초안: prompts.json (현재 이미지 생성에 사용하지 않음)\n\n## 33종 상세')
doc.write_text(text,encoding='utf-8')
validation={'designCount':len(data['monsters']),'uniqueIds':len({m['id'] for m in data['monsters']}),'images':[],'unityModified':False,'visualReview':'11 images reviewed: three enemies each; A slug corrected from background book to foreground ledge. Concept scale and some material detail vary; not sprite-ready.'}
for g in data['groups']:
 with Image.open(P/(g['id']+'.png')) as im:
  validation['images'].append({'group':g['id'],'size':im.size,'mode':im.mode})
assert len(validation['images'])==11 and validation['uniqueIds']==33
(P/'validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
print('Finalized 11 map mockups, 33 designs, original-map links and positions.')
