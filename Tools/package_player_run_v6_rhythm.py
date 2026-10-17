"""Eight distinct run phases: preserve four drawings and use four edited contacts."""
from pathlib import Path
from PIL import Image, ImageDraw
import json, hashlib, copy, shutil
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Art/animations/player-c-v6-sprint'
OUT=ROOT/'Art/animations/player-c-v6-rhythm'
KEEP=[2,3,5,6,8,9,11,12]
EDITS={1:0,2:1,5:2,6:3}
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def main():
 (OUT/'frames').mkdir(exist_ok=True)
 old=json.loads((BASE/'registration.json').read_text(encoding='utf-8'));frames=[];meta=[]
 for i,prior in enumerate(KEEP):
  m=copy.deepcopy(old['frames'][prior-1]);m.update(frame=i+1,previousFrame=prior,referenceFrame=prior-1,durationMs=80,inserted=False)
  if i not in EDITS:
   source=BASE/'frames'/f'{prior-1:02}.png';shutil.copy2(source,OUT/'frames'/f'{i:02}.png');out=Image.open(source).convert('RGBA')
   m['source']=f'../player-c-v6-sprint/frames/{prior-1:02}.png';m['reusedExact']=True
  else:
   j=EDITS[i];name='bent-contacts.png' if j==3 else 'bent-contacts-draft.png'
   sheet=Image.open(OUT/'sources'/name).convert('RGBA');cw,ch=sheet.width//2,sheet.height//2
   rect=(j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch);cell=sheet.crop(rect)
   a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();left=int(cw*.5);rows=[]
   for y in range(b[1]+int(ch*.03),b[1]+int(ch*.15)):
    row=a.crop((left,y,cw,y+1)).getbbox()
    if row:rows.append((row[2]-row[0],left+(row[0]+row[2])/2))
   w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(364-hx*s);ty=round(448-b[3]*s)
   scaled=cell.resize((round(cw*s),round(ch*s)),Image.Resampling.LANCZOS)
   out=Image.new('RGBA',(512,512));out.alpha_composite(scaled,(tx,ty));ty+=448-bounds(out)[3]
   out=Image.new('RGBA',(512,512));out.alpha_composite(scaled,(tx,ty))
   m.update(source='sources/'+name,sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[364,round(hy*s+ty)],headWidth=160,bounds=bounds(out),footGap=0,reusedExact=False)
   out.save(OUT/'frames'/f'{i:02}.png')
  assert bounds(out)[3]<=448 and bounds(out)[2]<512
  frames.append(out);meta.append(m)
 atlas=Image.new('RGBA',(2048,1024));contact=Image.new('RGB',(1280,700),'#242a30');d=ImageDraw.Draw(contact);gif=[]
 for i,f in enumerate(frames):
  atlas.alpha_composite(f,((i%4)*512,(i//4)*512));thumb=f.resize((320,320),Image.Resampling.LANCZOS);x,y=(i%4)*320,(i//4)*350
  contact.paste(thumb,(x,y),thumb);d.text((x+15,y+325),f'{i+1}   previous {KEEP[i]}   80ms',fill='#eadfce')
  bg=Image.new('RGBA',f.size,'#242a30');bg.alpha_composite(f);gif.append(bg.convert('RGB'))
 atlas.save(OUT/'run-atlas.png');contact.save(OUT/'run-contact-sheet.jpg',quality=95)
 pal=gif[0].quantize(colors=240);g=[f.quantize(palette=pal,dither=Image.Dither.NONE) for f in gif]
 g[0].save(OUT/'run-preview.gif',save_all=True,append_images=g[1:],duration=80,loop=0,disposal=2,optimize=False)
 data=dict(frameCount=8,inbetweenCount=0,loopMs=640,fps=12.5,cellSize=512,columns=4,rows=2,groundY=448,pivot=[364/512,1-448/512],removedPreviousFrames=[1,4,7,10],frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 checks=dict(frameCount=8,uniqueFrames=len(set(hashlib.sha256(f.tobytes()).hexdigest() for f in frames)),loopMs=640,durationMs=[80]*8,previousFrames=KEEP,headY=[m['headCenter'][1] for m in meta],footGaps=[m['footGap'] for m in meta],exactReusedFrames=[i+1 for i,m in enumerate(meta) if m['reusedExact']],note='Automatic checks cover files and timing, not perceived motion quality.')
 assert checks['uniqueFrames']==8
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
