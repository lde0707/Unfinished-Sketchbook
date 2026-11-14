import json, os
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def save(p,s):
    t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
def doc(prefix): return next((root/'Docs').glob(prefix+'*'))
p=doc('18');s=p.read_text(encoding='utf-8').replace('플레이어 전용 피격 원화와 경직은 아래 시안 단계이며 아직 Unity에 연결하지 않았다.','플레이어 전용 피격 4프레임과 0.10초 행동 정지를 Unity에 연결했다. 일반 적은 별도 Hurt 원화 없이 현재 이동·공격을 유지하며 색 변화와 파편으로 피격을 표시한다.')
s+='''

## 2026-10-24 · 실제 맵 애니메이션 누락 보완

- 플레이어: 기존 피격 4장을 등록해 총 0.22초 재생한다. 처음 0.10초만 플레이어 이동·공격·대시를 막고, 이후에는 공격으로 즉시 복귀할 수 있다. 게임 전체 시간과 적은 멈추지 않는다. 사망·부활이 피격보다 우선한다.
- 패링: 즉시 방어 자세→짧은 유지→복귀 4장과, 성공 시 튕기기→망토 반동→정돈 4장을 추가했다. 기존 판정 0.16초와 획득 조건은 유지한다. 같은 방어 중 중복 충돌해도 성공 반동은 한 번만 시작한다. 지상·공중에서 작동하며 대시·사망이 우선한다.
- 광산 감독 지우개: 23번 적. 이동 8장·돌진 8장·죽음 8장. 체력 12, 0.50초 준비→최대 0.75초 물리 돌진→0.70초 회복. 벽·낭떠러지를 감지해 정지한다. 사망 연출 후 연필 깎기를 한 번만 지급한다.
- 덩굴 사냥꾼: 44번 적. 이동 8장·도약 베기 8장·죽음 8장. 체력 14, 0.45초 준비→0.32초 도약→0.55초 회복. 3.1칸 전진과 0.75칸 높이의 실제 호를 이동한다. 사망 연출 후 체력 조각 1개를 한 번만 지급한다.
- 두 엘리트는 맞을 때 공격 프레임을 재시작하거나 별도 Hurt 동작으로 끊지 않는다. 공격 예고와 활성 도형은 같은 충돌 범위를 사용한다.
- 원화의 각 행 간격을 알파 투영으로 찾아 분리한다. 일정한 6등분을 가정하지 않는다. 죽음 그림의 바닥을 각각 등록해 낙엽·지우개 조각이 공중에 뜨지 않게 한다.
- 기존 플레이어 Idle·Run·Jump·Dash·벽타기·공격·Death·Revive 원본과 비율·재생 시간은 유지한다. 카메라 밝기 0.9도 유지한다.
- 리소스: `Book/Assets/Unfin/Resources/PlayerReactions`, `Region1Elites`. 생성 프롬프트: `Art/samples/animation-completion-v1/generation.json`. 실제 동작 검증: `Book/Validation/animation-completion.json`.
'''
save(p,s)
p=doc('06');s=p.read_text(encoding='utf-8');s+='''

### 2026-10-24 · 두 엘리트 실제 적용

광산 감독(23)과 덩굴 사냥꾼(44)의 임시 사각형을 전신 원화 24장씩으로 교체했다. 감독의 물리 돌진과 사냥꾼의 실제 도약, 지형 감지, 피해·사망·일회성 보상을 연결했다. 첫 방문 기본 이동·점프·벽 점프만으로 회피하며 대시·패링은 요구하지 않는다. 동작과 수치는 18 문서의 최신 보완 항목을 따른다. 일반 33종의 별도 피격 원화는 추가하지 않는다.
''';save(p,s)
p=doc('19');s=p.read_text(encoding='utf-8');s+='''

### 2026-10-24 · 최신 누락 보완 원화

플레이어 피격 4장(기존 시안 연결), 패링·성공 반동 8장(신규), 광산 감독 24장, 덩굴 사냥꾼 24장을 `PlayerReactions`/`Region1Elites`에 등록했다. 현재 1·2지역 맵의 모든 적과 활성 마커를 실제 연결 기준으로 검사한다. 확정 전인 3~5지역 전용 원화와 흑연 사용처의 신규 UI는 이번 적용 범위에 포함하지 않는다. 해당 항목은 미래 콘텐츠이며 현재 맵의 누락 텍스처로 취급하지 않는다.
''';save(p,s)
p=root/'Art/latest-assets.json';j=json.loads(p.read_text(encoding='utf-8'));j.update(animationCompletion='Art/samples/animation-completion-v1',runtimePlayerReactions='Book/Assets/Unfin/Resources/PlayerReactions',runtimeRegion1Elites='Book/Assets/Unfin/Resources/Region1Elites');save(p,json.dumps(j,ensure_ascii=False,indent=2)+'\n')
print('ANIMATION_COMPLETION_DOCS_UPDATED')
