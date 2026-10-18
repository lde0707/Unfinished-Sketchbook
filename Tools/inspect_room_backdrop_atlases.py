"""Read generated alpha and record UV bounds. Does not modify any image pixels."""
import json
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[1]
art=root/'Art/room-backdrops-v1'
sources=json.loads((art/'generation.json').read_text(encoding='utf-8'))['assets']+json.loads((art/'hero-generation.json').read_text(encoding='utf-8'))['assets']
# The generated atlases have intentionally unequal silhouettes; inspected gutters are not all 50/50.
# Bounds are x0,y0,x1,y1 in top-left normalized image coordinates.
regions={
 'A':[(0,0,.59,.50),(.59,0,1,.50),(0,.65,.60,1),(.60,.50,1,1)],
 'B':[(0,0,.50,.51),(.51,0,1,.51),(0,.52,.51,1),(.51,.52,1,1)],
 'C':[(0,0,.635,.515),(.635,0,1,.515),(0,.52,.53,1),(.535,.52,1,1)],
 'D':[(0,0,.55,.51),(.55,0,1,.51),(0,.52,.59,1),(.59,.515,1,1)],
 'E':[(0,0,.667,.51),(.667,0,1,.51),(0,.53,.54,1),(.54,.53,1,1)],
 'F':[(0,0,.55,.50),(.55,0,1,.50),(0,.51,.53,1),(.53,.50,1,1)],
 'G':[(0,0,.58,.52),(.59,0,1,.52),(0,.54,.535,1),(.54,.54,1,1)],
 'H':[(0,0,.56,.50),(.57,0,1,.48),(0,.51,.64,1),(.65,.48,1,1)]}
out=[]
for src in sources:
 im=Image.open(art/src['file'])
 assert im.mode=='RGBA', (src['id'],im.mode)
 alpha=im.getchannel('A');hist=alpha.histogram()
 assert hist[0]/(im.width*im.height)>.15, 'Real transparent background required'
 crops=[]
 for region in regions.get(src['id'],[(0,0,1,1)]):
  rect=tuple(round(v*(im.width if j%2==0 else im.height)) for j,v in enumerate(region))
  mask=alpha.crop(rect).point(lambda a:255 if a>20 else 0)
  bbox=mask.getbbox();assert bbox
  x0=max(rect[0],rect[0]+bbox[0]-2);y0=max(rect[1],rect[1]+bbox[1]-2)
  x1=min(rect[2],rect[0]+bbox[2]+2);y1=min(rect[3],rect[1]+bbox[3]+2)
  crops.append(dict(x=x0/im.width,y=y0/im.height,w=(x1-x0)/im.width,h=(y1-y0)/im.height))
 out.append(dict(file=src['file'],width=im.width,height=im.height,transparentFraction=round(hist[0]/(im.width*im.height),4),crops=crops))
data=json.dumps(dict(atlases=out),ensure_ascii=False,indent=2)+'\n'
(art/'atlas-layouts.json').write_text(data,encoding='utf-8')
(root/'Book/Assets/Unfin/Art/RoomBackdropsV1/atlas-layouts.json').write_text(data,encoding='utf-8')
print(json.dumps([dict(file=a['file'],size=[a['width'],a['height']],alpha=a['transparentFraction']) for a in out]))
