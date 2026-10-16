# Unfinished-Sketchbook

스케치북 세계의 2D 메트로배니아. 고해상도 비도트 그래픽과 여러 타일셋을 사용하는 타일맵으로 제작합니다.

- [기획 문서](Docs/README.md)
- [고해상도 타일맵 공통 규격](Docs/21_고해상도_타일맵_규격.md): 아트 마스터는 256×256px, 현재 Unity 맵 프로토타입은 **128×128px/칸, PPU 128**입니다. 기존 HTML의 16은 충돌 계산용 논리 단위입니다.
- 실제 Unity 프로젝트는 `Book/`입니다. Unity Hub에서 이 폴더를 열고, `Book/ProjectSettings/ProjectVersion.txt`에 기록된 **6000.5.3f1** 버전을 사용합니다.
- `Book/Assets`, `Book/Packages`, `Book/ProjectSettings`는 Git으로 관리합니다. `Library`, `Logs`, `UserSettings` 등은 로컬에서 다시 생성되는 파일이므로 제외합니다.
- Unity에서 `Book/Assets/Scenes/World12Prototype.unity`를 열고 Play하면 HTML World 1·2의 통합 맵을 탐색할 수 있습니다. **1920×1080에서 한 칸 128px**, 카메라 Orthographic Size **4.21875**입니다.
- [Unity 프로토타입 실행·조작·검증 방법](Book/README.md). 적·아이템은 원본 위치의 표식이며, 전투·진행 시스템은 아직 이식하지 않았습니다.
