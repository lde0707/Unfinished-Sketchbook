from pathlib import Path
import json,os
root=Path(__file__).resolve().parents[1]
def save(p,s):
    p=root/p;t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
def edit(p,old,new):
    s=(root/p).read_text(encoding='utf-8-sig')
    if new in s:return
    if old not in s:raise RuntimeError('Missing '+old[:70])
    save(p,s.replace(old,new,1))
edit('Book/Assets/Unfin/Runtime/Region2Presentation.cs','public sealed class Region2Presentation : MonoBehaviour','[DefaultExecutionOrder(200)]\n    public sealed class Region2Presentation : MonoBehaviour')
p='Book/Assets/Unfin/Runtime/PaperEnemyAnimation.cs'
edit(p,'bool deathParticles;','bool deathParticles,enraged;')
edit(p,'Health=data.health>0?data.health:3;','enraged=Region2&&GetComponent<MapMarker>().originalOptions.Contains("\\\"red\\\":true");Health=(data.health>0?data.health:3)+(enraged?2:0);')
edit(p,'width=Mathf.Max(.25f,visual.bounds.size.x*.33f);','width=Mathf.Max(.25f,visual.bounds.size.x*.33f);var m=GetComponent<MapMarker>();if(m.sourceIndex==74||m.sourceIndex==75)gameObject.AddComponent<Region2KeeperVisual>().Initialize(world,m.sourceIndex);')
edit(p,'Health=source.health>0?source.health:3;','Health=(source.health>0?source.health:3)+(enraged?2:0);')
edit(p,'dt=Mathf.Min(dt,.05f);clock+=dt;cooldown-=dt;','dt=Mathf.Min(dt,.05f);if(enraged&&state<2)dt*=1.5f;clock+=dt;cooldown-=dt;')
edit(p,'if(SpeciesId=="H03"&&attackSound&&!secondShot','if(SpeciesId=="R201"&&attackSound&&!secondShot&&clock>=timings[0]+timings[1]+.36f){secondShot=true;GameAudio.Emit(SoundCue.EnemyAttack,transform.position,.65f);Region2Presentation.Burst(world,transform.position+Vector3.up*.5f,SpeciesId,direction);}\n                if(SpeciesId=="H03"&&attackSound&&!secondShot')
p='Book/Assets/Unfin/Runtime/PaperRegion2Encounter.cs'
edit(p,'static Region2Actors catalog;','static readonly string[] KnightStates={"Thrust","Throw","Rush","Slam"};\n        static Region2Actors catalog;')
edit(p,'knight?new[]{"Thrust","Throw","Rush","Slam"}[Pattern]','knight?KnightStates[Pattern]')
edit(p,'WorldPrototype world;PaperSpritePlayer player;','MaterialPropertyBlock properties;WorldPrototype world;PaperSpritePlayer player;')
edit(p,'var block=new MaterialPropertyBlock();visual.GetPropertyBlock(block);','if(properties==null)properties=new MaterialPropertyBlock();var block=properties;visual.GetPropertyBlock(block);')
p='Book/Assets/Unfin/Editor/Region2Integration.cs'
edit(p,'float s=Mathf.Abs(x-r.x-r.w*.5f)+(r.y+r.h-y)*.3f;','float s=Mathf.Abs(x-r.x-r.w*.5f)+(r.y+r.h-y)*.3f;foreach(var e in w.Data.enemies)if(Mathf.Abs(e.tx-x)<3&&Mathf.Abs(e.floor-y)<2)s+=12;')
edit(p,'Vector2Int best=new Vector2Int(r.x+2,r.y+r.h);','if(r.n==20&&r.r==2)return new Vector2Int(518,50);if(r.n==10&&r.r==2)return new Vector2Int(379,94);Vector2Int best=new Vector2Int(r.x+2,r.y+r.h);')
edit(p,'w.Respawn(spawn.x,spawn.y);player.Tick(.001f);','w.Respawn(spawn.x,spawn.y);w.Motor.vx=0;w.Motor.vy=20;w.Motor.Move(.1f);Private(w,"SyncView");player.Tick(.001f);')
edit(p,'w.Respawn(465,124);player.Tick(.001f);','w.Respawn(465,124);w.Motor.vx=0;w.Motor.vy=20;w.Motor.Move(.1f);Private(w,"SyncView");player.Tick(.001f);')
p='Book/Assets/Unfin/Resources/EnemyAnimations/manifest.json';d=json.loads((root/p).read_text(encoding='utf-8'))
for item in d['placements']:
    if item['sourceIndex']==80:item['overrideX']=476.5
save(p,json.dumps(d,ensure_ascii=False,indent=2))
print('REGION2_PRESENTATION_AND_CAPTURE_POLISHED')
