"""Slice and register eight imagegen-edited v6 drawings; no limb rigging."""
from pathlib import Path
from PIL import Image, ImageDraw
import json

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Art/animations/player-c-v6'
OUT=ROOT/'Art/animations/player-c-v6-cloaked'

def main():
    (OUT/'frames').mkdir(exist_ok=True)
    reg=json.loads((BASE/'registration.json').read_text(encoding='utf-8'))
    frames=[]; checks=[]
    for i,meta in enumerate(reg['frames']):
        sheet=Image.open(OUT/'sources'/('cloaked-a.png' if i<4 else 'cloaked-b.png')).convert('RGBA')
        cw,ch=sheet.width//2,sheet.height//2; j=i%4
        cell=sheet.crop((j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch))
        alpha=cell.getchannel('A').point(lambda v:255 if v>=128 else 0)
        bounds=alpha.getbbox(); rows=[]
        for y in range(bounds[1]+int(ch*.03),bounds[1]+int(ch*.20)):
            row=alpha.crop((0,y,cw,y+1)).getbbox()
            if row: rows.append((row[2]-row[0],(row[0]+row[2])/2))
        width,hx=max(rows); hy=bounds[1]+width/2
        scale=160/width; targetY=meta['headCenter'][1]
        aligned=Image.new('RGBA',(512,512))
        aligned.alpha_composite(cell.resize((round(cw*scale),round(ch*scale)),Image.Resampling.LANCZOS),(round(350-hx*scale),round(targetY-hy*scale)))
        out=aligned
        bb=out.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
        correction=448-bb[3] if meta['supportLeg'] else min(0,448-bb[3])
        if correction:
            shifted=Image.new('RGBA',(512,512)); shifted.alpha_composite(out,(0,correction));out=shifted
        meta['headCenter'][1]+=correction
        meta['bounds']=out.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
        meta['footGap']=448-meta['bounds'][3]
        meta['source']='cloaked-a.png' if i<4 else 'cloaked-b.png'
        meta['sourceRect']=[j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch]
        meta['scale']=scale
        meta['translation']=[round(350-hx*scale),round(targetY-hy*scale)+correction]
        out.save(OUT/'frames'/f'{i:02}.png');frames.append(out)
        checks.append({'frame':i+1,'footGap':meta['footGap'],'headY':meta['headCenter'][1],'durationMs':100})
    atlas=Image.new('RGBA',(2048,1024)); sheet=Image.new('RGB',(1280,700),'#242a30'); d=ImageDraw.Draw(sheet);gif=[]
    for i,f in enumerate(frames):
        atlas.alpha_composite(f,((i%4)*512,(i//4)*512))
        thumb=f.resize((320,320),Image.Resampling.LANCZOS);x,y=(i%4)*320,(i//4)*350
        sheet.paste(thumb,(x,y),thumb);d.text((x+15,y+325),str(i+1),fill='#eadfce')
        bg=Image.new('RGBA',(512,512),'#242a30');bg.alpha_composite(f);gif.append(bg.convert('RGB'))
    atlas.save(OUT/'run-atlas.png');sheet.save(OUT/'run-contact-sheet.jpg',quality=95)
    palette=gif[0].quantize(colors=240);g=[f.quantize(palette=palette,dither=Image.Dither.NONE) for f in gif]
    g[0].save(OUT/'run-preview.gif',save_all=True,append_images=g[1:],duration=100,loop=0,disposal=2,optimize=False)
    reg['artEdit']='Imagegen edited complete v6 drawings with both arms hidden. Uniform scale and translation only. Original 8-frame order and 800ms duration; not pixel-identical to v6.'
    (OUT/'registration.json').write_text(json.dumps(reg,indent=2),encoding='utf-8')
    (OUT/'validation.json').write_text(json.dumps({'frameCount':8,'loopMs':800,'checks':checks},indent=2),encoding='utf-8')
    print(json.dumps(checks))

if __name__=='__main__':main()
