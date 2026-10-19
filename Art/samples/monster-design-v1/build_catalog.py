"""Build the review catalog; this does not modify Unity enemy placement or AI."""
import json, html
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
groups = [
 ('common','공통 · 여러 지역',0,'공통','#b9b09b'),
 ('r1','연필 숲 · 지역 전용',1,'지역 전용','#89979c'),
 ('a','구겨진 종이굴',1,'구역 전용','#b3a18c'),
 ('b','흑연 광산',1,'구역 전용','#8096aa'),
 ('c','스케치 나무 속',1,'구역 전용','#ad8a65'),
 ('d','엉킨 뿌리 심부',1,'구역 전용','#88a18a'),
 ('r2','크레파스 언덕 · 지역 전용',2,'지역 전용','#c59067'),
 ('e','노을 크레파스 굴',2,'구역 전용','#d59970'),
 ('f','밀랍 공방',2,'구역 전용','#c3ad76'),
 ('g','색칠 지하 협곡',2,'구역 전용','#9498bb'),
 ('h','기사의 탑',2,'구역 전용','#b78081'),
]
# name | appearance EN | scale | role | first placement proposal | tell/action/recovery seconds | attack | no-parry response | parry reward | placement caution
raw = {
'common': '''구겨진 종이공|Low round crumpled parchment armadillo with three large folded shell plates, tiny dark feet and a narrow ink eye slit|0.65|수평 돌진|1지역 3번 종이 비탈 / 2지역 2번 비탈|0.65/0.35/0.90|몸을 압축한 뒤 한 방향으로 구름. 벽 충돌 시 납작하게 펼쳐짐|점프로 넘기거나 뒤로 물러난 뒤 펼쳐진 몸 공격|구르는 몸은 반사 불가. 벽 충돌로 같은 빈틈 확보|낭떠러지 바로 옆 첫 배치 금지; 지역별 겉종이 색만 변경
종이 나방|Small hovering moth with two broad curled paper wings and a dark seed body, asymmetrical missing wing corner, restrained ivory and slate|0.55|공중 접근|1지역 2번 후반 / 2지역 3번 넓은 공간|0.70/0.30/0.85|날개를 접고 조준선을 고정한 뒤 짧은 대각선 급강하|조준 고정 후 한 걸음 옆으로 이동; 저공 회복 때 공격|획득 후 급강하 접촉을 튕기면 바닥에 떨어져 1초 노출|첫 등장 1마리. 발판 착지 위치를 매복하지 않음
잉크 방울|Squat ink-sac creature held in an open folded paper cup, one soft droplet nozzle and two short feet, matte blue-black core|0.70|곡사 견제|1지역 18번 덩굴 다리 / 2지역 15번 넓은 바닥|0.80/0.15/1.10|주머니가 부풀고 큰 잉크탄 한 발. 바닥 착탄 그림자 뒤 작은 웅덩이 1.5초|탄을 베거나 착탄 표시 밖으로 이동; 웅덩이를 점프로 넘음|탄 반사 시 사수 경직 1초. 웅덩이 자체는 패링 불가|다리 전체를 웅덩이가 막지 않도록 안전 폭 유지''',
'r1': '''흑연 벌레|Low elongated graphite grub with four broad smooth pencil-core plates, blunt snout and short ivory underfeet, charcoal blue gray|0.55|기초 근접|1지역 1번 시작의 백지길|0.65/0.18/0.85|머리를 들어 멈춘 뒤 몸 길이만큼 짧게 들이받음|뒤로 한 걸음 피하고 긴 회복에 공격; 대시 불필요|재방문 시 머리 타격을 튕기면 즉시 뒤집힘|시작 세이브에서 충분히 떨어진 평지에 단독 배치
연필깎이 포탑|Grounded squat wooden pencil sharpener creature with one circular muzzle and folded wooden flanks, visible chunky graphite pellet, no mechanical gun barrel|0.85|수평 탄|1지역 2번 바람 드는 종이 틈|0.75/0.12/1.30|구멍 안에 흑연이 모인 뒤 수평으로 단발 사격|점프로 넘기거나 탄을 베기; 총구 뒤 안전|재방문 탄 반사로 긴 경직. 6번→25번 과녁 문 탄 공급 역할 유지|1지역 전용으로 재분류. 기존 2지역 15번 포탑 역할은 G03으로 교체 제안
지우개 짐꾼|Hunched squat eraser beast carrying a broad curved graphite-stained shield slab on its back, short sturdy feet and a small exposed pale belly|1.00|정면 압박|1지역 5번 갈림길 굴 전투 공간|0.85/0.25/1.10|앞판을 세우고 짧게 밀친 뒤 배가 드러남. 정면 상시 무적 아님|밀치기 거리 밖에서 기다리거나 점프로 뒤를 잡고 공격|재방문 정면 밀치기 패링으로 앞판 벌어짐|통로 출구를 완전히 봉쇄하지 않음; 기존 엘리트 지우개와 크기·장비 구별''',
'a': '''접힌 입 종이벌레|Flat beige paper slug with a large folded envelope mouth, two dark eye dots and rounded accordion body, broad sparse crease patterns|0.55|짧은 물기|1지역 3번 상단 평지|0.75/0.15/0.90|봉투 입을 벌려 멈춘 뒤 앞쪽 반 몸통 거리 물기|공격 범위 밖에서 기다렸다 닫힌 입 공격|재방문 입 닫힘 타격 패링 시 경직|벽 점프 획득 전후 모두 지상 대응 가능
종이 귀뚜라미|Small long-backed folded paper cricket with broad bent hind legs and curled antennae, warm taupe paper, compact non-spiky silhouette|0.65|착지 공격|1지역 4번 풀 그림자 굴 바닥|0.80/0.45/1.00|뒷다리를 접고 낮은 포물선으로 한 번 뛰어 착지|착지 그림자 밖으로 걷고 착지 후 공격|재방문 착지 몸통 타격 패링으로 넘어진 상태|머리 위 화면 밖에서 뛰어들지 않음
책장 소라|Small snail carrying a curved rolled book-page shell with one thick page lip, charcoal soft foot and visible light face|0.80|방어와 빈틈|1지역 6번 갈림굴 다락 넓은 선반|0.90/0.20/1.20|껍질 속에 숨었다가 페이지를 한 번 펼쳐 앞쪽을 쓸음|범위를 빠져나온 뒤 노출된 얼굴 공격|재방문 페이지 쓸기 패링으로 껍질 열린 상태 유지|기존 포탑 2마리와 동시 첫 소개 금지''',
'b': '''흑연 두더지|Rounded blue-gray graphite mole with large blunt shovel forepaws, pale snout, three chunky ore plates on back|0.80|지면 예고|1지역 7번 광산 입구|0.70/0.25/1.20|이동 흙더미가 멈춘 위치에서 솟아오르고 잠깐 노출|흙더미를 옆으로 피하고 나온 뒤 공격|솟아오르는 몸은 패링 불가; 지상 앞발 타격만 가능|불투명 바닥 위에서만 잠복; 첫 등장 단독 유지
흑연 등딱지|Low wide beetle with two smooth heavy graphite ore shields, bright worn seams and six tiny sturdy feet, one front scraping plate|0.85|낮은 횡공격|1지역 8번 흑연 갱도 평지|0.85/0.25/1.15|앞판으로 바닥을 긁고 낮게 1.5몸 길이 밀어냄|점프로 넘긴 뒤 뒤쪽 열린 틈 공격|재방문 긁는 앞판 패링 시 옆으로 뒤집힘|6칸 높이 갱도에서 높은 점프를 요구하지 않음
갱등 주머니|Hanging soft graphite dust pouch with a heavy folded lantern hood and one dim amber seam, dark strap loop, broad rounded silhouette|0.70|천장 낙하|1지역 9번 무너진 갱도 안전 선반 위|0.90/0.30/1.30|고리가 흔들리고 바닥 그림자 표시 후 수직 낙하|그림자 밖 이동; 떨어진 주머니 공격|몸통 낙하는 패링 불가|무너지는 발판 첫 점프 구간과 중첩 금지; 복귀 이동은 무피해''',
'c': '''지우개 거미|Soft worn eraser abdomen with four pairs of short curved graphite legs, one visible hanging paper thread, warm wood-gray palette|0.65|수직 견제|1지역 13번 옹이 방|0.80/0.35/1.00|실을 팽팽히 당긴 뒤 정해진 높이까지 내려와 다리 쓸기|실 아래에서 비켜나고 낮아진 몸 공격|획득 후 다리 쓸기만 패링 가능; 실은 공격으로 절단|첫 배치는 넓은 바닥; 화면 밖 수직 기습 금지
옹이 도마뱀|Small bark lizard made of overlapping broad warm brown curled wood-paper plates, large rounded knot cheek and short curved tail|0.75|벽 이동|1지역 14번 나무 속 탑의 넓은 중간층|0.75/0.25/0.95|벽에 붙어 머리를 돌리고 선반으로 짧게 도약|도약 방향 고정 후 이동; 착지 중 노출|재방문 도약의 머리 타격 패링으로 선반에 쓰러짐|벽 점프 입력을 가로막는 좁은 벽에 배치 금지
연필씨 부엉이|Small squat owl shaped from curved pencil shavings, two broad leaf wings, dark round seed chest and ivory crescent face|0.80|수평 활공|1지역 17번 새 둥지|0.85/0.40/1.05|날개를 넓게 펴고 일정 높이로 한 번 활공|낮은 활공은 점프; 높은 활공은 아래로 통과|재방문 날개 타격 패링 시 착지 경직|횡단 중 플레이어 추적 회전 금지''',
'd': '''뿌리 매듭개|Low prowling root hound with broad intertwined sage-gray paper root bands, rounded hunched shoulders and one dark eye slit|0.90|추격 후 물기|1지역 18번 덩굴 다리의 넓은 끝부분|0.70/0.20/0.90|일정 거리까지 접근 후 멈추고 목을 낮춰 한 번 물기|멈춤을 보고 점프 또는 후퇴, 물기 종료 후 공격|재방문 물기 패링 시 매듭이 느슨해져 경직|첫 잉크 방울 튜토리얼과 구분된 공간에서 소개
씨주머니 보행자|Pear-shaped root seed pod walker with three broad folded green-gray husk petals, short root feet and a dark round central seed|0.85|시간차 탄|1지역 19번 위쪽 갈림의 평지|0.90/0.15/1.20|껍질이 열리고 땅에 닿아 한 번 튀는 씨앗 하나 발사|첫 탄을 베거나 튀는 높이 아래로 이동|재방문 씨앗 반사로 껍질 열림 1.2초|탄은 한 개만 유지; 좁은 필수 착지점 겨냥 금지
덩굴 손아귀|Stationary stump creature with one thick curled paper vine arm ending in three rounded gripping lobes, pale cut wood center|1.10|영역 쓸기|1지역 23번 뿌리 내리막|0.95/0.30/1.30|손을 뒤로 말고 지면 가까이 한 번 쓸어냄|점프로 손을 넘거나 범위 밖 후 공격|재방문 손끝 타격 패링 시 팔이 뒤로 접힘|연필 깎기로 제거하는 기믹 덩굴과 실루엣 구분; 잡기 QTE 없음''',
'r2': '''크레용 병정|Small heavy crayon soldier with blunt rounded orange wax helmet, short thick legs and one dark crayon-tipped short spear, worn paper collar|1.00|두 번 찌르기|2지역 2번 첫 크레파스 비탈|0.70/0.46/1.05|창을 당기고 2회 찌름. 두 타 사이 0.28초, 두 번째까지 방향 고정|뒤로 거리를 벌리거나 등 뒤로 이동해 두 번째 뒤 공격|습득 후 두 타 모두 패링하면 1.2초 그로기; 한 번만 성공해도 해당 타격 방어|0.46초 공격 구간은 두 타와 사이 텀 포함; 첫 소개에 다른 원거리 금지
밀랍 투척꾼|Squat ochre wax creature with a broad ladle-like forearm and a rounded candle-pot belly carrying one large amber wax ball|0.95|곡사 반사|2지역 3번 노을빛 큰 굴|0.85/0.15/1.20|팔 위 밀랍을 크게 들고 한 덩이 투척; 착탄에 둔화 웅덩이|몸을 이동해 착탄 회피; 근접 중 긴 회복 공격|탄 반사로 경직과 색 막대 장치 가동. 14번 장치용 개체 별도 관리|장치용은 다리 작동 전 처치 불가 표시+재공급 보장; 일반 개체는 정상 처치
색가루 박쥐|Low-saturation mauve bat with thick scalloped wax-paper wings and a small heavy crayon chip torso, two soft ear fins|0.60|대각 급강하|2지역 3번 넓은 천장 공간|0.80/0.28/1.00|가루가 모이고 목표 방향 고정 후 짧은 급강하, 낮게 복귀|예고 후 옆으로 이동; 복귀 중 공격|습득 후 급강하 패링 시 바닥에 눕고 1초 노출|공통 나방보다 느린 대기·빠른 직선, 동시 배치 금지로 역할 중복 완화''',
'e': '''노을 등짐충|Broad rust-orange wax beetle carrying three rounded sunset crayon slabs on its back with small charcoal feet and pale belly|0.90|무거운 돌진|2지역 2번 비탈의 넓은 다음 구간|0.90/0.35/1.15|등짐을 앞으로 기울이고 비탈 따라 짧게 돌진|점프로 넘기고 멈춘 뒤 배 공격|획득 후 정면 돌진 패링으로 등짐 흔들림|종이공과 다른 평지에서 소개; 비탈 아래 안전 공간 확보
색띠 달팽이|Wide low snail with a rolled orange-peach crayon ribbon shell, matte layered color bands and a dark soft body|0.75|국소 바닥 봉쇄|2지역 3번 큰 굴 가장자리|0.90/0.20/1.25|몸을 들어 뒤쪽에 짧은 밀랍 띠 하나 흘림, 1.5초 후 소멸|앞쪽으로 돌아 접근하거나 띠를 점프로 넘음|바닥 띠는 패링 불가; 전방 머리 밀기만 가능|길 전체 봉쇄 금지; 달팽이 사망 즉시 띠 제거
바람개비 새|Small asymmetric pinwheel bird with three broad rounded folded wax-paper wing vanes, muted terracotta and cream, tiny dark beak|0.65|통로 가로지르기|2지역 11번 바람개비 환기굴의 출발 선반|0.85/0.40/1.10|날개가 한 번 접힌 뒤 수평으로 고정 경로 횡단|출발 선반에서 기다리거나 지나간 뒤 이동|패링 가능하지만 성공 없이 징검다리 통과 가능|녹는 발판 위에서 정지 대기를 강요하지 않음''',
'f': '''심지 꼬마|Squat soot-dark wax worker creature with a rounded candle hood, one short unlit curled wick and broad oven-mitt hands, muted honey edges|0.75|근접 내려치기|2지역 6번 공방 입구 전투 공간|0.80/0.20/1.00|두 손을 들어 앞쪽 지면을 한 번 두드림|한 걸음 빠진 뒤 손이 바닥에 붙은 동안 공격|습득 후 손 타격 패링 시 굳은 손 부서져 긴 경직|세이브 안전 구역 밖; 화염 지속 피해 없음
주걱 밀랍게|Low sideways wax crab with two unequal broad spatula claws, flattened amber shell and four short curved legs|0.80|방향 읽기|2지역 7번 밀랍 저장고|0.90/0.25/1.15|큰 주걱을 들어 그쪽으로 한 번 쓸기, 반대 집게는 공격 안 함|작은 집게 쪽으로 돌아가 공격|습득 후 큰 집게 패링으로 옆구리 노출|전방 영구 방패 없음; 패링 없을 때도 등 뒤 접근 가능
도가니 배불뚝이|Round soot-stained wax crucible creature with a wide dark open rim, short sturdy feet and two broad cream drip lobes, no flame|1.10|안전지대 읽기|2지역 8번 지워진 그림 창고 전투 입구|1.00/0.20/1.40|몸이 부풀고 양옆 짧은 거리로 밀랍 두 덩이 분출, 바닥 표시|몸 가까운 중앙 또는 바깥으로 이동 후 공격|습득 후 개별 탄 반사 가능; 폭발 자체 패링 불가|선택 방용. 패링 획득 전 전투, 좁은 보상 자리에는 배치하지 않음''',
'g': '''지층 도약충|Tall folded violet-gray layered crayon grasshopper with two broad bent legs and blunt layered head, slate blue edges|0.80|높낮이 도약|2지역 12번 협곡 중턱 넓은 선반|0.80/0.40/1.10|목표 선반을 보고 다리를 접어 한 번 건너뛰기|목표 그림자 밖으로 이동; 착지 경직 공략|패링으로 착지 경직 연장 가능하지만 회피만으로 처치 가능|공방 전 진입 가능. 패링 보유 여부로 공격 속도 바꾸지 않음
덧칠 방패벌레|Wide short wax beetle carrying two overlapping blue-violet paint slabs like an open clamshell shield, visible soft orange flank|0.95|가드 해제|2지역 17번 광맥의 넓은 시작 바닥|0.95/0.25/1.25|앞판을 닫고 짧게 밀친 뒤 과하게 벌어짐|밀치기를 피하고 열린 옆구리 공격; 무리한 정면 연타 유도 안 함|패링 시 앞판 즉시 벌어져 1.4초 빈틈|늪 위 발판에는 배치하지 않음; 패링 필수 장갑 금지
안료 사수|Stationary rounded blue-gray paint jar creature with a single horizontal rolled-paper nozzle and visible golden bead at muzzle, short wax feet|0.85|정밀 반사 탄|2지역 15번 협곡 바닥 과녁 장치 앞|0.90/0.12/1.40|총구 안에 큰 안료 구슬이 모여 수평 단발 발사|점프로 넘기거나 탄을 베고 뒤에서 공격|15→16 과녁 문은 반사 필수. 일반 전투는 반사 없이 처치|기존 포탑/잉크 사수 명칭 충돌을 이 종으로 통일 제안. 장치 작동 전 보호 표식·탄 재공급''',
'h': '''무딘 창 시종|Short rose-gray wax tower sentry with a rounded broken helmet and one broad stubby lance, small dark visor and heavy paper sash|1.05|지연 단발|2지역 21번 탑 옆 틈새 접근 선반|0.95/0.18/1.20|창을 비스듬히 들고 잠깐 멈춘 뒤 수평 단발 찌르기|멈춤을 보고 거리 확보; 찌르기 뒤 긴 회복 공격|단발 패링으로 창이 바닥에 박혀 1.4초 노출|보스 뒤 추가 도전으로 도입. 안전 방 19·22와 보스 방 20에 일반 몬스터 추가 안 함
밀랍 방패 인형|Compact burgundy-gray wax puppet with one large rounded rectangular paper shield and tiny feet, asymmetrical open right shoulder|0.90|방패 반격|2지역 크레파스 탑 넓은 중간층|0.80/0.22/1.10|방패를 두드려 예고 후 짧은 밀치기; 연타 자동 반격 없음|등 뒤 이동하거나 밀치기 종료 노출 때 공격|패링 시 방패가 열려 큰 빈틈; 비패링도 같은 약점 공략 가능|벽타기 중인 플레이어를 밀랍 벽에서 떼는 매복 금지
봉인 리본 망령|Floating folded rose-gray wax-paper seal with a broad oval stamped head and two short heavy curled ribbon tails, no humanoid skeleton|0.80|시간차 횡탄|2지역 크레파스 탑 상단 넓은 선반|0.95/0.40/1.20|리본을 벌리고 느린 봉인 탄 둘을 0.25초 차이로 같은 높이에 발사|같은 점프로 두 탄을 넘기거나 한 발씩 베기|두 탄 반사 시 빠른 처치; 첫 반사만으로 경직해 두 번째 발사 취소|22번 세이브 내부 제외. 좁은 벽타기 통로에 탄막 금지'''
}

monsters=[]
for key,title,region,scope,color in groups:
    for index,line in enumerate(raw[key].splitlines(),1):
        cells=line.split('|')
        assert len(cells)==10, (key,cells)
        name,appearance,size,role,first,timing,attack,dodge,parry,caution=cells
        monsters.append(dict(id=f'{key.upper()}{index:02}',group=key,name=name,region=region,scope=scope,zone=title,color=color,appearance=appearance,height=float(size),role=role,first=first,timing=[float(v) for v in timing.split('/')],attack=attack,response=dodge,parry=parry,caution=caution,image=f'{key}.png',panel=index))

policy='''# 일반 몬스터 33종 — 1·2지역 설계 시안 v1

2026-10-18 · 외형/전투 기획 시안. Unity AI, 애니메이션, 공격 판정, 기존 배치는 아직 변경하지 않음.

## 범위와 진행 기준

- 공통 3종 + 1지역 전용 3종 + 1지역 4구역×3종 + 2지역 전용 3종 + 2지역 4구역×3종 = 고유 33종.
- 한 지역의 후보는 18종, 한 구역의 후보는 공통3+지역3+구역3=9종. 모든 방에 9종을 넣지 않는다. 한 전투 공간은 보통 1~2종, 학습 이후 최대 3역할로 제한한다.
- 기존 06 문서의 1지역 7종/52마리와 월드 문서의 지역당 6~12종은 이전 배치 기준이다. 이번 확장안은 신규 제작 후보 수이며 배치 수를 늘린다는 뜻이 아니다.
- **패링은 2지역 10번 ‘거북의 굴’의 깎인 크레파스 거북을 처치한 뒤 획득.** 거북 자체는 패링 없이 정면 공격을 피하고 뒤를 공격해 이겨야 한다.
- 1지역 첫 방문: 걷기·점프·공격으로 대응. 벽 점프는 2번 서쪽에서 획득. 대시는 1지역 보스 뒤 획득하므로 초반 대응법에 요구하지 않는다.
- 2지역 E·F는 패링 전 학습. G의 12·15·17·18번도 패링 전에 방문 가능하므로 전투를 패링으로 막지 않는다. 14번 다리와 16번 과녁 문은 패링 기믹 관문으로 유지.
- H는 물감 문과 보스 진행 이후. 19·22번 안전실과 20번 보스 아레나는 일반 적 추가 금지. 21번/탑 중간층의 선택 전투로 전용종을 소개한다.
- 지역 전용은 해당 지역에만, 구역 전용은 해당 구역에만 등장한다. 공통종의 지역별 재질 변형은 별도 종으로 세지 않고 공격 타이밍·실루엣을 유지한다.

## 패링과 공격 가독성

일반 전투의 패링 필수 의존도는 모두 0. 획득 뒤에는 경직·반사·가드 해제 보상을 제공한다. 장치용 탄의 반사는 전투와 별개로 필수이며, 장치용 사수는 작동 전 보호 표식과 탄 재공급을 보장한다. 해금 뒤 10번 출구의 넓은 공간에서 기존 밀랍 투척꾼 1마리로 선택 연습을 제공하고, 피하기/근접 처치도 가능하게 한다. 장치 연습은 14번에서 진행한다.

반사 가능한 탄은 밝은 중심+둥근 테두리와 선명한 소리, 타격 패링은 무기 끝의 짧은 섬광, 지속 웅덩이/낙하 환경 공격은 바닥 면 표시로 구별한다. 색만으로 판정하지 않는다. 화면 밖 공격 금지, 예고 후 추적 방향 고정, 첫 새 종은 단독 소개. 공격 예고·실행·회복 수치는 초 단위 제안이며 플레이 테스트 전 확정 밸런스가 아니다. 패링 입력 허용 시간은 별도 시스템 조정 대상으로 이 문서에서 고정하지 않는다.

패링 없이 보통 2~4회 안전한 공격 기회로 처치 가능하도록 체력 조정. 패링 보상은 약 1회의 공격 기회를 추가하는 수준부터 검증한다. 보스급 영구 가드, 패링 실패 즉사, 피할 수 없는 복수 탄막은 일반종에 적용하지 않는다.

## 외형·애니메이션 공통 규격

부드러운 외곽과 큰 재료 면, 절제된 종이/흑연/밀랍 질감. 미세한 털·선·가시 반복과 실사 광택을 피한다. 주인공의 밝은 아이보리보다 어둡게, 배경보다 외곽은 읽히게 한다. 위험한 부위 한 곳만 강조한다. 각 종의 형태가 공격 범위를 설명해야 한다. 컨셉 시트의 옆모습/약한 3/4 시점은 외형 검토용이며 실제 스프라이트는 정측면으로 다시 제작한다.

그림은 종류별 디자인 원화이며 런타임 스프라이트 시트가 아니다. 투명 배경 분리, 정측면 정렬, 피벗·실제 크기·프레임 연결·판정 검증은 후속 단계다. 카드의 키는 플레이어 직립 높이 대비 제안이며 원화 시트의 그림 크기와는 별개다.

제작 시 기본 상태: 대기 4~6프레임, 이동 6~8프레임, 예고 3프레임, 타격 1~2프레임, 회복 3~4프레임, 피격/사망 및 패링 경직. 프레임별 지속 시간을 달리해 예고는 읽히고 실제 공격은 빠르게 한다. 숫자는 작업량 가이드이며 고정 FPS 요구가 아니다.

## 도입과 배치

새 전용종은 기존 몬스터 슬롯을 교체하며 소개한다. 1지역 18종을 짧은 첫 방문에 모두 강제 학습시키지 않는다. 선택 방/재방문으로 분산하고 세이브·보상방 안전 공간은 유지한다. 아래 ‘첫 배치’는 제안 지점이며 방 크기/충돌/진행 검사 후 확정한다. 낭떠러지·무너지는 발판과 새 공격을 동시에 처음 소개하지 않는다.

1지역: 흑연 벌레 → 종이공/나방/포탑 → 선택 종이굴 적 → 두더지 → 광산 전용 적 → 나무 전용 적 → 잉크 방울/뿌리 전용 적 순서. 2지역: 병정 → 투척꾼 → 공방 지상 적 → 거북(패링 해금) → 선택 반사 연습 → 협곡 반사 장치 → 보스/벽타기 → 탑 전용 적.

1지역 전용 포탑을 2지역에 두는 기존 데이터와는 충돌한다. 2지역 15번의 과녁 탄 공급 역할을 안료 사수 G03으로 교체하는 안을 제안하며, 기존 문서의 ‘잉크 사수/연필깎이 포탑’ 명칭도 그때 함께 통일한다. 기존 지우개 거미·흑연 두더지는 이제 각각 나무/광산 전용 후보로 옮긴다. 다른 구역 기존 배치는 AI 적용 시 교체 검토하며 현재 게임은 변경하지 않는다.

## 33종 상세
'''
for g,title,region,scope,color in groups:
    policy+=f'\n### {title} — {scope}\n\n![{title}](../Art/samples/monster-design-v1/{g}.png)\n'
    for m in [x for x in monsters if x['group']==g]:
        policy+=f"\n#### {m['id']} {m['name']}\n\n- 역할 / 크기: {m['role']} / 플레이어 키 ×{m['height']}.\n- 도입 제안: {m['first']}.\n- 공격: {m['attack']}.\n- 예고 / 실행 / 회복: {' / '.join(map(str,m['timing']))}초.\n- 패링 없이: {m['response']}.\n- 패링 획득 후: {m['parry']}.\n- 배치 주의: {m['caution']}.\n"
policy+='\n## 근거 문서\n\n- [지역 구성](03_월드_5지역.md)\n- [1지역 방 구조](04_1지역_연필숲_맵.md)\n- [기존 몬스터](06_적_몬스터.md)\n- [능력 해금](10_능력_성장.md)\n- [2지역 방 구조·패링 관문](13_2지역_크레파스언덕_맵.md)\n- [아트 방향](16_아트_컨셉.md)\n'
(ROOT/'Docs/22_일반_몬스터_설계.md').write_text(policy,encoding='utf-8')
(HERE/'design.json').write_text(json.dumps({'status':'concept_not_implemented','count':33,'groups':[dict(id=a,name=b,region=c,scope=d,color=e) for a,b,c,d,e in groups],'monsters':monsters},ensure_ascii=False,indent=2),encoding='utf-8')
base='''Create a professional 2D metroidvania enemy concept lineup, a single wide landscape image with exactly THREE separate full-body creatures in three equally sized columns. Use the supplied game screenshot only as a material and softness/style reference, not an edit target. Clean orthographic side or very slight three-quarter side view facing right, readable silhouettes, restrained matte paper/wax/graphite material, broad smooth rounded forms, gentle painted shadows, large sparse surface patterns, medium detail, mysterious mature tone. NOT photorealistic, NOT pixel art, NOT cute glossy toys, no tiny hatching or spiky fur, no borrowed franchise characters. Neutral dark desaturated slate background, subtle soft contact shadow, generous space around each creature; no scenery, no overlapping creatures, no UI. Give each equal visual attention. Only text is the exact short ID underneath each creature. Visual designs must express the specified attack appendage; do not add weapons that contradict it. This is an original design concept sheet, not animation frames. Palette: PALETTE. Left to right:\n'''
prompts=[]
for g,title,region,scope,color in groups:
    entries=[m for m in monsters if m['group']==g]
    prompt=base.replace('PALETTE',f'{title}, primary muted {color}, darker ink shadows and restrained warm highlights')+'\n'.join(f"{m['id']}: {m['appearance']}." for m in entries)
    prompts.append(dict(group=g,prompt=prompt,mode='built-in image_gen',reference='Book/Validation/wall-hops-wall-hops-start.png',output=f'{g}.png'))
(HERE/'prompts.json').write_text(json.dumps(prompts,ensure_ascii=False,indent=2),encoding='utf-8')

cards=''
for g,title,region,scope,color in groups:
    cards+=f'<section data-region="{region}" data-scope="{scope}"><h2>{html.escape(title)} <small>{scope}</small></h2><a href="{g}.png" target="_blank"><img class="sheet" src="{g}.png" alt="{html.escape(title)} 3종 외형 시안" loading="lazy"></a><div class="cards">'
    for m in [x for x in monsters if x['group']==g]:
        cards+=f'<article style="--accent:{color}"><span class="id">{m["id"]} · {m["role"]}</span><h3>{m["name"]}</h3><p class="muted">키 ×{m["height"]} · {m["first"]}</p>'
        for label,value in [('공격',m['attack']),('패링 없이',m['response']),('획득 후 패링',m['parry']),('배치 조건',m['caution'])]:
            cards+=f'<p><b>{label}</b>{html.escape(value)}</p>'
        cards+=f'<p class="timing">예고 {m["timing"][0]}s → 실행 {m["timing"][1]}s → 회복 {m["timing"][2]}s</p></article>'
    cards+='</div></section>'
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>일반 몬스터 33종 · 디자인 v1</title><style>
*{box-sizing:border-box}body{margin:0;background:#151719;color:#ddd9ce;font:16px/1.75 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:48px 28px}h1{font-size:38px;line-height:1.25}h2{margin-top:55px;font-size:27px}h3{font-size:23px;margin:6px 0 14px}small,.muted{color:#aaa79f;font-size:14px}.intro{max-width:1000px}.notice{border-left:3px solid #c6aa76;padding:12px 20px;background:#24272a;margin:22px 0}.controls{position:sticky;top:0;z-index:2;padding:14px 0;background:#151719ed;display:flex;gap:12px;flex-wrap:wrap}select,a.button{padding:10px;background:#2b3033;color:#eee;border:1px solid #555;border-radius:5px;font:inherit}a{color:#d5bd91}.sheet{width:100%;display:block;border-radius:9px;border:1px solid #444}.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:18px;margin-top:20px}article{background:#202326;border-top:3px solid var(--accent);padding:22px;border-radius:6px}.id{color:var(--accent);font-size:14px;letter-spacing:.05em}b{display:block;color:#eee5d7;font-size:14px}p{margin:12px 0}.timing{font-size:13px;color:#beb8a9;border-top:1px solid #414345;padding-top:12px}section[hidden]{display:none}footer{margin-top:48px;color:#aaa;font-size:14px}@media(max-width:800px){.cards{grid-template-columns:1fr}main{padding:24px 16px}h1{font-size:29px}}</style><main>
<header><p>UNFINISHED SKETCHBOOK / ENEMY DESIGN 01</p><h1>일반 몬스터 33종</h1><div class="intro"><p>공통 3종 · 지역 전용 6종 · 구역 전용 24종. 그림은 각 묶음의 왼쪽부터 카드 순서와 같습니다. 클릭하면 원본을 엽니다.</p><div class="notice"><b>기획·외형 시안 — Unity 미적용</b>패링은 2지역 10번 거북의 굴 클리어 후 해금됩니다. 모든 일반 적은 패링 없이 처치 가능하며, 14번 다리·16번 과녁 문만 반사 장치 조건을 유지합니다. 협곡 일부는 패링 전에도 진입할 수 있습니다.</div><p>각 구역의 후보 9종을 한꺼번에 배치하지 않습니다. 첫 소개는 단독, 같은 전투 공간은 1~2종부터. 수치는 제안이며 실제 플레이 검증 전입니다. 원화 크기는 실제 키 비율이 아닙니다.</p></div></header>
<div class="controls"><select id="region" aria-label="지역"><option value="all">전체 지역</option><option value="1">1지역 + 공통</option><option value="2">2지역 + 공통</option></select><select id="scope" aria-label="종류"><option value="all">전체 종류</option><option>공통</option><option>지역 전용</option><option>구역 전용</option></select><a class="button" href="design.json">설계 데이터</a><a class="button" href="prompts.json">생성 프롬프트</a></div>
'''+cards+'''<footer>기준: Docs/03·04·06·10·13·16 / 상세 설계: Docs/22_일반_몬스터_설계.md<br>이미지: built-in image_gen · 애니메이션/투명 스프라이트/AI/공격 판정은 후속 제작.</footer></main><script>const r=document.querySelector('#region'),s=document.querySelector('#scope');function filter(){document.querySelectorAll('section').forEach(x=>x.hidden=!((r.value==='all'||x.dataset.region==='0'||x.dataset.region===r.value)&&(s.value==='all'||s.value===x.dataset.scope)))}r.onchange=s.onchange=filter;</script></html>'''
(HERE/'preview.html').write_text(page,encoding='utf-8')
assert len(monsters)==33 and len({m['id'] for m in monsters})==33
print('Created design.json, prompts.json, preview.html and Docs/22: 33 unique designs in 11 groups.')
