"""Register complete crouch/tuck drawings without a limb rig or body deformation."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[1]; ART=ROOT/'Art/animations'; OUT=ART/'player-c-jump-v2'; OLD=ART/'player-c-jump-v1'
DURATIONS=[35,55,30,150,100,210,40,60,80]
NAMES=['load','deep-crouch','takeoff','rise-tuck','apex-tuck','fall','contact','compression','recovery']
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def main():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
 for state in ['run','idle','jump']:
  for file,digest in lock[state]['files'].items():assert hashlib.sha256((ART/file).read_bytes()).hexdigest()==digest,file
 (OUT/'frames').mkdir(parents=True,exist_ok=True)
 sheet=Image.open(OUT/'sources/crouch-tuck-keys.png').convert('RGBA');assert sheet.getchannel('A').getextrema()==(0,255)
 cw,ch=sheet.width//2,sheet.height//2
 oldreg=json.loads((OLD/'registration.json').read_text()); frames=[];meta=[];start=0
 for i,(old,key) in enumerate([(0,None),(None,0),(1,None),(None,1),(None,2),(4,None),(5,None),(None,3),(7,None)]):
  airborne=i in [3,4,5]
  if old is not None:
   im=Image.open(OLD/'frames'/f'{old:02}.png').convert('RGBA');m=dict(oldreg['frames'][old]);m['source']=f'../player-c-jump-v1/frames/{old:02}.png';m['reusedUnchanged']=True
  else:
   rect=(key%2*cw,key//2*ch,(key%2+1)*cw,(key//2+1)*ch);cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
   for y in range(b[1]+int(ch*.03),b[1]+int(ch*.20)):
    r=a.crop((int(cw*.32),y,cw,y+1)).getbbox()
    if r:rows.append((r[2]-r[0],int(cw*.32)+(r[0]+r[2])/2))
   w,hx=max(rows);hy=b[1]+w/2;s=160/w;tx=round(365-hx*s);ty=round(203-hy*s) if airborne else round(448-b[3]*s)
   sprite=cell.resize((round(cw*s),round(ch*s)),Image.Resampling.LANCZOS);im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
   if not airborne:
    ty+=448-bounds(im)[3];im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty))
   m=dict(source='sources/crouch-tuck-keys.png',sourceRect=rect,scale=s,translation=[tx,ty],headCenter=[365,round(hy*s+ty)],reusedUnchanged=False)
  b=bounds(im);assert b[0]>0 and b[1]>0 and b[2]<512 and b[3]<512
  m.update(frame=i+1,phase=NAMES[i],bounds=b,airborne=airborne,startMs=start,durationMs=DURATIONS[i]);start+=DURATIONS[i]
  dest=OUT/'frames'/f'{i:02}.png'
  if old is not None:shutil.copyfile(OLD/'frames'/f'{old:02}.png',dest)
  else:im.save(dest)
  frames.append(im);meta.append(m)
 atlas=Image.new('RGBA',(1536,1536));contact=Image.new('RGB',(960,1050),'#242a30');d=ImageDraw.Draw(contact)
 for i,im in enumerate(frames):
  atlas.alpha_composite(im,(i%3*512,i//3*512));thumb=im.resize((320,320),Image.Resampling.LANCZOS);x,y=i%3*320,i//3*350;contact.paste(thumb,(x,y),thumb);d.text((x+10,y+325),f'{i+1} {NAMES[i]} {DURATIONS[i]}ms',fill='#eadfce')
 atlas.save(OUT/'jump-atlas.png');contact.save(OUT/'jump-contact-sheet.jpg',quality=95)
 reg=dict(frameCount=9,durationMs=start,cellSize=512,columns=3,rows=3,pivot=[365/512,1-448/512],groundY=448,flightStartMs=120,flightEndMs=580,jumpHeightPx=130,horizontalDisplacementPx=0,frames=meta)
 (OUT/'registration.json').write_text(json.dumps(reg,indent=2),encoding='utf-8')
 assert meta[1]['headCenter'][1]>meta[0]['headCenter'][1]+25
 assert meta[4]['bounds'][3]<oldreg['frames'][3]['bounds'][3]-15
 checks=dict(frameCount=9,durationMs=start,headY=[m['headCenter'][1] for m in meta],groundedBottoms=[m['bounds'][3] for m in meta if not m['airborne']],approvedAssetsUnchanged=True,transparentSource=True)
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
