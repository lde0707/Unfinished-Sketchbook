from pathlib import Path
import os,json
root=Path(__file__).resolve().parents[1]
def save(p,s):
    p=root/p
    t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
def edit(p,old,new):
    s=(root/p).read_text(encoding='utf-8-sig')
    if new in s:return
    if old not in s:raise RuntimeError('Missing '+old[:75])
    save(p,s.replace(old,new,1))
p='Book/Assets/Unfin/Runtime/PaperEnemyAnimation.cs'
edit(p,'{ public int sourceIndex; public string id; }','{ public int sourceIndex; public string id; public float overrideX; }')
edit(p,'component.Initialize(data);return true;','component.Initialize(data);if(placement.overrideX>0)component.Relocate(placement.overrideX);return true;')
edit(p,'void OnDestroy(){if(receiver!=null)','public void Relocate(float x){transform.position=new Vector3(x,transform.position.y,0);origin=patrolOrigin=transform.position;}\n        void OnDestroy(){if(receiver!=null)')
edit(p,'float reach=shooter?7.5f:', 'float reach=shooter?(GetComponent<MapMarker>().sourceIndex==74?18:7.5f):')
p='Book/Assets/Unfin/Runtime/EnemyProjectileSystem.cs'
edit(p,'s.life=s.duration=3.2f;s.puddleWidth=0;','if(id=="R202"&&owner is PaperEnemyAnimation caster&&caster.GetComponent<MapMarker>().sourceIndex==74){float seconds=Mathf.Clamp(Vector2.Distance(muzzle,target)/7,1.2f,3);s.gravity=6;s.velocity=BallisticVelocity(muzzle,target,6,seconds);}\n            s.life=s.duration=3.2f;s.puddleWidth=0;')
p='Book/Assets/Unfin/Runtime/Region2Presentation.cs'
edit(p,'float ambient;int serial;','float ambient,bridgeFocus;int serial;')
edit(p,'void OnDestroy(){if(instance==this)instance=null;}','void LateUpdate(){if(world==null)return;var p=world.player.position;bool bridge=p.x>=455&&p.x<=462&&p.y>=-90&&p.y<=-82;bridgeFocus=Mathf.MoveTowards(bridgeFocus,bridge?1:0,Time.deltaTime*3);world.worldCamera.orthographicSize=Mathf.Lerp(world.Data.orthographicSize,6.5f,bridgeFocus);if(bridgeFocus>0){var c=world.worldCamera.transform.position;c.x=Mathf.Lerp(c.x,(p.x+470.5f)*.5f,bridgeFocus);world.worldCamera.transform.position=c;}}\n        void OnDestroy(){if(instance==this)instance=null;}')
p='Book/Assets/Unfin/Resources/EnemyAnimations/manifest.json'
d=json.loads((root/p).read_text(encoding='utf-8'))
for x in d['placements']:
    if x['sourceIndex'] in [74,75]:x['overrideX']=470.5 if x['sourceIndex']==74 else 419.0
save(p,json.dumps(d,ensure_ascii=False,indent=2))
print('REGION2_PUZZLES_REFINED')
