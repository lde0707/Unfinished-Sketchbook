# Unfinished-Sketchbook

스케치북 세계의 2D 메트로배니아. 고해상도 비도트 그래픽과 여러 타일셋을 사용하는 타일맵으로 제작합니다.

- [기획 문서](Docs/README.md)
- [고해상도 타일맵 공통 규격](Docs/21_고해상도_타일맵_규격.md): 지형 256×256px/칸, PPU 256. 기존 HTML의 16은 충돌 계산용 논리 단위입니다.
- 실제 Unity 프로젝트는 `Book/`입니다. Unity Hub에서 이 폴더를 열고, `Book/ProjectSettings/ProjectVersion.txt`에 기록된 **6000.5.3f1** 버전을 사용합니다.
- `Book/Assets`, `Book/Packages`, `Book/ProjectSettings`는 Git으로 관리합니다. `Library`, `Logs`, `UserSettings` 등은 로컬에서 다시 생성되는 파일이므로 제외합니다.
- 현재 `Book/Assets`는 빈 시작 구조입니다. 지역별 바닥·벽 이미지와 미리보기는 `Art/samples/regional-pairs-v4/`에 있으며, Unity 씬 적용은 별도 작업입니다.
