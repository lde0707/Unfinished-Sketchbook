"""Slice hand-drawn imagegen poses and register whole sprites, never rig limbs."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, hashlib

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Art/animations/player-c-v6-sprint'
# Old v6 poses with two newly drawn recovery poses after 4 and 8.
POSES=[('lean-a.png',0,'1',100,0),('lean-a.png',1,'2',100,6),('lean-a.png',2,'3',80,3),('lean-a.png',3,'4',40,0),
       ('bridges.png',0,'4>A',40,0),('bridges.png',1,'4>B',40,0),
       ('lean-b.png',0,'5',100,0),('lean-b.png',1,'6',100,6),('lean-b.png',2,'7',80,3),('lean-b.png',3,'8',40,0),
       ('bridges.png',2,'8>A',40,0),('bridges.png',3,'8>B',40,0)]

def solid_bounds(im):
    return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()

def main():
    (OUT/'frames').mkdir(parents=True,exist_ok=True)
    frames=[];meta=[]
    for index,(name,j,label,duration,gap) in enumerate(POSES):
        sheet=Image.open(OUT/'sources'/name).convert('RGBA');cw,ch=sheet.width//2,sheet.height//2
        rect=(j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch)
        cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox()
        rows=[]; head_left=int(cw*.58)
        # Stop before the collar/cape intersects the head's horizontal extent.
        for y in range(b[1]+int(ch*.03),b[1]+int(ch*.15)):
            row=a.crop((head_left,y,cw,y+1)).getbbox()
            if row:rows.append((row[2]-row[0],head_left+(row[0]+row[2])/2))
        w,hx=max(rows);hy=b[1]+w/2;scale=160/w
        sprite=cell.resize((round(cw*scale),round(ch*scale)),Image.Resampling.LANCZOS)
        tx=round(364-hx*scale);ty=round(448-gap-b[3]*scale)
        out=Image.new('RGBA',(512,512));out.alpha_composite(sprite,(tx,ty))
        correction=448-gap-solid_bounds(out)[3];ty+=correction
        out=Image.new('RGBA',(512,512));out.alpha_composite(sprite,(tx,ty))
        bb=solid_bounds(out)
        assert bb[0]>0 and bb[1]>0 and bb[2]<512 and bb[3]<=448
        out.save(OUT/'frames'/f'{index:02}.png');frames.append(out)
        meta.append(dict(frame=index+1,oldPose=label,source=name,sourceRect=rect,scale=scale,translation=[tx,ty],headCenter=[364,round(hy*scale+ty)],headWidth=160,bounds=bb,footGap=448-bb[3],durationMs=duration,referenceFrame=int(label.split('>')[0])-1,inserted='>' in label))
    atlas=Image.new('RGBA',(2048,1536));contact=Image.new('RGB',(1280,1050),'#242a30');d=ImageDraw.Draw(contact);gif=[]
    for i,f in enumerate(frames):
        atlas.alpha_composite(f,((i%4)*512,(i//4)*512));thumb=f.resize((320,320),Image.Resampling.LANCZOS)
        x,y=(i%4)*320,(i//4)*350;contact.paste(thumb,(x,y),thumb)
        d.text((x+15,y+325),f'{i+1:02}  (v6 {meta[i]["oldPose"]})  {meta[i]["durationMs"]}ms',fill='#eadfce')
        bg=Image.new('RGBA',(512,512),'#242a30');bg.alpha_composite(f);gif.append(bg.convert('RGB'))
    atlas.save(OUT/'run-atlas.png');contact.save(OUT/'run-contact-sheet.jpg',quality=95)
    pal=gif[0].quantize(colors=240);g=[f.quantize(palette=pal,dither=Image.Dither.NONE) for f in gif]
    g[0].save(OUT/'run-preview.gif',save_all=True,append_images=g[1:],duration=[m['durationMs'] for m in meta],loop=0,disposal=2,optimize=False)
    data=dict(frameCount=12,inbetweenCount=4,loopMs=sum(m['durationMs'] for m in meta),cellSize=512,columns=4,rows=3,groundY=448,pivot=[364/512,1-448/512],frames=meta)
    (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
    checks=dict(frameCount=12,uniqueFrames=len(set(hashlib.sha256(f.tobytes()).hexdigest() for f in frames)),loopMs=data['loopMs'],headY=[m['headCenter'][1] for m in meta],footGaps=[m['footGap'] for m in meta],insertedFrames=[m['frame'] for m in meta if m['inserted']],note='Image bounds and timing checks do not establish anatomical continuity; review drawings separately.')
    assert checks['uniqueFrames']==12 and checks['loopMs']==800
    (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))

if __name__=='__main__':main()
