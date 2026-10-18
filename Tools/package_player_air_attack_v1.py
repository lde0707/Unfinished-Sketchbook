"""Package generated whole-body air attacks; only crop, uniform-scale and register."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib,json
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Art/animations';OUT=ART/'player-c-air-attack-v1'
SEEDS={'rise':[(420,250),(375,250),(444,177),(368,177)],'fall':[(440,190),(410,200),(460,173),(435,173)]}
DURATIONS=[35,70,45,70]
def head_measure(cell,seed):
 a=cell.getchannel('A');sx,sy=seed;rows=[]
 for y in range(sy-28,sy+29):
  if a.getpixel((sx,y))<128:continue
  l=r=sx
  while l>0 and a.getpixel((l-1,y))>=128:l-=1
  while r<cell.width-1 and a.getpixel((r+1,y))>=128:r+=1
  rows.append((r-l+1,(l+r)/2,y))
 width=max(row[0] for row in rows);widest=[r for r in rows if r[0]>=width-1]
 return width,sum(r[1] for r in widest)/len(widest),sum(r[2] for r in widest)/len(widest)
def main():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'));checked=0
 for key in ['run','idle','jump']:
  for name,digest in lock[key]['files'].items():
   assert hashlib.sha256((ART/name).read_bytes()).hexdigest()==digest,name
   checked+=1
 clips={};thumbs=[]
 for kind in ['rise','fall']:
  source=f'sources/{kind}-keys.png' if kind=='rise' else 'sources/fall-forward-keys-v2.png'
  sheet=Image.open(OUT/source).convert('RGBA');assert sheet.size==(1254,1254)
  assert sheet.getchannel('A').getextrema()[0]==0
  dst=OUT/kind;(dst/'frames').mkdir(parents=True,exist_ok=True);records=[];frames=[];elapsed=0
  for i in range(4):
   rect=(i%2*627,i//2*627,(i%2+1)*627,(i//2+1)*627);cell=sheet.crop(rect)
   frame_source=source
   if kind=='fall' and i in (0,3):
    frame_source='sources/fall-keys.png';cell=Image.open(OUT/frame_source).convert('RGBA').crop(rect)
   width,cx,cy=head_measure(cell,SEEDS[kind][i]);assert 210<width<290,(kind,i,width)
   scale=160/width;tx=round(384-cx*scale);ty=round(232-cy*scale)
   im=Image.new('RGBA',(768,640));im.alpha_composite(cell.resize((round(627*scale),round(627*scale)),Image.Resampling.LANCZOS),(tx,ty))
   bounds=im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox();assert bounds[0]>4 and bounds[1]>4 and bounds[2]<764 and bounds[3]<636,(kind,i,bounds)
   im.save(dst/'frames'/f'{i:02}.png');frames.append(im);thumbs.append(im)
   records.append(dict(frame=i+1,startMs=elapsed,durationMs=DURATIONS[i],phase=['gather','single-swing','follow-through','return'][i],headCenter=[384,232],source=frame_source,sourceRect=rect,sourceHeadCenter=[cx,cy],sourceHeadWidth=width,scale=scale,translation=[tx,ty],bounds=bounds));elapsed+=DURATIONS[i]
  atlas=Image.new('RGBA',(768*4,640))
  for i,im in enumerate(frames):atlas.alpha_composite(im,(768*i,0))
  atlas.save(dst/'atlas.png');clips[kind]=dict(durationMs=elapsed,frames=records,activeMs=[35,105])
 reg=dict(revision='forward-fall-v2',cellSize=[768,640],headWidth=160,clips=clips,gravityPolicy='unmodified locomotion',cancelOn=['landing','dash','new-jump','reset'],unityApplied=False)
 (OUT/'registration.json').write_text(json.dumps(reg,indent=2)+'\n',encoding='utf-8')
 contact=Image.new('RGB',(1536,690),'#202629');draw=ImageDraw.Draw(contact)
 for i,im in enumerate(thumbs):
  t=im.resize((384,320),Image.Resampling.LANCZOS);x=i%4*384;y=i//4*345;contact.paste(t,(x,y),t);draw.text((x+10,y+318),f"{'RISE' if i<4 else 'FALL'} {i%4+1} / {DURATIONS[i%4]}ms",fill='#eee1c9')
 contact.save(OUT/'air-attack-contact.jpg',quality=94)
 report=dict(passed=True,approvedLocomotionHashesChecked=checked,generatedFullBodyDrawings=8,generatedAlphaPreserved=True,noLimbRigOrMorph=True)
 (OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
if __name__=='__main__':main()
