"""Full-drawing fall poses and a separate gravity-driven preview controller."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[1]; ART=ROOT/'Art/animations'; OUT=ART/'player-c-fall-v1'
NAMES=['step-off','early-fall','sustained-fall','landing','recovery']
def bounds(im):return im.getchannel('A').point(lambda x:255 if x>=128 else 0).getbbox()
def main():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
 for state in ['run','idle','jump']:
  for f,h in lock[state]['files'].items():assert hashlib.sha256((ART/f).read_bytes()).hexdigest()==h,f
 sheet=Image.open(OUT/'sources/fall-keys.png').convert('RGBA');assert sheet.getchannel('A').getextrema()==(0,255)
 cw,ch=sheet.width//2,sheet.height//2;(OUT/'frames').mkdir(exist_ok=True);frames=[];meta=[]
 for i in range(4):
  rect=(i%2*cw,i//2*ch,(i%2+1)*cw,(i//2+1)*ch);cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
  left=[250,240,310,280][i]  # Exclude upward cape folds from the head-width scan.
  for y in range(b[1]+int(ch*.03),b[1]+int(ch*.20)):
   r=a.crop((left,y,cw,y+1)).getbbox()
   if r:rows.append((r[2]-r[0],left+(r[0]+r[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(365-hx*s);ty=round(203-hy*s) if i<3 else round(448-b[3]*s)
  sprite=cell.resize((round(cw*s),round(ch*s)),Image.Resampling.LANCZOS);im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  if i==3:
   ty+=448-bounds(im)[3];im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  bb=bounds(im);assert bb[0]>0 and bb[1]>0 and bb[2]<512 and bb[3]<=448,bb
  im.save(OUT/'frames'/f'{i:02}.png');frames.append(im);meta.append(dict(frame=i+1,phase=NAMES[i],sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[365,round(hy*s+ty)],bounds=bb))
 original=ART/'player-c-jump-v2/frames/08.png';shutil.copyfile(original,OUT/'frames/04.png');im=Image.open(original).convert('RGBA');frames.append(im);meta.append(dict(frame=5,phase='recovery',source='../player-c-jump-v2/frames/08.png',headCenter=[365,218],bounds=bounds(im)))
 atlas=Image.new('RGBA',(1536,1024));contact=Image.new('RGB',(960,700),'#242a30');d=ImageDraw.Draw(contact)
 for i,im in enumerate(frames):
  atlas.alpha_composite(im,(i%3*512,i//3*512));thumb=im.resize((320,320),Image.Resampling.LANCZOS);x,y=i%3*320,i//3*350;contact.paste(thumb,(x,y),thumb);d.text((x+10,y+325),f'{i+1} {NAMES[i]}',fill='#eadfce')
 atlas.save(OUT/'fall-atlas.png');contact.save(OUT/'fall-contact-sheet.jpg',quality=95)
 data=dict(frameCount=5,cellSize=512,columns=3,rows=2,pivot=[368/512,1-448/512],groundY=448,gravityPxPerSecondSquared=1800,terminalSpeedPxPerSecond=900,entryMs=65,earlyFallMs=90,loopPoseMs=140,landingMs=65,recoveryMs=65,frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 checks=dict(approvedAssetsUnchanged=True,frameCount=5,newDrawings=4,headY=[m['headCenter'][1] for m in meta],bounds=[m['bounds'] for m in meta],wholeSpriteRegistrationOnly=True)
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
