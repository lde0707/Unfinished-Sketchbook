# World 1 + 2 Unity 맵 프로토타입

Unity **6000.5.3f1**에서 이 `Book` 폴더를 열고 `Assets/Scenes/World12Prototype.unity`를 열어 Play한다. Game 뷰를 **1920×1080**으로 설정한다. 에디터 패널의 Fit 축소율은 실제 게임 출력 해상도와 별개다.

## 웹 플레이와 빌드

[웹에서 플레이](https://unfinished-sketchbook-unity.creamykrill16.chatgpt.site) — 소유자 본인의 ChatGPT 계정으로 로그인하는 비공개 배포다. PC 키보드로 조작한다.

실제 Unity 씬을 WebAssembly/WebGL로 컴파일한다. HTML 프로토타입을 대신 실행하는 방식이 아니다. 웹에서도 같은 World 1·2, 128px 타일, 둥근 연결부, A/B/C 재질과 격자 충돌을 사용한다.

- Unity Hub의 **6000.5.3f1 → Add Modules → Web Build Support**가 필요하다.
- 저장소 루트에서 `powershell -ExecutionPolicy Bypass -File Tools/build_webgl.ps1`을 실행한다. 다른 에디터 경로는 `-Unity "경로/Unity.exe"`로 지정한다.
- 빌드는 `Builds/UnityWebGLProject`에 Assets/Packages/ProjectSettings만 복사해 진행하므로 열려 있는 `Book` 에디터를 닫을 필요가 없다. 이 전용 복사본의 세 소스 폴더는 매번 원본과 동기화된다. 직접 작업하는 폴더로 사용하지 않는다.
- 결과는 `Builds/WebGLSite/dist`, 로그는 `Builds/webgl-build.log`다. 첫 빌드는 패키지 임포트와 WebAssembly 컴파일 때문에 더 오래 걸린다.
- `node Tools/verify_webgl.cjs`로 로더/스크립트 문법, Brotli 무결성, WebAssembly 유효성을 검사하고 `Validation/webgl-build.json`에 크기와 해시를 기록한다.
- `node Tools/serve_webgl.cjs`를 실행한 뒤 `http://127.0.0.1:8080`에서 플레이한다. 종료는 터미널의 Ctrl+C. `file://` 더블클릭 실행은 지원하지 않는다.
- 에디터 메뉴 **Unfin → Build Web Player**도 제공한다. 현재 프로젝트에서 직접 빌드할 때 사용하며 웹 빌드 설정이 저장된다.

웹 화면은 내부 **1920×1080**, 표시 비율 **16:9**를 유지한다. 화면 크기가 달라도 같은 범위가 보이고 전체 화면에서는 남는 공간을 검게 채운다. PC 키보드 조작을 제공하며 터치 조작은 아직 없다. 브라우저 기능과 겹칠 수 있는 F1/F2 대신 숫자 **1/2**로 지역을 이동할 수 있다.

Brotli + Decompression Fallback을 사용해 별도 압축 헤더 설정이 없는 정적 호스팅에서도 실행한다. 배포할 때는 `dist` 전체를 함께 올린다. 빌드 산출물과 빌드용 Library는 GitHub에 포함하지 않고 소스와 재현 도구를 관리한다. Sites 배포의 프로젝트 식별자/정적 경로는 `../Tools/webgl-hosting.json`에 보존하며, 이를 빌드 폴더의 `.openai/hosting.json`으로 복사한다. 인증 토큰은 저장하지 않는다.

타일 이미지는 **128×128px**, PPU **128**, Grid Cell Size **1×1**이다. 카메라는 Orthographic Size **4.21875**이며 1920×1080에서 타일 한 칸이 **128×128 화면 픽셀**이다. 화면에 가로 15칸, 세로 8.4375칸이 들어온다. 다른 출력 해상도에서는 같은 세로 시야를 유지한다.

## 원본과 구조

2026-10-16: 사용자 요청에 따라 천장 높이기 이전의 1지역 지도로 복원했다. 시작 방 18→9칸, 갈림길 굴 33→12칸 등 원래 방 높이·통로·수직굴·배치를 복구했으며, 2지역은 +24행 오프셋만 제거했다. 전체 격자는 원본 Git 이력과 정확히 일치하고 2지역의 상대 지형 차이는 0칸이다. 검증 기록은 `Validation/ceiling-restoration.json`에 있다.

- 원본: `../Prototype/unfin_world.html`. `world_map_data.js`와 모든 지도 상수·셀을 대조한 뒤 추출한다.
- 통합 지도 560×140칸, 1지역/2지역 경계 X=280. 빈칸을 포함한 78,400칸과 지형 57,482칸을 그대로 유지한다.
- 설계된 방·통로를 제외한 영역은 어두운 땅으로 채운 사각형이다. 지도 밖도 땅으로 판정하므로 사각형 외곽에 밝은 노출면을 만들지 않는다. 전체 구조는 `Validation/world12-rectangular-map.png`, 지역별 모습은 `world1-rectangular-map.png`와 `world2-rectangular-map.png`에서 확인한다. `Unfin → Render Rectangular World Maps`로 갱신한다.
- 방 56개, 수직 통로 14개, 적 표식 95개, 아이템·장치 표식 67개. 방·통로·수직 통로는 Hierarchy에서 별도로 찾을 수 있다.
- Unity 타일 좌표는 `(HTML x, -HTML y - 1)`. 플레이어·적·장치의 위치와 크기는 HTML 논리 좌표를 16으로 나눈다. 박쥐의 천장 배치와 아이템별 크기 보정도 반영한다.
- 지역별로 Solid terrain, One-way platforms, Water 타일맵을 분리한다. 원본 타일 ID는 `PrototypeTile.htmlType`과 추출 JSON에 보존한다.
- TilemapCollider2D와 CompositeCollider2D를 제공한다. 플레이어는 HTML의 격자 충돌 계산을 포팅한 GridMotor로 움직인다. Rigidbody2D 물리 플레이어가 아니다.
- 1지역은 `Art/samples/region1-tiles-v8`의 구역별 컬러 연속 재질을 사용하며, 2지역은 `regional-variants-v6`의 크레파스 바닥/벽을 사용한다. 표면 요철·둥근 연결·점진적 암부를 유지한다. 특수 지형과 캐릭터는 구조 확인용 표식이다.

## 1지역 구역별 컬러 타일맵 · v8

1지역의 30개 방과 통로에 **종이굴 회갈색 / 광산 청회색 / 나무 갈색 / 뿌리 회녹색**을 적용한다. 일반 지형 28,940칸을 3,207개의 128px 타일과 15장의 아틀라스로 구성한다. 큰 무늬는 네 칸 너비로 이어지고, 깊이 세 칸에 걸쳐 검게 어두워진다. 원화의 상단 높이 정보를 별도로 보존해 약 2~12px의 불규칙한 윤곽을 적용하며, 각 구역 바닥·벽·천장의 실제 알파 경계에 높이 변화가 남는지 검사한다. 바닥·벽·천장·단차를 같은 재질로 연결하고 구역 경계는 약 네 칸 범위에서 혼합한다.

실제 에셋은 `Assets/Unfin/Art/Region1V8`, 배치 카탈로그는 `Region1Terrain.asset`이다. 원화·비교 시안·Unity용 에셋 구분과 전체 재생성 순서는 [타일 정리 문서](../Art/samples/region1-tiles-v8/UNITY.md)에 있다. `node Tools/export_region1_terrain.cjs`와 `node Tools/verify_region1_terrain.cjs`는 저장소 루트에서 실행하고, Unity 메뉴 **Unfin → Apply Region 1 Colored Tilemaps**로 씬을 갱신한다. 맵 구조가 변경되면 먼저 타일을 다시 내보내야 한다.

현재 맵의 연결 상태를 베이크한 방식으로, 임의 지형 파괴용 자동 타일 시스템은 아니다. 게이트 열기·임시 발판 소멸/복구는 기존 규칙을 지원하며 새 재질을 유지한다. 2지역과 특수 지형 표식은 기존 v6 체계를 유지한다. 아래 v6 연결 타일 카탈로그는 2지역 및 별도 검수 씬에 사용한다.

## 1지역 배경 · v3 적용

`World12Prototype` 씬에 종이굴·흑연 광산·나무 속·뿌리 심부의 배경을 적용했다. 원본은 `Art/samples/region1-backgrounds-v3`, Unity 에셋은 `Assets/Unfin/Art/Region1BackgroundV3`다. 1254px 원본을 보존하고 Unity 임포트 크기는 최대 1024px로 설정한다.

`Unfin/Region1 Background` 셰이더가 8유닛 크기의 큰 조각을 6유닛 간격, 2유닛 겹침으로 합성한다. 128px 지형 기준으로 1024/768/256px이다. 결정적 반전과 정규화된 겹침으로 연결하고, 공기색 RGB(22,25,29) 위에 0.52 불투명도로 합성한다. Unity Linear 색 공간에서도 미리보기의 어두운 명도를 유지한다.

구역 가중치는 바닥과 동일한 방 거리장·4칸 전환 규칙이다. 방 진입 이벤트로 그림을 교체하지 않으며 배경과 구역 경계 모두 월드 좌표에 고정된다. 현재는 단일 뒷벽 레이어로, 별도 패럴랙스 레이어는 없다. 1지역 사각 범위에만 렌더링하며 지형·충돌·카메라 배율을 바꾸지 않는다.

Unity 메뉴 **Unfin → Apply Region 1 Backgrounds and Validate**로 재적용한다. 전체 맵 재생성에도 자동 적용된다. 맵 방 구조가 바뀌면 이 메뉴로 구역 가중치를 다시 생성한다. `Unfin.Editor.Region1BackgroundImporter.RenderTransitions`로 이동 전후 화면을 출력하고 저장소 루트에서 `node Tools/verify_region1_background.cjs`로 검사한다. `Validation/background-v3-start.png`는 실제 시작 화면, `background-v3-transitions-overview.png`는 네 구역 연결부다. 4개 연결부의 1유닛 카메라 이동 비교와 2지역 영향 없음 검사를 통과했다.

## 모서리·계단 연결 타일 · v6 카탈로그

그래파이트와 크레파스에 **패턴 3종 × 연결 모양 47종 = 지역당 141종, 총 282종**의 128px 지형 연결 타일을 제공한다. 상하좌우와 대각선 8방향을 확인해 직선면, 바깥 모서리, 안쪽 오목 모서리, 계단 단차, 얇은 기둥, 돌출부, 독립 블록을 자동 선택한다. 윗면이 있는 타일에서도 좌우면·아랫면 재질을 함께 그리므로 계단의 수직면이 빠지지 않는다.

원본 바닥·벽 재질을 노출면까지의 거리로 혼합하고, A/B/C 양 끝의 색·명도·높이를 맞춰 연결한다. 바깥 모서리는 **반경 28px의 곡선**으로 다듬고 재질의 방향과 깊이도 곡선을 따른다. 안쪽 접합부에는 **반경 24px의 오목한 연결 조각**을 추가한다. 평평한 구간의 불규칙 윤곽은 최대 3px이며 일직선 띠를 덧그리지 않는다. 충돌 격자·지형 좌표·카메라 배율은 그대로다. 모서리의 시각적 곡선과 사각형 충돌 판정은 서로 다르다.

안쪽 연결 조각은 지역별 별도 Tilemap에 배치하며 콜라이더가 없다. 빈칸에서 이웃한 두 면과 그 사이 대각선이 모두 일반 지형일 때만 붙인다. 물·특수 발판·게이트 칸에는 덮어 그리지 않는다. 15개 모서리 조합 × 3패턴 × 2지역으로 장식 조각 90종을 제공한다. 지형이 변경되면 주변 연결 조각도 생성·제거한다. 원래 282종 지형과 함께 `Validation/terrain-rounded-corners.json`에서 곡선·배치·비충돌 레이어를 검증한다.

2026-10-16 추가 수정: 내장 이미지 생성으로 잔입자와 가는 선을 줄이고 큰 흙덩어리·넓은 명암 면을 그렸다. A/B/C 3종을 좌표에 따라 고정 배치하며 상하좌우에 같은 변형이 연속되지 않는다. 타일 전체 깊이에 걸쳐 어두워지며 중앙부터 검게 만드는 이전 페이드는 제거했다. 원화 12장과 생성 프롬프트는 [v6 규격](../Art/samples/regional-variants-v6/SPEC.md)에 기록한다.

`Assets/Unfin/Generated/r1_t1_e*.asset`와 `r2_t1_e*.asset`가 연결용 타일이다. 기본 파일은 A, `_v1`은 B, `_v2`는 C이며 `PrototypeTile.patternVariant`는 0/1/2다. `exposedEdges`의 하위 비트 1/2/4/8은 위/오른쪽/아래/왼쪽 노출면, 16/32/64/128은 좌상/우상/우하/좌하 안쪽 모서리를 뜻한다. 전체 조합을 저장하므로 현재 맵에 없는 형태도 사용할 수 있다. 플레이 중 지형 변경 시 주변 타일의 이웃 상태와 기존 위치의 패턴을 함께 적용한다.

검증은 256가지 이웃 배치, 282종 이미지의 노출면·안쪽 모서리, 실제 맵 전체의 타일 선택을 확인한다. `Validation/terrain-corners.json`과 `terrain-patterns.json`에 연결부·패턴 배치·요철·깊이별 명도 검증을 기록한다. `world1-corner-tiles.png`와 `world2-corner-tiles.png`는 타일 목록, `world*-patterns-abc.png`는 혼합 연결 예시, `world*-stairs-and-corners.png`는 계단·천장·기둥 검수 화면이다. 검수 화면 재생성은 배치 메서드 `Unfin.Editor.TerrainCornerValidation.Render`를 그래픽 장치와 함께 실행한다.

`Assets/Scenes/World1TerrainCorners.unity`와 `World2TerrainCorners.unity`는 연결부만 살펴보는 별도 Tilemap 검수 씬이며 실제 월드 배치와 다르다. 이 씬도 사각형 땅으로 둘러싸고 지도 밖을 땅으로 취급해 밝은 외곽 테두리를 없앤다. 두 씬의 카메라는 1920×1152에서 한 칸 128px로 15×9칸을 보여 준다. 실제 설계 맵은 위의 전체 지도 이미지와 World12Prototype에서 확인한다. 플레이용 World12Prototype의 1920×1080 설정과 빌드 시작 씬은 유지한다.

## 조작

- A/D 또는 좌우 방향키: 이동.
- Space: 점프, 벽에 붙어서 다시 누르면 벽 점프. 키를 빨리 놓으면 낮게 점프한다.
- S+Space: 통과형 발판 아래로 내려가기.
- Shift: 대시. 이동 능력은 맵 검증을 위해 처음부터 활성화되어 있다.
- F: 충돌을 무시하는 자유 비행. WASD/방향키로 이동한다.
- E: 가까운 연결된 게이트 하나를 여는 QA 조작.
- 1/2 또는 F1/F2: 1지역 시작점 / 2지역 첫 세이브 위치로 이동. 웹에서는 1/2를 권장한다.
- M: 전체 지도. 빈칸을 클릭하면 자유 비행으로 해당 위치를 확인한다.
- R: 씬을 다시 불러와 변경된 지형과 플레이어를 초기화한다.

통과형 발판(4/6/15), 고체 벽, 위험 지형(3), 물(9)을 구분한다. 위험 지형과 물에 닿으면 마지막 안전한 발판으로 돌아온다. 무너지는 발판(6)은 0.45초 후 사라졌다 2.6초 후 복구되고, 녹는 발판(15)은 1.5초 후 사라졌다 3초 후 복구된다. 밀랍 바닥(16)은 이동을 느리게 한다.

적 AI·전투·능력 획득·수집·영구 세이브·리본 이동·보스전은 이식하지 않았다. 적과 아이템은 **원본 초기 배치를 확인하는 표식**이다. E는 실제 열쇠/패링/물감 조건을 대체하는 QA 기능이다. 밀랍 벽의 특수 이동 제한, 화로에 의한 용해 시간 변경, 수영 등 세부 기믹은 아직 없다. 정적 지도 구조와 전체 게임 동작의 이식 범위를 구분한다.

## 재생성과 검증

저장소 루트에서 `node Tools/export_unity_world.cjs`를 실행한다. HTML과 별도 지도 JS가 다르면 오류로 중단한다. Unity 메뉴 **Unfin → Rebuild World 1 + 2 Prototype**으로 씬과 파생 에셋을 생성한다. 이 명령은 생성 전용 씬을 덮어쓰므로 수작업 수정은 별도 씬에 저장한다.

**Unfin → Validate World 1 + 2 Prototype**은 저장된 씬을 읽어 모든 셀의 타일 ID·레이어·위치, 이미지 해상도/PPU, 화면 배율, 적/아이템 표식 좌표를 대조한다. 발판 착지·하강 통과·상승 통과·고체 착지·대시 벽 충돌도 검증한다. 결과는 `Validation/world12-validation.json`에 저장한다.

배치 실행 메서드는 `Unfin.Editor.WorldPrototypeBuilder.Build`, `Validate`, `RenderPreviews`다. 렌더 미리보기는 그래픽 장치가 필요하므로 `-nographics` 없이 실행한다. 생성 결과의 `.meta`와 씬/타일/텍스처를 함께 Git으로 관리하므로 다음 컴퓨터에서 다시 빌드하지 않아도 씬을 열 수 있다.

Play 모드 자동 검증은 배치 실행에서 `-executeMethod Unfin.Editor.WorldPrototypeSmoke.Run`을 사용하고 `-quit`을 생략한다. 검증기가 가상 키보드로 이동·점프·2지역 이동·비행·게이트 열기·초기화를 확인한 후 Unity를 종료한다. 결과는 `Validation/world12-playmode.txt`, 실제 카메라 미리보기는 `Validation/world1-1920x1080.png`와 `world2-1920x1080.png`다.


## 현재 플레이어 스프라이트 (2026-10-17)

`World12Prototype`의 플레이어에 HTML 통합 테스트에서 선택한 전신 원화42장을 적용했다. `PaperSpritePlayer.prefab`과 `Animations/PlayerSprites/PlayerSprites.asset`이 실제 게임에서 사용된다. 예전 관절 리그는 씬에서 제거했으며, 이전 에셋은 비교용으로 보존한다.

- Idle: `player-c-idle-v1`, 6장·1800ms.
- Run: `player-c-v6-rhythm`, 8장·640ms.
- 출발/정지: `player-c-run-idle-v1`, 4장·160ms (출발은 역순).
- 제자리 점프: `player-c-jump-v2`, 9장. 이동 점프: `player-c-moving-jump-v1`, 6장.
- 대시: `player-c-dash-v2`, 양 다리를 접은 4장·180ms. 지상/공중 공통, 방향 고정, 공중1회, 접지 후 회복.
- 독립 낙하/착지: `player-c-fall-v1`, 5장. 점프 없이 발판에서 내려오거나 공중 대시가 끝날 때 사용한다.

PNG는 원본 바이트 그대로 512px, PPU256, Bilinear, 무압축으로 임포트한다. 발 기준점과 자세별 x 기준점은 HTML 미리보기와 같다. 실제 맵의 12×22 논리 충돌 영역과 기존 점프 속도·중력을 유지하며, 상승/정점/하강/착지를 실제 속도와 접지에 맞춘다. 따라서 HTML 검토 화면의 고정 점프 높이130px를 맵의 이동 능력으로 적용하지 않는다. 점프와 착지 원화가 이동 입력을 잠그지 않는다.

대시는 기존 지상 달리기125의3.2배인400 논리px/s로180ms 이동한다. 종료 후220ms 재사용 간격이며 점프 도중 사용하면 남은 상승을 취소하고 Fall로 전환한다. 낙하 중 사용하면 대시 전 하강 속도를 복원한다. 착지 직전120ms 점프 입력 기억과 접지 즉시 재점프를 지원한다. 지상·이동 3단 공격과 상승·낙하 공격을 J 또는 마우스 왼쪽 클릭으로 재생한다. 피격·사망 원화와 실제 대미지 판정은 아직 연결하지 않았다.

다시 적용: 저장소 루트에서 `python Tools/export_player_unity.py` 실행 후 Unity 메뉴 **Unfin → Apply Current Player Sprites**. 별도 빌드용 프로젝트를 통해 적용하려면 `powershell -ExecutionPolicy Bypass -File Tools/apply_player_unity.ps1`을 사용한다. 이 명령은 원본 Book의 Library 대신 기존 빌드용 프로젝트를 사용하고 생성된 에셋·메타·프리팹·씬을 Book으로 복사한다. 검증 기록은 `Validation/player-sprites-import.json`, `player-sprites-runtime.json`, 실제 게임 화면은 `player-sprites-in-game.png`다. 편집용 SpriteRenderer AnimationClip 11개와 AnimatorController도 함께 제공한다. 실제 플레이어는 물리 상태와 동기화하는 프레임 드라이버로 재생한다.

웹 배포 데이터가25MiB를 넘으면 `node Tools/prepare_webgl_site.cjs`가16MiB 조각으로 무손실 분할한다. 브라우저에서 각 조각의 SHA-256 무결성을 확인한 뒤 합쳐 Unity 기본 압축 해제로 전달한다. PNG 해상도·색·텍스처 압축 설정을 낮추지 않는다. `Tools/build_webgl.ps1`에서 자동 실행하며 `Tools/verify_webgl.cjs`가 조각 크기·해시·재조립 데이터와 WebAssembly를 검사한다.


## 플레이어 공격과 지역별 발판 적용 (2026-10-18)

현재 라이브러리는 기존 이동 원화42장과 공격 원화44장, 총86장·19개 시퀀스를 사용한다. 기존 이동 PNG는 바이트 그대로 유지하고 공격768×640 원본은 PPU256·무압축·최대1024로 임포트한다. 지상 원화 발 기준은560px, 공중 원화는 현재 이동 자세의 머리 기준에 맞춘다. 지우개 찌르기 이펙트도 별도 SpriteRenderer에 적용한다.

- J / 마우스 왼쪽 클릭: 지상에서는 1타 베기 → 2타 올려치기 → 3타 찌르기. 이동 중에는 MoveAttack 원화를 쓰며 실제 이동 속도를 유지한다.
- 제자리 공격 시간:360/290/460ms. 이동 공격:480/400/420ms. 2타 휘두름은 한 장이며 마무리140ms를 유지한다.
- 종료 후250ms 이내 입력은 다음 타격으로 연결한다. 미리 누른 입력은150ms 동안 기억하고, 3타 종료 후500ms 회복 시간이 있다.
- 상승/낙하 공격은 각220ms. 낙하 공격도 정면으로 베며, 수평 이동·중력·공격 시작 방향을 유지한다. 점프·대시·착지에 맞춰 공격을 취소한다. 지상에서 반대 방향으로 전환하면 공격을 취소한다.
- 높은 곳에서 점프한 뒤 출발 높이 아래로 내려가도 접지하지 않았다면 Fall로 연결한다. 실제 접지 전에는 착지 원화를 재생하지 않는다. 기존 맵의 중력·점프 높이를 유지한다.

지역별 한 칸 발판15종은 `Assets/Unfin/Art/RegionalPlatforms`, 타입4/6/15별 기본 타일45개는 `Assets/Unfin/Platforms`에 있다. 현재 배치는 아래의 8구역 팔레트 적용 기준을 따른다. 3~5지역은 지도 제작 전이므로 원화만 준비했다. 폭128px·PPU128·1칸이며 원본 투명 PNG를 유지한다. 타입6/15는 옅은 식별색을 추가하고 기존 무너짐·녹음·복구 및 통과 판정을 유지한다.

재현: `python Tools/export_player_unity.py`, `python Tools/export_regional_platforms_unity.py` 실행 후 `powershell -ExecutionPolicy Bypass -File Tools/apply_player_unity.ps1`. Unity 메뉴 **Unfin → Apply Current Player Sprites**는 플레이어와 발판을 함께 적용한다. 전체 맵 재생성에도 새 발판 배치를 반영했다.

검증: `Validation/player-combat-runtime.json`, `Validation/regional-platforms.json`, 기존 이동/전체 맵 검사. 실제 Unity 렌더는 `player-attack-in-game.png`, `player-air-attack-in-game.png`, `platform-world1-in-game.png`에 기록한다. 로컬 Unity 소스·씬 변경이며 공개 웹사이트의 배포 상태와는 별개다.

## 큰 방의 중간층 (2026-10-18)

1지역의 무너진 갱도·나무 속 탑, 2지역의 노을빛 큰 굴·삼색 광장·밀랍 저장고·물감 폭포 홀에 총12개의 고체 중간층을 추가했다. 두께2칸의 층을 얇은 발판과 연결하며, 방마다 순환 경로·수직굴 진입·상층 수집품 접근에 활용한다. 현재 지도는78,400셀 중57,774셀이 지형이며, 한 칸 발판은1지역367칸·2지역260칸이다.

재현 순서는 `node Tools/add_room_mezzanines.cjs` → `node Tools/export_unity_world.cjs` → `node Tools/export_region1_terrain.cjs` → `node Tools/verify_region1_terrain.cjs` → `powershell -ExecutionPolicy Bypass -File Tools/apply_mezzanines_unity.ps1`이다. 마지막으로 `Tools/build_webgl.ps1`로 로컬 웹을 갱신한다. 원본 셀과 새 셀의 변경 이력 및 방별 좌표는 `Assets/Unfin/Data/room-mezzanines.json`에 있다.

`Validation/room-mezzanines-routes.json`은 실제 GridMotor를 사용해39개 이동 구간을 일반 점프로 검증한다. 대시·벽 점프·게이트 해제는 검사에 사용하지 않는다. 얇은 발판의 머리 공간, 기존 통로·수직굴·마커와 폭포 동쪽 벽타기 공간도 검사한다. `mezzanine-room-지역-방.png`는 방 전체, `mezzanine-play-지역-방.png`는 실제 카메라 배율, `mezzanine-web-지역-방.png`는 웹 실행 화면이다.

로컬 웹에서 `?spawn=78,123`(갱도), `?spawn=108,48`(나무 속 탑), `?spawn=372,28`(큰 굴), `?spawn=430,30`(광장), `?spawn=300,88`(저장고), `?spawn=527,115`(폭포 홀)로 중간층에서 바로 검토할 수 있다. 이는 기존 비행·지역 이동과 같은 프로토타입 검토용 기능이다.

## 8구역 타일 구분 (2026-10-18)

1지역은 기존의 네 가지 지형 원화와 경계 혼합을 유지한다. 얇은 발판은 A 종이굴의 회베이지, B 흑연 광산의 청회색, C 나무 속의 목재색으로 구분하고, D 뿌리 심부는 녹빛 뿌리 지형에 자주빛 발판을 배치한다. 2지역은 E 노을 굴의 주황색, F 밀랍 공방의 황토·밀랍색, G 지하 협곡의 청색, H 기사의 탑의 적갈색으로 구분한다. 2지역 팔레트는 일반 바닥·벽·천장·중간층·안쪽 모서리와 얇은 발판에 적용한다.

기존 발판 원화15종 중 재질이 맞는 세트를 재사용하고, 구역8종 × 변형3종 × 발판 타입3종의 타일72개를 별도로 만든다. `RegionalTilePalette.cs`가 가까운 방의 구역을 고르며, 2지역 지형은 경계4칸에 걸쳐 색을 혼합한다. `RegionalTiles.shader`는 원화의 밝기·질감·투명도를 유지하며 구역 색을 입힌다. 게이트·위험 타일은 원래 식별색을 유지한다. 발판 복구와 게이트 제거 후 지형 갱신에도 구역 팔레트를 적용한다.

Unity 메뉴 **Unfin → Apply Zone Tile Palettes** 또는 `powershell -ExecutionPolicy Bypass -File Tools/apply_mezzanines_unity.ps1 -Method Unfin.Editor.RegionalPlatformImporter.ApplyZonesAndRender`로 재적용한다. 전체 맵/플레이어 재생성 시에도 구역별 타일이 유지된다. 검증은 `Validation/regional-platforms.json`, 8구역 렌더는 `Validation/zone-tiles-A.png`부터 `zone-tiles-H.png`까지다.

## 방별 가까운 배경 (2026-10-18)

현재 56개 방에 공용 원화32종·전용 대형 원화3종을 조합한 배경 조각127개를 배치했다. `Assets/Unfin/Art/RoomBackdropsV1/room-layouts.json`에 방 크기·구성 의도·좌표를 기록한다. 긴 방은 비대칭 가로 구성, 높은 방은 연속된 세로 구조물, 작은 방은 한두 개의 대표 소재를 사용한다. 기존 반복 벽 강도는 원래 값0.52로 복원하고 새 배경을 -65/-40/-20 세 레이어에 배치했다. 방별 배경 구조물은 배치한 월드 좌표에 고정하며 카메라를 따라 이동하지 않는다. 지형과 플레이어보다 뒤에 그리며 충돌을 추가하지 않는다.

Unity 메뉴 **Unfin → Apply Room Backdrops and Render**, 또는 `Tools/apply_room_backdrops_unity.ps1`로 적용한다. 상세 재현 방법과 생성 프롬프트는 `Art/room-backdrops-v1/README.md`를 참고한다. 검증 결과는 `Validation/room-backdrops.json`, 방 전체 실제 렌더는 `Validation/room-backdrops/`에 있으며, 웹 검토 화면은 `Art/samples/room-backdrops-v1/preview.html`이다.

## 카메라 반응과 배경 밝기 (2026-10-18)

`PlatformCameraFollow.cs`는 좌우 위치를 매 프레임 플레이어 중심에 즉시 맞춘다. 좌우 여유 영역, 감쇠, 속도에 따른 전방 쏠림은 사용하지 않는다. 상하는 플레이어 중심보다1칸 위를 목표로 점프 시작부터 따라가며, 상승은0.14초, 하강·착지·비행은0.12초 SmoothDamp를 적용한다. 수치는 완료 시간이 아닌 감쇠 파라미터다. 이전의 큰 수직 여유 영역과 착지까지 높이를 유지하는 방식은 사용자 피드백에 따라 제거했다.

화면 끝 안전 여백은 플레이어 중심에서0.8칸이며 월드 경계를 넘지 않는다. 리스폰·지역 이동·검토 좌표 이동은 즉시 재정렬한다. 카메라는 LateUpdate에서 이동 충돌체 중심을 추적하며 배경 구조물은 월드 좌표에 고정한다. 애니메이션의 머리 흔들림은 추적하지 않는다.

기존 1지역 뒷벽 타일의 불투명도를0.08에서 원래 값0.52로 복원했다. `RoomBackdropImporter`도0.52를 사용하므로 배경을 재생성해도 복원된 밝기를 유지한다. 새 방별 배경 구조물의 원화·색상·배치는 유지한다.

참고: [Itay Keren, GDC Scroll Back](https://media.gdcvault.com/gdc2015/presentations/Keren_Itay_ScrollBack.pdf)과 [Unity Position Composer](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachinePositionComposer.html)의 축별 추적·감쇠 개념을 현재 GridMotor에 맞춰 구현했다. Cinemachine 패키지를 새로 설치하지 않았다.

검증과 웹 빌드: `Tools/build_webgl.ps1 -ValidateCamera`. 배경 불투명도0.52, 점프 첫100ms 추적, 최대 점프4회, 좌우·반전·대시 즉시 추적, 긴 낙하·착지·리스폰과30/60/120fps 수직 상승을 검사한다. 결과는 `Validation/camera-follow.json`이다. 점프 높이·속도·대시·공격 및 맵 데이터는 변경하지 않는다.

## 공격 이펙트 확대 (2026-10-18)

`PaperAttackArc.cs`가 1·2타와 이동·상승·낙하 공격에 종이색의 큰 바깥 궤적을 그린다. 기존 프레임에 그려진 작은 궤적은 안쪽 잔상으로 유지한다. 캐릭터 원본 PNG와 비율은 바꾸지 않는다. 새 궤적은 기존 휘두르기 프레임에서만 켜지고 회수·취소 시 숨긴다. 공중 공격의 위치 보정과 좌우 반전을 함께 따르며 캐릭터보다 뒤에 그린다.

3타 찌르기 별도 이펙트는 기존 크기의1.6배다. 공격 지속 시간·콤보 입력 시간·이동 속도는 그대로다. 이는 시각 효과 변경이며 전투 판정을 추가하지 않는다.

`Tools/build_webgl.ps1 -ValidateCombat`로 기존 전투 회귀 검사와 여섯 가지 이펙트 렌더를 수행하고 웹 빌드한다. 검증은 `Validation/attack-effects.json`, 캡처는 `Validation/attack-fx-*.png`다.
