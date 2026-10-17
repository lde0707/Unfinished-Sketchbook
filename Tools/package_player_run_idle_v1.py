"""Register four complete stop drawings by the planted sole. Preserve run/idle."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Art/animations';OUT=ART/'player-c-run-idle-v1'
DURATIONS=[30,35,40,55]
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def sole(im):
 a=im.getchannel('A');runs=[];start=None
 for x in range(512):
  active=any(a.getpixel((x,y))>=128 for y in range(445,448))
  if active and start is None:start=x
  if not active and start is not None:runs.append((start,x));start=None
 if start is not None:runs.append((start,512))
 runs=[r for r in runs if r[1]-r[0]>=8]
 left,right=runs[-1];return (left+right)/2
def main():
 (OUT/'frames').mkdir(exist_ok=True);start=Image.open(ART/'player-c-v6-rhythm/frames/01.png').convert('RGBA');idle=Image.open(ART/'player-c-idle-v1/frames/00.png').convert('RGBA');anchor=sole(start)
 sheet=Image.open(OUT/'sources/stop-keys.png').convert('RGBA');cw,ch=sheet.width//2,sheet.height//2;frames=[];meta=[]
 for j in range(4):
  # The upper-left drawing extends slightly across the nominal middle line.
  # Split at the transparent gutter, not through its head.
  split=round(sheet.width*.5183) if j<2 else cw
  rect=(0 if j%2==0 else split,j//2*ch,split if j%2==0 else sheet.width,(j//2+1)*ch)
  cell=sheet.crop(rect);cellw=cell.width;a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
  for y in range(b[1]+int(ch*.03),b[1]+int(ch*.15)):
   r=a.crop((int(cellw*.45),y,cellw,y+1)).getbbox()
   if r:rows.append((r[2]-r[0],int(cellw*.45)+(r[0]+r[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(364-hx*s);ty=round(448-b[3]*s);sprite=cell.resize((round(cellw*s),round(ch*s)),Image.Resampling.LANCZOS)
  im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty));ty+=448-bounds(im)[3]
  im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty));dx=round(anchor-sole(im));tx+=dx
  im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty));assert bounds(im)[3]==448
  im.save(OUT/'frames'/f'{j:02}.png');frames.append(im);meta.append(dict(frame=j+1,durationMs=DURATIONS[j],sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[364+dx,round(hy*s+ty)],soleCenterX=sole(im),groundY=448))
 idle_offset=round(anchor-sole(idle));aligned_idle=Image.new('RGBA',(512,512));aligned_idle.alpha_composite(idle,(idle_offset,0))
 atlas=Image.new('RGBA',(2048,512));contact=Image.new('RGB',(1536,285),'#242a30');d=ImageDraw.Draw(contact)
 for i,im in enumerate(frames):atlas.alpha_composite(im,(i*512,0))
 for i,im in enumerate([start,*frames,aligned_idle]):
  thumb=im.resize((256,256),Image.Resampling.LANCZOS);contact.paste(thumb,(i*256,0),thumb);d.text((i*256+12,264),['RUN CONTACT','STOP 1','STOP 2','STOP 3','STOP 4','IDLE'][i],fill='#eadfce')
 atlas.save(OUT/'stop-atlas.png');contact.save(OUT/'transition-contact-sheet.jpg',quality=95)
 data=dict(frameCount=4,durationMs=sum(DURATIONS),groundY=448,plantedSoleX=anchor,runExitFrame=1,idleRenderOffset=[idle_offset,0],frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
 for f,h in lock['run']['files'].items():assert hashlib.sha256((ART/f).read_bytes()).hexdigest()==h
 checks=dict(frameCount=4,durationMs=sum(DURATIONS),headY=[m['headCenter'][1] for m in meta],soleCenterX=[m['soleCenterX'] for m in meta],idleOffsetX=idle_offset,approvedRunUnchanged=True,note='Whole-sprite offsets keep the planted sole near its run-contact position; no approved artwork is edited.')
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
