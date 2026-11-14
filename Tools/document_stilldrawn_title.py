from pathlib import Path
import json,os
root=Path(__file__).resolve().parents[1]
rows=[
('Hollow Knight','https://interfaceingame.com/screenshots/hollow-knight-main-menu/','어두운 공간과 밝은 로고, 절제된 선택 장식으로 분위기를 먼저 전달한다.','짙은 흑연 배경과 상아색 문자, 작은 선택 표식','다른 게임 고유의 문장과 장식 복제'),
('Ori and the Blind Forest','https://www.mobygames.com/game/72146/ori-and-the-blind-forest/screenshots/','숲과 빛으로 모험의 정서를 타이틀부터 만든다.','숲 깊이와 책의 국소광','고채도 발광과 배경 전역의 복잡한 움직임'),
('Ori and the Will of the Wisps','https://www.mobygames.com/game/142015/ori-and-the-will-of-the-wisps/screenshots/windows/1013872/','배경 풍경을 크게 보이고 메뉴를 풍경에 얹는다.','배경을 가리지 않는 좁은 메뉴 열','실사에 가까운 재질과 큰 광원'),
('ENDER LILIES','https://www.digitaltq.com/ender-lilies-all-endings','메뉴 배경에도 세계의 상태와 정서를 반영한다.','첫 장의 어두운 종이 세계라는 정체성','아직 구현하지 않은 엔딩별 타이틀 변화를 약속하기'),
('Nine Sols','https://ninesols.wiki.gg/wiki/Tips','진행 상태별 메뉴 배경과 저장 관리가 세계관의 일부가 된다.','실제 기록이 있을 때만 이어하기 활성화','최종 보스와 미래 지역을 타이틀에 노출하기'),
('Blasphemous','https://interfaceingame.com/screenshots/blasphemous-main-menu/','세계관의 상징과 타이포그래피가 강하게 결합된다.','책과 미완의 선을 중심 상징으로 사용','종교 문양과 금속 장식을 그대로 가져오기'),
('Blasphemous 2','https://www.spriters-resource.com/pc_computer/blasphemous2/asset/457361/','메인 메뉴 아트가 독립된 배경 자산으로 구성된다.','UI 문자와 배경을 분리한 고해상도 원화','메뉴 문자를 배경 이미지에 굽기'),
('Dead Cells','https://www.mobygames.com/game/89051/dead-cells/screenshots/','공개된 초기 버전 메뉴는 시작·안내·상태 정보를 구분한다.','작은 버전 정보와 분명한 시작 경로','뉴스·업데이트 홍보 패널과 로그라이크 모드 목록'),
('Afterimage','https://www.mobygames.com/game/202203/afterimage/screenshots/windows/','그려진 풍경과 타이틀로 탐험의 분위기를 잡는다.','넓은 그림과 게임과 맞는 재질','지금 지원하지 않는 난이도 선택'),
('Celeste','https://www.mobygames.com/game/101142/celeste/screenshots/','여정의 장소와 메뉴가 연결되고 옵션을 별도 화면으로 나눈다.','장소 중심 배경과 읽기 쉬운 조작 안내','메트로배니아 탐험을 스테이지 선택식으로 바꾸기'),
('GRIS','https://interfaceingame.com/games/gris/','그림과 여백을 살리고 메뉴를 시각적으로 절제한다.','충분한 여백, 적은 항목, 낮은 정보 밀도','진행에 맞지 않는 흰 화면 중심 팔레트'),
('TUNIC','https://www.mobygames.com/game/181370/tunic/screenshots/','세계의 기호와 간결한 메뉴를 결합한다.','책갈피라는 저장 은유','메뉴 기능까지 해독해야 하는 낯선 문자'),
("Death’s Door",'https://www.mobygames.com/game/168005/deaths-door/screenshots/','타이틀과 메뉴가 게임의 상징과 분위기를 공유한다.','하나의 강한 책 모티프','상호작용 장치를 한 화면에 여러 개 쌓기'),
('Hades','https://interfaceingame.com/screenshots/hades-main-menu/','왼쪽 세로 메뉴와 오른쪽 아트가 기능과 시선을 분리한다.','왼쪽 메뉴 / 오른쪽 주제 원화','상점과 외부 커뮤니티 버튼'),
('Hades II','https://www.mobygames.com/game/223472/hades-ii/screenshots/windows/','타이틀·메인 메뉴를 구별하면서 고유한 미술을 유지한다.','재진입 시 일관된 포커스와 정서','반복 진입 때 반드시 거쳐야 하는 긴 시작 연출'),
('Dark Souls III','https://www.mobygames.com/game/77737/dark-souls-iii/screenshots/windows/837344/','배경과 메뉴를 절제하고 게임 이름과 시작 경로를 강조한다.','불필요한 상자 없이 명확한 선택 상태','과한 검정 여백으로 우리 책 소재까지 지우기'),
('Elden Ring','https://interfaceingame.com/screenshots/elden-ring-main-menu/','하나의 상징과 밝은 로고가 어두운 바탕에서 주목점을 만든다.','책의 세로 빛과 따뜻한 제목','온라인 로그인·서버 안내'),
('Sekiro: Shadows Die Twice','https://www.youtube.com/watch?v=Sv_ImlzQhAU','타이틀부터 세계관의 재료감과 음악을 이어 간다.','종이 재질과 기존의 절제된 배경음','전투의 긴장감을 타이틀에서 과한 타격 효과로 표현하기'),
('Control','https://www.mobygames.com/game/129106/control/screenshots/','크고 명료한 문자와 통일된 정렬이 인상을 만든다.','로고·메뉴·보조 문구의 분명한 크기 차이','차갑고 각진 사무적인 스타일 전체'),
('Alan Wake II','https://www.mobygames.com/game/210927/alan-wake-ii/screenshots/windows/1150080/','어두운 화면과 절제된 메뉴가 이야기의 분위기를 예고한다.','불안을 주되 읽을 수 있는 밝기','영상 왜곡과 화면 전체 깜박임'),
('Outer Wilds','https://medium.com/super-jump/outer-wilds-a-ux-critique-60c6fb8ac2e2','좌측 메뉴·우측 작은 세계와 느린 움직임으로 탐험을 예고한다.','책 속 세계와 조용한 종이 입자','선택을 막는 긴 강제 인트로'),
('Little Nightmares II','https://www.spriters-resource.com/pc_computer/littlenightmaresii/asset/483367/','배경·선택선·밝기·슬라이더가 일관된 메뉴 자산으로 나뉜다.','같은 선과 작은 마름모를 메뉴와 설정에 반복','너무 낮은 대비의 작은 문자'),
('LIMBO','https://umpir.ump.edu.my/id/eprint/40150/1/CD19071.pdf','흑백 배경과 간단한 세로 메뉴가 게임의 재료를 공유한다.','저채도 배경과 밝기 조절','탐험 세계의 따뜻한 종이색까지 없애기'),
('INSIDE','https://interfaceingame.com/games/inside/','게임 장면과 메뉴 사이의 이질감을 줄이고 정보량을 억제한다.','게임과 같은 재질, 조용한 일시정지 화면','옵션의 기능을 숨길 정도의 미니멀함'),
('Prince of Persia: The Lost Crown','https://interfaceingame.com/games/prince-of-persia-the-lost-crown/','주제 아트와 설정을 분리하고 조작 선택을 명확히 한다.','기본 음량·밝기·조작 안내를 시작 전 제공','지원하지 않는 접근성 기능을 빈 항목으로 나열하기'),
('Disco Elysium','https://www.transcript-verlag.de/media/pdf/84/2c/d4/oa9783839460771iW9jXgYdrI9aS.pdf','메뉴의 그림 자체가 이야기의 시점을 전달한다.','장식 풍경 대신 미완성 스케치북을 핵심 서사 그림으로','다른 게임 도시 풍경이나 유화 붓질을 복제하기'),
('NieR:Automata','https://www.platinumgames.co.jp/dev-nier-automata/article/183','개발자 UI 해설은 세계 설정과 기능의 시각적 통일을 설명한다.','종이·먹·작은 기하 표식으로 UI 일관성 유지','밀도 높은 장비·상태 화면을 타이틀에 가져오기'),
('The Witcher 3: Wild Hunt','https://www.mobygames.com/game/73001/the-witcher-3-wild-hunt/screenshots/windows/783198/','주요 인물·공간을 오른쪽에 두고 왼쪽에 시작 메뉴를 정렬한다.','시각 초점과 메뉴의 분리, 이어하기의 우선순위','추가 콘텐츠와 보상 홍보 메뉴'),
('Cyberpunk 2077','https://interfaceingame.com/games/cyberpunk-2077/','장르의 색과 일관된 UI 문법이 메뉴에도 이어진다.','우리 세계에 맞춘 제한된 상아·흑연·황동 팔레트','고채도 네온, 글리치와 뉴스 배너'),
('Bastion','https://www.mobygames.com/game/53411/bastion/screenshots/windows/529828/','고유한 그림을 타이틀에서도 유지해 게임의 첫인상을 만든다.','기존 캐릭터와 맞는 별도 타이틀 원화','게임과 질감이 다른 실사 배경')
]
assert len(rows)==30
data={'title':'Stilldrawn','date':'2026-10-24','method':'공개된 타이틀·메뉴 캡처, 캡처 목록, UI 해설과 메뉴 문서를 바탕으로 한 정성 비교. 게임 30개를 직접 설치하거나 사용자 실험을 한 결과가 아님. 버전별 메뉴 차이가 있으며 Dead Cells 자료는 초기 버전이다. 이미지와 음악은 참고만 하고 게임에서 추출하여 사용하지 않았다.','games':[{'number':i+1,'game':r[0],'source':r[1],'observation':r[2],'adopt':r[3],'exclude':r[4]} for i,r in enumerate(rows)],'implemented':['실제 기록에만 활성화되는 이어하기','새 게임 덮어쓰기 확인','전체/효과음/배경음/화면 밝기 저장','타이틀 움직임 줄이기','키보드·마우스·게임패드 메뉴 탐색','실제 조작 안내','제작진과 사용 음원/서체','Esc 일시정지·저장 후 타이틀 복귀','개발 버전 표시','네이티브 빌드 종료, 웹은 브라우저 전체화면 버튼'],'art':{'file':'Book/Assets/Unfin/Resources/Title/sketchbook.png','generator':'built-in image_gen','prompt':'Dark premium 16:9 2D metroidvania title background, no text or character. Empty charcoal-blue left 45% for title and vertical menu; monumental open aged sketchbook on right, graphite forest drawn on ivory pages, subtle silver-teal light in book crease and one unfinished brass line. Layered torn paper rock ledges and quiet forest silhouettes, tiny floating paper flecks, no photorealism, no UI, no watermark. Match existing warm ivory layered-paper protagonist and dark graphite cave materials.'}}
p=root/'Art/title/stilldrawn-v1/research.json';p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def save(p,s):
 t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
p=next((root/'Docs').glob('07_*.md'));s=p.read_text(encoding='utf-8-sig');s+='''

## 2026-10-24 · Stilldrawn 타이틀

현재 타이틀 표기는 **Stilldrawn**이다. 공개 PC 게임 30종의 타이틀·메뉴 캡처와 UI 자료를 정성 비교한 출처·관찰·채택·제외 기록은 `Art/title/stilldrawn-v1/research.json`에 둔다. 직접 30개 게임을 설치해 측정한 결과가 아니다.

- 핵심 방향: 왼쪽 여백에 상아색 로고와 짧은 세로 메뉴, 오른쪽에 펼쳐진 미완성 스케치북. 흑연 숲·찢긴 종이 지형·펼쳐진 책으로 장면을 구성한다. 별도로 얹었던 플레이어와 접지 그림자는 제거한다. 큰 광원·광고·추가 모드 목록 없이 종이 입자만 느리게 움직인다.
- 이어하기 / 새 이야기 / 설정 / 제작진. 저장이 없거나 현재 맵과 호환되지 않으면 이어하기가 비활성화된다. 새 이야기는 기존 기록이 있을 때만 덮어쓰기 확인을 받는다. 웹에서 작동하지 않는 종료 버튼은 숨기고 브라우저 전체화면 버튼을 제공한다.
- 설정: 전체·효과음·배경음·화면 밝기와 타이틀 움직임 줄이기. 실제 게임 음원과 카메라에 반영하며 다음 실행에도 유지한다. 키보드·마우스·게임패드로 메뉴를 탐색한다. 게임패드 게임 조작 전체를 추가한 것은 아니다.
- 저장: 마지막 안전한 책갈피, 능력, 아이템·친구 획득, 강화, 발견 지도와 능력 게이트 기록, 열린 지형, 처치한 주요 적을 로컬 PlayerPrefs에 보관한다. 체크포인트 변경·30초 간격·일시정지에서 타이틀 복귀 시 저장한다. 사라지는 발판·보스의 임시 가열 지형은 영구 저장하지 않는다. 일반 적은 재입장 때 되살아난다.
- Esc로 일시정지하고 설정·조작 안내·저장 후 타이틀 복귀를 제공한다. 메뉴 중 월드·전투·행동 입력을 정지한다. 재개 입력은 게임 공격이나 점프로 전달하지 않는다.
- 웹 저장은 현재 사이트/브라우저의 로컬 기록이다. 클라우드 저장·여러 슬롯·다른 기기 동기화는 제공하지 않는다. 기존 검토 링크의 `spawn=x,y`는 타이틀을 건너뛰고 저장을 덮어쓰지 않는 검토 모드다.
- 서체: Cormorant Garamond (SIL OFL), 기존 Noto Sans CJK KR. 새 배경은 내장 image_gen으로 제작한다. 배경과 글자를 분리하여 해상도와 한글 가독성을 유지한다.

Unity 검증: `Book/Validation/stilldrawn-title.json`. 브라우저 기능·장면 검증: `Book/Validation/stilldrawn-title-web.json`.
''';save(p,s)
p=next((root/'Docs').glob('01_*.md'));s=p.read_text(encoding='utf-8-sig');s=s.replace('**Unfin** (저장소명: Unfinished-Sketchbook, 구 가제: 미완의 스케치북)','**Stilldrawn** (저장소명: Unfinished-Sketchbook, 구 가제: Unfin / 미완의 스케치북)');s=s.replace('## 🔑 제목 장치 ✅','## 🔑 기존 가제의 제목 장치 · Stilldrawn 표기 적용 후 재검토');save(p,s)
print('STILLDRAWN_RESEARCH_30_SAVED')
