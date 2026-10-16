"""Crop generated animation parts and package frames rendered by the JS rig."""
from pathlib import Path
from PIL import Image
import argparse, hashlib, json, shutil
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'Art/animations/player-c-v3'

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path)
    parser.add_argument('--package', action='store_true')
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    if args.source:
        shutil.copy2(args.source, OUT/'parts-source.png')
        source = Image.open(args.source).convert('RGBA')
        assert source.size == (1254, 1254)
        rects = dict(body=(0,0,1254,860), thigh=(180,880,370,1170), shin=(570,910,730,1170), foot=(930,1010,1160,1170))
        for name, rect in rects.items():
            part = source.crop(rect)
            if name != 'body':
                bounds = part.getchannel('A').point(lambda a:255 if a>128 else 0).getbbox()
                assert bounds
                part = part.crop(bounds)
            part.save(OUT/f'{name}.png')
        (OUT/'parts.json').write_text(json.dumps(rects, indent=2), encoding='utf-8')
    if args.package:
        frames = [Image.open(OUT/'frames'/f'{i:02}.png').convert('RGBA') for i in range(36)]
        assert len({hashlib.sha256(im.tobytes()).hexdigest() for im in frames}) == 36
        atlas = Image.new('RGBA', (420*6,420*6)); previews = []
        for n, frame in enumerate(frames):
            assert frame.size == (420,420)
            bounds = frame.getchannel('A').point(lambda a:255 if a>128 else 0).getbbox()
            assert bounds and min(bounds[:2])>0 and max(bounds[2:])<420, (n,bounds)
            atlas.alpha_composite(frame, (n%6*420,n//6*420))
            bg=Image.new('RGBA', frame.size, '#090c10');bg.alpha_composite(frame)
            previews.append(bg.convert('RGB'))
        atlas.save(OUT/'run-aligned.png');previews[0].save(OUT/'run-pose.png')
        palette=previews[0].quantize(colors=240)
        gifs=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in previews]
        gifs[0].save(OUT/'run-preview.gif',save_all=True,append_images=gifs[1:],duration=[20,20,10]*12,loop=0,disposal=2,optimize=False)
        gif=Image.open(OUT/'run-preview.gif');total=0
        for i in range(gif.n_frames):gif.seek(i);total+=gif.info['duration']
        assert gif.n_frames==36 and total==600
        print('36 distinct rendered alpha frames; 60 fps / 600 ms; bounds and GIF verified.')

if __name__=='__main__':main()
