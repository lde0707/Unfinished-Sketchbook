"""Docs/20_배경_제작_파이프라인.md 만들기 — 표는 Art/asset_catalog.json · Art/bg_unity_manifest.json · Art/bg_links.json 에서 자동
python Tools/build_doc20.py   (build_prompts.py → bg_manifest.py build 다음에)
"""
import json, os
from collections import defaultdict

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(BASE, 'Art')
OUT = os.path.join(BASE, 'Docs', '20_배경_제작_파이프라인.md')

cat = {a['id']: a for a in json.load(open(os.path.join(ART, 'asset_catalog.json'), encoding='utf-8'))['assets']}
man = json.load(open(os.path.join(ART, 'bg_unity_manifest.json'), encoding='utf-8'))
links = json.load(open(os.path.join(ART, 'bg_links.json'), encoding='utf-8'))['links']
areas = json.load(open(os.path.join(ART, 'bg_areas.json'), encoding='utf-8'))
PPU = man['units']['ppu']

ZONE = dict(A=('구겨진 종이굴', '#C2C1B9'), B=('흑연 광산', '#B8B8BC'), C=('스케치 나무 속', '#B6BDB6'), D=('엉킨 뿌리 심부', '#C0B8B6'),
            E=('노을 크레파스 굴', '#C5BCB1'), F=('밀랍 공방', '#C0BAB3'), G=('색칠 지하 협곡', '#BABBC2'), H=('기사의 탑', '#C2B8B5'))
ZCH = dict(A=1, B=1, C=1, D=1, E=2, F=2, G=2, H=2)
ICON = dict(hang='⬇️', rise='⬆️', span='↕️', wall='🧱')
TYPE_KO = dict(BG_WALL='구획 뒷벽 (A·B·C)', BG_WALL_TALL='세로 전용 뒷벽', BG_WALL_LINK='구획 이음 (뒷벽)', BG_SET='세트 조각 (대표·공용)',
               BG_FORE='전경 테두리', TERRAIN='지형 속 채움 (+ 골든)', EDGE='지형 가장자리 띠 (+ 띠 이음)', TERRAIN_LINK='구역 지형 이음 (속)',
               CORNER='지형 모서리', PLATFORM='발판', SPECIAL='특수 지형', DECOR='방 장식 (투명 오버레이)')
LAYER = dict(BG_WALL='L1', BG_WALL_TALL='L1', BG_WALL_LINK='L1', BG_SET='L2', BG_FORE='L4', TERRAIN='L3', EDGE='L3', TERRAIN_LINK='L3',
             CORNER='L3', PLATFORM='L3', SPECIAL='L3', DECOR='L3')
HOW = dict(BG_WALL='A = 타일링 옵션으로 생성 → B·C = `variant-base` + 안쪽 인페인팅', BG_WALL_TALL='`tall-base` 밑그림 → 1024 조각씩 아래→위 인페인팅',
           BG_WALL_LINK='`link-base` 밑그림 → 가운데만 인페인팅', BG_SET='낱개 투명 PNG (↕️ 조각은 세로 타일링)', BG_FORE='가로 타일링 옵션, 가운데 투명',
           TERRAIN='fill_a = 타일링 → b·c = `variant-base`', EDGE='3:2로 뽑아 가운데 4:1 띠 → `prep --seamless`', TERRAIN_LINK='`link-base` 두 구역 fill_a',
           CORNER='투명 낱개', PLATFORM='투명 낱개', SPECIAL='투명 낱개', DECOR='ControlNet(방 윤곽) 0.4~0.6, 투명')


TERRAIN_PPU = 256
TERRAIN_TYPES = {'TERRAIN', 'EDGE', 'TERRAIN_LINK', 'CORNER', 'PLATFORM', 'SPECIAL'}


def units(final, ppu=PPU):
    w, h = map(int, final.replace('×', 'x').split('x'))
    return f'{round(w / ppu, 2):g} × {round(h / ppu, 2):g}'


def size_table():
    rows = ['| 층 | 그림 | 개수 | ① AI에 요청 (비율 · 보통 크기) | ② 최종 PNG | ③ 유니티 크기 (유닛) | 만드는 법 |', '|---|---|---|---|---|---|---|']
    for t in ['BG_WALL', 'BG_WALL_TALL', 'BG_WALL_LINK', 'BG_SET', 'BG_FORE', 'TERRAIN', 'EDGE', 'TERRAIN_LINK', 'CORNER', 'PLATFORM', 'SPECIAL', 'DECOR']:
        xs = [a for a in cat.values() if a['type'] == t]
        if not xs: continue
        combos = sorted({(a['gen_ar'], a['gen_size'], a['final']) for a in xs}, key=lambda c: -sum(1 for a in xs if (a['gen_ar'], a['gen_size'], a['final']) == c))
        gen = '<br>'.join(f"{g} · {s}" for g, s, _ in combos); fin = '<br>'.join(f for _, _, f in combos); uu = '<br>'.join(units(f, TERRAIN_PPU if t in TERRAIN_TYPES else PPU) for _, _, f in combos)
        rows.append(f"| {LAYER[t]} | {TYPE_KO[t]} | {len(xs)} | {gen} | **{fin}** | {uu} | {HOW[t]} |")
    return rows


def zone_tables():
    by_area = defaultdict(list)
    for r in man['rooms']: by_area[r['area']].append(r)
    md = []
    for z, (zname, tint) in ZONE.items():
        ch = ZCH[z]
        shared = sorted(i for i in cat if i.startswith(f'B{ch}-{z}-set_'))
        tz = sorted(i for i in cat if i.startswith(f'TZ{ch}-{z}-'))
        md += ['', f'#### {z} · {zname} ({ch}장) · 색 기운 `{tint}`', '',
               f"- 공용 세트 조각 5: " + ' · '.join(f"`{i}` {ICON.get(_anc(i), '')}" for i in shared),
               f"- 지형 (구역 전용): " + ' · '.join(f'`{i}`' for i in tz), '',
               '| 구획 | 이름 | 뒷벽 (변형 3) | 방 · 수직굴 (화면 수 가로×세로) | 대표 조각 | 세로 뒷벽 |', '|---|---|---|---|---|---|']
        for aid, a in areas.items():
            if a['zone'] != z: continue
            rs = sorted(by_area.get(aid, []), key=lambda r: (r['kind'] != 'room', r['id']))
            sp = '<br>'.join(f"`{r['id']}` {r['name']} ({r['screens'][0]}×{r['screens'][1]})" for r in rs)
            land = ' '.join(f"`{x['id']}`{ICON.get(x['anchor'], '')}" for r in rs for x in r['layers']['set']['landmarks'])
            tall = ' '.join(f"`{r['layers']['wall']['tall']}` → `{r['id']}`" for r in rs if r['layers']['wall']['tall'])
            md.append(f"| **{aid}** | {a['name']} | `B{ch}-{aid}-wall_a` (테두리 원본) · `_b` · `_c` | {sp} | {land or '—'} | {tall or '—'} |")
    return md


def _anc(i):
    n = cat[i]['note']
    return 'hang' if '⬇️' in n else 'rise' if '⬆️' in n else 'span' if '↕️' in n else 'wall' if '🧱' in n else ''


def link_table():
    place = defaultdict(list)
    for r in man['rooms']:
        for l in r['links']:
            if l['wall_link']: place[l['wall_link']].append(f"`{r['id']}` {l['side']}")
    rows = ['| 구획 이음 | 방향 | 앞 → 뒤 | 들어가는 방 (변) | 지형 이음 (구역이 바뀔 때) |', '|---|---|---|---|---|']
    for l in links:
        wid = f"BL-{l['first']}{l['second']}-wall"
        tl = sorted({t for r in man['rooms'] for x in r['links'] if x['wall_link'] == wid for t in x['terrain_links']})
        rows.append(f"| `{wid}`{' ⭐ 장 경계' if l['chapter_border'] else ''} | {'가로 (왼→오)' if l['dir'] == 'h' else '세로 (위→아래)'} | "
                    f"{l['first']} {l['first_name']} → {l['second']} {l['second_name']} | {' · '.join(place[wid]) or '—'} | {' '.join(f'`{t}`' for t in tl) or '같은 구역'} |")
    return rows


nroom = sum(r['kind'] == 'room' for r in man['rooms']); nshaft = len(man['rooms']) - nroom
ntile = sum(len(r['layers']['wall']['tiles']) for r in man['rooms'])
cnt = lambda t: sum(1 for a in cat.values() if a['type'] == t)

md = f"""# 20. 배경 제작 파이프라인 — 해상도 → 제작 → 연결 → 유니티 → 검증 🧱

> 배경(뒷벽·세트 조각·전경)과 그 앞의 지형을 **한 줄로** 만드는 법. 프롬프트 문장은 [17](17_AI_아트_프롬프트.md) · 에셋별 완성 프롬프트는 [19](19_에셋_목록_프롬프트.md) / `Art/ALL_PROMPTS.md`, 이 문서는 **크기·순서·이어 붙이기·유니티 배치·검사**만 다뤄.
> 표는 `python Tools/build_doc20.py`로 자동 생성 (손으로 고치지 말고 `build_prompts.py` → 이 스크립트).
> 그림체: **분위기 중심의 선명한 2D 일러스트 · 강한 실루엣 · 깊은 공간** — MSB v5 / UNIFY v5. 할로우 나이트를 분위기 레퍼런스로 삼되 Unfin의 화구 세계를 표현한다.
> **v5 적용 상태:** 아래 자동화의 평균 밝기 검사·종이색 후처리는 이전 v3/v4 기준이다. 새 골든 장면에 맞게 프로파일을 재조정한 뒤 자동 채택을 사용한다. 현재 문서/프롬프트 개정만으로 검사 도구의 스타일 기준이 바뀐 것은 아니다.

---

## 0. 한눈에 🗺️

```
  [1 크기]          [2 만들기]                          [3 잇기]                 [4 유니티]                    [5 검증]
 AI 요청 크기  →  뽑기 → prep(자르고 키우기)  →  A 테두리 원본 → B·C·세로·이음  →  bg_manifest.py build  →  art_check → seam_check
 (1024~1536)       → unify_pass(색·명도)             (공통 테두리 띠 10%)         → BgRoomBuilder로 자동 배치      → bg_manifest check/preview
                                                                                                                    → 유니티 화면 검수 → approve
```

| 숫자 | 값 | 어디서 |
|---|---|---|
| 배경 구획 | **22개** (구역 8 × 2~3) · 구획마다 뒷벽 3장 = {cnt('BG_WALL')}장 | §4 |
| 구획 이음 | **{cnt('BG_WALL_LINK')}장** (지도에서 실제로 맞닿은 쌍만) | §3 · §6 |
| 세로 전용 뒷벽 | **{cnt('BG_WALL_TALL')}장** (1.5화면 이상 높은 곳) | §4 |
| 세트 조각 | **{cnt('BG_SET')}개** (방 대표 + 높은 방 추가 + 구역 공용 40) | §4 |
| 지형 | 속 채움·띠 {cnt('TERRAIN') + cnt('EDGE')} · 구역 이음 {cnt('TERRAIN_LINK')} | §6-3 |
| 방 · 수직굴 | **{nroom} + {nshaft}** · 뒷벽 칸 {ntile}개 자동 배치 | `Art/bg_unity_manifest.json` |

---

## 1. 해상도 📐

### 1-1. 기준 숫자

> [21 고해상도 타일맵 규격](21_고해상도_타일맵_규격.md)이 공통 기준이다. 지형·캐릭터 PPU 256, 기존 큰 배경·방 전체 장식 PPU 128을 구분한다. 표의 큰 지형 원본은 최종 256px 셀로 잘라 쓰거나 여러 칸을 덮는 그림으로 사용한다.

| 항목 | 값 | 설명 |
|---|---|---|
| 지도 1칸 | **유니티 1유닛** = HTML 16 논리 단위 (이미지 픽셀 아님) | 프로토타입 지도 좌표 그대로 |
| 화면 | 시야 **30×16.875유닛** → 1080p·1440p·4K 직접 렌더링 | 카메라 Orthographic Size **8.4375** |
| 지형·캐릭터 | **256px/유닛, PPU 256** · 단일 타일 256×256px | 1080p에서 1/4, 4K에서 1/2로 축소 |
| 큰 배경·방 오버레이 | **PPU 128** 유지 | 지형 타일과 별도 크기; 2배 고밀도 교체 시 이미지와 PPU를 함께 2배 |
| 뒷벽 한 장 | 2048² = **16 × 16 유닛** (화면 반 장 조금 넘음) | 뒷벽은 0.2배로 움직여서 한 방에 보통 2~6장 |
| 세로 뒷벽 | 2048×4096 = **16 × 32 유닛** | 2.7화면 높이 방도 위아래는 뒷벽 A로 이어 채움 |
| 방 장식 | 3840×2160 = **30 × 16.875 유닛 = 화면 한 장** | |

### 1-2. 종류별 원본 크기 (AI 요청 → 최종 원본 → 유니티)

> L3 지형 원본은 PPU 256, 나머지 큰 배경과 방 장식은 PPU 128로 환산한다. 모서리·발판·특수 원본의 표기 크기는 자르기 전 전체 캔버스다. 실제 배치 칸 수에 맞게 자르고 256px/유닛으로 내보내며, 원본 전체를 무조건 한 칸에 축소하지 않는다.

> 프로젝트 기본 생성 예시는 **한 변 약 1024~1536px · 비율 1:1 / 3:2 / 2:3 / 16:9**다. 실제 지원 크기는 도구별로 확인하고, 최종 크기로 직접 생성할 수 있으면 불필요한 업스케일을 생략한다.
> 자르기·키우기: `python Tools/tile_kit.py prep 뽑은그림.png --id <에셋ID> [--seamless]` (Lanczos = 임시, 최종은 **일러스트용 AI 업스케일러** ×2 권장)

""" + '\n'.join(size_table()) + """

- 최종 원본 크기에 못 미치는 생성물은 제작용 중간 결과다. 원래 배치 크기를 맞추려고 PPU만 낮추면 화질이 부족해질 수 있다. 고해상도 재생성 또는 일러스트용 업스케일·선 보정 후 최종 크기와 실제 화면 품질을 함께 검수한다. 낮은 해상도 도트를 크게 늘리는 방식은 금지한다.
- ⚠️ 업스케일 후 **선 굵기 확인** (지형 원본은 1080p 굵기 ×4, 기존 배경은 ×2): 뒷벽 2~3px · 세트 조각 3px · 지형·발판 10~12px @ PPU 256 ([16](16_아트_컨셉.md) §2-2)
- ⚠️ 이어지는 그림(뒷벽·채움·띠·이음·전경·↕️ 조각)은 **업스케일 뒤에 다시 이어짐 검사** — 업스케일러가 가장자리를 바꿀 수 있음

---

## 2. 층 구조 · 패럴랙스 🥞

| 층 | 무엇 | 정렬 레이어 (Sorting Layer) | 패럴랙스 f (가로, 세로) | 그림 |
|---|---|---|---|---|
| L0 | 종이 바탕 | 카메라 배경색 (`BgPaperColor`) | 화면 고정 | 장 종이색 (1장 `#BDBCB5`, 2장 `#C7BBA8`) |
| L1 | **구획 뒷벽** | `BG_Wall` | **0.2, 0.6** | 뒷벽 A·B·C · 세로 뒷벽 · 구획 이음 |
| L2 | **세트 조각** | `BG_Set` | **0.5, 0.8** | 방 대표 조각 · 공용 조각 |
| L3 | 플레이 | `Terrain` < `Decor` < `Actors` < `Fx` | 1, 1 | 지형(고해상도 다중 Tilemap) · 방 장식 · 캐릭터 |
| L4 | 전경 테두리 | `Fore` | **1.3, 0** (세로는 화면 고정) | 위·아래 가장자리 매달린 덩어리, 투명도 40% |

**패럴랙스 식 (방 가운데 기준)** — `Tools/unity/Runtime/BgParallax.cs`
```
층 위치 = 방 가운데 + (카메라 − 방 가운데) × (1 − f)
f = 플레이 층이 1 움직일 때 이 층이 화면에서 움직이는 양 (0 = 화면에 붙음, 1 = 지형과 같이)
```
- **방 가운데 기준**이라 카메라가 방 가운데일 때 = 씬에 놓은 자리 그대로 보임 → 배치는 "카메라가 방 가운데" 기준으로 하면 됨
- 세로 f가 가로보다 큰 이유: 높은 방에서 위아래로 오갈 때 배경이 덜 미끄러지게 (세트 조각이 지형 위로 떠 보이는 것 방지)
- 이 층이 보일 수 있는 범위(한쪽) = `화면/2 + max(0, 방/2 − 화면/2) × f + 1` → 뒷벽은 이 범위를 칸으로 덮음 (`bg_manifest.py`가 계산)

---

## 3. 제작 방식 🛠️

### 3-1. 만드는 순서 (앞 단계가 뒤 단계의 테두리·밑그림이 됨)

| # | 무엇 | 왜 이 순서 | 통과해야 다음으로 |
|---|---|---|---|
| 0 | 골든 G1~G5 (+ 장 골든) | 모든 그림의 스타일·명도 기준 | 사람 승인 |
| 1 | 구획 뒷벽 **A** 22장 | **테두리 원본** — B·C·세로·이음이 전부 A의 띠를 씀 | `art_check` + `tile_kit check A` |
| 2 | 구획 뒷벽 **B·C** 44장 | A의 띠 그대로 + 안쪽만 새로 | `tile_kit check A B C` (테두리 차이 ≤ 4) |
| 3 | 세로 전용 뒷벽 6장 | 위·아래 띠 = 그 구획 A | `seam_check check` (A 위·아래로 붙여 검사) |
| 4 | 구획 이음 22장 | 양 끝 = 두 구획의 A | `seam_check check` |
| 5 | 지형 fill_a → b·c → 띠 3종 (구역마다) | 뒷벽과 같은 방식 (fill_a = 테두리 원본) | `art_check` 이어짐 + 3단 가장자리 |
| 6 | 지형 구역 이음 (속 + 띠) | 두 구역 fill_a·띠가 있어야 밑그림 가능 | `seam_check check` |
| 7 | 공용 세트 조각 40 → 방 대표 조각 64 | 뒷벽 위에 올려 명도 확인하면서 | `art_check` (투명·발판처럼 평평한 위 금지) |
| 8 | 전경 2장 · 방 장식 57장 | 방 화면이 거의 다 갖춰진 뒤 | 한 화면 검수 |

### 3-2. 그림마다 한 장 만드는 흐름 (공통)

```
① 프롬프트 복사       Art/ALL_PROMPTS.md 에서 에셋 ID 찾기 (MSB v5 + TYPE + MAT + UNIFY v5 + 네거티브가 이미 조립돼 있음)
② AI에 요청          📐 줄의 비율로 (예: 뒷벽 1:1 · 세로 뒷벽 2:3 · 띠 3:2) + 레퍼런스 = 골든 + 같은 구획 A
③ 자르고 키우기       python Tools/tile_kit.py prep 뽑은.png --id B1-A1-wall_a --seamless
④ 넣기               Art/inbox/B1-A1-wall_a.png  (파일 이름 = 에셋 ID)
⑤ 자동 처리          art_check: 크기·이어짐·명도·반복 검사 → unify_pass(장 색·종이 결) → Art/out/<경로> 또는 Art/rejected/<ID>/이유.txt
```

### 3-3. 종류별 만드는 법

**① 구획 뒷벽 A (테두리 원본)**
- SD: A1111 `Tiling` 체크 / ComfyUI Seamless Tile(순환 패딩) · Midjourney `--tile` · **타일링 옵션 없는 AI** → 뽑은 뒤 `tile_kit.py seamless 그림.png` → 가운데 십자 이음 자리만 인페인팅
- 확인: `tile_kit.py offset A.png` (반 칸 밀어 보기) → 가운데 십자에 선이 보이면 다시
- 하늘·지평선·바닥·위아래 방향 ❌ (어느 쪽으로 이어 붙여도 말이 되는 벽 질감) · 가장자리 어둡게(비네트)·테두리 선 ❌

**② 같은 구획 B·C**
```
python Tools/tile_kit.py variant-base B1-A1-wall_a.png      → _base.png (안쪽 = A를 반 칸 민 그림) + _base_mask.png (흰색 = 새로 그릴 곳)
인페인팅: 밑그림 + 마스크, 같은 프롬프트 (variation B/C), 디노이즈 0.55~0.7, 마스크 바깥(테두리 띠 10%) 절대 안 건드림
python Tools/tile_kit.py check A.png B.png C.png            → 좌↔우·위↔아래 ≤ 2.0 · 테두리 차이 ≤ 4
```

**③ 세로 전용 뒷벽** (2048×4096 한 번에 못 뽑음)
```
python Tools/tile_kit.py tall-base B1-C2-wall_a.png --ratio 2   → A를 세로로 2장 이은 밑그림 + 마스크(위·아래 띠만 지킴)
인페인팅: 1024 높이 조각씩 아래 → 위로 (아래 = 무겁고 진함, 위 = 가볍고 밝은 틈) · 좌우 타일링(Tiling X)만 켬
```
- 위·아래 띠가 A와 같아서 **세로 뒷벽 위아래로 A·B·C를 이어 깔 수 있음** (방이 32유닛보다 높아도 OK)
- 좌우는 자기 자신과 이어짐 → 높은 방은 **세로 뒷벽을 가로로 반복**해서 깔아 (A와 옆으로 붙이지 않음)

**④ 구획 이음**
```
python Tools/tile_kit.py link-base B1-A2-wall_a.png B1-A1-wall_a.png --dir h --out BL-A2A1-wall_base.png
인페인팅: 양 끝 10% 띠 지키고 가운데만, 프롬프트 = BL-A2A1-wall (왼쪽은 A2 재료, 오른쪽은 A1 재료)
```
- 가로 이음(h): 왼쪽 끝 = 앞 구획 A의 왼쪽 띠, 오른쪽 끝 = 뒤 구획 A의 오른쪽 띠, **위↔아래는 자기 자신과 이어짐**
- 세로 이음(v): 위 끝 = 앞 구획, 아래 끝 = 뒤 구획, **좌↔우는 자기 자신과 이어짐**

**⑤ 세트 조각** — 투명 낱개, 붙는 곳 4종 (프롬프트에 끝 처리 문구가 이미 들어 있음)

| 붙는 곳 | 그림 끝 | 유니티 |
|---|---|---|
| ⬇️ 매달림 | 위 끝이 잘림 (천장 뒤로) | 위 끝을 천장 지형 뒤로 `buryMin` 이상 |
| ⬆️ 솟음 | 밑동이 어둡게 흐려지거나 잘림 | 밑동을 바닥 지형 뒤로 `buryMin` 이상 |
| ↕️ 바닥~천장 | 위아래 잘림 + **세로로 이어짐** | Draw Mode Tiled로 방 높이만큼 늘림 |
| 🧱 벽에 붙음 | 가장자리가 뒷벽으로 흐려짐 | 자유 배치 |

- 위가 **평평한 조각 금지** (발판으로 착각) · 지형보다 밝고 뒷벽보다 진하게 (뒷벽보다 15~20 어둡게)

**⑥ 전경** — 2:1, 가로로 이어짐 (Tiling X), **가운데 60%는 투명**, 위·아래 가장자리만 덩어리

---

## 4. 구역 · 구획별 이미지 🧭

> 같은 장 = 같은 재료·명도 → **한 지역으로 보이게**. 구역마다 **디테일·아주 옅은 색 기운**만 달라서 → **다른 구역에 온 느낌**. 구획(구역 안 2~3개)마다 뒷벽이 다르고, 방마다 대표 조각이 달라서 **같은 구획 안에서도 방마다 다르게** 보임.
> 화면 수 = 방 크기 ÷ 화면 (1.0×1.0 = 딱 한 화면). 세로 1.5 이상인 곳에 세로 뒷벽.
""" + '\n'.join(zone_tables()) + """

---

## 5. 유니티에 넣는 법 🎮

### 5-0. 준비물 (이 저장소 → 유니티 프로젝트 `Unfin/`)

| 이 저장소 | 유니티 | 비고 |
|---|---|---|
| `Art/out/**` (승인된 그림) | `Assets/Art/**` (경로 그대로) | 경로가 같아야 매니페스트가 찾음 |
| `Art/bg_unity_manifest.json` | `Assets/Art/bg_unity_manifest.json` | 방마다 깔 그림 목록 (자동 생성) |
| `Tools/unity/Runtime/*.cs` | `Assets/Scripts/Unfin/Background/` | `BgParallax` · `BgSetPiece` · `BgPaperColor` |
| `Tools/unity/Editor/*.cs` | `Assets/Scripts/Unfin/Background/Editor/` | `UnfinArtImporter` · `BgRoomBuilder` (**Editor 폴더 안에**) |

### 5-1. 가져오기 목표 설정

> **구현 상태:** 현재 `UnfinArtImporter`는 모든 이미지에 PPU 128을 적용한다. 아래 지형·캐릭터 PPU 256 분기는 아직 미구현이다. Unity 적용 전에 타입별 분기를 반영해야 하며, 배경 매니페스트 PPU 128은 유지한다. 문서 수정만으로 자동 가져오기가 바뀌지는 않는다.

| 항목 | 값 | 이유 |
|---|---|---|
| Texture Type / Mode | Sprite (2D and UI); 타일 시트 Multiple, 낱개 Single | 타일 내용 256×256px, 여백 별도 |
| Pixels Per Unit | **지형·캐릭터 256 / 큰 배경·방 전체 오버레이 128** | 256px 지형 셀 = 1유닛. 배경의 기존 월드 크기 보존 |
| Filter | **Trilinear** + **Generate Mip Maps** | 🚫 Point·Pixel Perfect 금지 (픽셀 아트 아님) |
| Wrap Mode | 타일 아틀라스·뒷벽·세트 = **Clamp** / 독립 반복 재료만 **Repeat** | 아틀라스 셀 밖 색상 확장 여백 최소 4px, 밉 단계에 따라 증가 |
| Mesh Type | 배경·지형 = **Full Rect** / 나머지 Tight | Draw Mode Tiled는 Full Rect만 |
| Alpha | 뒷벽·채움 = 없음 (불투명) / 나머지 = 투명 | |
| Max Size | 2048 (세로 뒷벽·방 장식·골든 4096) | |
| Compression | **High Quality** (PC BC7) · Crunch 끔 | 넓은 그라디언트에 띠 생기면 None |
| Pivot | Center | 매니페스트 좌표가 가운데 기준 |

### 5-2. 정렬 레이어 · 카메라

- 메뉴 **Unfin ▸ 배경 ▸ 정렬 레이어 만들기** → `BG_Paper < BG_Wall < BG_Set < Terrain < Decor < Actors < Fx < Fore`
- 카메라: Orthographic, Size **8.4375**, Clear = Solid Color. 출력 해상도에서 직접 렌더링하며 480×270 저해상도 화면 확대·픽셀 스냅은 사용하지 않는다.
- URP 2D면 반복 배경 재료는 중립 조명으로 유지하고, 방 단위의 국소광·공기색·안개 레이어를 별도로 합성한다. 플레이어·적·탄의 가독성은 최종 장면에서 확인한다

### 5-3. 방 배경 자동 배치 (`BgRoomBuilder`)

```
python Tools/build_prompts.py           (자동으로 bg_manifest.py build 까지)
→ Art/out, Art/bg_unity_manifest.json 을 유니티 Assets/Art/ 로 복사
→ 유니티 메뉴 Unfin ▸ 배경 ▸ 방 배경 만들기 → 방 ID (예: ch1-r09) 또는 '모든 방 만들기'
```

만들어지는 것 (방 하나 = `BG_<방ID>` 오브젝트, 위치 = 방 가운데):

| 자식 | 내용 | 컴포넌트 |
|---|---|---|
| `L1_Wall` | 매니페스트의 칸 목록 그대로 — 변형 A·B·C (왼쪽·아래 칸과 같은 변형 피함, 방마다 고정 시드) · 이음 칸 · 세로 뒷벽 | `BgParallax (0.2, 0.6)` |
| `L2_Set` | 방 대표 조각을 붙는 곳에 맞는 기본 자리에 (사람이 옮겨서 다듬기), 옵션으로 공용 1개 | `BgParallax (0.5, 0.8)` + 조각마다 `BgSetPiece` |
| `L4_Fore` | 전경 띠, 화면 높이에 맞춰 가로로 늘림, 투명도 40% | `BgParallax (1.3, 0)` |

- 칸 고르기는 **파이썬(`bg_manifest.py`)이 한 번만** 정함 → 유니티 결과 = `Art/bg_preview/<방ID>.png` 와 똑같음
- **돌리기·뒤집기 금지** (테두리 띠가 어긋남). 반복 숨기기는 변형 섞기 + 대표 조각 + 공용 조각 몫
- 방을 다시 만들면 `BG_<방ID>`를 지우고 새로 만듦 → **손으로 옮긴 세트 조각은 프리팹으로 따로 저장**해 두기

### 5-4. 세트 조각 다듬기

- `BgSetPiece`를 선택하면 **빨간 선** = 지형이 최소 여기까지 가려 줘야 하는 높이 (`buryMin`)
- `buryMin = min(화면 높이, 카메라 세로 이동) × (1 − 0.8) + 0.5` — 바닥이 보이는 동안 조각이 바닥보다 덜 움직이는 만큼 묻어야 안 떠 보임
- 기본 자리는 **카메라가 맨 아래(바닥이 보일 때)·맨 위(천장이 보일 때)** 기준 → 그대로 두면 바닥·천장에 맞음
- 방마다 대표 조각 1개(높은 방은 2~3개) + 공용 1~3개, **플레이 동선 뒤에 큰 덩어리를 두지 않기** (지형과 겹쳐 읽힘)

### 5-5. 지형 (L3, 고해상도 다중 Tilemap)

- **Grid:** Cell Size 1×1유닛. 충돌 Tilemap은 숨기고 이미지와 분리한다.
- **Terrain Fill:** 구역별 A~C 재료 세트, 256px/칸. 2048² 큰 채움 원본은 8×8유닛 영역에 사용한다.
- **Terrain Detail:** 최소 4가지 무늬 계열, 인접 반복 금지·고정 시드. 여러 칸을 덮는 장식으로 일정한 격자감을 없앤다.
- **Terrain Edge:** 바닥·벽·천장·안팎 모서리를 Rule Tile 또는 같은 이웃 판정으로 선택한다. 아틀라스의 셀 내용 256px와 여백을 구분한다.
- **Decor:** 방 고유 장식·재료 조각을 추가한다. 방 전체 오버레이는 별도 PPU 128이다.
- **전환:** 구역 경계는 TL- 재료로 연결한다. 2048px 전환 원본의 기본 폭은 8유닛이며, 더 넓은 전환은 여러 조각으로 연결한다.
- **Sprite Shape:** 긴 곡선 구간에만 선택 사용. 지형 재료 PPU 256과 월드 UV 정렬을 Tilemap 쪽과 맞춘다.
- **검수:** 실제 발 딛는 높이와 벽 접촉은 충돌에 맞추고, 이미지 해상도 변경 때문에 통로·점프 거리를 바꾸지 않는다.

### 5-6. 방이 바뀔 때

- 방마다 `BG_<방ID>`를 켜고 끔 (방 들어갈 때 켜기 → `BgPaperColor`가 종이색 적용). 옆 방 배경은 미리 켜 두지 않아도 됨 — 각 방이 자기 가운데 기준
- 화면 전환(밀기·페이드) 중엔 두 방 배경이 같이 보일 수 있음 → 경계 방은 **이음 칸이 문 쪽 화면 끝**에 오게 이미 밀려 있음 (§6-2)

---

## 6. 배경끼리 연결하는 법 🔗

### 6-1. 같은 구획 안 — 공통 테두리 띠
- A·B·C의 **바깥 10% 띠가 같은 그림** → 어떤 순서로 섞어 깔아도 이음새 없음 (가로·세로 모두)
- 세로 뒷벽의 위·아래 띠 = A 띠 → 위아래로 A·B·C와 붙음

### 6-2. 구획 사이 — 구획 이음 칸
- 지도에서 맞닿은 구획 쌍마다 이음 1장 (`seam_check.py links`가 지도에서 자동으로 찾음)
- **맞닿은 곳에서 가장 가까운 방**의 그 변 끝 한 줄(가로 이음 = 맨 왼쪽/오른쪽 열, 세로 이음 = 맨 위/아래 줄)이 이음 칸
- 뒷벽 격자를 밀어서 **이음 칸 바깥 끝 = 카메라가 끝까지 갔을 때 화면 끝** → 문 앞에서 이음이 통째로 보이고, 옆 방은 뒤 구획 뒷벽으로 시작
- 이음 칸 안쪽 끝 = 이 구획 띠 → 옆 칸(이 구획 A·B·C)과 이음새 없음

""" + '\n'.join(link_table()) + """

### 6-3. 구역 사이 지형 — 지형 이음
- 구역이 바뀌는 구획 이음 자리엔 지형도 바뀜 → **채움 원본 1장 기준 경계 8유닛 폭**에 `TL-<앞><뒤>-fill` (속) + 띠 이음 (가로 경계 = `edge_top`·`edge_bottom`, 세로 경계 = `edge_side`)
- 띠 이음 양 끝 = 각 구역 띠 → 바닥 선이 구역 경계를 지나도 끊기지 않음
- ⭐ **장 경계 D→E** (`TL-DE-*`, `BL-D?E?`): 연필 → 크레파스 재료가 섞이는 유일한 곳. 명도는 같게, 재료만 서서히

### 6-4. 높은 곳
- 1.5화면 이상 = 세로 뒷벽 (가운데 두 줄, 가로로 반복) + 위·아래는 A·B·C
- 세로 뒷벽 줄과 좌우 이음이 겹치는 곳 2곳(`ch2-r18` 물감 폭포 홀, `ch2-s14` 크레파스 탑)은 세로 뒷벽이 우선 → **유니티 화면에서 꼭 확인**

---

## 7. 검증 ✅

| 단계 | 언제 | 명령 | 통과 기준 | 실패하면 |
|---|---|---|---|---|
| ① 그림 한 장 | inbox에 넣을 때마다 (자동) | `python Tools/art_check.py run` (또는 `art_watch.bat` 감시) | 크기·비율 = 최종 · 이어짐 ≤ 2.0 · 기존 명도 띠는 v5 스타일 판정에서 제외·재조정 필요 · 순백 ≤ 3% · 반복·단색·딱딱한 가장자리 없음 | `Art/rejected/<ID>/이유.txt` 보고 다시 뽑기 |
| ② 같은 구획 묶음 | B·C 만든 뒤 | `python Tools/tile_kit.py check A.png B.png C.png` · `preview A B C` | 이어짐 ≤ 2.0 · 테두리 차이 ≤ 4 · 미리보기에 격자 선 없음 | 마스크 밖이 바뀜 → 다시 인페인팅 |
| ③ 이음새 | 이음·세로 뒷벽·지형 이음 넣은 뒤 | `python Tools/seam_check.py check` | 쌍마다 밝기 차 ≤ 6 · 색 차 ≤ 5 · 선 밀도 0.70~1.43배 · 윤곽 튐 ≤ 2 → `Art/seam_report.md` + `Art/seam_preview/` | 미리보기 빨간 쌍 → 이음 다시 |
| ④ 방 단위 | 유니티 넣기 전 | `python Tools/bg_manifest.py check` · `preview <방ID>` | 방에 필요한 그림 전부 ✅ → `Art/bg_room_report.md` · 주황 상자(카메라 네 구석 화면)가 전부 그림 안 | 빠진 그림부터 만들기 |
| ⑤ 유니티 화면 | 방 배치 후 | 플레이 모드로 방 네 구석까지 이동 | 아래 체크리스트 | 조각 위치·묻는 깊이 조정 |
| ⑥ 승인 | 사람이 보고 | `python Tools/art_check.py approve <ID>` | 🟡 자동 통과 → ✅ 승인 | |
| ⑦ 푸시 때 | GitHub Actions (자동) | `art-check.yml` | 전체 재검사 + 방별 준비 상태 → Actions 요약 · 보고서 다운로드 | |

![방 미리보기](images/bg_room_preview_demo.png)
> `bg_manifest.py preview` 예시 (그림이 아직 없어서 구획 색 상자로 대신). 왼쪽 `ch1-r09`: 오른쪽 끝 열이 구획 이음 `BL-B2B1` — 격자가 밀려서 카메라가 오른쪽 끝일 때 화면 끝 = 이음 끝. 오른쪽 `ch1-s06` 수직굴: 가운데 두 줄 = 세로 뒷벽, 위아래 = 구획 A·B·C. 초록 = 방 가운데에서 본 화면, 주황 = 네 구석에서 본 화면

**⑤ 유니티 화면 체크리스트**
- [ ] 뒷벽에 **격자 선·같은 무늬 반복**이 안 보임 (특히 방 가운데 → 네 구석으로 이동할 때)
- [ ] 화면 끝에 **뒷벽이 비는 곳(종이색만 보이는 곳)이 없음**
- [ ] 구획 경계 방: 문 앞에서 다음 구획 재료가 보이기 시작하고, 옆 방이 그 재료로 시작
- [ ] 세트 조각 밑동·윗동이 **지형 뒤로 숨음** (점프·낙하 중에도 공중에 뜬 순간 없음)
- [ ] 세트 조각 위가 **발판처럼 안 보임**, 플레이어(분필 흰색)가 화면에서 가장 밝음
- [ ] 명도: 방별 분위기·먼 배경의 낮은 대비·전경 실루엣·선명한 접촉 면. 종이 70% 고정 금지; 주인공·적·탄이 읽히는지 확인
- [ ] 전경이 플레이를 가리지 않음 (가운데 비어 있음)
- [ ] 1920×1080·2560×1440·3840×2160 세 해상도에서 선이 뭉개지거나 계단지지 않음

---

## 8. 다른 AI에게 맡길 때 (복사해서 붙여넣기) 🤖

**8-1. 이미지 생성 AI** (그림 한 장)
```
너는 Unfin 게임의 배경 그림을 만든다. 그림체는 부드러운 그래픽 디자인: 선명하고 일정한 굵기의 외곽선, 평평한 색 + 부드러운 2단 음영,
거친 선·해칭·사실적 미세 질감 금지. 강한 실루엣, 층별 공간 깊이, 방별 낮은~중간 명도, 장면의 국소광과 얇은 안개. 타일 원본에는 광원을 굽지 않는다.
아래 프롬프트와 네거티브를 그대로 쓰고, 비율은 📐 줄의 'AI에 요청' 비율로 만든다. 이어지는 그림이면 타일링(이어지는 질감) 옵션을 켠다.
하늘·지평선·바닥선·사람·글자를 넣지 않는다. 가장자리를 어둡게 하거나 테두리를 그리지 않는다.
[Art/ALL_PROMPTS.md 에서 해당 에셋 블록(### <ID> 부터 네거티브까지)을 붙여넣기]
```

**8-2. 제작 관리 AI** (저장소를 읽고 배경 전체를 진행)
```
Unfin 저장소의 Docs/20_배경_제작_파이프라인.md 를 따라 배경을 만든다.
1) Art/bg_room_report.md 를 열어 아직 없는(·) 그림을 §3-1 순서대로 고른다 (구획 뒷벽 A가 먼저, B·C·세로·이음은 A가 승인된 뒤).
2) 그림마다 Art/ALL_PROMPTS.md 의 프롬프트로 생성 → python Tools/tile_kit.py prep <파일> --id <ID> --seamless
   B·C는 tile_kit.py variant-base, 세로 뒷벽은 tall-base --ratio 2, 이음은 link-base 밑그림 + 인페인팅 (테두리 띠 10% 고정).
3) 결과를 Art/inbox/<ID>.png 로 넣고 python Tools/art_check.py run → 탈락이면 Art/rejected/<ID>/이유.txt 를 읽고 다시.
4) 묶음이 끝나면 python Tools/tile_kit.py check A B C · python Tools/seam_check.py check · python Tools/bg_manifest.py check 를 돌리고
   실패 항목과 미리보기(Art/seam_preview, Art/bg_preview) 경로를 보고한다. 승인(approve)은 사람이 한다.
금지: 그림을 돌리거나 뒤집어서 변형 만들기, 테두리 띠를 다시 그리기, Art/out 의 기존 파일 지우기, 크기를 최종 크기와 다르게 넣기.
```

**8-3. 유니티 작업 AI**
```
Unfin 유니티 프로젝트에 배경을 넣는다. Docs/20 §5를 따른다.
- Tools/unity/Runtime/*.cs → Assets/Scripts/Unfin/Background/, Tools/unity/Editor/*.cs → 같은 폴더의 Editor/ 하위.
- Art/out/** → Assets/Art/** (경로 유지), Art/bg_unity_manifest.json → Assets/Art/.
- 메뉴 Unfin ▸ 배경 ▸ 정렬 레이어 만들기 → 방 배경 만들기(모든 방).
- 카메라 Orthographic Size 8.4375, 지형·캐릭터 PPU 256 / 큰 배경·방 오버레이 PPU 128. 먼저 기존 공통 PPU 128 가져오기 코드를 타입별로 분리한다. 필터 Trilinear + 밉맵 (Point/Pixel Perfect 금지).
- 패럴랙스는 BgParallax의 방 가운데 기준 식만 쓴다 (뒷벽 0.2/0.6, 세트 0.5/0.8, 전경 1.3/0). 뒷벽 칸 배치는 매니페스트 그대로, 임의로 돌리기·뒤집기 금지.
- 지형은 256px/칸 Tilemap/Rule Tile, 채움·무늬·가장자리·장식 레이어를 분리한다. 2048px 채움 원본은 8유닛 주기, 구역 경계에는 TL- 이음 그림. Sprite Shape는 필요한 긴 곡선에만 선택 사용.
- 끝나면 §7 ⑤ 체크리스트를 방마다 확인하고, 이상한 방 ID와 스크린샷을 보고한다.
```

---

## 9. 문제가 생기면 🩹

| 증상 | 원인 | 해결 |
|---|---|---|
| 뒷벽에 격자 선 | B·C 인페인팅 때 테두리 띠가 바뀜 / 업스케일러가 가장자리를 바꿈 | `tile_kit check` 테두리 차이 확인 → 마스크 다시, 업스케일 후 `prep --seamless` |
| 칸 사이 가는 밝은 선 | 밉맵·압축 번짐 | Wrap Clamp 확인, 압축 None으로 비교, 카메라 위치 소수점 → 픽셀 스냅 없이 그대로 |
| 화면 끝에 종이색 | 매니페스트가 오래됨 (방 크기 바뀜) | `build_prompts.py` 다시 → 매니페스트 복사 → 방 다시 만들기 |
| 세트 조각이 떠 보임 | 묻는 깊이 부족 / 방 가운데 기준이 아님 | 빨간 선까지 지형 뒤로, `roomCenter` 확인 |
| 구획 경계에서 뚝 바뀜 | 이음 칸이 그 방에 없음 / 이음 그림 없음 | `bg_room_report.md` 이음 칸 확인, `seam_check links`가 쌍을 찾았는지 |
| 지형 무늬가 경계에서 어긋남 | Fill PPU·UV 기준이 다름 | 지형 PPU 256, 256px 셀과 8유닛 큰 채움 원본의 정렬 확인 |
| 전체가 너무 밝음/어두움 | unify_pass 안 거침 / 2D 조명 | 최종 방 조명·안개·노출 확인; 이전 종이색 후처리를 무조건 적용하지 않기 |

---

### 변경 기록
| 날짜 | 내용 |
|---|---|
| 2026-10-15 | 고해상도 타일맵 개정: 지형 256px/칸·PPU 256, 큰 배경 128 분리, 지형 원본 유닛 재계산, Tilemap/Rule Tile 기본, 기존 가져오기 도구의 미반영 상태 명시. [21 공통 규격](21_고해상도_타일맵_규격.md) |
| 2026-10-15 | 처음 작성 — 해상도(AI 요청 → 최종 → 유니티 유닛), 제작 순서·종류별 방법, 구역·구획별 이미지 표, 유니티 가져오기 설정·정렬 레이어·방 가운데 패럴랙스, `bg_manifest.py`(방별 뒷벽 칸 자동 배치·준비 상태·미리보기), `Tools/unity/`(BgParallax·BgSetPiece·BgPaperColor·UnfinArtImporter·BgRoomBuilder), 이음 칸을 문 쪽 화면 끝에 맞추는 격자 밀기, 세트 조각 묻는 깊이 식, 전경 세로 화면 고정, 검증 7단계, 다른 AI용 지시문 3종 |
"""
open(OUT, 'w', encoding='utf-8').write(md)
print('→', os.path.relpath(OUT, BASE), md.count('\n'), '줄')
