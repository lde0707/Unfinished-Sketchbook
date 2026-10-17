"""Register complete dash poses; no separated joints or drawn body deformation."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1]; ART=ROOT/'Art/animations'; OUT=ART/'player-c-dash-v1'
DURATIONS=[30,80,40,30];NAMES=['launch','low-burst','stride-catch','run-exit']
def bounds(im):return im.getchannel('A').point(lambda x:255 if x>=128 else 0).getbbox()
def main():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
 for state in ['run','idle','jump']:
  for f,h in lock[state]['files'].items():assert hashlib.sha256((ART/f).read_bytes()).hexdigest()==h,f
 sheet=Image.open(OUT/'sources/dash-keys.png').convert('RGBA');assert sheet.getchannel('A').getextrema()[0]==0
 cw,ch=sheet.width//2,sheet.height//2;(OUT/'frames').mkdir(exist_ok=True);frames=[];meta=[];start=0
 for i in range(4):
  rect=(i%2*cw,i//2*ch,(i%2+1)*cw,(i//2+1)*ch);cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
  # Per-cell head scan excludes the long cape that overlaps the head-height band.
  left=[510,470,500,460][i]
  for y in range(b[1]+15,b[1]+100):
   r=a.crop((left,y,cw,y+1)).getbbox()
   if r:rows.append((r[2]-r[0],left+(r[0]+r[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(388-hx*s);ty=round(308-hy*s) if i==1 else round(448-b[3]*s)
  sprite=cell.resize((round(cw*s),round(ch*s)),Image.Resampling.LANCZOS);im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  if i!=1:
   ty+=448-bounds(im)[3];im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  bb=bounds(im);assert bb[0]>0 and bb[1]>0 and bb[2]<512 and bb[3]<512,bb
  im.save(OUT/'frames'/f'{i:02}.png');frames.append(im);meta.append(dict(frame=i+1,phase=NAMES[i],startMs=start,durationMs=DURATIONS[i],sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[388,round(hy*s+ty)],bounds=bb));start+=DURATIONS[i]
 atlas=Image.new('RGBA',(2048,512));contact=Image.new('RGB',(1280,350),'#242a30');d=ImageDraw.Draw(contact)
 for i,im in enumerate(frames):
  atlas.alpha_composite(im,(i*512,0));thumb=im.resize((320,320),Image.Resampling.LANCZOS);contact.paste(thumb,(i*320,0),thumb);d.text((i*320+10,325),f'{i+1} {NAMES[i]} {DURATIONS[i]}ms',fill='#eadfce')
 atlas.save(OUT/'dash-atlas.png');contact.save(OUT/'dash-contact-sheet.jpg',quality=95)
 data=dict(frameCount=4,durationMs=start,cellSize=512,columns=4,rows=1,pivot=[340/512,1-448/512],groundY=448,speedPxPerSecond=640,cooldownAfterEndMs=220,groundOnly=False,airDashesPerJump=1,frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 checks=dict(approvedAssetsUnchanged=True,frameCount=4,durationMs=start,headY=[m['headCenter'][1] for m in meta],bounds=[m['bounds'] for m in meta],wholeSpriteRegistrationOnly=True)
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
