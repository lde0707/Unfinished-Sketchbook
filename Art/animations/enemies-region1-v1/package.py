"""Slice and register generated frames; no procedural drawing or rig animation."""
import json, shutil, hashlib, statistics
from pathlib import Path
from PIL import Image
from atlas_components import split_frames
P=Path(__file__).resolve().parent
ROOT=P.parents[2]
design=json.loads((ROOT/'Art/samples/monster-design-v1/design.json').read_text(encoding='utf-8'))
enemies=[m for m in design['monsters'] if m['region']!=2]
OUT=ROOT/'Book/Assets/Unfin/Resources/EnemyAnimations'
OUT.mkdir(parents=True,exist_ok=True)
flying={'COMMON02','C03','B03','C01'}
stationary={'R102','B03','D03','COMMON03','D02'}
manifest={'version':2,'cell':384,'columns':4,'rows':3,'states':['Move','Attack','Death'],'enemies':[]}
for m in enemies:
 source=P/'sources'/f'{m["id"]}.png'
 if not source.exists():continue
 im=Image.open(source).convert('RGBA');w,h=im.size
 alpha=im.getchannel('A'); assert alpha.getextrema()[0]==0, f'{m["id"]}: background is not transparent'
 # Keep a connected attack silhouette intact when generation crosses a nominal cell edge.
 frames=split_frames(im);boxes=[(0,0,t.width,t.height) for t in frames]
 maxw=max(b[2]-b[0] for b in boxes);maxh=max(b[3]-b[1] for b in boxes)
 moveheight=statistics.median(b[3]-b[1] for b in boxes[:4])
 scale=min(236/moveheight,340/maxw,305/maxh)
 atlas=Image.new('RGBA',(1536,1152));frame_dir=P/'frames'/m['id'];frame_dir.mkdir(parents=True,exist_ok=True)
 for obsolete in range(12,16):
  old=frame_dir/f'{obsolete:02}.png'
  assert old.resolve().is_relative_to((ROOT/'Art/animations/enemies-region1-v1').resolve())
  old.unlink(missing_ok=True)
 hashes=[]
 for i,(tile,b) in enumerate(zip(frames,boxes)):
  # Uniform scale per species. Translate the drawn content to a common ground pivot.
  crop=tile.crop(b);crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
  frame=Image.new('RGBA',(384,384))
  x=round(192-(b[2]-b[0])*scale/2)
  y=round(192-crop.height/2) if m['id'] in flying and i<8 else 336-crop.height
  frame.alpha_composite(crop,(x,y));frame.save(frame_dir/f'{i:02}.png')
  atlas.alpha_composite(frame,((i%4)*384,(i//4)*384));hashes.append(hashlib.sha256(frame.tobytes()).hexdigest())
 assert len(set(hashes))>=12, (m['id'],'insufficient distinct frames')
 (P/'atlases').mkdir(exist_ok=True);atlas.save(P/'atlases'/f'{m["id"]}.png')
 shutil.copy2(P/'atlases'/f'{m["id"]}.png',OUT/f'{m["id"]}.png')
 tell,active,recovery=m['timing']
 durations=[[.12]*4,[tell*.55,tell*.45,active,recovery],[.10,.13,.18,.35]]
 entry={**m,'file':f'atlases/{m["id"]}.png','pixelsPerUnit':round(moveheight*scale/(1.375*m['height']),3),'pivotY':.5 if m['id'] in flying else .125,'stationary':m['id'] in stationary,'flying':m['id'] in flying,'durations':durations,'frameHashes':hashes}
 manifest['enemies'].append(entry)
(P/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
# Unity JSON wrapper uses concrete state classes, not unsupported jagged arrays.
runtime={'enemies':[dict(id=m['id'],name=m['name'],group=m['group'],pixelsPerUnit=m['pixelsPerUnit'],pivotY=m['pivotY'],stationary=m['stationary'],flying=m['flying'],clips=[dict(name=n,durations=d) for n,d in zip(manifest['states'],m['durations'])]) for m in manifest['enemies']]}
for e in runtime['enemies']:
 m=next(m for m in manifest['enemies'] if m['id']==e['id'])
 e['bodyHeight']=1.375*m['height'];e['bodyWidth']=e['bodyHeight']*1.15
world=json.loads((ROOT/'Book/Assets/Unfin/Data/world12.json').read_text(encoding='utf-8-sig'))
placements=[]
default={'walker':'R101','range':'R102','fly':'COMMON02','ball':'COMMON01','blob':'COMMON03','mole':'B01','spider':'C01'}
for index,e in enumerate(world['enemies']):
 if e['tx']>=280 or e['type'] not in default:continue
 x=e['x']/16;y=e['y']/16
 rooms=[r for r in world['rooms'] if r['r']==1]
 def distance(r):return max(r['x']-x,0,x-r['x']-r['w'])**2+max(r['y']-y,0,y-r['y']-r['h'])**2
 room=min(rooms,key=distance);zone=room['z'];ident=default[e['type']]
 if ident=='B01' and zone!='B':ident='R101'
 if ident=='C01' and zone!='C':ident='COMMON02'
 placements.append(dict(sourceIndex=index,id=ident,zone=zone,room=room['n'],kind=e['type']))
used=set()
requests=[('A01','A',['walker'],[3,4,5,6]),('A02','A',['ball'],[3,4,5,6]),('A03','A',['range'],[5,6]),('R103','A',['walker','ball'],[5,6]),('B02','B',['walker','ball','range'],[8,9]),('B03','B',['fly'],[8,9]),('C02','C',['walker','mole','ball'],[13,14,15,17]),('C03','C',['fly'],[14,17,15]),('D01','D',['walker'],[18,19,23]),('D02','D',['blob'],[19,23,18]),('D03','D',['range','walker'],[23,19,18])]
for ident,zone,kinds,room_ids in requests:
 candidates=[p for p in placements if p['zone']==zone and p['kind'] in kinds and p['sourceIndex'] not in used and p['room'] in room_ids]
 assert candidates, (ident,'no safe existing marker slot')
 chosen=min(candidates,key=lambda p:(kinds.index(p['kind']),room_ids.index(p['room']),p['sourceIndex']))
 chosen['id']=ident;used.add(chosen['sourceIndex'])
runtime['placements']=[p for p in placements if p['id'] in {m['id'] for m in manifest['enemies']}]
if len(manifest['enemies'])==18:assert len({p['id'] for p in placements})==18
(P/'placements.json').write_text(json.dumps(placements,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'manifest.json').write_text(json.dumps(runtime,ensure_ascii=False,indent=2),encoding='utf-8')
print(f"Packaged {len(manifest['enemies'])}/18 enemies; {len(manifest['enemies'])*12} frames. Hurt removed.")
