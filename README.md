# StillDrawn

<!-- WINDOWS-RELEASE-START -->
## Windows에서 바로 플레이

**[StillDrawn · Windows 실행 파일 다운로드](https://github.com/lde0707/Unfinished-Sketchbook/releases/tag/portfolio-windows-2026-11-14)**

스케치북 세계를 탐험하는 개발 중 2D 액션 프로젝트.

1. 위 Releases 페이지의 `StillDrawn-Windows-x64.zip`을 내려받습니다. `Source code` 파일은 실행 파일이 아닙니다.
2. ZIP 전체를 한 폴더에 압축 해제합니다.
3. `StillDrawn.exe`를 실행합니다. Unity 설치는 필요 없습니다.

`StillDrawn_Data`, `UnityPlayer.dll`, 나머지 동봉 폴더를 함께 유지해야 합니다. Windows 64비트용이며 모바일·macOS에서는 실행되지 않습니다.

**조작:** PC 키보드와 마우스를 사용합니다. 타이틀의 조작 방법과 게임 안의 키 안내를 확인하세요.

[실행·검증 범위](PLAY.md) · [QA 포트폴리오](https://leedoeon.vercel.app/qa/index.html)
<!-- WINDOWS-RELEASE-END -->

스케치북 세계의 2D 메트로배니아. 고해상도 비도트 그래픽과 여러 타일셋을 사용하는 타일맵으로 제작합니다.

- [기획 문서](Docs/README.md)
- [고해상도 타일맵 공통 규격](Docs/21_고해상도_타일맵_규격.md): 아트 마스터는 256×256px, 현재 Unity 맵 프로토타입은 **128×128px/칸, PPU 128**입니다. 기존 HTML의 16은 충돌 계산용 논리 단위입니다.
- 실제 Unity 프로젝트는 `Book/`입니다. Unity Hub에서 이 폴더를 열고, `Book/ProjectSettings/ProjectVersion.txt`에 기록된 **6000.5.3f1** 버전을 사용합니다.
- `Book/Assets`, `Book/Packages`, `Book/ProjectSettings`는 Git으로 관리합니다. `Library`, `Logs`, `UserSettings` 등은 로컬에서 다시 생성되는 파일이므로 제외합니다.
- Unity에서 `Book/Assets/Scenes/World12Prototype.unity`를 열고 Play하면 HTML World 1·2의 통합 맵을 탐색할 수 있습니다. **1920×1080에서 한 칸 128px**, 카메라 Orthographic Size **4.21875**입니다.
- [Unity 프로토타입 실행·조작·검증 방법](Book/README.md). 적·아이템은 원본 위치의 표식이며, 전투·진행 시스템은 아직 이식하지 않았습니다.
- [Unity 웹 플레이](https://unfinished-sketchbook-unity.creamykrill16.chatgpt.site): 본인 ChatGPT 계정으로 로그인하는 비공개 WebGL 배포. PC 키보드로 이동·점프·지역 전환을 할 수 있습니다.
