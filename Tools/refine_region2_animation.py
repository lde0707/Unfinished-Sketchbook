from pathlib import Path
import os,json
root=Path(__file__).resolve().parents[1]
def edit(p,old,new):
    p=root/p;s=p.read_text(encoding='utf-8-sig')
    if new in s:return
    if old not in s:raise RuntimeError('Missing '+old[:70])
    t=p.with_name(p.name+'.tmp');t.write_text(s.replace(old,new,1),encoding='utf-8');os.replace(t,p)
p='Book/Assets/Unfin/Runtime/EnemyProjectileSystem.cs'
edit(p,'s.position=muzzle+new Vector2(-facing*.65f,-.45f);MakePuddle(s,owner is PaperEnemyAnimation snail?snail.transform.position.y:s.position.y);','s.position=(Vector2)(owner is PaperEnemyAnimation snail?snail.transform.position:(Vector3)muzzle)+new Vector2(-facing*.8f,.035f);MakePuddle(s,owner is PaperEnemyAnimation slug?slug.transform.position.y:s.position.y);')
edit(p,'other.owner=owner;other.position=muzzle;other.velocity=','other.owner=owner;other.position=owner is PaperEnemyAnimation vat?(Vector2)vat.transform.position+new Vector2(-facing*vat.BodyBounds.size.x*.55f,vat.BodyBounds.size.y*.55f):muzzle;other.velocity=')
p='Book/Assets/Unfin/Runtime/EnemyAttackMotion.cs'
edit(p,'case "R203":return new Profile(3.6f,0,3.6f);','case "R203":return new Profile(6,0,6);')
p='Book/Assets/Unfin/Runtime/PaperEnemyAnimation.cs'
edit(p,'if(cooldown<=0&&(!shooter||OnScreen())','if(cooldown<=0&&(!shooter&&!Region2||OnScreen())')
edit(p,'float verticalReach=shooter?4:source.flying?5:2;','float verticalReach=shooter?4:SpeciesId=="R203"?6:SpeciesId=="G01"?4:source.flying?5:2;')
edit(p,'Vector2 attackTarget;','Vector2 attackTarget,hopLanding;')
edit(p,'if(SpeciesId=="E03")attackAim=new Vector2(direction*4,0);','if(SpeciesId=="E03")attackAim=new Vector2(direction*4,0);\n                if(SpeciesId=="G01")hopLanding=FindHopLanding();')
edit(p,'void MoveAttack()','Vector2 FindHopLanding(){var best=(Vector2)attackOrigin;float score=float.PositiveInfinity;for(float distance=3;distance>=.5f;distance-=.25f){float x=attackOrigin.x+direction*distance;for(int row=Mathf.FloorToInt(-attackOrigin.y)-3;row<=Mathf.FloorToInt(-attackOrigin.y)+3;row++){int a=world.Motor.Get(Mathf.FloorToInt(x-.35f),row),b=world.Motor.Get(Mathf.FloorToInt(x+.35f),row);if((!GridMotor.Wall(a)&&!GridMotor.Platform(a))||(!GridMotor.Wall(b)&&!GridMotor.Platform(b))||GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(x),row-1))||GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(x),row-2)))continue;float s=Mathf.Abs(-row+source.bodyHeight*.5f-attackTarget.y)+(3-distance)*.4f;if(s<score){score=s;best=new Vector2(x,-row);}}}return best;}\n        void MoveAttack()')
edit(p,'Vector2 desired=(Vector2)attackOrigin+offset;\n            MoveSafely(desired,!source.flying,attackOrigin.y);','Vector2 desired=(Vector2)attackOrigin+offset;\n            if(SpeciesId=="G01"){float t=Mathf.Clamp01((clock-timings[0]-timings[1])/timings[2]);desired=Vector2.Lerp(attackOrigin,hopLanding,t)+Vector2.up*(1.2f*4*t*(1-t));MoveSafely(desired,false,hopLanding.y);}\n            else MoveSafely(desired,!source.flying,attackOrigin.y);')
p='Book/Assets/Unfin/Runtime/PaperBossRegion1.cs'
edit(p,'if(pause<=0)BeginPattern(NextPattern());','if(Mathf.Abs(pp.x-transform.position.x)>6.5f)pause=Mathf.Max(pause,.1f);else if(pause<=0)BeginPattern(NextPattern());')
p='Book/Assets/Unfin/Runtime/PaperRegion2Encounter.cs'
edit(p,'if(pause<=0)BeginPattern(Next());','if(Mathf.Abs(pp.x-transform.position.x)>6.5f)pause=Mathf.Max(pause,.1f);else if(pause<=0)BeginPattern(Next());')
p=root/'Book/Assets/Unfin/Resources/EnemyAnimations/manifest.json';d=json.loads(p.read_text(encoding='utf-8'));r=next(e for e in d['enemies'] if e['id']=='R201');r['clips'][1]['durations']=[.175,.175,.175,.175,.08,.28,.1,1.05];t=p.with_name(p.name+'.tmp');t.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8');os.replace(t,p)
p='Book/Assets/Unfin/Editor/Region2Integration.cs'
edit(p,'var spawn=Spawn(w,r);w.Respawn(spawn.x,spawn.y);','var spawn=Spawn(w,r);w.worldCamera.orthographicSize=w.Data.orthographicSize;w.worldCamera.aspect=16f/9;w.Respawn(spawn.x,spawn.y);')
edit(p,'camera.transform.position=new Vector3(spawn.x+.5f,-spawn.y+3.2f,-10);Capture(camera,','Capture(camera,')
print('REGION2_FRAME_AND_MOTION_REFINED')
