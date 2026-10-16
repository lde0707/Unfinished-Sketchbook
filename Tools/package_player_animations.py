"""Package Unity-rendered animation frames; does not generate or repaint artwork."""
from pathlib import Path
import argparse
import json
import math
import shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
NAMES = ['Idle','Walk','Run','RunStart','RunStop','Jump','AirRise','AirApex','AirFall','Land','Dash','WallSlide','Turn','Attack1','Attack2','Attack3','Hurt','Death']
LABELS = ['대기','걷기','달리기','달리기 시작','멈추기','점프 시작','상승','정점','낙하','착지','대시','벽 미끄러짐','방향 전환','공격 1','공격 2','공격 3','피격','쓰러짐']
LOOPS = {'Idle','Walk','Run','AirRise','AirApex','AirFall','WallSlide'}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--frames', type=Path, default=ROOT/'Builds/PlayerAnimationFrames')
    args = parser.parse_args()
    out = ROOT/'Art/animations/player-c-v1'
    sheets = out/'sheets'; sheets.mkdir(parents=True, exist_ok=True)
    animations = []; frames = {}
    for name, label in zip(NAMES, LABELS):
        paths = sorted((args.frames/name).glob('*.png'))
        if not paths: raise RuntimeError(f'Missing rendered frames: {name}')
        frames[name] = [Image.open(p).convert('RGB') for p in paths]
        sheet = Image.new('RGB', (192*8, 192*math.ceil(len(paths)/8)), '#353b47')
        for i, frame in enumerate(frames[name]):
            sheet.paste(frame.resize((192,192), Image.Resampling.LANCZOS), ((i%8)*192,(i//8)*192))
        sheet.save(sheets/(name+'.png'), optimize=True)
        animations.append(dict(name=name, label=label, frames=len(paths), fps=60, loop=name in LOOPS, file='sheets/'+name+'.png'))
    (out/'clips.json').write_text(json.dumps(animations, ensure_ascii=False, indent=2), encoding='utf-8')
    shutil.copy2(args.frames/'in-game.png', out/'in-game.png')
    # A compact 30 fps GIF for chat; the interactive sheet player and Unity clips run at 60 fps.
    selected=['Idle','Run','Dash','AirRise','AirFall','Land','Attack1','Attack2','Attack3']
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
    board=[]
    for tick in range(90):
        canvas=Image.new('RGB',(768,840),'#242a35'); draw=ImageDraw.Draw(canvas)
        for n,name in enumerate(selected):
            seq=frames[name]; local=tick*2
            # One-shots replay after a brief readable hold; cyclic clips loop directly.
            cycle=len(seq) if name in LOOPS else len(seq)+24
            index=min(local%cycle,len(seq)-1)
            x=(n%3)*256; y=(n//3)*280
            canvas.paste(seq[index].resize((256,256),Image.Resampling.LANCZOS),(x,y))
            draw.text((x+16,y+255),LABELS[NAMES.index(name)],font=font,fill='#e8e4dc')
        board.append(canvas)
    palette=board[0].quantize(colors=192)
    gifs=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in board]
    gifs[0].save(out/'animation-preview.gif',save_all=True,append_images=gifs[1:],loop=0,duration=[30,30,40]*30,optimize=False,disposal=2)
    board[12].save(out/'animation-contact-sheet.png')
    print(json.dumps({'clips':len(animations),'frames':sum(a['frames'] for a in animations),'output':str(out)},ensure_ascii=False))

if __name__=='__main__': main()
