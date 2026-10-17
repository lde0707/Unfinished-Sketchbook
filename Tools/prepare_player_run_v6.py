"""Pose diagrams used only as drawing guides, not rendered animation parts."""
from pathlib import Path
from PIL import Image, ImageDraw
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Art/animations/player-c-v6'
(OUT/'sources').mkdir(parents=True,exist_ok=True)
# Fixed physical identity: R/near is blue in this guide; L/far is orange.
# Coordinates are an adapted short-limbed pose plan, not source joint data.
POSES=[
 {'headY':198,'near':[(332,344),(374,349),(342,366),(330,375)],'far':[(316,344),(280,389),(252,438),(261,448)],'front':'near','ground':'far'},
 {'headY':198,'near':[(332,340),(372,343),(345,374),(355,382)],'far':[(316,340),(283,377),(262,416),(270,424)],'front':'near','ground':None},
 {'headY':202,'near':[(332,348),(354,381),(357,424),(368,432)],'far':[(316,348),(291,377),(266,350),(252,347)],'front':'near','ground':None},
 {'headY':206,'near':[(332,354),(345,401),(326,439),(338,448)],'far':[(316,354),(302,388),(280,406),(285,415)],'front':'passing','ground':'near'},
 {'headY':202,'near':[(332,348),(289,391),(252,438),(262,448)],'far':[(316,348),(370,350),(337,367),(327,376)],'front':'far','ground':'near'},
 {'headY':190,'near':[(332,337),(290,376),(263,416),(271,424)],'far':[(316,337),(369,340),(339,370),(350,380)],'front':'far','ground':None},
 {'headY':194,'near':[(332,342),(295,373),(267,347),(253,344)],'far':[(316,342),(351,378),(355,424),(366,432)],'front':'far','ground':None},
 {'headY':206,'near':[(332,354),(306,388),(279,405),(285,415)],'far':[(316,354),(342,401),(325,439),(337,448)],'front':'passing','ground':'far'},
]
for half in range(2):
 im=Image.new('RGB',(1024,1024),'#242a30');d=ImageDraw.Draw(im)
 for j,p in enumerate(POSES[half*4:half*4+4]):
  ox=(j%2)*512;oy=(j//2)*512
  d.line((ox+32,oy+448,ox+480,oy+448),fill='#bec5ca',width=2)
  d.ellipse((ox+270,oy+p['headY']-80,ox+430,oy+p['headY']+80),outline='#eee1cb',width=3)
  d.line((ox+345,oy+p['headY']+77,ox+325,oy+345),fill='#d1c7b7',width=30)
  for role,col in [('far','#df8d47'),('near','#6bb8e8')]:
   points=[(ox+x,oy+y) for x,y in p[role]]
   d.line(points,fill=col,width=17,joint='curve')
   for x,y in points:d.ellipse((x-8,y-8,x+8,y+8),fill=col)
   if p['ground']==role:
    x,y=points[-1];d.line((x-13,y,x+13,y),fill=col,width=6)
  d.text((ox+20,oy+16),f'FRAME {half*4+j+1} / FRONT: {p["front"]} / SUPPORT: {p["ground"]}',fill='white')
  d.text((ox+20,oy+475),'BLUE = RIGHT / NEAR     ORANGE = LEFT / FAR',fill='white')
 im.save(OUT/'sources'/f'pose-guide-{half+1}.png')
(OUT/'pose-plan.json').write_text(json.dumps({'note':'Drawing guide only, not original skeletal data or a rig animation. Physical leg identity stays fixed throughout.', 'headYSourceDeltaPx':[0,0,1,2,1,-2,-1,2], 'headYAdaptedPx':[p['headY'] for p in POSES], 'poses':POSES},indent=2),encoding='utf-8')
