"""Register intact moving-attack drawings; approved locomotion is read-only."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, json
from collections import deque
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'Art/animations';OUT=ART/'player-c-move-attack-v1'
SIZE=(768,640);GROUND=560
SEEDS=[[(384,220),(361,220),(357,213),(389,177),(378,161),(374,163)],[(388,210),(378,207),(358,195),(350,168),(375,154),(385,170)],[(384,210),(348,210),(364,220),(348,160),(348,160),(356,160)]]
OVERRIDES={(0,3):(180,389),(1,3):(170,351),(1,4):(175,375),(1,5):(172,385),(2,0):(166,384),(2,1):(174,348),(2,2):(174,364),(2,3):(176,348),(2,4):(176,348),(2,5):(174,356)}
# Keep the existing entry timing while snapping the thrust forward in12ms.
# One swing drawing then140ms settled pose; zero-duration drawings remain inspectable.
DURATIONS=[[80,80,60,100,80,80],[80,80,0,100,0,140],[90,90,12,48,60,120]]
PHASES=['contact-ready','push-windup','pass-swing','opposite-contact-hit','opposite-push-follow','pass-recover']
def measure(cell,seed):
 a=cell.getchannel('A');sx,sy=seed;rows=[]
 for y in range(sy-12,sy+13):
  if a.getpixel((sx,y))<128:continue
  l=r=sx
  while l>0 and a.getpixel((l-1,y))>=128:l-=1
  while r<cell.width-1 and a.getpixel((r+1,y))>=128:r+=1
  rows.append((r-l+1,(l+r)/2,y))
 return max(rows)
def body_bounds(cell):
 # Measure the largest connected alpha silhouette. Detached swing effects must
 # not shift the character above the floor. Pixels are measured, never removed.
 w,h=cell.size;mask=bytearray(cell.getchannel('A').point(lambda v:1 if v>=128 else 0).tobytes());best=(0,None)
 for p in range(w*h):
  if not mask[p]:continue
  mask[p]=0;q=deque([p]);count=0;left=w;right=0;top=h;bottom=0
  while q:
   j=q.popleft();x=j%w;y=j//w;count+=1;left=min(left,x);right=max(right,x);top=min(top,y);bottom=max(bottom,y)
   for k in ([j-1] if x else [])+([j+1] if x<w-1 else [])+([j-w] if y else [])+([j+w] if y<h-1 else []):
    if mask[k]:mask[k]=0;q.append(k)
  if count>best[0]:best=(count,(left,top,right+1,bottom+1))
 return best[1]
def main():
 lock=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'));checked=0
 for key in ['run','idle','jump']:
  for file,digest in lock[key]['files'].items():
   assert hashlib.sha256((ART/file).read_bytes()).hexdigest()==digest,file
   checked+=1
 clips=[];all_frames=[]
 for s in range(3):
  source_name='attack1-keys-refined.png' if s==0 else 'attack3-thrust-keys.png' if s==2 else f'attack{s+1}-keys.png'
  sheet=Image.open(OUT/'sources'/source_name).convert('RGBA');assert sheet.size==(1536,1024)
  assert sheet.getchannel('A').getextrema()[0]==0
  dst=OUT/f'attack{s+1}';(dst/'frames').mkdir(parents=True,exist_ok=True);records=[];frames=[];elapsed=0
  for i in range(6):
   rect=(i%3*512,i//3*512,(i%3+1)*512,(i//3+1)*512)
   shift=0
   if s==0 and i in (3,4):
    boundary=526 if s==0 else 554
    if i==3:rect=(0,512,boundary,1024)
    else:rect=(boundary,512,1024,1024);shift=boundary-512
   cell=sheet.crop(rect);seed=SEEDS[s][i];width,cx,cy=measure(cell,(seed[0]-shift,seed[1]))
   if (s,i) in OVERRIDES:width,cx=OVERRIDES[(s,i)];cx-=shift
   assert 145<width<215,(s,i,width)
   bb=body_bounds(cell);scale=160/width;gap=[0,0,6,0,0,6][i]
   tx=round(384-cx*scale);ty=round(GROUND-gap-bb[3]*scale)
   im=Image.new('RGBA',SIZE);im.alpha_composite(cell.resize((round(cell.width*scale),round(cell.height*scale)),Image.Resampling.LANCZOS),(tx,ty))
   if s==2 and i==0: im=all_frames[11].copy()
   bounds=im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox();assert bounds[0]>4 and bounds[1]>4 and bounds[2]<764 and bounds[3]<635,(s,i,bounds)
   im.save(dst/'frames'/f'{i:02}.png');frames.append(im);all_frames.append(im)
   phase=PHASES[i] if s<2 else ['swing-end','chamber','thrust','thrust-impact','retract','recovery'][i]
   records.append(dict(frame=i+1,globalFrame=s*6+i+1,phase=phase,startMs=elapsed,durationMs=DURATIONS[s][i],sourceRect=rect,scale=scale,translation=[tx,ty],sourceHeadWidth=width,headCenter=[384,round(cy*scale+ty)],footGap=gap,bounds=bounds,sharedFrom='MoveAttack2/6' if s==2 and i==0 else None));elapsed+=DURATIONS[s][i]
  if s==2:
   for key in ['sourceRect','scale','translation','sourceHeadWidth','headCenter','footGap']: records[0][key]=clips[1]['frames'][5][key]
  atlas=Image.new('RGBA',(SIZE[0]*6,SIZE[1]))
  for i,im in enumerate(frames):atlas.alpha_composite(im,(i*SIZE[0],0))
  atlas.save(dst/'atlas.png');clips.append(dict(name=f'MoveAttack{s+1}',durationMs=elapsed,frames=records,entryFrame=0,chainMs=elapsed,activeMs=[sum(DURATIONS[s][:2]),sum(DURATIONS[s][:4])]))
 for s,clip in enumerate(clips):
  clip['playbackFrames']=[i for i,d in enumerate(DURATIONS[s]) if d>0]
  if s==1:clip.update(swingFrames=[3],postSwingHoldMs=140)
 reg=dict(status='preview_pending_user_review',cellSize=list(SIZE),groundY=GROUND,pivot=[.5,.125],pixelsPerUnit=256,frameCount=18,playbackFramesPerClip=[6,4,6],clips=clips,inputBufferMs=150,comboWindowMs=250,finisherRecoveryMs=500,previewMoveSpeed=320,movementMultiplierDuringAttack=1,runExitFrame=2,unityApplied=False)
 (OUT/'registration.json').write_text(json.dumps(reg,indent=2)+'\n',encoding='utf-8')
 contact=Image.new('RGB',(1536,750),'#202629');d=ImageDraw.Draw(contact)
 for n,im in enumerate(all_frames):
  thumb=im.resize((256,213),Image.Resampling.LANCZOS);x=n%6*256;y=n//6*250;contact.paste(thumb,(x,y),thumb);d.text((x+10,y+220),f'{n+1:02} / MOVE {n//6+1} / {DURATIONS[n//6][n%6]}ms',fill='#e6d8bf')
 contact.save(OUT/'move-attack-contact-sheet.jpg',quality=94)
 report=dict(passed=True,approvedHashesChecked=checked,fullBodyDrawings=18,alphaPreserved=True,registrationOnly=True,clipsMs=[c['durationMs'] for c in clips],unityApplied=False)
 (OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
if __name__=='__main__':main()
