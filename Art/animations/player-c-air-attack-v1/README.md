# 점프·낙하 공격 · 정면 낙하 v2

상승 중에는 무릎을 접고 앞쪽 베기, 하강 중에는 망토를 올린 채 가슴 높이 정면 베기를 재생한다. 발판에서 점프 없이 떨어져도 낙하 공격을 사용한다.

- 각4장 전신 원화, 35 / 70 / 45 / 70ms, 총220ms. 실제 휘두름은 한 장.
- `rise/frames`, `fall/frames`와 각 `atlas.png`: 정렬한 투명 스프라이트.
- `sources/*-keys.png`: 내장 imagegen 원본. 정확한 프롬프트는 `design.json`과 `sources/*-prompt.txt`.
- 크기768×640, 머리 폭160px, 기준 머리 중심(384,232). 기존 이동 자세의 머리 위치에 맞춰 표시.
- J 또는 화면 클릭으로 공중 공격. 공격 중 위치·점프 시간·중력은 유지한다.
- 착지·대시·재점프·초기화 시 공격 취소. 공격 중 중복 입력은 무시하고220ms 이후 다시 공격 가능.
- 미리보기: `../player-state-test/preview.html?demo=longFallAttack&v=forward-fall2`. 상승·발판 낙하·상승 후 하강 공격 시연 버튼 제공.
- `air-attack-contact.jpg`: 원화8장 비교. `browser-validation.json`: 궤적 동일성 및 입력·전환 검사.

재생성: `python Tools/package_player_air_attack_v1.py`

검사: `node Tools/check_player_air_attack_v1.cjs` (저장소 루트, 로컬8092 서버 필요).

Unity World12Prototype에 적용했다(2026-10-18). 실제 대미지 판정은 별도 구현 대상이다.

## 정면 낙하 v2

낙하 휘두름·회수 두 원화만 새로 그렸다. 상승4장과 낙하 준비·복귀2장은 기존 파일 해시를 보존했다. 새 원본은 `sources/fall-forward-keys-v2.png`, 정확한 프롬프트는 `sources/fall-forward-prompt-v2.txt`다.

높은 발판에서 점프하면 정점80ms 뒤 하강 자세에서 Fall 반복 자세로 연결한다. 높이·하강 속도·이동·공격·대시 사용 여부를 이어받고 실제 아래 바닥에 닿아야 착지한다. 머리 기준점 차이는80ms 동안 전신 위치만 조정하며 완화한다. `높은 곳 점프 → 계속 낙하`와 `긴 낙하 중 정면 공격` 버튼으로 확인한다.

검사: `node Tools/check_player_jump_to_fall_v2.cjs`. 양방향·제자리와 두 높이6가지 연결, 공중 착지 방지, 실제 접지·점프 버퍼·공격·대시 상태 보존을 검사했다. 결과는 `jump-to-fall-validation.json`.
