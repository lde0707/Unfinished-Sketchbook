"""Whole-drawing jump poses plus separate root-motion timing; no limb rig."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Art/animations';OUT=ART/'player-c-jump-v1'
DURATIONS=[60,40,140,80,180,40,60,80]
NAMES=['anticipation','takeoff','rise','apex','fall','contact','compression','recovery']
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def verify_approved():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
 for state in ['run','idle']:
  for name,digest in lock[state]['files'].items():assert hashlib.sha256((ART/name).read_bytes()).hexdigest()==digest,name
 return True
def main():
 verify_approved();(OUT/'frames').mkdir(exist_ok=True);frames=[];meta=[];start=0
 for i in range(8):
  replacements={0:0,5:1,6:2,7:3}
  name='grounded-keys.png' if i in replacements else ('jump-up.png' if i<4 else 'jump-down.png')
  sheet=Image.open(OUT/'sources'/name).convert('RGBA');cw,ch=sheet.width//2,sheet.height//2;j=replacements.get(i,i%4)
  rect=(j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch);cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
  for y in range(b[1]+int(ch*.03),b[1]+int(ch*.17)):
   r=a.crop((int(cw*.32),y,cw,y+1)).getbbox()
   if r:rows.append((r[2]-r[0],int(cw*.32)+(r[0]+r[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(365-hx*s)
  airborne=i in [2,3,4];ty=round(203-hy*s) if airborne else round(448-b[3]*s)
  sprite=cell.resize((round(cw*s),round(ch*s)),Image.Resampling.LANCZOS);im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  if not airborne:
   ty+=448-bounds(im)[3];im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
  bb=bounds(im);assert bb[0]>0 and bb[1]>0 and bb[2]<512 and bb[3]<512
  im.save(OUT/'frames'/f'{i:02}.png');frames.append(im)
  meta.append(dict(frame=i+1,phase=NAMES[i],source=name,sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[365,round(hy*s+ty)],bounds=bb,airborne=airborne,startMs=start,durationMs=DURATIONS[i]));start+=DURATIONS[i]
 atlas=Image.new('RGBA',(2048,1024));contact=Image.new('RGB',(1280,700),'#242a30');d=ImageDraw.Draw(contact)
 for i,im in enumerate(frames):
  atlas.alpha_composite(im,((i%4)*512,(i//4)*512));thumb=im.resize((320,320),Image.Resampling.LANCZOS);x,y=(i%4)*320,(i//4)*350;contact.paste(thumb,(x,y),thumb);d.text((x+10,y+325),f'{i+1} {NAMES[i]} {DURATIONS[i]}ms',fill='#eadfce')
 atlas.save(OUT/'jump-atlas.png');contact.save(OUT/'jump-contact-sheet.jpg',quality=95)
 data=dict(frameCount=8,durationMs=start,cellSize=512,columns=4,rows=2,pivot=[365/512,1-448/512],groundY=448,flightStartMs=100,flightEndMs=500,jumpHeightPx=100,horizontalDisplacementPx=0,rootMotion='During 100..500ms: y=-4*100*u*(1-u), u=(t-100)/400; otherwise y=0. Root x is constant. Apply to whole sprite only.',frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 checks=dict(frameCount=8,uniqueFrames=len(set(hashlib.sha256(f.tobytes()).hexdigest() for f in frames)),durationMs=start,headY=[m['headCenter'][1] for m in meta],groundedBottoms=[m['bounds'][3] for m in meta if not m['airborne']],approvedRunAndIdleUnchanged=verify_approved(),note='World-space jump arc is separate from pose artwork. Files do not prove perceived fluidity.')
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
