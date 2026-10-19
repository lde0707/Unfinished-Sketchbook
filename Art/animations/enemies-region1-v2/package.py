"""Keep every original frame; register and insert eight newly drawn poses per selected species."""
import json, shutil, statistics, hashlib, sys
from pathlib import Path
from PIL import Image
P=Path(__file__).resolve().parent
BASE=P.with_name('enemies-region1-v1');ROOT=P.parents[2]
OUT=ROOT/'Book/Assets/Unfin/Resources/EnemyAnimations'
selected={'COMMON01','COMMON02','A02','C02'}
# Same connected-component extraction as v1, explicitly a two-row supplemental sheet.
code=(BASE/'atlas_components.py').read_text().replace('rows=3 if w/h>1.2 else 4','rows=2')
ns={};exec(compile(code,'atlas_components_v2','exec'),ns)
manifest=json.loads((BASE/'manifest.json').read_text(encoding='utf-8'))
runtime=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
manifest.update(version=3,totalFrames=248)
for m in manifest['enemies']:
 ident=m['id'];old=[Image.open(BASE/'frames'/ident/f'{i:02}.png').convert('RGBA') for i in range(12)]
 m.update(columns=4,rows=3,frameCounts=[4,4,4],starts=[0,4,8])
 frames=old
 r=next(e for e in runtime['enemies'] if e['id']==ident)
 # Canonical v1 timing is retained separately from the more detailed pose schedule.
 r['clips'][1]['motionDurations']=m['durations'][1][:]
 if ident in selected:
  source=Image.open(P/'sources'/f'{ident}.png').convert('RGBA')
  assert source.getchannel('A').getextrema()[0]==0,ident+' requires alpha'
  cuts=ns['split_frames'](source);assert len(cuts)==8
  heights=[im.getbbox()[3]-im.getbbox()[1] for im in old[:4]]
  scale=statistics.median(heights)/statistics.median(im.height for im in cuts[:4])
  scale=min(scale,346/max(im.width for im in cuts),312/max(im.height for im in cuts))
  extra=[]
  for cut in cuts:
   cut=cut.resize((round(cut.width*scale),round(cut.height*scale)),Image.Resampling.LANCZOS)
   frame=Image.new('RGBA',(384,384));y=round(192-cut.height/2) if m['flying'] else 336-cut.height
   frame.alpha_composite(cut,(round(192-cut.width/2),y));extra.append(frame)
  frames=[f for i in range(4) for f in (old[i],extra[i])]
  frames += [old[4],extra[4],old[5],extra[5],old[6],extra[6],extra[7],old[7]]+old[8:]
  tell1,tell2,active,recovery=m['durations'][1]
  m['durations']=[[.06]*8,[tell1/2,tell1/2,tell2/2,tell2/2,active*.65,active*.35,recovery*.4,recovery*.6],m['durations'][2]]
  m.update(rows=5,frameCounts=[8,8,4],starts=[0,8,16])
  for i,clip in enumerate(r['clips']):clip['durations']=m['durations'][i]
  assert all(any(im.tobytes()==original.tobytes() for im in frames) for original in old),ident+' original changed'
  assert len({hashlib.sha256(im.tobytes()).hexdigest() for im in frames})==20
 r.update(columns=4,rows=m['rows'])
 atlas=Image.new('RGBA',(1536,m['rows']*384));folder=P/'frames'/ident;folder.mkdir(parents=True,exist_ok=True)
 for i,frame in enumerate(frames):
  frame.save(folder/f'{i:02}.png');atlas.alpha_composite(frame,((i%4)*384,(i//4)*384))
 (P/'atlases').mkdir(exist_ok=True);atlas.save(P/'atlases'/f'{ident}.png')
 shutil.copy2(P/'atlases'/f'{ident}.png',OUT/f'{ident}.png')
 m['frameHashes']=[hashlib.sha256(im.tobytes()).hexdigest() for im in frames]
for name in ['placements.json','map-background.png']:shutil.copy2(BASE/name,P/name)
(P/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'manifest.json').write_text(json.dumps(runtime,ensure_ascii=False,indent=2),encoding='utf-8')
print('Packaged 248 frames: 32 newly drawn poses; all 216 originals preserved.')
