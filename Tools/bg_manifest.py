"""
bg_manifest.py — 방마다 '어떤 배경 그림을 어디에 까는지'를 정해서 유니티가 그대로 읽게 만드는 도구

  python Tools/bg_manifest.py build            지도 + 에셋 목록 → Art/bg_unity_manifest.json (방 56 + 수직굴 14)
  python Tools/bg_manifest.py check            방마다 필요한 그림이 Art/out 에 다 있는지·승인됐는지 → Art/bg_room_report.md
  python Tools/bg_manifest.py preview <방ID>   그 방의 뒷벽 깔기 + 카메라가 보는 범위 그림 → Art/bg_preview/<방ID>.png
  python Tools/bg_manifest.py preview all      전부 (그림이 아직 없으면 구획 색 + 이름 상자로 대신 그림)

좌표: 지도 1칸 = 16 논리 px = 유니티 1유닛. 화면 = 30 × 16.875 유닛. 그림 128px = 1유닛 (PPU 128).
뒷벽 한 장(2048²) = 16 × 16 유닛. 패럴랙스 배율 f = '플레이 층이 1 움직일 때 이 층이 화면에서 움직이는 양'
  → 층 위치 = 방 가운데 + (카메라 − 방 가운데) × (1 − f)   (Tools/unity/BgParallax.cs 와 같은 식)
뒷벽 칸 고르기(변형 A·B·C, 왼쪽·아래 칸과 같은 변형 피하기)는 여기서 한 번만 정하고, 유니티는 그대로 깔기만 함.
"""
import json, math, os, re, sys
from collections import Counter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seam_check

BASE = seam_check.BASE
ART = os.path.join(BASE, 'Art')
MANIFEST = os.path.join(ART, 'bg_unity_manifest.json')
REPORT = os.path.join(ART, 'bg_room_report.md')
PREV = os.path.join(ART, 'bg_preview')

PPU = 128                      # 그림 px / 유닛
TILE = 2048 // PPU             # 뒷벽 한 장 = 16 유닛
SCREEN = (30.0, 16.875)        # 480×270 논리 px ÷ 16
PARALLAX = dict(paper=(0.0, 0.0), wall=(0.2, 0.6), set=(0.5, 0.8), play=(1.0, 1.0), fore=(1.3, 0.0))   # 전경: 가로 1.3배, 세로는 화면에 고정(위·아래 테두리)
MARGIN = 1.0                   # 화면 끝에서 1유닛 더 깔기 (흔들림·확대 여유)
ZONE_TINT = dict(A='#C2C1B9', B='#B8B8BC', C='#B6BDB6', D='#C0B8B6', E='#C5BCB1', F='#C0BAB3', G='#BABBC2', H='#C2B8B5')
SIDE_OF = {('h', 0): 'right', ('h', 1): 'left', ('v', 0): 'bottom', ('v', 1): 'top'}   # (방향, 쌍에서 몇 번째) → 이음이 들어갈 방의 변


def u32(v): return v & 0xFFFFFFFF


def fnv(s):
    h = 2166136261
    for b in s.encode('utf-8'): h = u32((h ^ b) * 16777619)
    return h


def mix(ix, iy, seed):
    h = u32(ix * 73856093) ^ u32(iy * 19349663) ^ seed
    h ^= h >> 13; h = u32(h * 0x5BD1E995); h ^= h >> 15
    return h


def catalog():
    return {a['id']: a for a in json.load(open(os.path.join(ART, 'asset_catalog.json'), encoding='utf-8'))['assets']}


def extent(size, screen, f):
    """방 가운데 기준, 이 층에서 화면에 보일 수 있는 최대 거리 (한쪽)"""
    return screen / 2 + max(0.0, size / 2 - screen / 2) * f + MARGIN


# ───────────────────────── build ─────────────────────────
def build(quiet=False):
    cat = catalog()
    lab, rooms, shafts, areas, names, W, H = seam_check.label_map()
    links = json.load(open(os.path.join(ART, 'bg_links.json'), encoding='utf-8'))['links']
    room_area = {(v['chapter'], n): k for k, v in areas.items() for n in v['rooms']}

    spaces = []                                                   # 방 + 수직굴
    for r in rooms:
        ch = seam_check.ZONE_CH[r['z']]
        spaces.append(dict(id=f"ch{ch}-r{r['n']:02d}", kind='room', n=r['n'], chapter=ch, zone=r['z'], name=r['name'],
                           area=room_area.get((ch, r['n'])), x=r['x'], y=r['y'], w=r['w'], h=r['h']))
    for i, s in enumerate(shafts):
        ch = seam_check.ZONE_CH[s['z']]
        cnt = Counter(lab[y, x] for y in range(s['y'], s['y'] + s['h']) for x in range(s['x'], s['x'] + s['w'])
                      if 0 <= y < H and 0 <= x < W and lab[y, x] and lab[y, x][0] == s['z'])
        spaces.append(dict(id=f"ch{ch}-s{i + 1:02d}", kind='shaft', n=None, chapter=ch, zone=s['z'], name=s.get('name', f'수직굴 {i + 1}'),
                           area=cnt.most_common(1)[0][0] if cnt else next(k for k, v in areas.items() if v['zone'] == s['z']),
                           x=s['x'], y=s['y'], w=s['w'], h=s['h']))

    # 세로 전용 뒷벽: 메모의 '(방 14' 또는 수직굴 이름으로 찾기
    tall_of = {}
    for a in cat.values():
        if a['type'] != 'BG_WALL_TALL': continue
        area = a['id'].split('-')[1]; label = a['note'].split(' · ')[1]
        m = re.search(r'\(방 (\d+)', label); nm = label.split(' (')[0]
        for s in spaces:
            if s['area'] != area and s['zone'] != area[0]: continue
            if (m and s['kind'] == 'room' and s['n'] == int(m.group(1))) or (not m and s['kind'] == 'shaft' and s['name'] == nm):
                tall_of[s['id']] = a['id']; s['area'] = area     # 세로 뒷벽 위아래 띠 = 그 구획 A 테두리 → 구획을 맞춤

    # 구획 이음: 맞닿은 지점에 가장 가까운 그 구획의 방(또는 수직굴)에 넣음
    def dist(s, p):
        dx = max(s['x'] - p[0], 0, p[0] - (s['x'] + s['w'] - 1)); dy = max(s['y'] - p[1], 0, p[1] - (s['y'] + s['h'] - 1))
        return dx + dy
    link_of = {s['id']: [] for s in spaces}
    for l in links:
        wid = f"BL-{l['first']}{l['second']}-wall"
        z1, z2 = l['first'][0], l['second'][0]
        tl = [i for i in cat if i.startswith(f'TL-{z1}{z2}-') or i.startswith(f'TL-{z2}{z1}-')] if z1 != z2 else []
        for k, area in enumerate((l['first'], l['second'])):
            cand = [s for s in spaces if s['area'] == area]
            if not cand: continue
            s = min(cand, key=lambda s: (dist(s, l['at']), s['kind'] != 'room'))
            link_of[s['id']].append(dict(side=SIDE_OF[(l['dir'], k)], to_area=l['second'] if k == 0 else l['first'],
                                         dir=l['dir'], wall_link=wid if wid in cat else None, terrain_links=sorted(tl),
                                         chapter_border=l['chapter_border']))

    out_rooms, used = [], set()
    for s in spaces:
        ch, z, area = s['chapter'], s['zone'], s['area']
        uw, uh = float(s['w']), float(s['h'])
        ux, uy = float(s['x']), float(H - s['y'] - s['h'])          # 유니티: 위가 +y
        cx, cy = ux + uw / 2, uy + uh / 2
        fx, fy = PARALLAX['wall']
        ex, ey = extent(uw, SCREEN[0], fx), extent(uh, SCREEN[1], fy)
        variants = [f'B{ch}-{area}-wall_{v}' for v in 'abc']
        tall = tall_of.get(s['id'])
        lk = {l['side']: l for l in link_of[s['id']] if l['wall_link']}
        # 격자 밀기: 이음 칸의 바깥 끝 = 카메라가 끝까지 갔을 때 화면 끝 → 문 앞에서 이음이 통째로 보임
        ox = (ex - math.ceil(ex / TILE) * TILE) if 'right' in lk else (-ex - math.floor(-ex / TILE) * TILE) if 'left' in lk else 0.0
        oy = 0.0 if tall else (ey - math.ceil(ey / TILE) * TILE) if 'top' in lk else (-ey - math.floor(-ey / TILE) * TILE) if 'bottom' in lk else 0.0
        x0, x1 = math.floor((-ex - ox) / TILE), math.ceil((ex - ox) / TILE)
        y0, y1 = math.floor((-ey - oy) / TILE), math.ceil((ey - oy) / TILE)
        if tall:                                                    # 세로 뒷벽(2칸 높이)은 가운데 두 줄 y=-1·0
            y0, y1 = min(y0, -1), max(y1, 1)
        seed = fnv(s['id']); grid = {}; tiles = []
        for iy in range(y0, y1):
            for ix in range(x0, x1):
                if tall and iy in (-1, 0):
                    if iy == -1: tiles.append(dict(x=ix, y=-1, id=tall, rows=2, role='tall'))
                    grid[(ix, iy)] = -1; continue
                role, aid = 'wall', None
                if 'right' in lk and ix == x1 - 1: role, aid = 'link', lk['right']['wall_link']
                elif 'left' in lk and ix == x0: role, aid = 'link', lk['left']['wall_link']
                elif 'top' in lk and iy == y1 - 1: role, aid = 'link', lk['top']['wall_link']
                elif 'bottom' in lk and iy == y0: role, aid = 'link', lk['bottom']['wall_link']
                if aid is None:
                    avoid = {grid.get((ix - 1, iy)), grid.get((ix, iy - 1))}
                    pick = [v for v in range(3) if v not in avoid] or [0, 1, 2]
                    v = pick[mix(ix, iy, seed) % len(pick)]; grid[(ix, iy)] = v; aid = variants[v]
                else:
                    grid[(ix, iy)] = -2
                tiles.append(dict(x=ix, y=iy, id=aid, rows=1, role=role))
        land = [i for i in cat if s['kind'] == 'room' and re.fullmatch(fr'BR{ch}-{s["n"]:02d}[bc]?', i)]
        shared = sorted(i for i in cat if i.startswith(f'B{ch}-{z}-set_'))
        tz = sorted(i for i in cat if i.startswith(f'TZ{ch}-{z}-'))
        def anchor(i):
            n = cat[i]['note']
            return 'hang' if '⬇️' in n else 'rise' if '⬆️' in n else 'span' if '↕️' in n else 'wall' if '🧱' in n else '?'
        sx, sy = PARALLAX['set']
        # 바닥이 보이는 동안(카메라가 맨 아래 ~ 화면 한 장 위) 조각이 바닥보다 덜 움직이는 양 + 여유 0.5
        bury = round(min(SCREEN[1], max(0.0, uh - SCREEN[1])) * (1 - sy) + 0.5, 2)
        warn = []
        if tall and any(l['side'] in ('left', 'right') for l in lk.values()):
            warn.append('세로 뒷벽 줄과 좌우 이음이 겹침 → 이음 칸 대신 세로 뒷벽이 들어감, 화면 확인 필요')
        r = dict(id=s['id'], kind=s['kind'], chapter=ch, zone=z, area=area, area_name=names.get(area, ''), name=s['name'],
                 map_rect=dict(x=s['x'], y=s['y'], w=s['w'], h=s['h']),
                 unity=dict(x=ux, y=uy, w=uw, h=uh, center=[cx, cy]),
                 screens=[round(uw / SCREEN[0], 2), round(uh / SCREEN[1], 2)],
                 layers=dict(
                     paper=dict(parallax=PARALLAX['paper'], sorting='BG_Paper', color=dict(zip('12345', ['#BDBCB5', '#C7BBA8', '#B4BEC6', '#C9C3B6', '#9E9A92']))[str(ch)]),
                     wall=dict(parallax=PARALLAX['wall'], sorting='BG_Wall', tile=TILE, cover=[round(ex, 2), round(ey, 2)],
                               grid=dict(x=[x0, x1], y=[y0, y1]), offset=[round(ox, 3), round(oy, 3)], tall=tall, tiles=tiles),
                     set=dict(parallax=PARALLAX['set'], sorting='BG_Set', bury_min=bury,
                              landmarks=[dict(id=i, anchor=anchor(i)) for i in land],
                              shared=[dict(id=i, anchor=anchor(i)) for i in shared], pick='공용 조각은 방마다 1~3개'),
                     fore=dict(parallax=PARALLAX['fore'], sorting='Fore', id=f'B{ch}-fore', alpha=0.4)),
                 terrain=dict(zone=z, assets=tz),
                 links=link_of[s['id']], warnings=warn)
        out_rooms.append(r)
        used |= {t['id'] for t in tiles} | set(land) | set(shared) | set(tz) | {f'B{ch}-fore'}
        used |= {t for l in link_of[s['id']] for t in l['terrain_links']}
    used = sorted(i for i in used if i in cat)
    m = dict(version=1, note='bg_manifest.py build 로 자동 생성. 손으로 고치지 말 것 (build_prompts.py → seam_check links → 이 파일)',
             units=dict(ppu=PPU, tile=TILE, screen=SCREEN, parallax=PARALLAX, formula='layer = center + (camera - center) * (1 - f)'),
             assets={i: dict(file=cat[i]['file'], type=cat[i]['type'], final=cat[i]['final']) for i in used},
             asset_list=[dict(id=i, file=cat[i]['file'], type=cat[i]['type'], final=cat[i]['final']) for i in used],   # 유니티 JsonUtility용 (사전 못 읽음)
             rooms=out_rooms)
    json.dump(m, open(MANIFEST, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    if not quiet:
        nl = sum(len(r['links']) for r in out_rooms)
        print(f'방 {sum(r["kind"] == "room" for r in out_rooms)} + 수직굴 {sum(r["kind"] == "shaft" for r in out_rooms)} · '
              f'뒷벽 칸 {sum(len(r["layers"]["wall"]["tiles"]) for r in out_rooms)} · 이음 자리 {nl} · 세로 뒷벽 {len(tall_of)} → {os.path.relpath(MANIFEST, BASE)}')
        for r in out_rooms:
            for w in r['warnings']: print(f"  ⚠️ {r['id']} {r['name']}: {w}")
    return m


# ───────────────────────── check ─────────────────────────
def check():
    m = build(quiet=True)
    sp = os.path.join(ART, 'status.json')
    st = json.load(open(sp, encoding='utf-8')) if os.path.exists(sp) else {}
    def state(i):
        f = os.path.join(ART, 'out', m['assets'][i]['file'])
        if not os.path.exists(f): return 'missing'
        return st.get(i, {}).get('state', 'unchecked')
    icon = dict(approved='✅', auto_pass='🟡', unchecked='⚪', rejected='❌', missing='·')
    md = ['# 🧱 방별 배경 준비 상태', '', '`python Tools/bg_manifest.py check` 로 자동 생성. ✅ 승인 · 🟡 자동 통과(사람 확인 전) · ⚪ 파일은 있는데 검사 안 함 · ❌ 탈락 · · 없음', '',
          '| 방 | 구획 | 화면 | 뒷벽 | 세로 | 이음 | 대표 조각 | 공용 | 지형 | 준비 |', '|---|---|---|---|---|---|---|---|---|---|']
    ready = 0
    for r in m['rooms']:
        L = r['layers']
        def grp(ids):
            ids = list(dict.fromkeys(ids))
            if not ids: return '—', True
            s = [state(i) for i in ids]; ok = all(x == 'approved' for x in s)
            return ''.join(icon[x] for x in s), ok
        walls = sorted({t['id'] for t in L['wall']['tiles'] if t['role'] == 'wall'})
        cols = [grp(walls), grp([L['wall']['tall']] if L['wall']['tall'] else []),
                grp([l['wall_link'] for l in r['links'] if l['wall_link']] + [t for l in r['links'] for t in l['terrain_links']]),
                grp([x['id'] for x in L['set']['landmarks']]), grp([x['id'] for x in L['set']['shared']]), grp(r['terrain']['assets'])]
        ok = all(c[1] for c in cols); ready += ok
        md.append(f"| `{r['id']}` {r['name']} | {r['area']} {r['area_name']} | {r['screens'][0]}×{r['screens'][1]} | "
                  + ' | '.join(c[0] for c in cols) + f" | {'✅' if ok else '…'} |")
    md[2] += f'\n\n**유니티에 넣을 준비 끝: {ready} / {len(m["rooms"])}**'
    open(REPORT, 'w', encoding='utf-8').write('\n'.join(md) + '\n')
    print(f'준비 끝 {ready}/{len(m["rooms"])} → {os.path.relpath(REPORT, BASE)}')
    return ready == len(m['rooms'])


# ───────────────────────── preview ─────────────────────────
def preview(rid, px=8):
    from PIL import Image, ImageDraw, ImageFont
    m = json.load(open(MANIFEST, encoding='utf-8')) if os.path.exists(MANIFEST) else build(quiet=True)
    rooms = m['rooms'] if rid == 'all' else [r for r in m['rooms'] if r['id'] == rid]
    if not rooms: print('그런 방 없음:', rid); return
    os.makedirs(PREV, exist_ok=True)
    try: font = ImageFont.truetype('/usr/share/fonts/truetype/nanum/NanumGothic.ttf', 13)
    except OSError: font = seam_check._font(13)
    cache = {}
    def tex(aid, w, h, role):
        k = (aid, w, h)
        if k in cache: return cache[k]
        f = os.path.join(ART, 'out', m['assets'][aid]['file'])
        if os.path.exists(f):
            im = Image.open(f).convert('RGB').resize((w, h), Image.LANCZOS)
        else:                                                         # 그림이 없으면 구획 색으로 대신
            area = aid.split('-')[1]
            if role == 'link':
                a, b = area[:2], area[2:4]
                ca, cb = (Image.new('RGB', (1, 1), ZONE_TINT[a[0]]).getpixel((0, 0)), Image.new('RGB', (1, 1), ZONE_TINT[b[0]]).getpixel((0, 0)))
                im = Image.new('RGB', (w, h)); d = ImageDraw.Draw(im)
                horiz = m_dir.get(aid, 'h') == 'h'
                n = w if horiz else h
                for i in range(n):
                    t = i / max(1, n - 1); c = tuple(int(ca[j] * (1 - t) + cb[j] * t) for j in range(3))
                    d.line([(i, 0), (i, h)] if horiz else [(0, i), (w, i)], fill=c)
            else:
                base = Image.new('RGB', (1, 1), ZONE_TINT[area[0]]).getpixel((0, 0))
                v = aid[-1]; k2 = {'a': 0, 'b': -10, 'c': 8}.get(v, -18)
                im = Image.new('RGB', (w, h), tuple(max(0, min(255, c + k2)) for c in base))
            d = ImageDraw.Draw(im); d.rectangle([0, 0, w - 1, h - 1], outline=(150, 146, 138))
            d.text((6, 4), aid, fill=(70, 66, 60), font=font)
        cache[k] = im; return im
    m_dir = {f"BL-{l['first']}{l['second']}-wall": l['dir'] for l in json.load(open(os.path.join(ART, 'bg_links.json'), encoding='utf-8'))['links']}
    for r in rooms:
        L = r['layers']['wall']; T = L['tile'] * px
        (x0, x1), (y0, y1) = L['grid']['x'], L['grid']['y']
        ox, oy = L['offset']
        Wp, Hp = (x1 - x0) * T, (y1 - y0) * T
        im = Image.new('RGB', (max(Wp, 1000), Hp + 46), (40, 40, 44)); d = ImageDraw.Draw(im)
        for t in L['tiles']:
            hh = T * t['rows']
            im.paste(tex(t['id'], T, hh, t['role']), ((t['x'] - x0) * T, Hp - (t['y'] - y0) * T - hh))
        def box(cx, cy, col, wdt=2):                                      # 층 좌표(방 가운데 = 0) → 그림 좌표
            hw, hh = SCREEN[0] / 2, SCREEN[1] / 2
            X0 = (cx - ox - hw - x0 * L['tile']) * px; X1 = (cx - ox + hw - x0 * L['tile']) * px
            Y0 = Hp - (cy - oy + hh - y0 * L['tile']) * px; Y1 = Hp - (cy - oy - hh - y0 * L['tile']) * px
            d.rectangle([X0, Y0, X1, Y1], outline=col, width=wdt)
        fx, fy = L['parallax']; uw, uh = r['unity']['w'], r['unity']['h']
        mx = max(0.0, uw / 2 - SCREEN[0] / 2) * fx; my = max(0.0, uh / 2 - SCREEN[1] / 2) * fy
        for sx in (-1, 1):
            for sy in (-1, 1): box(sx * mx, sy * my, (230, 150, 60), 2)
        box(0, 0, (90, 200, 120), 3)
        for x in range((x1 - x0) + 1): d.line([(x * T, 0), (x * T, 5)], fill=(200, 60, 50), width=2)
        info = (f"{r['id']} {r['name']} · 구획 {r['area']} {r['area_name']} · 방 {uw:g}×{uh:g}유닛 ({r['screens'][0]}×{r['screens'][1]}화면) · 뒷벽 {fx}/{fy}배"
                + (f" · 세로 {L['tall']}" if L['tall'] else '') + ''.join(f" · 이음 {l['side']}→{l['to_area']}" for l in r['links']))
        d.text((8, Hp + 6), info, fill=(235, 232, 224), font=font)
        d.text((8, Hp + 25), '초록 = 카메라가 방 가운데일 때 화면 · 주황 = 카메라가 방 네 구석일 때 화면 (주황 상자가 전부 그림 안이면 통과)', fill=(200, 196, 186), font=font)
        p = os.path.join(PREV, f"{r['id']}.png"); im.save(p)
        if rid != 'all': print('→', os.path.relpath(p, BASE))
    if rid == 'all': print(f'{len(rooms)}장 → {os.path.relpath(PREV, BASE)}/')


if __name__ == '__main__':
    a = sys.argv[1:] or ['build']
    if a[0] == 'build': build()
    elif a[0] == 'check': sys.exit(0 if check() else 1)
    elif a[0] == 'preview': preview(a[1] if len(a) > 1 else 'all')
    else: print(__doc__)
