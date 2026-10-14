"""
통일 후처리 (Unify Pass) — AI로 뽑은 모든 그림을 '같은 게임'으로 맞추는 마지막 단계

프롬프트만으로는 조명·질감·색이 조금씩 새어 나와. 그래서 채택한 그림은 전부
이 스크립트를 **같은 설정으로** 통과시켜. 모든 에셋이 같은 종이 결·같은 채도 상한·같은 선 색을 갖게 됨.

사용법:
  python Tools/unify_pass.py <입력 png 또는 폴더> --ch 1 --layer play [--out 결과폴더]
    --ch     : 1~5 (장)  · 0 = 공용 (주인공 등, 1장 값 사용)
    --layer  : play(지형·방 장식) / char(주인공·적·소품·이펙트) / mid(중경) / far(먼 배경) / fore(전경) / ui
  python Tools/unify_pass.py --demo   → Docs/images/unify_pass_before_after.png (전·후 비교)

처리 순서 (고정, 바꾸지 말 것):
  1. 채도 상한 (장별)          → 원색이 튀는 것 방지
  2. 밝은 종이 영역 → 장 종이색 (char 레이어는 '분필 흰색') → 종이색 통일 · 미드키 (v3: 종이 명도 약 70%)
  3. 가장 어두운 선 → 장 선색   → 외곽선 색 통일 (순흑 제거)
  4. 레이어 대비 (먼 배경일수록 종이색 쪽으로) → 깊이 규칙 통일
  5. 공용 종이 결 텍스처 곱하기 (모든 에셋이 '같은 종이' 위에)
  6. 너무 밝은 곳 눌러주기: 배경·지형은 종이색 위로, 캐릭터는 분필 흰색 위로 못 올라감
     → 화면에서 가장 밝은 건 항상 주인공(분필 흰색) = 전투 중에도 바로 찾음
"""
import sys, os, glob, argparse
import numpy as np
from PIL import Image

CH = {  # 종이색, 선색, 채도 상한 — v3 미드키: 종이를 '어두운 책상 스탠드 아래' 톤으로 낮춤 (16 문서 §4-6)
    1: ((189, 188, 181), (37, 39, 44), 0.30),     # #BDBCB5
    2: ((199, 187, 168), (122, 74, 48), 0.55),    # #C7BBA8
    3: ((180, 190, 198), (78, 94, 120), 0.50),    # #B4BEC6
    4: ((201, 195, 182), (58, 58, 68), 0.55),     # #C9C3B6
    5: ((158, 154, 146), (20, 18, 26), 0.45),     # #9E9A92
}
CHALK = (240, 238, 230)   # 분필 흰색 #F0EEE6 — 주인공 몸 등 '화면에서 가장 밝은 것'의 상한
LAYER = {'play': 0.0, 'char': 0.0, 'mid': 0.45, 'far': 0.8, 'fore': 0.25, 'ui': 0.0}
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
    light = np.array(CHALK) / 255 if layer == 'char' else paper
    rgb = rgb * (1 - pm[..., None]) + light * pm[..., None]
    # 3. 어두운 선 → 장 선색
    lm = np.clip((0.28 - v) / 0.18, 0, 1)[..., None]
    rgb = rgb * (1 - lm) + ink * lm
    # 4. 레이어 대비
    f = LAYER[layer]; rgb = rgb * (1 - f) + paper * f
    # 6. 하이라이트 평탄화 (종이색보다 밝은 곳은 종이색으로)
    if layer != 'ui': rgb = np.minimum(rgb, light)
    # 5. 종이 결 곱하기 (모든 에셋 같은 텍스처, 세기 고정)
    gr = grain(im.size)[..., None]; rgb = rgb * (0.94 + 0.06 * gr / gr.mean())
    o = np.concatenate([np.clip(rgb, 0, 1), al], -1)
    os.makedirs(out, exist_ok=True)
    Image.fromarray((o * 255).astype(np.uint8)).save(os.path.join(out, os.path.basename(path)))

def demo():
    """일부러 튀게 만든 그림(순백 바탕·순흑 선·쨍한 색·흰 주인공) → 2장 설정으로 통일한 결과를 나란히"""
    from PIL import ImageDraw
    import tempfile
    W, H = 960, 540; tmp = tempfile.mkdtemp()
    bg = Image.new('RGBA', (W, H), (255, 255, 255, 255)); d = ImageDraw.Draw(bg)
    d.polygon([(0, 330), (220, 250), (420, 300), (640, 220), (960, 290), (960, 540), (0, 540)], fill=(255, 140, 30), outline=(0, 0, 0))
    d.polygon([(0, 420), (300, 380), (600, 410), (960, 370), (960, 540), (0, 540)], fill=(230, 60, 40), outline=(0, 0, 0))
    for x in range(0, 960, 14): d.line([(x, 430), (x - 40, 540)], fill=(120, 20, 10), width=2)
    d.rectangle([600, 300, 760, 320], fill=(250, 210, 0), outline=(0, 0, 0), width=4)
    d.ellipse([820, 40, 920, 140], fill=(255, 250, 120))
    ch = Image.new('RGBA', (W, H), (0, 0, 0, 0)); c = ImageDraw.Draw(ch)
    c.rounded_rectangle([452, 330, 492, 382], 12, fill=(255, 255, 255, 255), outline=(0, 0, 0, 255), width=4)
    c.ellipse([446, 286, 498, 338], fill=(255, 255, 255, 255), outline=(0, 0, 0, 255), width=4)
    c.ellipse([478, 306, 486, 314], fill=(0, 0, 0, 255))
    bg.save(os.path.join(tmp, 'bg.png')); ch.save(os.path.join(tmp, 'ch.png'))
    process(os.path.join(tmp, 'bg.png'), 2, 'play', os.path.join(tmp, 'o')); process(os.path.join(tmp, 'ch.png'), 2, 'char', os.path.join(tmp, 'o'))
    before = Image.alpha_composite(bg, ch)
    after = Image.alpha_composite(Image.open(os.path.join(tmp, 'o', 'bg.png')).convert('RGBA'), Image.open(os.path.join(tmp, 'o', 'ch.png')).convert('RGBA'))
    out = Image.new('RGB', (W * 2 + 20, H), (255, 255, 255)); out.paste(before.convert('RGB'), (0, 0)); out.paste(after.convert('RGB'), (W + 20, 0))
    dst = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Docs', 'images', 'unify_pass_before_after.png')
    out.save(dst); print('→', os.path.normpath(dst))

def main():
    if '--demo' in sys.argv: return demo()
    ap = argparse.ArgumentParser(); ap.add_argument('src'); ap.add_argument('--ch', type=int, default=1)
    ap.add_argument('--layer', default='play', choices=list(LAYER)); ap.add_argument('--out', default=None); a = ap.parse_args()
    files = sorted(glob.glob(os.path.join(a.src, '*.png'))) if os.path.isdir(a.src) else [a.src]
    out = a.out or os.path.join(os.path.dirname(files[0]), '_unified')
    for f in files: process(f, a.ch, a.layer, out)
    print(f'통일 후처리 {len(files)}장 → {out} (장 {a.ch} · 레이어 {a.layer})')

if __name__ == '__main__': main()
