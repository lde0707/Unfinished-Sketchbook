"""
이어지는 배경 만들기 도구 (tile_kit) — 뒷벽·지형 채움을 가로·세로 4방향으로 이어지게

원칙: "공통 테두리 띠"
  ① 구획마다 변형 A를 '테두리 원본'으로 먼저 만든다 (스스로 4방향 이어짐 = seamless/tiling 옵션)
  ② 변형 B·C는 A의 바깥 테두리 띠(기본 10%)를 그대로 두고 안쪽만 새로 그린다 (인페인팅)
     → A·B·C 어느 것을 옆에 붙이든 위에 쌓든 이음새가 없음 (테두리가 같으니까)
  ③ 세로 전용 뒷벽은 위·아래 띠 = 그 구획 A의 위·아래 띠, 좌우는 스스로 이어짐
  ④ 구획 사이 이음은 두 구획 A를 이어 섞은 밑그림에서 시작 → 한쪽 끝 = 앞 구획, 반대쪽 끝 = 뒤 구획
  ⚠️ 테두리를 공유하는 그림은 돌리거나 뒤집으면 이음새가 생김 → 섞기만, 돌리기·뒤집기 금지

명령
  python Tools/tile_kit.py mask  [--w 2048 --h 2048 --band 0.1]
        인페인팅 마스크 (흰색 = 새로 그릴 안쪽, 검정 = 그대로 둘 테두리 띠) → Art/tile/mask_<w>x<h>.png
  python Tools/tile_kit.py variant-base A.png [--band 0.1]
        변형 B·C용 밑그림: A의 테두리 띠는 그대로, 안쪽은 A를 반 칸 밀어 둔 것 (인페인팅 시작점) → A_base.png
  python Tools/tile_kit.py tall-base A.png --ratio 2
        세로 전용 뒷벽 밑그림: A를 세로로 ratio장 이어 붙임 (위·아래 띠가 A와 같음) → A_tall2.png + 마스크
  python Tools/tile_kit.py link-base A.png B.png --dir h|v
        구획 이음 밑그림: A → B로 서서히 섞은 그림 (h = 왼쪽 A·오른쪽 B, v = 위 A·아래 B) → link_base.png
  python Tools/tile_kit.py offset 그림.png
        반 칸씩 밀어 보기 (가운데 십자에 이음새가 보이면 안 이어지는 그림) → 그림_offset.png
  python Tools/tile_kit.py preview a.png b.png c.png [--cols 4 --rows 3]
        변형을 무작위로 섞어 깐 미리보기 (실제 게임처럼) → tile_preview.png
  python Tools/tile_kit.py prep 그림.png --id TZ1-A-edge_top [--seamless] [--out 결과.png]
        AI가 뽑은 그림(1024~1536px, 1:1·3:2·2:3·16:9)을 목록의 최종 비율로 잘라 최종 크기로 키움 (+ 이어지게)
        ※ 키우기는 Lanczos라 임시용. 최종은 일러스트용 AI 업스케일러가 더 깨끗함 (17 문서 §9)
  python Tools/tile_kit.py seamless 그림.png [--dir hv|h|v]
        타일링 옵션이 없는 AI(예: 대화형 AI의 그림 생성)로 뽑은 그림을 4방향(또는 한 방향)으로 이어지게 만듦
        (반 칸 민 그림과 가운데만 부드럽게 섞음 → 그다음 이음 자리를 인페인팅으로 다듬으면 더 좋음)
  python Tools/tile_kit.py check 그림.png [다른 변형.png ...]
        스스로 4방향 이어지는지 + 변형끼리 테두리가 같은지 숫자로 판정
"""
import os, sys, argparse
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__)); BASE = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, HERE)


def load(p): return np.asarray(Image.open(p).convert('RGB'), dtype=np.float64)
def save(a, p): Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(p); print('→', p); return p
def stem(p, tail): b, e = os.path.splitext(p); return f'{b}_{tail}.png'


def mask(w, h, band, soft=0.25):
    """흰색 = 새로 그릴 곳(안쪽), 검정 = 지킬 곳(테두리 띠). 경계는 살짝 흐리게 (띠의 soft 비율)"""
    bx, by = int(w * band), int(h * band)
    yy, xx = np.mgrid[0:h, 0:w]
    dx = np.minimum(xx, w - 1 - xx) - bx; dy = np.minimum(yy, h - 1 - yy) - by
    d = np.minimum(dx / max(1, bx * soft), dy / max(1, by * soft))
    return (np.clip(d, 0, 1) * 255).astype(np.uint8)


def variant_base(A, band):
    """안쪽은 A를 반 칸 민 그림(다른 무늬), 테두리 띠는 A 그대로 → 인페인팅 시작점"""
    h, w = A.shape[:2]
    shifted = np.roll(np.roll(A, h // 2, 0), w // 2, 1)
    m = mask(w, h, band)[..., None] / 255
    return A * (1 - m) + shifted * m, m


def tall_base(A, ratio, band):
    T = np.concatenate([A] * ratio, 0)
    h, w = T.shape[:2]; m = np.zeros((h, w), np.uint8)
    by = int(A.shape[0] * band); m[by:h - by] = 255                        # 위·아래 띠만 지킴 (좌우는 A가 이미 이어짐)
    return T, m


def link_base(A, B, d):
    if d == 'v': A, B = A.transpose(1, 0, 2), B.transpose(1, 0, 2)
    h = min(A.shape[0], B.shape[0]); w = min(A.shape[1], B.shape[1]); A, B = A[:h, :w], B[:h, :w]
    t = np.linspace(0, 1, w)[None, :, None]; t = t * t * (3 - 2 * t)        # 부드러운 S자 섞기
    C = A * (1 - t) + B * t                                                 # 두 그림 모두 위아래로 이어지니 섞은 것도 위아래로 이어짐
    return C if d == 'h' else C.transpose(1, 0, 2)


def wrap_score(A, d):
    """스스로 이어지는지: 마지막 줄과 첫 줄의 차이 ÷ 그림 안쪽 이웃 줄끼리의 평소 차이 (1 근처 = 이어짐)"""
    L = A @ np.array([0.299, 0.587, 0.114])
    if d == 'v': L = L.T
    inner = np.mean([np.abs(L[:, k] - L[:, k + 1]).mean() for k in range(5, L.shape[1] - 5, max(1, L.shape[1] // 40))])
    return float(np.abs(L[:, -1] - L[:, 0]).mean() / max(inner, 1.0))   # 1단계 미만 차이는 눈에 안 보임


def border_diff(A, B, band=0.1):
    """두 변형의 바깥 테두리 띠가 얼마나 같은지 (0 = 똑같음, 밝기 단위)"""
    h, w = min(A.shape[0], B.shape[0]), min(A.shape[1], B.shape[1]); A, B = A[:h, :w], B[:h, :w]
    m = mask(w, h, band, soft=0.0) < 128
    return float(np.abs(A - B).mean(-1)[m].mean())


def preview(files, cols, rows, size=512):
    ims = [Image.open(f).convert('RGB').resize((size, size)) for f in files]
    rng = np.random.default_rng(7); out = Image.new('RGB', (cols * size, rows * size))
    for r in range(rows):
        for c in range(cols): out.paste(ims[rng.integers(len(ims))], (c * size, r * size))
    d = ImageDraw.Draw(out)
    for c in range(1, cols): d.line([(c * size, 0), (c * size, 6)], fill=(200, 60, 50), width=2)
    for r in range(1, rows): d.line([(0, r * size), (6, r * size)], fill=(200, 60, 50), width=2)
    return out


def make_seamless(A, d='hv'):
    """반 칸 민 그림(B)의 끝은 원래 그림의 가운데라서 서로 이어짐. 가운데는 원본, 끝으로 갈수록 B를 보이게 섞음"""
    h, w = A.shape[:2]; B = A.copy()
    yy, xx = np.mgrid[0:h, 0:w]
    wx = np.ones((h, w)); wy = np.ones((h, w))
    if 'h' in d:
        B = np.roll(B, w // 2, 1); wx = np.clip(1 - np.abs(xx - w / 2) / (w / 2), 0, 1)
    if 'v' in d:
        B = np.roll(B, h // 2, 0); wy = np.clip(1 - np.abs(yy - h / 2) / (h / 2), 0, 1)
    wgt = np.clip(np.minimum(wx, wy) * 2.2, 0, 1)[..., None]                 # 가운데 넓게 원본, 끝 쪽만 B
    return A * wgt + B * (1 - wgt)


def prep(path, aid, seamless=False, out=None):
    import json
    cat = {x['id']: x for x in json.load(open(os.path.join(BASE, 'Art', 'asset_catalog.json'), encoding='utf-8'))['assets']}
    a = cat[aid]; im = Image.open(path); im.load()
    p, q = map(int, a['ar'].split(':')); want = p / q; W, H = im.size
    if W / H > want: nw = round(H * want); box = ((W - nw) // 2, 0, (W - nw) // 2 + nw, H)       # 너무 넓으면 좌우 자르기
    else: nh = round(W / want); top = (H - nh) // 2; box = (0, top, W, top + nh)                    # 너무 높으면 위아래(띠는 가운데) 자르기
    im = im.crop(box)
    fw, fh = map(int, a['final'].replace('×', 'x').split('x'))
    im = im.resize((fw, fh), Image.LANCZOS)
    if seamless:
        d = 'hv' if a['type'] in ('BG_WALL', 'TERRAIN') else 'h' if a['type'] in ('BG_WALL_TALL', 'BG_FORE', 'EDGE') else 'v' if '↕️' in a.get('note', '') else ''
        if d:
            arr = np.asarray(im.convert('RGBA'), dtype=np.float64)
            rgb = make_seamless(arr[..., :3], d); al = make_seamless(arr[..., 3:].repeat(3, -1), d)[..., :1]
            im = Image.fromarray(np.clip(np.concatenate([rgb, al], -1), 0, 255).astype(np.uint8), 'RGBA')
            if a['type'] not in ('EDGE', 'BG_FORE', 'BG_SET'): im = im.convert('RGB')
    out = out or os.path.join(os.path.dirname(os.path.abspath(path)), f'{aid}.png')
    im.save(out); print(f'→ {out} ({fw}×{fh}, 최종 비율 {a["ar"]}' + (', 이어지게 처리' if seamless else '') + ')')
    return out


if __name__ == '__main__':
    ap = argparse.ArgumentParser(description='이어지는 배경 만들기 도구'); sp = ap.add_subparsers(dest='cmd', required=True)
    m = sp.add_parser('mask'); m.add_argument('--w', type=int, default=2048); m.add_argument('--h', type=int, default=2048); m.add_argument('--band', type=float, default=0.1)
    v = sp.add_parser('variant-base'); v.add_argument('a'); v.add_argument('--band', type=float, default=0.1)
    t = sp.add_parser('tall-base'); t.add_argument('a'); t.add_argument('--ratio', type=int, default=2); t.add_argument('--band', type=float, default=0.1)
    l = sp.add_parser('link-base'); l.add_argument('a'); l.add_argument('b'); l.add_argument('--dir', default='h', choices=['h', 'v']); l.add_argument('--out', default='link_base.png')
    o = sp.add_parser('offset'); o.add_argument('img')
    p = sp.add_parser('preview'); p.add_argument('imgs', nargs='+'); p.add_argument('--cols', type=int, default=4); p.add_argument('--rows', type=int, default=3); p.add_argument('--out', default='tile_preview.png')
    c = sp.add_parser('check'); c.add_argument('imgs', nargs='+')
    pr = sp.add_parser('prep'); pr.add_argument('img'); pr.add_argument('--id', required=True); pr.add_argument('--seamless', action='store_true'); pr.add_argument('--out', default=None)
    sm = sp.add_parser('seamless'); sm.add_argument('img'); sm.add_argument('--dir', default='hv', choices=['hv', 'h', 'v'])
    a = ap.parse_args()
    if a.cmd == 'mask':
        d = os.path.join(BASE, 'Art', 'tile'); os.makedirs(d, exist_ok=True)
        Image.fromarray(mask(a.w, a.h, a.band)).save(os.path.join(d, f'mask_{a.w}x{a.h}.png')); print('→', os.path.join(d, f'mask_{a.w}x{a.h}.png'))
    elif a.cmd == 'variant-base':
        base, mk = variant_base(load(a.a), a.band); save(base, stem(a.a, 'base'))
        Image.fromarray((mk[..., 0] * 255).astype(np.uint8)).save(stem(a.a, 'base_mask')); print('→', stem(a.a, 'base_mask'))
    elif a.cmd == 'tall-base':
        T, mk = tall_base(load(a.a), a.ratio, a.band); save(T, stem(a.a, f'tall{a.ratio}')); Image.fromarray(mk).save(stem(a.a, f'tall{a.ratio}_mask')); print('→', stem(a.a, f'tall{a.ratio}_mask'))
    elif a.cmd == 'link-base':
        save(link_base(load(a.a), load(a.b), a.dir), a.out)
    elif a.cmd == 'offset':
        A = load(a.img); save(np.roll(np.roll(A, A.shape[0] // 2, 0), A.shape[1] // 2, 1), stem(a.img, 'offset'))
    elif a.cmd == 'preview':
        preview(a.imgs, a.cols, a.rows).save(a.out); print('→', a.out)
    elif a.cmd == 'prep':
        prep(a.img, a.id, a.seamless, a.out)
    elif a.cmd == 'seamless':
        save(make_seamless(load(a.img), a.dir), stem(a.img, 'seamless'))
    elif a.cmd == 'check':
        ok = True; As = [load(f) for f in a.imgs]
        for f, A in zip(a.imgs, As):
            sh, sv = wrap_score(A, 'h'), wrap_score(A, 'v'); good = sh <= 2.0 and sv <= 2.0; ok &= good
            print(f"{'✅' if good else '❌'} {os.path.basename(f)}: 좌↔우 {sh:.2f} · 위↔아래 {sv:.2f} (2.0 이하 = 이어짐)")
        for i in range(1, len(As)):
            bd = border_diff(As[0], As[i]); good = bd <= 4.0; ok &= good
            print(f"{'✅' if good else '❌'} 테두리 띠 {os.path.basename(a.imgs[0])} ↔ {os.path.basename(a.imgs[i])}: 차이 {bd:.1f} (4 이하 = 같은 테두리)")
        sys.exit(0 if ok else 1)
