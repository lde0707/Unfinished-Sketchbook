"""
방 장식용 위치 참고(컨트롤) 이미지 생성기 (1920×1080) — 장식이 땅 가장자리에 붙고 공중·발판을 비우도록

사용법:  python Tools/make_controls.py
입력:    Prototype/world_map_data.js (1·2장 통합 지도)
결과:    Art/control/r1_01.png … r2_26.png, border_1_2.png
색 규칙 (AI가 읽는 약속):
  짙은 회색 = 단단한 땅·벽 / 밝은 종이색 = 빈 공간 / 중간 회색 가는 막대 = 통과 발판
  검보라 = 위험(잉크·늪) / 파랑 = 물 / 그 밖의 색 = 특수 블록 (모양 유지)
방 하나 + 주변 3타일 여유를 16:9로 맞춰 자름 → 이웃 방과 자연스럽게 겹침
네모 타일 모양을 그대로 주면 AI가 네모·도트처럼 그려서, 모서리를 둥글게 하고 가장자리를 살짝 흔들어 부드러운 곡선으로 줌
(통과 발판 높이는 그대로 유지)
"""
import re, json, os
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); BASE = os.path.join(HERE, '..')
src = open(os.path.join(BASE, 'Prototype', 'world_map_data.js'), encoding='utf-8').read()
W = int(re.search(r'W=(\d+)', src).group(1)); H = int(re.search(r'H=(\d+)', src).group(1))
rle = re.search(r"MAPRLE='([^']*)'", src).group(1)
ROOMS = json.loads(re.search(r'const ROOMS=(\[.*?\]);\n', src, re.S).group(1))
g = []
for s in rle.split(','):
    v, n = s.split('.'); g += [int(v, 36)] * int(n, 36)
COL = {0: (236, 236, 232), 1: (74, 74, 78), 2: (190, 120, 115), 3: (40, 32, 56), 4: (150, 150, 156), 5: (110, 140, 100),
       6: (150, 140, 120), 7: (150, 120, 80), 8: (205, 175, 120), 9: (90, 120, 170), 13: (225, 195, 100), 14: (150, 120, 80),
       15: (232, 160, 96), 16: (226, 212, 168), 18: (175, 135, 175)}
THIN = {4, 6, 15}
OUT = os.path.join(BASE, 'Art', 'control'); os.makedirs(OUT, exist_ok=True)
from scipy.ndimage import gaussian_filter, map_coordinates
import numpy as np
SOLID = {1}                      # 단단한 땅·벽 (짙은 회색)
SPECIAL = [2, 3, 5, 7, 8, 9, 13, 14, 16, 18]  # 특수 블록·위험·물 (각자 색)
_rng = np.random.default_rng(7)
_noise = gaussian_filter(_rng.normal(0, 1, (2, 540, 960)), (0, 18, 18))
_noise = _noise / np.abs(_noise).max()

def render(cx, cy, w, h, name):
    # 16:9로 맞추기
    w, h = max(w, h * 16 / 9), max(h, w * 9 / 16)
    x0, y0 = cx - w / 2, cy - h / 2
    S = 1920 / w
    # 픽셀마다 어느 타일인지 (네모 → 부드러운 곡선으로 바꾸기 위해 실수 좌표 사용)
    yy, xx = np.mgrid[0:1080, 0:1920].astype(np.float32)
    # 유기적인 흔들림: 가장자리를 최대 0.22타일까지 부드럽게 밀고 당김 (발판 높이는 따로 그려서 영향 없음)
    amp = 0.22 * S
    dx = np.kron(_noise[0], np.ones((2, 2)))[:1080, :1920] * amp
    dy = np.kron(_noise[1], np.ones((2, 2)))[:1080, :1920] * amp
    tx = np.floor(x0 + (xx + dx) / S).astype(int); ty = np.floor(y0 + (yy + dy) / S).astype(int)
    inside = (tx >= 0) & (tx < W) & (ty >= 0) & (ty < H)
    G = np.array(g, dtype=np.int16).reshape(H, W)
    T = np.where(inside, G[np.clip(ty, 0, H - 1), np.clip(tx, 0, W - 1)], 1)
    img = np.zeros((1080, 1920, 3), np.float32); img[:] = COL[0]
    sig = 0.35 * S   # 모서리 둥글게
    def paint(mask, col):
        m = gaussian_filter(mask.astype(np.float32), sig)
        a = np.clip((m - 0.5) * 6 + 0.5, 0, 1)[..., None]   # 부드럽게 안티앨리어싱된 가장자리
        img[:] = img * (1 - a) + np.array(col, np.float32) * a
    paint(np.isin(T, list(SOLID) + [6, 14, 7]) & ~np.isin(T, [6]), COL[1])
    for t in SPECIAL:
        if (T == t).any(): paint(T == t, COL.get(t, (120, 120, 120)))
    im = Image.fromarray(img.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(im)
    # 통과 발판: 둥근 끝 막대 (높이는 정확히 유지)
    for ty_ in range(int(y0) - 1, int(y0 + h) + 2):
        tx_ = int(x0) - 1
        while tx_ < int(x0 + w) + 2:
            t = 0 if not (0 <= tx_ < W and 0 <= ty_ < H) else g[ty_ * W + tx_]
            if t in THIN:
                st = tx_
                while tx_ < int(x0 + w) + 2 and 0 <= tx_ < W and g[ty_ * W + tx_] == t: tx_ += 1
                px0, px1, py = (st - x0) * S, (tx_ - x0) * S, (ty_ - y0) * S
                d.rounded_rectangle([px0 + 2, py, px1 - 2, py + S * 0.38], radius=int(S * 0.18), fill=COL[t])
            else: tx_ += 1
    im.save(os.path.join(OUT, name))
n = 0
for r in ROOMS:
    reg = r.get('r', 1); m = 3
    render(r['x'] + r['w'] / 2, r['y'] + r['h'] / 2, r['w'] + 2 * m, r['h'] + 2 * m, f"r{reg}_{r['n']:02d}.png"); n += 1
render(280, 28, 34, 20, 'border_1_2.png'); n += 1
print('컨트롤 이미지', n, '장 →', OUT)
