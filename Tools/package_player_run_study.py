"""Slice and register generated whole-body poses; no painted or invented inbetweens."""
from pathlib import Path
from PIL import Image
import argparse, json, shutil

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,required=True)
    args=parser.parse_args()
    out=ROOT/'Art/animations/player-c-v2'
    (out/'frames').mkdir(parents=True,exist_ok=True)
    shutil.copy2(args.source,out/'run-source.png')
    source=Image.open(args.source).convert('RGBA')
    assert source.size==(1536,1024)
    assert source.getchannel('A').getextrema()[0]==0
    frames=[]; metadata=[]
    # Register the head, preserving a small deliberate two-step body bounce.
    # Normalizing whole sprites never bends a limb or synthesizes an extra frame.
    bounce=[0,8,2,-5,-10,-4]*2
    for n in range(12):
        col=n%4;row=n//4
        rect=(col*384,round(row*1024/3),(col+1)*384,round((row+1)*1024/3))
        cell=source.crop(rect)
        mask=cell.getchannel('A').point(lambda v:255 if v>128 else 0)
        bb=mask.getbbox(); head=mask.crop((0,bb[1]+15,384,bb[1]+75)).getbbox()
        head_width=head[2]-head[0]; scale=132/head_width
        cx=(head[0]+head[2])/2;cy=bb[1]+head_width*.47
        position=(round(307-cx*scale),round(129+bounce[n]-cy*scale))
        sprite=cell.resize((round(cell.width*scale),round(cell.height*scale)),Image.Resampling.LANCZOS)
        frame=Image.new('RGBA',(420,420),(0,0,0,0));frame.alpha_composite(sprite,position)
        solid=frame.getchannel('A').point(lambda v:255 if v>128 else 0).getbbox()
        assert solid and solid[0]>0 and solid[1]>0 and solid[2]<420 and solid[3]<420,(n,solid)
        frame.save(out/'frames'/f'{n:02}.png')
        frames.append(frame)
        metadata.append(dict(frame=n,sourceRect=rect,scale=scale,translation=position,bounds=solid))
    atlas=Image.new('RGBA',(420*4,420*3),(0,0,0,0))
    rendered=[]
    for i,frame in enumerate(frames):
        atlas.alpha_composite(frame,((i%4)*420,(i//4)*420))
        bg=Image.new('RGBA',frame.size,'#090c10');bg.alpha_composite(frame);rendered.append(bg.convert('RGB'))
    atlas.save(out/'run-aligned.png')
    palette=rendered[0].quantize(colors=240)
    gifs=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in rendered]
    gifs[0].save(out/'run-preview.gif',save_all=True,append_images=gifs[1:],duration=[40,40,40,40,40,50]*2,loop=0,disposal=2,optimize=False)
    rendered[0].save(out/'run-pose.png')
    (out/'registration.json').write_text(json.dumps(dict(frameCount=12,fps=24,cellSize=420,columns=4,frames=metadata),indent=2),encoding='utf-8')
    print('12 alpha poses registered; no silhouette touches the frame edge; 24 fps study exported.')

if __name__=='__main__':main()
