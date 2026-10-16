# v6 · 큰 입자의 불규칙 지형과 A/B/C 패턴

2026-10-16 사용자 수정 반영. 현재 Unity World 1(그래파이트)과 World 2(크레파스)에 적용하는 활성 원화다. 이전 v4/v5 이미지는 이력으로 보존한다.

- 가는 선·잔입자를 줄이고 큰 흙덩어리와 넓은 명암 면을 사용한다. 디테일 70은 미술 방향의 목표이며 이미지 측정값이 아니다. 입자를 크게 다시 그렸고 단순 블러로 대체하지 않았다.
- 지역마다 같은 스타일의 A/B/C 3종. 각 변형에 바닥·벽이 있어 256×256px 마스터는 총 12장이다. Unity에서는 128×128px, PPU 128로 출력한다.
- 밟는 윗선과 벽 외곽은 원화의 불규칙 윤곽을 따른다. 내보내기에서 외곽을 정렬해 128px 기준 최대 3px의 얕은 요철로 제한한다. 일직선의 2px 표면 띠를 덧그리지 않는다. 별도의 원근 윗면은 없다.
- 충돌 격자는 유지한다. 실루엣이 들어간 곳에서는 판정면과 그림 사이에 최대 3px 차이가 있다. 물리적으로 높낮이가 달라지는 지형으로 변경한 것은 아니다.
- 표면에서 타일의 끝까지 명도를 서서히 낮춘다. 8개 깊이 구간 평균의 감소를 검증한다. 중앙부터 갑자기 검게 만드는 이전 페이드는 제거한다. 마지막 8px에서 내부 채움색과 연결한다.
- A/B/C의 접합부는 공통 명도·색상과 높이를 공유한다. 좌표 기반 배치로 바로 옆·위아래에 같은 변형이 연속되지 않는다. 게이트를 열어도 기존 패턴 위치는 유지한다.
- 패턴마다 8방향 이웃 기반 47종 연결 모양을 생성한다. 지역당 141종, 두 지역 총 282종이다. 안쪽·바깥쪽 모서리, 계단의 수직면, 천장, 얇은 기둥을 포함한다.

## 원화와 재생성

`sources/*-atlas.png`는 내장 `image_gen`으로 만든 최종 원화다. 각 원화는 3열×2행이며 위쪽 A/B/C 바닥, 아래쪽 A/B/C 벽이다. 생성과 요철 수정 프롬프트는 [prompts.json](prompts.json)에 저장한다.

`node Art/samples/regional-variants-v6/export.cjs`로 원화를 분리·축소하고, 표면 높이를 등록하며, 연결부와 깊이별 밝기를 맞춘다. `validation.json`에 마스터 해상도·깊이별 명도를 기록한다. `active-patterns-abc.png`는 두 지역을 모아 본 마스터 시트다.

Unity 메뉴 **Unfin → Rebuild World 1 + 2 Prototype**에서 실사용 타일과 맵을 재생성한다. `Book/Validation/world*-patterns-abc.png`는 실사용 128px의 연결 예시이고, 계단·모서리 미리보기는 `world*-stairs-and-corners.png`다.

## 참고 자료

[Rocky Cave tileset](https://gameartpartners.com/downloads/rocky-cave-area-seamless-platformer-tileset/)의 큰 면 구성과 모듈 연결, [Cavern Platformer](https://rafaelchm.itch.io/handpainted-cavern-platformer)의 바닥 변형 구성을 참고했다. 외부 에셋을 복사하거나 생성 도구의 입력으로 사용하지 않았다. 생성 참조 이미지는 직접 생성한 이전 지형이다.
