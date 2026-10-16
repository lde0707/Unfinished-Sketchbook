"""Package whole-body imagegen drawings. No rigging, limb warping or morphing."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, hashlib, shutil

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/animations/player-c-v5'
SIZE = 512
GROUND = 448
HEAD_WIDTH = 164
SHEETS = ['keys-a-depth.png', 'between-a-depth.png', 'keys-b-depth.png', 'between-b.png']
# Intended foot clearance, in output pixels. Contact poses register to GROUND.
CLEARANCE = [6, 14, 19, 9, 3, 0, 0, 0, 9, 0, 26, 8, 0, 0, 0, 0]

def solid_bounds(im):
    return im.getchannel('A').point(lambda x: 255 if x >= 128 else 0).getbbox()

def head_measure(cell):
    """Head is the isolated upper silhouette; find its widest upper-half scanline."""
    a = cell.getchannel('A')
    bounds = solid_bounds(cell)
    top = bounds[1]
    rows = []
    for y in range(top + 30, top + round(cell.width * .25)):
        row = a.crop((0, y, cell.width, y + 1)).point(lambda x: 255 if x >= 128 else 0).getbbox()
        if row:
            rows.append((row[2] - row[0], (row[0] + row[2]) / 2, y))
    width, cx, y = max(rows)
    return width, cx, top + width / 2, bounds

def main():
    (OUT / 'frames').mkdir(parents=True, exist_ok=True)
    frames, records = [], []
    sheets = [Image.open(OUT / 'sources' / name).convert('RGBA') for name in SHEETS]
    for n in range(16):
        sheet_index = (n // 8) * 2 + n % 2
        cell_index = (n % 8) // 2
        src = sheets[sheet_index]
        cw, ch = src.width // 2, src.height // 2
        x, y = (cell_index % 2) * cw, (cell_index // 2) * ch
        rect = (x, y, x + cw, y + ch)
        cell = src.crop(rect)
        source_name = SHEETS[sheet_index]
        if n in (0, 8):
            compact = Image.open(OUT / 'sources/push-pair-compact.png').convert('RGBA')
            cw, ch = compact.width // 2, compact.height
            x = (n // 8) * cw
            rect = (x, 0, x + cw, ch)
            cell = compact.crop(rect)
            source_name = 'push-pair-compact.png'
        width, head_x, head_y, bounds = head_measure(cell)
        scale = HEAD_WIDTH / width
        tx = round(354 - head_x * scale)
        ty = round(GROUND - CLEARANCE[n] - bounds[3] * scale)
        sprite = cell.resize((round(cw * scale), round(ch * scale)), Image.Resampling.LANCZOS)
        frame = Image.new('RGBA', (SIZE, SIZE))
        frame.alpha_composite(sprite, (tx, ty))
        bb = solid_bounds(frame)
        assert bb and min(bb[:2]) > 0 and max(bb[2:]) < SIZE, (n, bb)
        frame.save(OUT / 'frames' / f'{n:02}.png')
        frames.append(frame)
        records.append(dict(frame=n, sourceSheet=source_name, sourceRect=rect,
            warriorFrame=(n//2)+1, inbetween=bool(n%2), nextWarriorFrame=((n//2+1)%8)+1,
            scale=scale, translation=[tx,ty], headWidth=width, headCenter=[354,round(head_y*scale+ty)],
            bounds=bb, footClearance=CLEARANCE[n], durationMs=40))
    atlas = Image.new('RGBA',(SIZE*4,SIZE*4))
    proof = Image.new('RGB',(320*4,340*4),'#23272c')
    d = ImageDraw.Draw(proof)
    rendered=[]
    for n, frame in enumerate(frames):
        atlas.alpha_composite(frame, ((n%4)*SIZE,(n//4)*SIZE))
        x,y=(n%4)*320,(n//4)*340
        small=frame.resize((320,320),Image.Resampling.LANCZOS)
        proof.paste(small,(x,y),small)
        d.line((x+15,y+280,x+305,y+280), fill='#54585e')
        d.text((x+18,y+315),f'{n+1:02} / '+('KEY '+str(n//2+1) if n%2==0 else 'INBETWEEN'),fill='#d9d1c3')
        bg=Image.new('RGBA',frame.size,'#202429'); bg.alpha_composite(frame)
        rendered.append(bg.convert('RGB'))
    atlas.save(OUT/'run-atlas.png')
    proof.save(OUT/'run-contact-sheet.jpg',quality=94)
    palette=rendered[0].quantize(colors=240)
    gif=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in rendered]
    gif[0].save(OUT/'run-preview.gif',save_all=True,append_images=gif[1:],duration=40,loop=0,disposal=2,optimize=False)
    rendered[0].save(OUT/'run-pose.png')
    # Preserve original local comparison frames, excluded from redistribution.
    refs=OUT/'reference'; refs.mkdir(exist_ok=True)
    raw=ROOT/'Art/inbox/warrior-motion-reference/original/Warrior/Individual Sprite/Run'
    source_records=[]
    for n in range(8):
        p=raw/f'Warrior_Run_{n+1}.png'
        if p.exists():
            shutil.copy2(p, refs/f'{n:02}.png')
            source_records.append(dict(frame=n+1,size=Image.open(p).size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    data=dict(frameCount=16,fps=25,loopMs=640,cellSize=SIZE,columns=4,rows=4,
        pivotNormalized=[354/SIZE,1-GROUND/SIZE],groundY=GROUND,
        method='16 whole-body drawings: 8 Warrior-referenced keys + 8 newly generated inbetweens. Uniform scaling and translation only.',
        sourceFrames=source_records,frames=records)
    (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
    hashes=[hashlib.sha256(f.tobytes()).hexdigest() for f in frames]
    report=dict(frameCount=len(frames),uniqueFrames=len(set(hashes)),transparent=True,clippedSolidSilhouettes=0,
        headWidthsNormalizedPx=HEAD_WIDTH,frameSizes=[SIZE,SIZE],loopMs=640,
        maxHeadYDeltaPx=max(abs(records[(i+1)%16]['headCenter'][1]-r['headCenter'][1]) for i,r in enumerate(records)),
        note='Packaging checks are not a claim of perfect drawing continuity or Unity foot-speed synchronization.')
    (OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report));print('headY', [r['headCenter'][1] for r in records])

if __name__=='__main__':main()
