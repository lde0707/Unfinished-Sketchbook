"""
통일 후처리 (Unify Pass) — AI로 뽑은 모든 그림을 '같은 게임'으로 맞추는 마지막 단계

프롬프트만으로는 조명·질감·색이 조금씩 새어 나와. 그래서 채택한 그림은 전부
이 스크립트를 **같은 설정으로** 통과시켜. 모든 에셋이 같은 종이 결·같은 채도 상한·같은 선 색을 갖게 됨.

사용법:
  python Tools/unify_pass.py <입력 png 또는 폴더> --ch 1 --layer play [--out 결과폴더]
    --ch     : 1~5 (장)  · 0 = 공용 (주인공 등, 1장 값 사용)
    --layer  : play(플레이·캐릭터·구조물) / mid(중경) / far(먼 배경) / fore(전경) / ui

처리 순서 (고정, 바꾸지 말 것):
  1. 채도 상한 (장별)          → 원색이 튀는 것 방지
  2. 밝은 종이 영역 → 장 종이색 → 종이색 통일
  3. 가장 어두운 선 → 장 선색   → 외곽선 색 통일 (순흑 제거)
  4. 레이어 대비 (먼 배경일수록 종이색 쪽으로) → 깊이 규칙 통일
  5. 공용 종이 결 텍스처 곱하기 (모든 에셋이 '같은 종이' 위에)
  6. 방향성 조명·하이라이트 눌러주기 (너무 밝은 반점 평탄화)
"""
import sys, os, glob, argparse
import numpy as np
from PIL import Image

CH = {  # 종이색, 선색, 채도 상한
    1: ((237, 237, 232), (37, 39, 44), 0.30),
    2: ((241, 236, 227), (122, 74, 48), 0.55),
    3: ((228, 236, 242), (78, 94, 120), 0.50),
    4: ((244, 241, 234), (58, 58, 68), 0.55),
    5: ((216, 212, 204), (20, 18, 26), 0.45),
}
LAYER = {'play': 0.0, 'mid': 0.45, 'far': 0.8, 'fore': 0.25, 'ui': 0.0}
GRAIN_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'paper_grain.png')

def grain(size):
    if not os.path.exists(GRAIN_PATH):  # 고정 시드로 한 번 만들고 계속 같은 걸 씀
        rng = np.random.default_rng(20261015); n = rng.normal(0, 1, (512, 512))
        from scipy.ndimage import gaussian_filter
        g = gaussian_filter(n, 1.2) * 0.6 + gaussian_filter(n, 6) * 0.4
        g = (g - g.min()) / (g.max() - g.min())
        Image.fromarray((200 + g * 55).astype(np.uint8)).save(GRAIN_PATH)
    t = np.array(Image.open(GRAIN_PATH)).astype(np.float32) / 255
    reps = (size[1] // t.shape[0] + 1, size[0] // t.shape[1] + 1)
    return np.tile(t, reps)[:size[1], :size[0]]

def rgb2hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]; mx = a.max(-1); mn = a.min(-1); d = mx - mn
    s = np.where(mx > 0, d / np.maximum(mx, 1e-6), 0); return mx, s

def process(path, ch, layer, out):
    im = Image.open(path).convert('RGBA'); a = np.array(im).astype(np.float32) / 255
    rgb, al = a[..., :3], a[..., 3:]
    paper, ink, smax = CH[ch or 1]; paper = np.array(paper) / 255; ink = np.array(ink) / 255
    # 1. 채도 상한
    v, s = rgb2hsv(rgb); gray = rgb.mean(-1, keepdims=True)
    k = np.where(s > smax, smax / np.maximum(s, 1e-6), 1.0)[..., None]
    rgb = gray + (rgb - gray) * k
    # 2. 종이 영역 → 장 종이색
    v, s = rgb2hsv(rgb); pm = np.clip((v - 0.80) / 0.12, 0, 1) * np.clip((0.18 - s) / 0.18, 0, 1)
    rgb = rgb * (1 - pm[..., None]) + paper * pm[..., None]
    # 3. 어두운 선 → 장 선색
    lm = np.clip((0.28 - v) / 0.18, 0, 1)[..., None]
    rgb = rgb * (1 - lm) + ink * lm
    # 4. 레이어 대비
    f = LAYER[layer]; rgb = rgb * (1 - f) + paper * f
    # 6. 하이라이트 평탄화 (종이색보다 밝은 곳은 종이색으로)
    rgb = np.minimum(rgb, np.maximum(paper, rgb * 0 + paper)) if layer != 'ui' else rgb
    # 5. 종이 결 곱하기 (모든 에셋 같은 텍스처, 세기 고정)
    gr = grain(im.size)[..., None]; rgb = rgb * (0.94 + 0.06 * gr / gr.mean())
    o = np.concatenate([np.clip(rgb, 0, 1), al], -1)
    os.makedirs(out, exist_ok=True)
    Image.fromarray((o * 255).astype(np.uint8)).save(os.path.join(out, os.path.basename(path)))

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('src'); ap.add_argument('--ch', type=int, default=1)
    ap.add_argument('--layer', default='play', choices=list(LAYER)); ap.add_argument('--out', default=None); a = ap.parse_args()
    files = sorted(glob.glob(os.path.join(a.src, '*.png'))) if os.path.isdir(a.src) else [a.src]
    out = a.out or os.path.join(os.path.dirname(files[0]), '_unified')
    for f in files: process(f, a.ch, a.layer, out)
    print(f'통일 후처리 {len(files)}장 → {out} (장 {a.ch} · 레이어 {a.layer})')

if __name__ == '__main__': main()
