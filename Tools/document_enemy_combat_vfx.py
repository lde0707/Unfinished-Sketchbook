from pathlib import Path
import os,json
root=Path(__file__).resolve().parents[1]
def save(p,s):
    t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
p=next((root/'Docs').glob('18*'));s=p.read_text(encoding='utf-8');s+='''

## 2026-10-24 · 적과 보스의 먹·종이 전투 효과

일반 적 33종, 현재 맵의 적 배치 95곳에 `EnemyCombatVfx` 관찰자를 연결한다. 기존 전신 원화·공격 시간·판정은 읽기만 하고 변경하지 않는다. 일반 적의 별도 Hurt 동작은 추가하지 않는다.

- 준비: 작은 종이 조각이 몸으로 모인다. 활성 공격 도형과 구별되며 피해가 없다.
- 근접 공격: 실제 공격 시점에 짧은 먹 날, 충격점과 넓게 튀는 파편. 연속 베기는 각 타격마다 따로 반응한다.
- 원거리 공격: 근접 베기와 구별되는 발사구의 먹 튐, 탄의 짧은 꼬리, 충돌 시 작은 파편. 실제 탄의 크기·궤도·수명은 유지한다.
- 돌진·도약: 실제 빠른 이동 중 전신 원화 잔상과 뒤로 끌리는 종이 궤적. 정지한 적에게 잔상을 찍지 않는다.
- 착지·내려찍기·뿌리: 해당 활성 지점에서 바닥 번짐과 좌우 분진을 방출한다. 타격 원화의 중심부는 기존 공격 타원 안에 맞추고, 바깥의 작은 파편에는 판정이 없다.
- 피격·사망: 맞은 방향으로 튀는 종이 조각, 죽을 때 먹 구름과 넓게 흩어지는 파편. 보호된 적은 작은 튕김만 보인다. 비치명 피격으로 기존 공격 프레임을 끊지 않는다.
- 1지역은 흑연·회백 종이 원화, 2지역은 갈색·황토·청보라·자주 안료 틴트. 일반 알파 합성만 사용하며 화면 전체 발광이나 점멸은 없다. 카메라 밝기 0.9를 따른다.
- `EnemyCombatEffects`는 256개의 고정 풀을 사용한다. 지도에서는 정지하고, 재스폰 시 남은 효과를 지운다. 효과에는 콜라이더·공격 수신기·피해 처리가 없다.

사용 원화는 프로젝트의 `Effects/Kenney` CC0 텍스처와 기존 `BossRegion1/Effects`, `Region2/Effects`이다. 새 원화 프레임으로 캐릭터를 바꾸지 않는다. Unity 검증 결과는 `Book/Validation/enemy-combat-effects.json`, 실제 화면은 같은 이름의 폴더에 둔다.
''';save(p,s)
p=root/'Art/latest-assets.json';j=json.loads(p.read_text(encoding='utf-8'));j['enemyCombatEffects']='Art/samples/enemy-effects-v1';save(p,json.dumps(j,ensure_ascii=False,indent=2)+'\n')
print('ENEMY_COMBAT_EFFECT_DOCS_UPDATED')
