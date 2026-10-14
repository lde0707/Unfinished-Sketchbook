"""
아트 자동 검증 파이프라인 (art_check) — AI가 뽑은 그림을 넣기만 하면 자동으로 검사·후처리·분류

흐름
  Art/inbox/ 에 그림을 넣음 (파일 이름 = 에셋 ID, 예: R1-09.png, B1-A1-wall_a_v2.png, TZ1-B-fill_a 3.png)
     │
     ├─ ① 이름으로 에셋 찾기 (Art/asset_catalog.json)
     ├─ ② 원본 보관 → Art/raw/<ID>/
     ├─ ③ 생김새 검사 (원본 기준): 비율 · 해상도 · 픽셀/도트 · 계단 가장자리 · 반복 무늬 · 투명 배경
     ├─ ④ 통일 후처리 unify_pass (장·레이어 자동 선택)
     ├─ ⑤ 색 검사 (후처리 결과): 채도 상한 · 순흑 선
     ├─ ⑥ 통과 → Art/out/<목록의 파일 경로>   /  탈락 → Art/rejected/<ID>/ + 이유.txt
     └─ ⑦ 배경이면 이음새 검사 (seam_check) — 양쪽 패널이 다 있으면 자동
  애니메이션: Art/inbox/anim/<이름>/ 폴더째 넣으면 anim_check로 검사 → Art/out/anim/<이름>/

명령
  python Tools/art_check.py watch            폴더 감시 (넣는 즉시 자동 처리, Ctrl+C로 끝). Windows: Tools/art_watch.bat 더블클릭
  python Tools/art_check.py run              inbox 를 한 번만 처리
  python Tools/art_check.py audit            Art/out 전체 다시 검사 (후처리·이동 없음). GitHub Actions가 푸시마다 실행
  python Tools/art_check.py file 그림.png [--id R1-09]   파일 하나만 검사 (이동 없음)
  python Tools/art_check.py approve R1-09    사람 확인 끝 표시 (§7-2 한 화면 검수 후)
  python Tools/art_check.py demo             가짜 그림으로 검사기가 제대로 잡는지 시연

결과
  Art/check_report.md   진행 현황 (전체 몇 개 중 몇 개 통과·확인) + 최근 결과 + 사람 확인 대기 목록
  Art/status.json       에셋별 상태 (auto_pass / approved / rejected) 기록
"""
import os, re, sys, json, time, shutil, argparse, datetime, subprocess
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

HERE = os.path.dirname(os.path.abspath(__file__)); BASE = os.path.normpath(os.path.join(HERE, '..'))
ART = os.path.join(BASE, 'Art')
INBOX, RAW, OUT, REJ = (os.path.join(ART, d) for d in ('inbox', 'raw', 'out', 'rejected'))
STATUS = os.path.join(ART, 'status.json'); REPORT = os.path.join(ART, 'check_report.md')
sys.path.insert(0, HERE)
for _s in (sys.stdout, sys.stderr):
    try: _s.reconfigure(encoding='utf-8')          # Windows 콘솔에서 이모지·한글 깨짐 방지
    except Exception: pass
import unify_pass, seam_check, tile_kit

# ───────────────────────── 기준 ─────────────────────────
ALPHA_TYPES = {'EDGE', 'CORNER', 'PLATFORM', 'SPECIAL', 'DECOR', 'PROP', 'CHAR', 'POSE', 'FX', 'UI', 'BG_FORE', 'BG_SET'}
MONO_TYPES = {'TERRAIN', 'TERRAIN_LINK', 'EDGE', 'SPECIAL'}
REPEAT_TYPES = {'TERRAIN', 'TERRAIN_LINK', 'EDGE', 'DECOR', 'BG_WALL', 'BG_WALL_LINK', 'BG_WALL_TALL', 'KEY', 'SPECIAL'}
MIN_LONG = {'TERRAIN': 2048, 'TERRAIN_LINK': 2048, 'EDGE': 4096, 'BG_WALL': 2048, 'BG_WALL_LINK': 2048, 'BG_WALL_TALL': 4096, 'BG_SET': 1536,
            'BG_FORE': 3840, 'DECOR': 3840, 'KEY': 3840}          # 목표 (16 문서 §14: 화면의 2배로 그림)
MIN_LONG_DEFAULT = 1024
# 미드키 밝기 대역 (평균 밝기 0~1, 통일 후처리 뒤 기준) — 16 문서 §4-6
VALUE_BAND = {'BG_WALL': (0.55, 0.80), 'BG_WALL_LINK': (0.55, 0.80), 'BG_WALL_TALL': (0.55, 0.80),
              'TERRAIN': (0.25, 0.70), 'TERRAIN_LINK': (0.25, 0.70), 'KEY': (0.35, 0.78)}
LIM = dict(wrap=2.0, white=0.03, mono=3.0, mono_warn=5.0, relief=0.03, relief_warn=0.05, rhythm=0.55, surface=4.0, aspect=0.04, nn_err=1.5, levels=0.35, hard=0.50, hard_warn=0.35, alpha_soft=0.15,
           repeat=0.45, repeat_warn=0.30, sat_over=0.02, black=12)


def layer_of(a):
    t = a['type']
    if t in ('BG_WALL', 'BG_WALL_LINK', 'BG_WALL_TALL'): return 'far'
    if t == 'BG_SET': return 'mid'
    if t == 'BG_FORE': return 'fore'
    if t == 'UI': return 'ui'
    if t in ('CHAR', 'POSE', 'PROP', 'FX'): return 'char'      # 분필 흰색까지 허용 (주인공이 화면에서 가장 밝게)
    return 'play'


# ───────────────────────── 검사 하나하나 ─────────────────────────
def lum(rgb): return rgb[..., :3] @ np.array([0.299, 0.587, 0.114])

def center_crop(a, n=1024):
    h, w = a.shape[:2]; y, x = max(0, (h - n) // 2), max(0, (w - n) // 2)
    return a[y:y + n, x:x + n]

def chk_aspect(im, a):
    if not a.get('ar'): return None
    p, q = map(float, a['ar'].split(':')); want = p / q; got = im.width / im.height
    err = abs(got / want - 1)
    return ('비율', 'fail' if err > LIM['aspect'] else 'ok', f'{im.width}×{im.height} (목표 {a["ar"]}, 오차 {err * 100:.1f}%)')

def chk_size(im, a):
    want = MIN_LONG.get(a['type'], MIN_LONG_DEFAULT); got = max(im.size)
    st = 'ok' if got >= want else ('warn' if got >= want / 2 else 'fail')
    return ('해상도', st, f'긴 변 {got}px (목표 {want}px 이상{"" if st == "ok" else " → 일러스트용 업스케일러로 키우기"})')

def chk_nearest(arr):
    """도트 그림을 최근접 확대한 그림인지: k×k 블록 격자에 '딱 맞춘 위치'에서만 블록 안이 같은 색이고,
    격자를 반 칸 비켜 잡으면 차이가 확 커짐. (아주 부드러운 그림은 어디서 잡아도 비슷해서 걸리지 않음)"""
    c = center_crop(arr, 512).astype(np.float64)
    if c.shape[-1] == 4: c = c[..., :3] * (c[..., 3:] / 255)          # 투명한 곳은 검게 → 빈 곳이 '블록'으로 잡히지 않게
    c = c[..., :3]
    for k in range(2, 9):
        ws = []
        for oy in range(k):
            for ox in range(k):
                sub = c[oy:, ox:]; hh, ww = (sub.shape[0] // k) * k, (sub.shape[1] // k) * k
                if hh < 8 * k or ww < 8 * k: continue
                blk = sub[:hh, :ww].reshape(hh // k, k, ww // k, k, 3)
                ws.append((np.abs(blk - blk[:, :1, :, :1]).mean(), blk))
        if not ws: continue
        within, blk = min(ws, key=lambda t: t[0]); typical = float(np.median([w for w, _ in ws]))
        means = blk.mean((1, 3)); between = np.abs(np.diff(means, axis=1)).mean() + np.abs(np.diff(means, axis=0)).mean()
        if within < LIM['nn_err'] and typical > 4 * max(within, 0.25) and between > 3 * max(within, 0.3):
            return ('도트 확대', 'fail', f'{k}×{k} 블록 안은 같은 색, 블록끼리는 다름 (도트 그림을 키운 것)')
    return ('도트 확대', 'ok', '블록 패턴 없음')

def chk_colors(arr):
    """밝기 단계가 '띄엄띄엄'인지: 도트·팔레트 그림은 몇 단계만 쓰고 사이가 비어 있음.
    손그림은 (흐린 먼 배경처럼 범위가 좁아도) 범위 안의 밝기를 빈틈없이 다 씀"""
    c = center_crop(arr, 1024); L = lum(c[..., :3].astype(np.float64)).reshape(-1)
    if c.shape[-1] == 4: L = L[c[..., 3].reshape(-1) > 200]
    if len(L) < 1000: return None
    lo, hi = np.percentile(L, 1), np.percentile(L, 99)
    if hi - lo < 4: return ('밝기 단계', 'ok', '거의 한 색 (판단 생략)')
    hist = np.bincount(np.clip(L, 0, 255).astype(int), minlength=256)[int(lo):int(hi) + 1]
    occ = float((hist > 0).mean())
    return ('밝기 단계', 'fail' if occ < LIM['levels'] else 'ok', f'범위 안 밝기 {occ * 100:.0f}% 사용 (도트·팔레트 그림은 띄엄띄엄)')

def chk_hard_edges(arr):
    """강한 경계가 '한 픽셀에서 뚝' 바뀌는 비율. 손그림(안티앨리어싱)은 2px 이상에 걸쳐 바뀜"""
    L = lum(center_crop(arr, 1024).astype(np.float64))
    tot = hard = 0
    for M in (L, L.T):
        d0 = M[:, 1:-2] - M[:, :-3]; d1 = M[:, 2:-1] - M[:, 1:-2]; d2 = M[:, 3:] - M[:, 2:-1]
        strong = np.abs(d1) > 60
        tot += strong.sum(); hard += (strong & (np.abs(d0) < 6) & (np.abs(d2) < 6)).sum()
    if tot < 200: return ('계단 가장자리', 'ok', '강한 경계가 거의 없음')
    r = hard / tot
    st = 'fail' if r > LIM['hard'] else 'warn' if r > LIM['hard_warn'] else 'ok'
    return ('계단 가장자리', st, f'뚝 끊기는 경계 {r * 100:.0f}% (부드러운 손그림은 낮음)')

def chk_alpha(arr, a):
    has = arr.shape[-1] == 4 and (arr[..., 3] < 250).mean() > 0.01
    if a['type'] not in ALPHA_TYPES:
        return None
    if not has:
        return ('투명 배경', 'warn' if a['cat'].startswith('0') else 'fail', '배경이 투명하지 않음 → 배경 지우기 필요')
    al = arr[..., 3].astype(np.float64)
    edge = (gaussian_filter(al, 1.0) - al) != 0
    inner = (al > 0) & (al < 255)
    near = gaussian_filter((al > 127).astype(float), 1.5); band = (near > 0.05) & (near < 0.95)
    r = (inner & band).sum() / max(band.sum(), 1)
    return ('투명 가장자리', 'fail' if r < LIM['alpha_soft'] else 'ok', f'가장자리 반투명 픽셀 {r * 100:.0f}% (0에 가까우면 계단)')

def _small_L(arr, n=512):
    """긴 변 n으로 줄인 밝기 + 불투명 마스크 (투명한 곳은 불투명 평균 밝기로 채워서 '경계선'이 무늬로 잡히지 않게)"""
    im = Image.fromarray(arr.astype(np.uint8)); s = n / max(im.size)
    sm = np.asarray(im.resize((max(8, int(im.width * s)), max(8, int(im.height * s))), Image.BILINEAR), dtype=np.float64)
    L = lum(sm); m = sm[..., 3] > 200 if sm.shape[-1] == 4 else np.ones(L.shape, bool)
    if m.any(): L = np.where(m, L, L[m].mean())
    return L, m, s

def chk_repeat(arr, a):
    """반복 무늬(복사): 중간 크기 무늬의 자기상관에서 '0이 아닌 위치'에 날카롭고 큰 봉우리가 있으면 같은 무늬가 되풀이된 것"""
    if a['type'] not in REPEAT_TYPES: return None
    L, m, s = _small_L(arr)
    band = gaussian_filter(L, 2) - gaussian_filter(L, 16)          # 해칭 같은 아주 잔 무늬는 빼고 '모티프' 크기만
    band = np.where(m, band, 0); band -= band[m].mean() if m.any() else 0; band[~m] = 0
    cnt_r = np.maximum(m.sum(1, keepdims=True), 1); cnt_c = np.maximum(m.sum(0, keepdims=True), 1)
    band = np.where(m, band - band.sum(1, keepdims=True) / cnt_r, 0)        # 가로로 긴 지층 줄·윤곽선은 '반복'이 아님 → 행 평균 빼기
    band = np.where(m, band - band.sum(0, keepdims=True) / cnt_c, 0)        # 세로 줄도 마찬가지
    v = (band ** 2).sum()
    if v < 1e-6: return ('반복 무늬', 'ok', '무늬 없음')
    F = np.fft.rfft2(band, s=(band.shape[0] * 2, band.shape[1] * 2))
    ac = np.fft.irfft2(F * np.conj(F))[:band.shape[0], :band.shape[1]] / v
    h, w = ac.shape; yy, xx = np.mgrid[0:h, 0:w]
    ov = (h - yy) * (w - xx) / (h * w)                              # 겹치는 넓이로 보정 (가장자리 봉우리 과대평가 방지)
    ac = np.where(ov > 0.25, ac / np.maximum(ov, 1e-6), 0)
    ac[(yy < 0.08 * h) & (xx < 0.08 * w)] = 0                       # 자기 자신 근처는 제외
    pk = float(ac.max()); y, x = np.unravel_index(ac.argmax(), ac.shape)
    st = 'fail' if pk > LIM['repeat'] else 'warn' if pk > LIM['repeat_warn'] else 'ok'
    return ('반복 무늬', st, f'되풀이 강도 {pk:.2f} (간격 {x / s:.0f}, {y / s:.0f}px)')

def chk_monotone(arr, a):
    """단조로운 질감: 큰 덩어리 명암 변화(지층·진한 곳·덜 칠한 곳)가 거의 없으면 '회색 천'처럼 보임"""
    if a['type'] not in MONO_TYPES: return None
    L, m, s = _small_L(arr)
    if m.sum() < 500: return None
    big = gaussian_filter(L, max(L.shape) * 0.02)
    from scipy.ndimage import binary_erosion
    core = binary_erosion(m, iterations=int(max(L.shape) * 0.04))  # 윤곽선 근처는 빼고 속만
    if core.sum() < 300: core = m
    sd = float(big[core].std())
    st = 'fail' if sd < LIM['mono'] else 'warn' if sd < LIM['mono_warn'] else 'ok'
    return ('질감 단조로움', st, f'큰 덩어리 명암 변화 {sd:.1f} (지층·진한 띠·덜 칠한 곳이 있으면 커짐)')

def _contour(arr):
    """가장자리 띠의 윤곽선 높이(열마다). 바닥(불투명이 아래) = 위쪽 경계, 천장(불투명이 위) = 아래쪽 경계"""
    al = arr[..., 3] > 127; h = al.shape[0]
    floor = al[h // 2:].mean() > al[:h // 2].mean()
    cols = np.where(al.any(0))[0]
    if len(cols) < 50: return None, floor
    y = np.array([np.argmax(al[:, c]) if floor else h - 1 - np.argmax(al[::-1, c]) for c in cols], dtype=float)
    return y, floor

def chk_edge(arr, a):
    """가장자리 띠: ① 윤곽 높낮이가 충분한지 ② 물결이 일정한 간격으로 반복되는지 ③ 윤곽 안쪽에 진한 '표면 띠'가 있는지"""
    if a['type'] != 'EDGE' or arr.shape[-1] != 4: return []
    y, floor = _contour(arr)
    if y is None: return []
    h = arr.shape[0]; out = []
    relief = (np.percentile(y, 97) - np.percentile(y, 3)) / h
    out.append(('윤곽 높낮이', 'fail' if relief < LIM['relief'] else 'warn' if relief < LIM['relief_warn'] else 'ok',
                f'높낮이 {relief * 100:.1f}% (띠 높이 대비, 너무 평평하면 자로 그은 선처럼 보임)'))
    d = y - gaussian_filter(y, len(y) * 0.08); d -= d.mean()
    if d.std() > 0.5:
        ac = np.correlate(d, d, 'full')[len(d) - 1:] / (d ** 2).sum()
        neg = np.where(ac < 0)[0]; lo = int(neg[0]) if len(neg) else len(d)      # 첫 '0 아래' 이후부터 = 다음 물결과 비교
        per = float(ac[lo:len(d) // 3].max()) if len(d) // 3 > lo + 2 else 0
        out.append(('물결 리듬', 'fail' if per > LIM['rhythm'] else 'ok', f'같은 간격 반복 정도 {per:.2f} (일정한 물결이면 높음)'))
    L = lum(arr[..., :3].astype(np.float64)); al = arr[..., 3] > 200
    bandv, inner = [], []
    for i, c in enumerate(np.where(arr[..., 3].any(0) if False else (arr[..., 3] > 127).any(0))[0][::8]):
        y0 = int(y[min(i * 8, len(y) - 1)]); sgn = 1 if floor else -1
        a1 = [y0 + sgn * k for k in range(int(0.03 * h), int(0.12 * h))]; a2 = [y0 + sgn * k for k in range(int(0.25 * h), int(0.45 * h))]
        a1 = [r for r in a1 if 0 <= r < h and al[r, c]]; a2 = [r for r in a2 if 0 <= r < h and al[r, c]]
        if a1 and a2: bandv.append(L[a1, c].mean()); inner.append(L[a2, c].mean())
    if bandv:
        diff = float(np.mean(inner) - np.mean(bandv))
        out.append(('표면 띠', 'ok' if diff >= LIM['surface'] else 'warn', f'윤곽 바로 안쪽이 속보다 {diff:.1f} 진함 (표면 띠가 있으면 커짐)'))
    return out

def chk_wrap(arr, a):
    """4방향 이어짐: 오른쪽 끝 → 왼쪽 끝, 아래 끝 → 위 끝이 끊김 없이 이어지는지 (뒷벽·지형 채움·세로 뒷벽·이음·전경·바닥~천장 조각)"""
    t = a['type']; dirs = []
    if t in ('BG_WALL', 'TERRAIN'): dirs = ['h', 'v']
    elif t in ('BG_WALL_TALL', 'BG_FORE', 'EDGE'): dirs = ['h']
    elif t == 'TERRAIN_LINK': dirs = ['v'] if '가로' in a.get('note', '') else ['h']
    if t == 'EDGE' and a['id'].startswith('TL-'): dirs = []          # 구역 이음 띠는 양 끝이 서로 다른 구역이라 스스로 이어지지 않음
    elif t == 'BG_SET' and '↕️' in a.get('note', ''): dirs = ['v']
    elif t == 'BG_WALL_LINK':
        lp = os.path.join(ART, 'bg_links.json')
        if os.path.exists(lp):
            for l in json.load(open(lp, encoding='utf-8'))['links']:
                if a['id'] == f"BL-{l['first']}{l['second']}-wall": dirs = ['v' if l['dir'] == 'h' else 'h']
    out = []
    rgb = arr[..., :3].astype(np.float64)
    if arr.shape[-1] == 4:                                           # 투명 배경 띠는 회색 위에 얹어서 비교 (윤곽 높이까지 맞아야 이어짐)
        al = arr[..., 3:].astype(np.float64) / 255; rgb = rgb * al + 128 * (1 - al)
    for d in dirs:
        sc = tile_kit.wrap_score(rgb, d)
        out.append(('이어짐 ' + ('좌↔우' if d == 'h' else '위↔아래'), 'ok' if sc <= LIM['wrap'] else 'fail',
                    f'끝과 끝의 차이 {sc:.2f} (2.0 이하 = 이어짐, 넘으면 붙였을 때 이음새가 보임)'))
    return out

def chk_color(arr, a):
    ch = a['ch'] or 1; smax = unify_pass.CH[ch][2]
    rgb = arr[..., :3].astype(np.float64) / 255
    m = arr[..., 3] > 200 if arr.shape[-1] == 4 else np.ones(arr.shape[:2], bool)
    if m.sum() < 100: return []
    v, s = unify_pass.rgb2hsv(rgb)
    over = (s[m] > smax + 0.03).mean()
    out = [('채도 상한', 'fail' if over > LIM['sat_over'] else 'ok', f'{ch}장 상한 {smax:.2f} 넘는 픽셀 {over * 100:.1f}%')]
    Lm = lum(arr[..., :3].astype(np.float64))[m]
    if a['type'] in VALUE_BAND:                                   # 미드키: 너무 밝지도 어둡지도 않게
        lo_, hi_ = VALUE_BAND[a['type']]; mv = Lm.mean() / 255
        st = 'ok' if lo_ <= mv <= hi_ else 'fail'
        out.append(('밝기 대역', st, f'평균 밝기 {mv * 100:.0f}% (목표 {lo_ * 100:.0f}~{hi_ * 100:.0f}%' + (', 너무 밝음' if mv > hi_ else ', 너무 어두움' if mv < lo_ else '') + ')'))
    if layer_of(a) != 'ui':
        wf = (Lm > 245).mean()
        out.append(('순백 영역', 'fail' if wf > LIM['white'] else 'ok', f'순백에 가까운 픽셀 {wf * 100:.1f}% (주인공도 분필 흰색까지만)'))
    lo = np.percentile(Lm, 0.5)
    out.append(('순흑 선', 'warn' if lo < LIM['black'] else 'ok', f'가장 어두운 쪽 밝기 {lo:.0f} (순흑이면 unify_pass 확인)'))
    return out


def inspect(arr, im, a, stage='raw'):
    """stage raw = 생김새 검사, final = 색 검사, audit = 둘 다"""
    res = []
    if stage in ('raw', 'audit'):
        for r in (chk_aspect(im, a), chk_size(im, a), chk_nearest(arr), chk_colors(arr), chk_hard_edges(arr),
                  chk_alpha(arr, a), chk_repeat(arr, a), chk_monotone(arr, a)):
            if r: res.append(r)
        res += chk_edge(arr, a) + chk_wrap(arr, a)
    if stage in ('final', 'audit'):
        res += chk_color(arr, a)
    return res

def verdict(res):
    return 'fail' if any(r[1] == 'fail' for r in res) else 'warn' if any(r[1] == 'warn' for r in res) else 'ok'


# ───────────────────────── 목록 · 상태 ─────────────────────────
def catalog():
    return {a['id']: a for a in json.load(open(os.path.join(ART, 'asset_catalog.json'), encoding='utf-8'))['assets']}

def match_id(name, cat):
    stem = os.path.splitext(os.path.basename(name))[0]
    best = None
    for i in cat:      # 가장 긴 ID부터 맞춤: 'TZ1-B-fill_a_v2.png' → TZ1-B-fill_a
        if (stem == i or (stem.startswith(i) and stem[len(i)] in '_ .(')) and (best is None or len(i) > len(best)):
            best = i
    return best

def load_status():
    try: return json.load(open(STATUS, encoding='utf-8'))
    except Exception: return {}

def save_status(st): json.dump(st, open(STATUS, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

def now(): return datetime.datetime.now().strftime('%Y-%m-%d %H:%M')

ICON = {'ok': '✅', 'warn': '⚠️', 'fail': '❌'}
def res_md(res): return ' · '.join(f"{ICON[s]} {n}: {m}" for n, s, m in res)


# ───────────────────────── 처리 ─────────────────────────
def process_file(path, cat, st, unify=True):
    aid = match_id(path, cat)
    name = os.path.basename(path)
    if not aid:
        os.makedirs(os.path.join(REJ, '_unknown'), exist_ok=True)
        shutil.move(path, os.path.join(REJ, '_unknown', name))
        print(f'❓ {name}: 목록에 없는 이름 → rejected/_unknown (파일 이름을 에셋 ID로)'); return
    a = cat[aid]; stamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    os.makedirs(os.path.join(RAW, aid), exist_ok=True)
    raw = os.path.join(RAW, aid, f'{stamp}_{name}'); shutil.copy2(path, raw)
    im = Image.open(path); im.load()
    arr = np.asarray(im.convert('RGBA' if im.mode in ('RGBA', 'LA', 'P') else 'RGB'))
    res = inspect(arr, im, a, 'raw')
    if verdict(res) != 'fail' and unify:                         # 후처리 → 색 검사
        tmp = os.path.join(ART, '_tmp_unify'); unify_pass.process(path, a['ch'], layer_of(a), tmp)
        fin = os.path.join(tmp, name); arr2 = np.asarray(Image.open(fin).convert('RGBA'))
        if a['type'] not in ALPHA_TYPES: arr2 = arr2[..., :3]
        res += inspect(arr2, im, a, 'final')
    else:
        fin = path
    v = verdict(res)
    if v == 'fail':
        d = os.path.join(REJ, aid); os.makedirs(d, exist_ok=True)
        shutil.move(path, os.path.join(d, name))
        open(os.path.join(d, os.path.splitext(name)[0] + '_이유.txt'), 'w', encoding='utf-8').write(
            '\n'.join(f'{ICON[s]} {n}: {m}' for n, s, m in res) + '\n')
        st[aid] = dict(state='rejected', time=now(), file=name, checks=res)
    else:
        dst = os.path.join(OUT, a['file']); os.makedirs(os.path.dirname(dst), exist_ok=True)
        if a['type'] in ALPHA_TYPES: Image.open(fin).save(dst)
        else: Image.open(fin).convert('RGB').save(dst)
        os.remove(path)
        st[aid] = dict(state='auto_pass', time=now(), file=name, checks=res, out=a['file'])
    shutil.rmtree(os.path.join(ART, '_tmp_unify'), ignore_errors=True)
    print(f"{ICON[v]} {aid} ({name}) → {'rejected' if v == 'fail' else 'out/' + a['file']}")
    for n, s, m in res:
        if s != 'ok': print(f'    {ICON[s]} {n}: {m}')
    if v != 'fail' and a['type'] in ('BG_WALL', 'BG_WALL_LINK'):
        seam_for(aid, a, st)

def seam_for(aid, a, st):
    """이 배경이 들어간 이음 쌍 중 세 장이 다 있는 것만 검사"""
    lp = os.path.join(ART, 'bg_links.json')
    if not os.path.exists(lp): return
    for l in json.load(open(lp, encoding='utf-8'))['links']:
        x, y, d = l['first'], l['second'], l['dir']
        for lay in ('wall',):
            ids = (f'B{seam_check.ZONE_CH[x[0]]}-{x}-wall_a', f'BL-{x}{y}-wall', f'B{seam_check.ZONE_CH[y[0]]}-{y}-wall_a')
            if aid not in ids: continue
            cat = catalog(); paths = [os.path.join(OUT, cat[i]['file']) for i in ids]
            if not all(os.path.exists(p) for p in paths):
                print(f'    🔗 {x}{y} {lay}: 짝 패널이 아직 없음 (다 모이면 자동 검사)'); continue
            pv = os.path.join(ART, 'seam_preview', f'{x}{y}_{lay}.png')
            r1, r2 = seam_check.check_pair(*paths, d, pv, f"{x}{y} {lay}")
            ok = r1['ok'] and r2['ok']
            print(f"    🔗 이음새 {x}{y} {lay}: {'✅ 통과' if ok else '❌ ' + ', '.join(sorted(set(r1['fails'] + r2['fails'])))} → {os.path.relpath(pv, BASE)}")
            st.setdefault('_seams', {})[f'{x}{y}-{lay}'] = dict(ok=ok, time=now(), fails=sorted(set(r1['fails'] + r2['fails'])))

def process_anim(folder, st):
    name = os.path.basename(folder.rstrip('/\\'))
    r = subprocess.run([sys.executable, os.path.join(HERE, 'anim_check.py'), folder, '--out', os.path.join(folder, '_check')],
                       capture_output=True, text=True, encoding='utf-8')
    ok = r.returncode == 0
    dst = os.path.join(OUT if ok else REJ, 'anim', name)
    if os.path.exists(dst): shutil.rmtree(dst)
    shutil.move(folder, dst)
    st.setdefault('_anim', {})[name] = dict(ok=ok, time=now())
    print(f"{'✅' if ok else '❌'} 애니메이션 {name} → {os.path.relpath(dst, BASE)} (자세한 결과: _check/report.md)")

def stable_files():
    os.makedirs(INBOX, exist_ok=True)
    fs = [os.path.join(INBOX, f) for f in os.listdir(INBOX) if f.lower().endswith(('.png', '.webp', '.jpg', '.jpeg'))]
    s1 = {f: os.path.getsize(f) for f in fs}; time.sleep(0.8)
    return [f for f in fs if os.path.exists(f) and os.path.getsize(f) == s1[f]]     # 복사 중인 파일은 다음 차례

def run_once(unify=True):
    cat = catalog(); st = load_status(); n = 0
    for f in stable_files(): process_file(f, cat, st, unify); n += 1
    ad = os.path.join(INBOX, 'anim')
    if os.path.isdir(ad):
        for d in sorted(os.listdir(ad)):
            p = os.path.join(ad, d)
            if os.path.isdir(p) and any(x.endswith('.png') for x in os.listdir(p)): process_anim(p, st); n += 1
    if n: save_status(st); write_report(st, cat)
    return n

def audit():
    cat = catalog(); st = load_status(); bad = 0; rows = []
    for aid, a in cat.items():
        p = os.path.join(OUT, a['file'])
        if not os.path.exists(p): continue
        im = Image.open(p); im.load(); arr = np.asarray(im.convert('RGBA' if im.mode in ('RGBA', 'LA', 'P') else 'RGB'))
        res = inspect(arr, im, a, 'audit'); v = verdict(res); bad += v == 'fail'
        rows.append((aid, v, res))
        prev = st.get(aid, {})
        st[aid] = dict(prev, state=prev.get('state') if v != 'fail' and prev.get('state') in ('approved', 'auto_pass') else ('auto_pass' if v != 'fail' else 'rejected'),
                       time=prev.get('time', now()), checks=res, out=a['file'])
    seam_ok = seam_check.check_all('all') if os.path.exists(os.path.join(ART, 'bg_links.json')) else True
    save_status(st); write_report(st, cat, audit_rows=rows)
    print(f'검사 {len(rows)}개 · 탈락 {bad} · 이음새 {"✅" if seam_ok else "❌"} → {os.path.relpath(REPORT, BASE)}')
    return bad == 0 and seam_ok


# ───────────────────────── 보고서 ─────────────────────────
def write_report(st, cat, audit_rows=None):
    total = len(cat)
    states = [st.get(i, {}).get('state') for i in cat]
    ap, au, rj = states.count('approved'), states.count('auto_pass'), states.count('rejected')
    bycat = {}
    for i, a in cat.items():
        c = bycat.setdefault(a['cat'], [0, 0, 0]); c[0] += 1
        s = st.get(i, {}).get('state'); c[1] += s in ('auto_pass', 'approved'); c[2] += s == 'approved'
    md = ['# 아트 자동 검증 현황 (art_check.py 자동 생성)', '', f'갱신: {now()}', '',
          f'| 전체 | 자동 통과 | 사람 확인 끝 | 탈락 (다시 뽑기) | 남음 |', '|---|---|---|---|---|',
          f'| {total} | {au + ap} | {ap} | {rj} | {total - au - ap - rj} |', '',
          '## 분류별', '| 분류 | 전체 | 자동 통과 | 확인 끝 | 진행 |', '|---|---|---|---|---|']
    for c, (n, p, ok) in bycat.items():
        bar = '█' * round(10 * p / n) + '░' * (10 - round(10 * p / n))
        md.append(f'| {c} | {n} | {p} | {ok} | `{bar}` |')
    wait = [i for i in cat if st.get(i, {}).get('state') == 'auto_pass']
    md += ['', f'## 👀 사람 확인 대기 ({len(wait)}) — 19 문서 §7-2 한 화면 검수 후 `python Tools/art_check.py approve <ID>`', '']
    md += [f"- `{i}` → `Art/out/{cat[i]['file']}`" + (f" · {res_md([r for r in st[i]['checks'] if r[1] != 'ok'])}" if any(r[1] != 'ok' for r in st[i]['checks']) else '') for i in wait[:80]]
    rej = [i for i in cat if st.get(i, {}).get('state') == 'rejected']
    md += ['', f'## ❌ 탈락 ({len(rej)}) — `Art/rejected/<ID>/` 에 이유 파일', '']
    md += [f"- `{i}` ({st[i].get('file', '')}) · {res_md([r for r in st[i]['checks'] if r[1] == 'fail'])}" for i in rej]
    if st.get('_seams'):
        md += ['', '## 🔗 배경 이음새', ''] + [f"- {k}: {'✅' if v['ok'] else '❌ ' + ', '.join(v['fails'])} ({v['time']})" for k, v in sorted(st['_seams'].items())]
    if st.get('_anim'):
        md += ['', '## 🎞️ 애니메이션', ''] + [f"- {k}: {'✅' if v['ok'] else '❌'} ({v['time']})" for k, v in sorted(st['_anim'].items())]
    if audit_rows is not None:
        md += ['', '## 전체 재검사 (audit)', '', '| ID | 판정 | 내용 |', '|---|---|---|']
        md += [f'| {i} | {ICON[v]} | {res_md([r for r in res if r[1] != "ok"]) or "모두 통과"} |' for i, v, res in audit_rows]
    open(REPORT, 'w', encoding='utf-8').write('\n'.join(md) + '\n')


# ───────────────────────── 시연 ─────────────────────────
def demo():
    """가짜 그림 6장으로 검사기가 제대로 잡는지 확인 (실제 Art 폴더는 건드리지 않음)"""
    rng = np.random.default_rng(1); cat = catalog()
    def hand_terrain(n=1024):
        yy, xx = np.mgrid[0:n, 0:n].astype(float)
        warp = gaussian_filter(rng.normal(0, 1, (n, n)), 40) * 900
        dens = gaussian_filter(rng.normal(0, 1, (n, n)), 60); dens = (dens - dens.min()) / (dens.max() - dens.min())
        ph = (xx + yy + warp) % 11
        stroke = np.clip(1 - np.abs(ph - 5) / (1.2 + 1.5 * dens), 0, 1)
        stroke *= gaussian_filter(rng.random((n, n)), 3) > 0.45
        big = gaussian_filter(rng.normal(0, 1, (n, n)), n * 0.12); big = big / np.abs(big).max()   # 큰 덩어리 (진한 곳·덜 칠한 곳)
        strata = np.exp(-((yy - n * 0.62 - 40 * np.sin(xx / 170)) / (n * 0.03)) ** 2)                  # 희미한 지층 줄 1개
        L = 236 - 120 * gaussian_filter(stroke, 0.8) * (0.4 + 0.6 * dens) + 22 * big - 18 * strata
        L += rng.normal(0, 3, (n, n))
        return np.clip(np.stack([L, L, L * 0.97], -1), 0, 255).astype(np.uint8)
    good = hand_terrain()
    tiled = np.tile(hand_terrain(256), (4, 4, 1))
    small = lum(np.asarray(Image.fromarray(good).resize((128, 128), Image.BILINEAR), dtype=np.float64))
    lv = np.digitize(small, np.percentile(small, [25, 50, 75])); pal = np.array([70, 130, 185, 236])[lv]
    pixel = np.asarray(Image.fromarray(np.stack([pal] * 3, -1).astype(np.uint8)).resize((1024, 1024), Image.NEAREST))
    from PIL import ImageDraw
    def lines(scale):
        im = Image.new('RGB', (1024 * scale, 1024 * scale), (236, 234, 228)); d = ImageDraw.Draw(im); r = np.random.default_rng(5)
        for _ in range(60):
            x0, y0 = r.integers(0, 1024, 2) * scale; d.line([(x0, y0), (x0 + r.integers(-300, 300) * scale, y0 + r.integers(-300, 300) * scale)],
                                                           fill=(50, 50, 56), width=3 * scale)
        return np.asarray(im.resize((1024, 1024), Image.LANCZOS) if scale > 1 else im)
    yy, xx = np.mgrid[0:1024, 0:1024]
    blob = ((xx - 512) ** 2 / 300 ** 2 + (yy - 520) ** 2 / 360 ** 2) < 1
    soft = gaussian_filter(blob.astype(float), 2.5)
    def rgba(alpha):
        c = np.zeros((1024, 1024, 4), np.uint8); c[..., :3] = good; c[..., 3] = alpha; return c
    prop_soft, prop_hard = rgba((soft * 255).astype(np.uint8)), rgba((blob * 255).astype(np.uint8))
    cases = [('손그림 지형 · 큰 변화 있음', 'TZ1-A-fill_a', good), ('같은 조각 4×4 반복', 'TZ1-A-fill_b', tiled),
             ('도트 그림을 키운 것', 'TZ1-A-fill_c', pixel), ('부드러운 선 (안티앨리어싱)', 'G1', lines(4)),
             ('계단 선 (안티앨리어싱 없음)', 'G1', lines(1)), ('소품 · 부드러운 투명 가장자리', 'P-save_statue-1', prop_soft),
             ('소품 · 계단 투명 가장자리', 'P-save_statue-1', prop_hard), ('소품 · 배경 안 지움', 'P-save_statue-1', good)]
    tiles = []; f = seam_check._font(18)
    for name, aid, arr in cases:
        a = dict(cat[aid]); im = Image.fromarray(arr)
        res = [r for r in inspect(arr, im, a, 'raw') if r[0] not in ('해상도', '비율') and not r[0].startswith('이어짐')]
        v = verdict(res); print(f'{ICON[v]} {name} [{aid}]'); [print(f'    {ICON[s]} {n}: {m}') for n, s, m in res if s != 'ok' or n in ('반복 무늬', '계단 가장자리', '투명 가장자리')]
        th = Image.new('RGB', (300, 380), (250, 248, 242)); bg = Image.new('RGBA', im.size, (250, 248, 242, 255))
        shown = Image.alpha_composite(bg, im.convert('RGBA')) if arr.shape[-1] == 4 else im
        if '투명 가장자리' in name: crop = shown.crop((770, 470, 850, 550))          # 소품 오른쪽 가장자리 확대
        elif '도트' in name or '선' in name: crop = shown.crop((448, 448, 576, 576))
        else: crop = shown
        th.paste(crop.convert('RGB').resize((280, 280), Image.NEAREST if crop is not shown else Image.BILINEAR), (10, 40))
        d = ImageDraw.Draw(th); col = {'ok': (70, 150, 90), 'warn': (210, 150, 40), 'fail': (200, 70, 60)}[v]
        d.text((10, 10), name, fill=(40, 40, 46), font=f)
        bad = [n for n, s, m in res if s == 'fail']
        d.text((10, 330), '통과' if v == 'ok' else '탈락: ' + ', '.join(bad[:2]) + (' 외' if len(bad) > 2 else '') if v == 'fail' else '경고', fill=col, font=f)
        d.rectangle([0, 0, 299, 379], outline=col, width=3); tiles.append(th)
    sheet = Image.new('RGB', (310 * 4 - 10, 390 * 2 - 10), 'white')
    for i, t in enumerate(tiles): sheet.paste(t, ((i % 4) * 310, (i // 4) * 390))
    dst = os.path.join(BASE, 'Docs', 'images', 'art_check_demo.png'); sheet.save(dst); print('→', os.path.relpath(dst, BASE))


# ───────────────────────── 실행 ─────────────────────────
if __name__ == '__main__':
    ap = argparse.ArgumentParser(description='아트 자동 검증')
    sp = ap.add_subparsers(dest='cmd', required=True)
    w = sp.add_parser('watch'); w.add_argument('--every', type=float, default=3); w.add_argument('--no-unify', action='store_true')
    r = sp.add_parser('run'); r.add_argument('--no-unify', action='store_true')
    sp.add_parser('audit')
    f = sp.add_parser('file'); f.add_argument('path'); f.add_argument('--id', default=None)
    p = sp.add_parser('approve'); p.add_argument('ids', nargs='+')
    sp.add_parser('demo')
    a = ap.parse_args()
    for d in (INBOX, os.path.join(INBOX, 'anim'), RAW, OUT, REJ): os.makedirs(d, exist_ok=True)
    if a.cmd == 'watch':
        print(f'👀 {os.path.relpath(INBOX, BASE)} 감시 중 (그림 이름 = 에셋 ID). 끝내려면 Ctrl+C')
        try:
            while True: run_once(not a.no_unify); time.sleep(a.every)
        except KeyboardInterrupt: print('감시 끝')
    elif a.cmd == 'run':
        n = run_once(not a.no_unify); print(f'처리 {n}개 → {os.path.relpath(REPORT, BASE)}' if n else 'inbox 가 비어 있음')
    elif a.cmd == 'audit': sys.exit(0 if audit() else 1)
    elif a.cmd == 'file':
        cat = catalog(); aid = a.id or match_id(a.path, cat)
        if not aid: sys.exit('목록에 없는 이름. --id 로 지정')
        im = Image.open(a.path); im.load(); arr = np.asarray(im.convert('RGBA' if im.mode in ('RGBA', 'LA', 'P') else 'RGB'))
        res = inspect(arr, im, cat[aid], 'audit'); print(f'{ICON[verdict(res)]} {aid}')
        for n, s, m in res: print(f'  {ICON[s]} {n}: {m}')
        sys.exit(1 if verdict(res) == 'fail' else 0)
    elif a.cmd == 'approve':
        cat = catalog(); st = load_status()
        for i in a.ids:
            if st.get(i, {}).get('state') == 'auto_pass': st[i]['state'] = 'approved'; st[i]['approved'] = now(); print(f'✅ {i} 확인 끝')
            else: print(f'⚠️ {i}: 자동 통과 상태가 아님 ({st.get(i, {}).get("state", "없음")})')
        save_status(st); write_report(st, cat)
    elif a.cmd == 'demo': demo()
