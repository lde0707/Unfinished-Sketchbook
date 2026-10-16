# World 1 + 2 Unity 맵 프로토타입

Unity **6000.5.3f1**에서 이 `Book` 폴더를 열고 `Assets/Scenes/World12Prototype.unity`를 열어 Play한다. Game 뷰를 **1920×1080**으로 설정한다. 에디터 패널의 Fit 축소율은 실제 게임 출력 해상도와 별개다.

타일 이미지는 **128×128px**, PPU **128**, Grid Cell Size **1×1**이다. 카메라는 Orthographic Size **4.21875**이며 1920×1080에서 타일 한 칸이 **128×128 화면 픽셀**이다. 화면에 가로 15칸, 세로 8.4375칸이 들어온다. 다른 출력 해상도에서는 같은 세로 시야를 유지한다.

## 원본과 구조

- 원본: `../Prototype/unfin_world.html`. `world_map_data.js`와 모든 지도 상수·셀을 대조한 뒤 추출한다.
- 통합 지도 560×215칸, 1지역/2지역 경계 X=280. 빈칸을 포함한 120,400칸과 지형 90,124칸을 그대로 유지한다.
- 방 56개, 수직 통로 14개, 적 표식 95개, 아이템·장치 표식 67개. 방·통로·수직 통로는 Hierarchy에서 별도로 찾을 수 있다.
- Unity 타일 좌표는 `(HTML x, -HTML y - 1)`. 플레이어·적·장치의 위치와 크기는 HTML 논리 좌표를 16으로 나눈다. 박쥐의 천장 배치와 아이템별 크기 보정도 반영한다.
- 지역별로 Solid terrain, One-way platforms, Water 타일맵을 분리한다. 원본 타일 ID는 `PrototypeTile.htmlType`과 추출 JSON에 보존한다.
- TilemapCollider2D와 CompositeCollider2D를 제공한다. 플레이어는 HTML의 격자 충돌 계산을 포팅한 GridMotor로 움직인다. Rigidbody2D 물리 플레이어가 아니다.
- `Art/samples/regional-pairs-v4`의 그래파이트·크레파스 바닥/벽을 128px 파생 이미지로 사용한다. 지중 내부는 거의 검정이다. 특수 지형과 캐릭터는 구조 확인용 표식이다.

## 모서리·계단 연결 타일

그래파이트와 크레파스에 **각 47종, 총 94종**의 128px 지형 연결 타일을 제공한다. 상하좌우와 대각선 8방향을 확인해 직선면, 바깥 모서리, 안쪽 오목 모서리, 계단 단차, 얇은 기둥, 돌출부, 독립 블록을 자동 선택한다. 윗면이 있는 타일에서도 좌우면·아랫면 재질을 함께 그리므로 계단의 수직면이 빠지지 않는다.

원본 바닥·벽 재질을 노출면까지의 거리로 혼합하고, 타일 양 끝의 재질을 이어 붙여 연결부를 완화한다. 안쪽 모서리의 표면은 곡선으로 이어지며, 바깥 꼭짓점은 최대 3px 범위에서 둥글게 처리한다. 표면 띠는 2px, 지중은 어두운 채움을 유지한다. 충돌 격자·지형 좌표·카메라 배율은 바꾸지 않는다.

`Assets/Unfin/Generated/r1_t1_e*.asset`와 `r2_t1_e*.asset`가 연결용 타일이다. `PrototypeTile.exposedEdges`의 하위 비트 1/2/4/8은 위/오른쪽/아래/왼쪽 노출면, 16/32/64/128은 좌상/우상/우하/좌하 안쪽 모서리를 뜻한다. 전체 조합을 저장하므로 현재 맵에 없는 형태도 사용할 수 있다. 플레이 중 지형 변경 시 주변 타일의 이웃 상태도 갱신한다.

검증은 256가지 이웃 배치, 94종 이미지의 노출면·안쪽 모서리, 실제 맵 전체의 타일 선택을 확인한다. `Validation/terrain-corners.json`은 결과, `world1-corner-tiles.png`와 `world2-corner-tiles.png`는 타일 목록, `world1-stairs-and-corners.png`와 `world2-stairs-and-corners.png`는 계단·천장·기둥 검수 화면이다. 검수 화면 재생성은 배치 메서드 `Unfin.Editor.TerrainCornerValidation.Render`를 그래픽 장치와 함께 실행한다.

`Assets/Scenes/World1TerrainCorners.unity`와 `World2TerrainCorners.unity`는 연결부만 살펴보는 별도 Tilemap 검수 씬이다. 두 씬의 카메라는 1920×1152에서 한 칸 128px로 15×9칸을 보여 준다. 플레이용 World12Prototype의 1920×1080 설정과 빌드 시작 씬은 유지한다.

## 조작

- A/D 또는 좌우 방향키: 이동.
- Space: 점프, 벽에 붙어서 다시 누르면 벽 점프. 키를 빨리 놓으면 낮게 점프한다.
- S+Space: 통과형 발판 아래로 내려가기.
- Shift: 대시. 이동 능력은 맵 검증을 위해 처음부터 활성화되어 있다.
- F: 충돌을 무시하는 자유 비행. WASD/방향키로 이동한다.
- E: 가까운 연결된 게이트 하나를 여는 QA 조작.
- F1/F2: 1지역 시작점 / 2지역 첫 세이브 위치로 이동.
- M: 전체 지도. 빈칸을 클릭하면 자유 비행으로 해당 위치를 확인한다.
- R: 씬을 다시 불러와 변경된 지형과 플레이어를 초기화한다.

통과형 발판(4/6/15), 고체 벽, 위험 지형(3), 물(9)을 구분한다. 위험 지형과 물에 닿으면 마지막 안전한 발판으로 돌아온다. 무너지는 발판(6)은 0.45초 후 사라졌다 2.6초 후 복구되고, 녹는 발판(15)은 1.5초 후 사라졌다 3초 후 복구된다. 밀랍 바닥(16)은 이동을 느리게 한다.

적 AI·전투·능력 획득·수집·영구 세이브·리본 이동·보스전은 이식하지 않았다. 적과 아이템은 **원본 초기 배치를 확인하는 표식**이다. E는 실제 열쇠/패링/물감 조건을 대체하는 QA 기능이다. 밀랍 벽의 특수 이동 제한, 화로에 의한 용해 시간 변경, 수영 등 세부 기믹은 아직 없다. 정적 지도 구조와 전체 게임 동작의 이식 범위를 구분한다.

## 재생성과 검증

저장소 루트에서 `node Tools/export_unity_world.cjs`를 실행한다. HTML과 별도 지도 JS가 다르면 오류로 중단한다. Unity 메뉴 **Unfin → Rebuild World 1 + 2 Prototype**으로 씬과 파생 에셋을 생성한다. 이 명령은 생성 전용 씬을 덮어쓰므로 수작업 수정은 별도 씬에 저장한다.

**Unfin → Validate World 1 + 2 Prototype**은 저장된 씬을 읽어 모든 셀의 타일 ID·레이어·위치, 이미지 해상도/PPU, 화면 배율, 적/아이템 표식 좌표를 대조한다. 발판 착지·하강 통과·상승 통과·고체 착지·대시 벽 충돌도 검증한다. 결과는 `Validation/world12-validation.json`에 저장한다.

배치 실행 메서드는 `Unfin.Editor.WorldPrototypeBuilder.Build`, `Validate`, `RenderPreviews`다. 렌더 미리보기는 그래픽 장치가 필요하므로 `-nographics` 없이 실행한다. 생성 결과의 `.meta`와 씬/타일/텍스처를 함께 Git으로 관리하므로 다음 컴퓨터에서 다시 빌드하지 않아도 씬을 열 수 있다.

Play 모드 자동 검증은 배치 실행에서 `-executeMethod Unfin.Editor.WorldPrototypeSmoke.Run`을 사용하고 `-quit`을 생략한다. 검증기가 가상 키보드로 이동·점프·2지역 이동·비행·게이트 열기·초기화를 확인한 후 Unity를 종료한다. 결과는 `Validation/world12-playmode.txt`, 실제 카메라 미리보기는 `Validation/world1-1920x1080.png`와 `world2-1920x1080.png`다.
