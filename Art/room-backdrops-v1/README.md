# 방별 가까운 배경 v1

현재 `world12.json`의 56개 방을 대상으로 만든 실제 게임 배경이다. 8구역 공용 조각 32종과 나무 속 탑·무너진 갱도·물감 폭포 홀의 전용 대형 원화 3종을 조합한다. 56장 모두 별도로 그린 원화가 아니라, 방마다 대표 소재와 구성·크기·층위를 다르게 지정한 56개 합성이다.

`room-layouts.json`에는 각 방의 실제 크기, 128px 타일/1920×1080 기준 화면 수, 발판 수, 중간층 수, 구성 의도와 모든 배치 좌표가 있다. 현재 최종 배치는 127개다. 70칸 가로 방은 긴 빈 구간을 포함하며, 46칸 세로 탑과 28칸 폭포 홀은 연속된 전용 구조물을 중심으로 구성한다. 비밀방에는 주로 한 조각만 배치한다.

원화는 내장 `image_gen`으로 생성했다. 최종 프롬프트와 원본 저장 위치는 `generation.json`, `hero-generation.json`에 보관한다. 실제 PNG는 이 폴더와 `Book/Assets/Unfin/Art/RoomBackdropsV1`에 저장했다. 알파를 유지하며 픽셀을 재가공하지 않는다. `atlas-layouts.json`의 UV 범위로 각 조각을 선택한다. 요청 해상도와 생성 해상도는 다를 수 있으며 실제 크기는 해당 JSON에 기록한다.

Unity는 뒤쪽부터 -65/-40/-20 정렬 순서로 배경을 그린다. 플레이어·지형보다 모두 뒤에 있으며, 기존 반복 뒷벽의 강도는 사용자 피드백에 따라 원래 값인 0.52로 복원했다. 원화 대비 밝기는 각각 0.38/0.52/0.62다. 모든 구조물은 배치한 월드 좌표에 고정되며 카메라에 따른 시차 이동을 사용하지 않는다. 기존 씬의 parallax 값은 호환성을 위해 남아 있지만 위치에 영향을 주지 않는다. 방 가장자리 1.25칸에서 부드럽게 사라진다. PNG 비율을 유지하여 강제로 늘리지 않는다. 배경에는 충돌을 추가하지 않는다.

재현 순서:

1. `node Tools/design_room_backdrops.cjs`
2. `C:/Users/doun0/miniconda3/python.exe -X utf8 Tools/inspect_room_backdrop_atlases.py` — 알파와 UV 분석만 수행, 이미지 픽셀 변경 없음.
3. `powershell -ExecutionPolicy Bypass -File Tools/apply_room_backdrops_unity.ps1`
4. `node Tools/package_room_backdrop_preview.cjs`
5. `powershell -ExecutionPolicy Bypass -File Tools/build_webgl.ps1`

Unity 메뉴는 **Unfin → Apply Room Backdrops and Render**. 씬을 저장하고 다시 연 뒤 56개 그룹·배치 수·재질·레이어·충돌 없음·전체 맵 보존을 검증한다. `Book/Validation/room-backdrops.json`과 `Book/Validation/room-backdrops/*.png`에 결과를 기록한다. 전체 맵 재생성도 새 배경을 다시 적용한다.

방별 실제 합성 미리보기: `Art/samples/room-backdrops-v1/preview.html`. 8093 아트 미리보기 서버에서 구역 필터·방 선택·원본 크기 보기를 사용할 수 있다.
