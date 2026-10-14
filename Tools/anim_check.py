"""
Unfin 애니메이션 프레임 검사기 (어니언 스킨 + 프레임 간 변화량)

사용법:
  python anim_check.py <프레임 폴더> [--fps 60] [--loop] [--smear 7,8] [--out 결과폴더]

  - 프레임: 투명 배경 PNG, 이름 순서대로 (run_000.png, run_001.png ...)
  - --loop  : 마지막 → 첫 프레임 이음새도 검사
  - --smear : 일부러 크게 바뀌는 '스미어(잔상) 프레임' 번호 (공격 순간 등). 이 프레임은 기준을 느슨하게 봄

검사 항목 (기준은 아래 LIMITS):
  1. 겹침률 IoU   : 앞 프레임과 실루엣이 얼마나 겹치는지. 낮으면 '확 바뀐' 프레임
  2. 무게중심 이동 : 몸 중심이 한 프레임에 몇 px 움직였는지
  3. 바뀐 면적    : 앞 프레임과 다른 픽셀 비율
  4. 최대 점프 거리 : 새로 생긴 픽셀이 앞 프레임 실루엣에서 얼마나 멀리 떨어져 있는지 (팔·다리 끝이 '휙' 순간이동했는지)
  5. 가속 튐      : 이동량이 앞뒤 프레임보다 갑자기 커지는지 (부드러운 가속·감속 확인)

결과물:
  - report.md          : 프레임별 수치 + 통과/경고/실패
  - onion_XXX.png      : 앞 3프레임을 색으로 겹친 어니언 스킨 (문제 프레임마다)
  - contact_sheet.png  : 전체 프레임 + 어니언 스킨 한눈에 보기
"""
import sys, os, glob, argparse, json
import numpy as np
try:
    from scipy.ndimage import distance_transform_edt as edt
except ImportError:
    edt = None
from PIL import Image, ImageDraw

# px 기준값은 '캐릭터 키 176px'일 때 값. 실제 프레임의 캐릭터 키(중앙값)에 맞춰 자동으로 비례 조정됨
REF_H = 176.0
LIMITS = {
    # 일반 프레임 (60fps 기준)
    'iou_fail': 0.55, 'iou_warn': 0.70,      # 겹침률 이 아래면 실패/경고
    'com_fail': 14.0, 'com_warn': 9.0,       # 무게중심 이동 px
    'diff_fail': 0.45, 'diff_warn': 0.30,    # 바뀐 면적 비율
    'jump_fail': 14.0, 'jump_warn': 8.0,     # 팔·다리 끝 최대 점프 거리 px (상위 2% 값)
    'jerk_ratio': 2.2,                       # 이동량이 앞뒤 평균의 몇 배를 넘으면 '튐'
    # 스미어 프레임 (공격 순간 등) 허용치
    'smear_iou_fail': 0.25, 'smear_com_fail': 40.0, 'smear_diff_fail': 0.80, 'smear_jump_fail': 60.0,
}

def load(folder):
    files = sorted(glob.glob(os.path.join(folder, '*.png')))
    if not files: sys.exit('PNG 프레임이 없어: ' + folder)
    imgs = [Image.open(f).convert('RGBA') for f in files]
    W = max(i.width for i in imgs); H = max(i.height for i in imgs)
    canv = []
    for im in imgs:
        c = Image.new('RGBA', (W, H), (0, 0, 0, 0)); c.paste(im, (0, 0)); canv.append(c)
    return files, canv

def mask(im): return np.array(im)[:, :, 3] > 40

def com(m):
    ys, xs = np.nonzero(m)
    return (xs.mean(), ys.mean()) if len(xs) else (0.0, 0.0)

def analyse(frames, loop, smear, fps):
    ms = [mask(f) for f in frames]; cs = [com(m) for m in ms]
    hs = [np.ptp(np.nonzero(m)[0]) + 1 if m.any() else 0 for m in ms]
    k = (float(np.median(hs)) / REF_H) if any(hs) else 1.0
    global LIMITS
    LIMITS = {key: (v * k if key.endswith(('com_fail', 'com_warn', 'jump_fail', 'jump_warn')) else v) for key, v in LIMITS.items()}
    pairs = list(range(1, len(frames))) + ([0] if loop else [])
    rows = []
    for i in pairs:
        a, b = ms[i - 1], ms[i]
        inter = np.logical_and(a, b).sum(); uni = np.logical_or(a, b).sum() or 1
        iou = inter / uni
        diff = np.logical_xor(a, b).sum() / uni
        d = float(np.hypot(cs[i][0] - cs[i - 1][0], cs[i][1] - cs[i - 1][1]))
        jump = 0.0
        if edt is not None:
            new = np.logical_and(b, ~a)
            if new.any():
                dist = edt(~a)[new]; jump = float(np.percentile(dist, 98))
        rows.append({'f': i, 'prev': (i - 1) % len(frames), 'iou': iou, 'diff': diff, 'com': d, 'jump': jump})
    # 가속 튐: 무게중심 이동량이 앞뒤 평균보다 급격히 큼
    for k, r in enumerate(rows):
        nb = [rows[j]['com'] for j in (k - 1, k + 1) if 0 <= j < len(rows)]
        avg = (sum(nb) / len(nb)) if nb else r['com']
        r['jerk'] = r['com'] / (avg + 1.0)
    for r in rows:
        sm = r['f'] in smear
        L = LIMITS
        fail = (r['iou'] < (L['smear_iou_fail'] if sm else L['iou_fail'])) or \
               (r['com'] > (L['smear_com_fail'] if sm else L['com_fail'])) or \
               (r['diff'] > (L['smear_diff_fail'] if sm else L['diff_fail'])) or \
               (r['jump'] > (L['smear_jump_fail'] if sm else L['jump_fail']))
        warn = (not sm) and (r['iou'] < L['iou_warn'] or r['com'] > L['com_warn'] or r['diff'] > L['diff_warn'] or r['jump'] > L['jump_warn'] or r['jerk'] > L['jerk_ratio'])
        r['state'] = '❌ 실패' if fail else ('⚠️ 경고' if warn else '✅')
        r['smear'] = sm
    return rows

def onion(frames, i, n=3):
    base = Image.new('RGBA', frames[0].size, (250, 248, 242, 255))
    tints = [(70, 130, 200), (120, 170, 220), (180, 205, 235)]
    for k in range(n, 0, -1):
        j = i - k
        if j < 0: continue
        m = mask(frames[j]); col = tints[min(k - 1, 2)]
        lay = np.zeros((*m.shape, 4), np.uint8); lay[m] = (*col, 110)
        base = Image.alpha_composite(base, Image.fromarray(lay))
    m = mask(frames[i]); lay = np.zeros((*m.shape, 4), np.uint8); lay[m] = (200, 70, 60, 170)
    base = Image.alpha_composite(base, Image.fromarray(lay))
    return Image.alpha_composite(base, frames[i])

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('folder'); ap.add_argument('--fps', type=int, default=60)
    ap.add_argument('--loop', action='store_true'); ap.add_argument('--smear', default='')
    ap.add_argument('--out', default=None); a = ap.parse_args()
    smear = {int(x) for x in a.smear.split(',') if x.strip()}
    files, frames = load(a.folder); out = a.out or os.path.join(a.folder, '_check'); os.makedirs(out, exist_ok=True)
    rows = analyse(frames, a.loop, smear, a.fps)
    nfail = sum(r['state'].startswith('❌') for r in rows); nwarn = sum(r['state'].startswith('⚠️') for r in rows)
    lines = [f'# 애니메이션 검사: {os.path.basename(os.path.abspath(a.folder))}', '',
             f'- 프레임 {len(frames)}장 · {a.fps}fps · 길이 {len(frames)/a.fps:.2f}초 · 루프 검사 {"예" if a.loop else "아니오"}',
             f'- 결과: **실패 {nfail} · 경고 {nwarn} · 통과 {len(rows)-nfail-nwarn}**', '',
             '| 프레임 | 겹침률 IoU | 바뀐 면적 | 중심 이동 px | 끝 점프 px | 가속 튐 | 스미어 | 판정 |', '|---|---|---|---|---|---|---|---|']
    for r in rows:
        lines.append(f"| {r['prev']}→{r['f']} | {r['iou']:.2f} | {r['diff']:.2f} | {r['com']:.1f} | {r['jump']:.1f} | {r['jerk']:.2f} | {'예' if r['smear'] else ''} | {r['state']} |")
        if not r['state'].startswith('✅'): onion(frames, r['f']).save(os.path.join(out, f"onion_{r['f']:03d}.png"))
    open(os.path.join(out, 'report.md'), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    # 컨택트 시트: 위 줄 = 프레임, 아래 줄 = 어니언 스킨
    W, H = frames[0].size; s = min(1.0, 96 / max(W, H)); tw, th = int(W * s), int(H * s); cols = min(16, len(frames))
    rowsN = (len(frames) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * tw, rowsN * th * 2 + 4), (250, 248, 242)); dr = ImageDraw.Draw(sheet)
    state = {r['f']: r['state'] for r in rows}
    for i, f in enumerate(frames):
        x, y = (i % cols) * tw, (i // cols) * th * 2
        bg = Image.new('RGBA', f.size, (250, 248, 242, 255)); sheet.paste(Image.alpha_composite(bg, f).resize((tw, th)).convert('RGB'), (x, y))
        sheet.paste(onion(frames, i).resize((tw, th)).convert('RGB'), (x, y + th))
        st = state.get(i, '✅'); col = (200, 60, 50) if st.startswith('❌') else (220, 160, 40) if st.startswith('⚠️') else (90, 150, 90)
        dr.rectangle([x, y, x + tw - 1, y + th * 2 - 1], outline=col, width=2); dr.text((x + 3, y + 2), str(i), fill=(40, 40, 40))
    sheet.save(os.path.join(out, 'contact_sheet.png'))
    print('\n'.join(lines[:4])); print('결과 폴더:', out)
    sys.exit(1 if nfail else 0)

if __name__ == '__main__': main()
