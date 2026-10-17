from pathlib import Path
import json,shutil,hashlib
root=Path('Art/animations'); out=Path('Book/Assets/Unfin/Art/PlayerSprites')
specs=[('Idle','player-c-idle-v1',368,300),('Run','player-c-v6-rhythm',340,80),('RunStop','player-c-run-idle-v1',340,0),('Jump','player-c-jump-v2',368,0),('MovingJump','player-c-moving-jump-v1',340,0),('Dash','player-c-dash-v2',364,0),('Fall','player-c-fall-v1',368,0)]
manifest={'pixelsPerUnit':256,'groundY':448,'clips':[]}
for name,folder,anchor,default in specs:
 reg=json.loads((root/folder/'registration.json').read_text(encoding='utf-8'))
 frames=[]; dest=out/name; dest.mkdir(parents=True,exist_ok=True)
 for i,f in enumerate(reg['frames']):
  src=root/folder/'frames'/f'{i:02}.png'; target=dest/src.name; shutil.copyfile(src,target)
  frames.append({'file':f'{name}/{src.name}','duration':(default or f.get('durationMs',140))/1000,'headX':f['headCenter'][0],'headY':f['headCenter'][1],'sha256':hashlib.sha256(src.read_bytes()).hexdigest()})
 manifest['clips'].append({'name':name,'source':folder,'anchorX':anchor,'loop':name in ('Idle','Run'),'frames':frames})
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print(sum(len(c['frames']) for c in manifest['clips']), 'unchanged source frames copied')
