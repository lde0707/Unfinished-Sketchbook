"""Measure source sheets for display; preserve original image pixels and alpha."""
from pathlib import Path
from PIL import Image
import json
root=Path(__file__).resolve().parents[1]/'Art/samples/player-death-revive-v1'
result={'scale':{}}
for name,standing in [('death',0),('revive',5)]:
 im=Image.open(root/f'{name}-sheet.png').convert('RGBA'); mask=im.getchannel('A').point(lambda a:255 if a>=128 else 0);w,h=im.size
 empty=[y for y in range(int(h*.4),int(h*.65)) if mask.crop((0,y,w,y+1)).getbbox() is None]
 assert empty,name+' has no clear row separator'
 split=round((min(empty)+max(empty))/2); cells=[]
 for i in range(6):
  x=i%3*(w//3);y=0 if i<3 else split;ch=split if i<3 else h-split;cw=w//3
  a=mask.crop((x,y,x+cw,y+ch));b=a.getbbox();assert b
  cells.append({'sourceRect':[x,y,cw,ch],'bounds':list(b),'bottom':b[3]})
 r=cells[standing];x,y,cw,ch=r['sourceRect'];a=mask.crop((x,y,x+cw,y+ch));b=r['bounds']
 widths=[]
 for yy in range(b[1],b[1]+int((b[3]-b[1])*.4)):
  row=a.crop((0,yy,cw,yy+1)).getbbox()
  if row:widths.append(row[2]-row[0])
 head=max(widths);result['scale'][name]=120/head
 foot=a.crop((0,b[3]-8,cw,b[3])).getbbox();anchor=(foot[0]+foot[2])/2
 for r in cells:r['anchorX']=anchor
 result[name]=cells
(root/'registration.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result))
