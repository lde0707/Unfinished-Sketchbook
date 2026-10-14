"""
배경 이음새 검사기 (seam_check) — 배경 패널이 끊김 없이 이어지는지 숫자로 판정

사용법
  python Tools/seam_check.py links              지도에서 실제로 맞닿은 구역 쌍을 찾아 Art/bg_links.json 저장
                                                (build_prompts.py가 이 파일로 이음 패널 목록을 만듦)
  python Tools/seam_check.py check [--layer far|mid|all]
                                                Art/out/ 의 구역 패널 + 이음 패널을 쌍마다 검사
                                                → Art/seam_report.md + Art/seam_preview/*.png
  python Tools/seam_check.py pair A.png LINK.png B.png [--dir h|v] [--out 미리보기.png]
                                                파일 3장을 바로 검사 (h = 왼→오, v = 위→아래)
  python Tools/seam_check.py demo               가짜 패널로 통과/탈락 예시 만들기 (Docs/images/seam_check_demo.png)

판정 항목 (이음새 한 곳마다, 양쪽 끝 띠를 비교)
  밝기 차이      두 띠의 평균 밝기 차 (0~255)                         ≤ 6
  색 차이        두 띠 평균색의 Lab ΔE                                ≤ 5
  선 밀도 비     두 띠의 선(경계) 픽셀 비율의 비                       0.70 ~ 1.43
  이음선 튐      맞닿는 두 줄의 차이 ÷ 그림 안쪽 이웃 줄끼리의 평소 차이   ≤ 2.0  (높으면 세로줄 이음새가 보임)
  윤곽 이어짐    이음새를 따라 잰 밝기 곡선의 상관                       ≥ 0.6  (능선·랜드마크가 이어지는지)
  지평선 (h만)   두 띠의 스카이라인 높이 차 (화면 높이 대비)             ≤ 2%
  참고           각 패널 스카이라인이 목표 62%에서 5% 넘게 벗어나면 ⚠️ 경고 (탈락 아님)
"""
import os, re, sys, json, argparse
from collections import deque, Counter
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__)); BASE = os.path.normpath(os.path.join(HERE, '..'))
ART = os.path.join(BASE, 'Art')
LIMITS = dict(dL=6.0, dE=5.0, dens_lo=0.70, dens_hi=1.43, jump=2.0, corr=0.6, sky=0.02)
HORIZON = 0.62
ZONE_CH = {'A': 1, 'B': 1, 'C': 1, 'D': 1, 'E': 2, 'F': 2, 'G': 2, 'H': 2}


# ───────────────────────── 1. 맞닿은 구역 쌍 ─────────────────────────
def find_links():
    src = open(os.path.join(BASE, 'Prototype', 'world_map_data.js'), encoding='utf-8').read()
    W = int(re.search(r'W=(\d+)', src).group(1)); H = int(re.search(r'H=(\d+)', src).group(1))
    g = []
    for t in re.search(r"MAPRLE='([^']*)'", src).group(1).split(','):
        v, n = t.split('.'); g += [int(v, 36)] * int(n, 36)
    g = np.array(g).reshape(H, W)
    zones = json.loads(re.search(r'const ZONES=(\{.*?\});\n', src, re.S).group(1))
    lab = np.full((H, W), '', dtype=object); q = deque()
    for k in ('ROOMS', 'PASS', 'SHAFTS'):           # 방·통로·수직굴에 구역 이름을 칠하고
        for r in json.loads(re.search(r'const ' + k + r'=(\[.*?\]);\n', src, re.S).group(1)):
            for y in range(r['y'], r['y'] + r['h']):
                for x in range(r['x'], r['x'] + r['w']):
                    if 0 <= x < W and 0 <= y < H and g[y, x] != 1 and not lab[y, x]:
                        lab[y, x] = r['z']; q.append((y, x))
    while q:                                         # 나머지 빈 공간은 가까운 구역으로 번지게
        y, x = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and g[ny, nx] != 1 and not lab[ny, nx]:
                lab[ny, nx] = lab[y, x]; q.append((ny, nx))
    pairs = Counter(); where = {}
    for y in range(H):
        for x in range(W):
            a = lab[y, x]
            if not a: continue
            for dy, dx in ((0, 1), (1, 0)):          # 오른쪽·아래 이웃이 다른 구역이면 맞닿음
                ny, nx = y + dy, x + dx
                if ny < H and nx < W and lab[ny, nx] and lab[ny, nx] != a:
                    key = (a, lab[ny, nx], 'h' if dx else 'v')
                    pairs[key] += 1; where.setdefault(key, (x, y))
    links = []
    for (a, b, d), n in sorted(pairs.items(), key=lambda kv: (ZONE_CH[kv[0][0]], kv[0])):
        links.append(dict(first=a, second=b, dir=d, contact_tiles=n, at=list(where[(a, b, d)]),
                          first_name=zones[a]['name'], second_name=zones[b]['name'],
                          chapter_border=ZONE_CH[a] != ZONE_CH[b],
                          meaning=('왼쪽 ' if d == 'h' else '위 ') + a + ' → ' + ('오른쪽 ' if d == 'h' else '아래 ') + b))
    out = os.path.join(ART, 'bg_links.json')
    json.dump(dict(note='seam_check.py links 로 자동 생성. h = first가 왼쪽, v = first가 위쪽', links=links),
              open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(f'맞닿은 구역 쌍 {len(links)}개 → {os.path.relpath(out, BASE)}')
    for l in links:
        print(f"  {l['first']}{l['second']} ({l['dir']}) {l['first_name']} ↔ {l['second_name']}  접촉 {l['contact_tiles']}칸"
              + ('  [장 경계]' if l['chapter_border'] else ''))
    return links


# ───────────────────────── 2. 이음새 측정 ─────────────────────────
def load(p):
    return np.asarray(Image.open(p).convert('RGB'), dtype=np.float64)

def lum(a): return a @ np.array([0.299, 0.587, 0.114])

def lab_mean(a):
    c = a.reshape(-1, 3).mean(0) / 255.0
    c = np.where(c > 0.04045, ((c + 0.055) / 1.055) ** 2.4, c / 12.92)
    X, Y, Z = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]]) @ c
    f = lambda t: t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    fx, fy, fz = f(X / 0.9505), f(Y), f(Z / 1.089)
    return np.array([116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)])

def edge_density(L):
    gx = np.abs(np.diff(L, axis=1))[:-1, :]; gy = np.abs(np.diff(L, axis=0))[:, :-1]
    return float(((gx + gy) > 18).mean())

def skyline(L, with_spread=False):
    """열마다 위에서부터 처음으로 종이보다 확실히 어두워지는 행 → 중앙값 (화면 높이 대비)"""
    paper = np.percentile(L, 90); h = L.shape[0]
    dark = L < paper - max(6.0, 0.35 * (paper - np.percentile(L, 5)))
    rows = np.array([np.argmax(col) if col.any() else h for col in dark.T]) / h
    med, iqr = float(np.median(rows)), float(np.percentile(rows, 75) - np.percentile(rows, 25))
    return (med, iqr) if with_spread else med

def to_h(a, d):
    """세로 이음(v)은 행·열을 바꿔서(전치) 가로 이음과 같은 계산을 씀 (위 → 왼쪽, 아래 → 오른쪽)"""
    return a if d == 'h' else a.transpose(1, 0, 2)

def seam(A, B, d):
    """A의 오른쪽(또는 아래) 끝과 B의 왼쪽(또는 위) 끝을 비교"""
    A, B = to_h(A, d), to_h(B, d)
    h = min(A.shape[0], B.shape[0])
    A, B = A[:h], B[:h]
    w = max(24, int(min(A.shape[1], B.shape[1]) * 0.03))
    sa, sb = A[:, -w:], B[:, :w]
    La, Lb = lum(sa), lum(sb)
    r = {}
    r['dL'] = abs(La.mean() - Lb.mean())
    r['dE'] = float(np.linalg.norm(lab_mean(sa) - lab_mean(sb)))
    da, db = edge_density(La), edge_density(Lb)
    r['dens'] = (da + 1e-4) / (db + 1e-4)
    LA, LB = lum(A), lum(B)
    inner = np.mean([np.abs(LA[:, -k] - LA[:, -k - 1]).mean() for k in range(2, 12)] +
                    [np.abs(LB[:, k] - LB[:, k + 1]).mean() for k in range(1, 11)])
    r['jump'] = np.abs(LA[:, -1] - LB[:, 0]).mean() / max(inner, 0.5)
    n = max(4, int(min(A.shape[1], B.shape[1]) * 0.005))     # 윤곽·지평선은 이음새에 딱 붙은 좁은 띠로 비교
    na, nb = LA[:, -n:], LB[:, :n]
    pa, pb = na.mean(1), nb.mean(1)
    k = max(1, h // 120); pa = np.convolve(pa, np.ones(k) / k, 'same'); pb = np.convolve(pb, np.ones(k) / k, 'same')
    r['corr'] = float(np.corrcoef(pa, pb)[0, 1]) if pa.std() > 1.5 and pb.std() > 1.5 else None
    r['sky'] = None
    if d == 'h':                                              # 능선이 또렷할 때만 (열마다 들쭉날쭉하면 지평선이 없는 그림)
        ia, ib = skyline(La, True)[1], skyline(Lb, True)[1]      # 들쭉날쭉한 정도는 넓은 띠로
        if ia < 0.06 and ib < 0.06: r['sky'] = abs(skyline(na) - skyline(nb))
    fails = []
    if r['dL'] > LIMITS['dL']: fails.append('밝기')
    if r['dE'] > LIMITS['dE']: fails.append('색')
    if not LIMITS['dens_lo'] <= r['dens'] <= LIMITS['dens_hi']: fails.append('선 밀도')
    if r['jump'] > LIMITS['jump']: fails.append('이음선 튐')
    if r['corr'] is not None and r['corr'] < LIMITS['corr']: fails.append('윤곽 끊김')
    if r['sky'] is not None and r['sky'] > LIMITS['sky']: fails.append('지평선 높이')
    r['fails'] = fails; r['ok'] = not fails
    return r

def fmt(r):
    s = f"밝기 {r['dL']:.1f} · 색 ΔE {r['dE']:.1f} · 밀도비 {r['dens']:.2f} · 튐 {r['jump']:.2f}"
    s += f" · 상관 {r['corr']:.2f}" if r['corr'] is not None else ' · 상관 -'
    if r['sky'] is not None: s += f" · 지평선차 {r['sky'] * 100:.1f}%"
    return s


# ───────────────────────── 3. 미리보기 ─────────────────────────
def _font(sz):
    for f in ('/usr/share/fonts/truetype/nanum/NanumGothic.ttf', '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc',
              'C:/Windows/Fonts/malgun.ttf', '/System/Library/Fonts/AppleSDGothicNeo.ttc'):
        if os.path.exists(f): return ImageFont.truetype(f, sz)
    return ImageFont.load_default()

def preview(imgs, d, results, title, out, max_len=2400):
    ims = [Image.fromarray(i.astype(np.uint8)) for i in imgs]
    if d == 'h':
        hh = min(i.height for i in ims); ims = [i.resize((int(i.width * hh / i.height), hh)) for i in ims]
        canvas = Image.new('RGB', (sum(i.width for i in ims), hh), 'white'); x = 0; cuts = []
        for i in ims: canvas.paste(i, (x, 0)); x += i.width; cuts.append(x)
        cuts = cuts[:-1]
    else:
        ww = min(i.width for i in ims); ims = [i.resize((ww, int(i.height * ww / i.width))) for i in ims]
        canvas = Image.new('RGB', (ww, sum(i.height for i in ims)), 'white'); y = 0; cuts = []
        for i in ims: canvas.paste(i, (0, y)); y += i.height; cuts.append(y)
        cuts = cuts[:-1]
    sc = min(1.0, max_len / max(canvas.size)); canvas = canvas.resize((int(canvas.width * sc), int(canvas.height * sc)))
    cuts = [int(c * sc) for c in cuts]
    bar = 70; full = Image.new('RGB', (canvas.width, canvas.height + bar), (250, 248, 242)); full.paste(canvas, (0, bar))
    dr = ImageDraw.Draw(full); f1, f2 = _font(22), _font(15)
    dr.text((10, 8), title, fill=(40, 40, 46), font=f1)
    for c, r in zip(cuts, results):
        col = (70, 150, 90) if r['ok'] else (200, 70, 60)
        if d == 'h':
            for yy in range(bar, full.height, 14): dr.line([(c, yy), (c, yy + 7)], fill=col, width=2)
            dr.text((max(4, c - 150), 40), ('통과' if r['ok'] else '탈락: ' + ', '.join(r['fails'])), fill=col, font=f2)
        else:
            for xx in range(0, full.width, 14): dr.line([(xx, c + bar), (xx + 7, c + bar)], fill=col, width=2)
            dr.text((8, c + bar + 6), ('통과' if r['ok'] else '탈락: ' + ', '.join(r['fails'])), fill=col, font=f2)
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True); full.save(out)
    return out


# ───────────────────────── 4. 실행 ─────────────────────────
def check_pair(pa, pl, pb, d, out=None, title=''):
    A, L, B = load(pa), load(pl), load(pb)
    r1, r2 = seam(A, L, d), seam(L, B, d)
    if out: preview([A, L, B], d, [r1, r2], title or os.path.basename(pl), out)
    return r1, r2

def check_all(layer):
    lp = os.path.join(ART, 'bg_links.json')
    links = json.load(open(lp, encoding='utf-8'))['links'] if os.path.exists(lp) else find_links()
    layers = ['far', 'mid'] if layer == 'all' else [layer]
    md = ['# 배경 이음새 검사 결과 (seam_check.py 자동 생성)', '', '| 이음 | 층 | 방향 | 앞 → 이음 | 이음 → 뒤 | 판정 |', '|---|---|---|---|---|---|']
    nok = nbad = nmiss = 0; warns = []
    for l in links:
        a, b, d = l['first'], l['second'], l['dir']
        for lay in layers:
            pa = os.path.join(ART, 'out', f'ch{ZONE_CH[a]}', 'bg', f'ch{ZONE_CH[a]}_bg_{a}_{lay}.png')
            pb = os.path.join(ART, 'out', f'ch{ZONE_CH[b]}', 'bg', f'ch{ZONE_CH[b]}_bg_{b}_{lay}.png')
            pl = os.path.join(ART, 'out', 'link', f'bg_{a}{b}_{lay}.png')
            miss = [os.path.relpath(p, ART) for p in (pa, pl, pb) if not os.path.exists(p)]
            if miss:
                nmiss += 1; md.append(f"| {a}{b} | {lay} | {d} | — | — | ⏳ 파일 없음: {', '.join('`' + m + '`' for m in miss)} |"); continue
            out = os.path.join(ART, 'seam_preview', f'{a}{b}_{lay}.png')
            r1, r2 = check_pair(pa, pl, pb, d, out, f"{a}{b} {lay} · {l['first_name']} → {l['second_name']}")
            ok = r1['ok'] and r2['ok']; nok += ok; nbad += not ok
            md.append(f"| {a}{b} | {lay} | {d} | {fmt(r1)} | {fmt(r2)} | {'✅' if ok else '❌ ' + ', '.join(sorted(set(r1['fails'] + r2['fails'])))} |")
            if d == 'h':
                for p in (pa, pb):
                    s = skyline(lum(load(p)))
                    if abs(s - HORIZON) > 0.05: warns.append(f'- ⚠️ `{os.path.relpath(p, ART)}` 스카이라인 {s * 100:.0f}% (목표 62%)')
    md += ['', f'통과 {nok} · 탈락 {nbad} · 파일 없음 {nmiss}', ''] + sorted(set(warns))
    md += ['', '미리보기: `Art/seam_preview/` (점선 = 이음새, 초록 = 통과, 빨강 = 탈락)']
    open(os.path.join(ART, 'seam_report.md'), 'w', encoding='utf-8').write('\n'.join(md))
    print(f'통과 {nok} · 탈락 {nbad} · 파일 없음 {nmiss} → Art/seam_report.md')
    return nbad == 0


def demo():
    """가짜 패널: 능선 + 45° 해칭. 좋은 이음 패널(이어 그림)과 나쁜 이음 패널(지평선·밝기 어긋남)을 비교"""
    rng = np.random.default_rng(3); H, W = 360, 1280
    yy, xx = np.mgrid[0:H, 0:W]
    def ridge(x0, base, amp, seed):
        r = np.random.default_rng(seed); ph = r.uniform(0, 6, 3)
        x = xx[0] + x0
        return H * base + amp * (np.sin(x / 140 + ph[0]) + 0.5 * np.sin(x / 57 + ph[1]) + 0.25 * np.sin(x / 23 + ph[2]))
    def panel(x0, base, ink, paper=(244, 240, 228), seed=1, amp=18, hatch_off=0):
        img = np.ones((H, W, 3)) * np.array(paper)
        top = ridge(x0, base, amp, seed)
        ground = yy > top[None, :]
        hatch = (((xx + x0 + yy + hatch_off) % 9) < 2) & ground
        img[ground] = img[ground] * 0.93 + np.array(ink) * 0.07
        img[hatch] = img[hatch] * 0.55 + np.array(ink) * 0.45
        line = np.abs(yy - top[None, :]) < 1.6
        img[line] = np.array(ink) * 0.9 + 25
        img += rng.normal(0, 2.0, img.shape)
        return np.clip(img, 0, 255)
    ink = (60, 60, 68)
    A = panel(0, 0.62, ink, seed=1)
    B = panel(2 * W, 0.62, ink, seed=1)
    good = panel(W, 0.62, ink, seed=1)                                   # 같은 능선이 그대로 이어짐
    bad = panel(W + 300, 0.50, (90, 70, 60), paper=(232, 226, 210), seed=9, amp=30, hatch_off=4)   # 지평선·종이색·능선 어긋남
    tmp = os.path.join(ART, 'seam_preview'); os.makedirs(tmp, exist_ok=True)
    rows = []
    for name, L in (('좋은 이음 패널', good), ('나쁜 이음 패널', bad)):
        r1, r2 = seam(A, L, 'h'), seam(L, B, 'h')
        p = preview([A, L, B], 'h', [r1, r2], f'demo · {name}', os.path.join(tmp, f'demo_{"good" if L is good else "bad"}.png'), max_len=1800)
        rows.append(Image.open(p)); print(name, '|', fmt(r1), '|', fmt(r2), '|', '통과' if r1['ok'] and r2['ok'] else '탈락')
    out = Image.new('RGB', (max(r.width for r in rows), sum(r.height for r in rows) + 10), 'white')
    y = 0
    for r in rows: out.paste(r, (0, y)); y += r.height + 10
    dst = os.path.join(BASE, 'Docs', 'images', 'seam_check_demo.png'); out.save(dst)
    print('→', os.path.relpath(dst, BASE))


if __name__ == '__main__':
    ap = argparse.ArgumentParser(description='배경 이음새 검사')
    sp = ap.add_subparsers(dest='cmd', required=True)
    sp.add_parser('links')
    c = sp.add_parser('check'); c.add_argument('--layer', default='all', choices=['far', 'mid', 'all'])
    p = sp.add_parser('pair'); p.add_argument('a'); p.add_argument('link'); p.add_argument('b')
    p.add_argument('--dir', default='h', choices=['h', 'v']); p.add_argument('--out', default='seam_pair_preview.png')
    sp.add_parser('demo')
    a = ap.parse_args()
    if a.cmd == 'links': find_links()
    elif a.cmd == 'check': sys.exit(0 if check_all(a.layer) else 1)
    elif a.cmd == 'pair':
        r1, r2 = check_pair(a.a, a.link, a.b, a.dir, a.out)
        for n, r in (('앞 → 이음', r1), ('이음 → 뒤', r2)): print(n, '|', fmt(r), '|', '✅' if r['ok'] else '❌ ' + ', '.join(r['fails']))
        print('미리보기 →', a.out); sys.exit(0 if r1['ok'] and r2['ok'] else 1)
    elif a.cmd == 'demo': demo()
