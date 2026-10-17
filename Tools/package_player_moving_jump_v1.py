"""Package whole forward-leap drawings; retain the approved run's scale and heading."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Art/animations';OUT=ART/'player-c-moving-jump-v1'
DURATIONS=[80,140,100,140,30,30]
NAMES=['push-off','rise','tucked-apex','descent','running-contact','stride-return']
def bounds(im):return im.getchannel('A').point(lambda x:255 if x>=128 else 0).getbbox()
def main():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
 for state in ['run','idle','jump']:
  for f,h in lock[state]['files'].items():assert hashlib.sha256((ART/f).read_bytes()).hexdigest()==h,f
 sheet=Image.open(OUT/'sources/moving-jump-keys.png').convert('RGBA');assert sheet.getchannel('A').getextrema()[0]==0
 cw,ch=sheet.width//3,sheet.height//2;(OUT/'frames').mkdir(exist_ok=True);frames=[];meta=[];start=0
 for i in range(6):
  rect=(i%3*cw,i//3*ch,(i%3+1)*cw,(i//3+1)*ch);cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
  for y in range(b[1]+int(ch*.03),b[1]+int(ch*.17)):
   r=a.crop((cw//2,y,cw,y+1)).getbbox()
   if r:rows.append((r[2]-r[0],cw//2+(r[0]+r[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(365-hx*s);ty=round(448-b[3]*s) if i in [0,4,5] else round(250-hy*s)
  sprite=cell.resize((round(cw*s),round(ch*s)),Image.Resampling.LANCZOS);im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  if i in [0,4,5]:
   ty+=448-bounds(im)[3];im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  bb=bounds(im);assert bb[0]>0 and bb[1]>0 and bb[2]<512 and bb[3]<512,bb
  im.save(OUT/'frames'/f'{i:02}.png');frames.append(im);meta.append(dict(frame=i+1,phase=NAMES[i],startMs=start,durationMs=DURATIONS[i],sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[365,round(hy*s+ty)],bounds=bb,airborne=i<4));start+=DURATIONS[i]
 atlas=Image.new('RGBA',(1536,1024));contact=Image.new('RGB',(960,700),'#242a30');d=ImageDraw.Draw(contact)
 for i,im in enumerate(frames):
  atlas.alpha_composite(im,(i%3*512,i//3*512));thumb=im.resize((320,320),Image.Resampling.LANCZOS);x,y=i%3*320,i//3*350;contact.paste(thumb,(x,y),thumb);d.text((x+10,y+325),f'{i+1} {NAMES[i]} {DURATIONS[i]}ms',fill='#eadfce')
 atlas.save(OUT/'jump-atlas.png');contact.save(OUT/'jump-contact-sheet.jpg',quality=95)
 data=dict(frameCount=6,durationMs=start,cellSize=512,columns=3,rows=2,pivot=[340/512,1-448/512],groundY=448,flightStartMs=0,flightEndMs=460,jumpHeightPx=130,horizontalSpeedPxPerSecond=200,frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 checks=dict(approvedAssetsUnchanged=True,frameCount=6,headY=[m['headCenter'][1] for m in meta],durationMs=start,flightMs=460,anticipationMs=0,wholeSpriteRegistrationOnly=True)
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
