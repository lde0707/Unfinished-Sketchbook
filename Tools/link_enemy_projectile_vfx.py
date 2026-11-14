from pathlib import Path
import os
root=Path(__file__).resolve().parents[1]
def edit(file,pairs):
    p=root/file;s=p.read_text(encoding='utf-8')
    for a,b in pairs:
        if a not in s: raise RuntimeError(f'{file}: {a[:65]}')
        s=s.replace(a,b,1)
    t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
edit('Book/Assets/Unfin/Runtime/EnemyProjectileSystem.cs',[
('public float gravity,life,duration,radius,puddleWidth;','public float gravity,life,duration,radius,puddleWidth,fxCooldown;'),
('s.owner=owner;s.position=muzzle;','s.owner=owner;s.position=muzzle;s.fxCooldown=0;'),
('Vector2 previous=s.position;', 's.fxCooldown-=dt;if(s.fxCooldown<=0){s.fxCooldown=.055f;EnemyCombatEffects.Get(world)?.Projectile(s.position,s.velocity,s.kind==Kind.Pigment?new Color(.46f,.5f,.78f):s.kind==Kind.Wax?new Color(.76f,.62f,.32f):new Color(.62f,.58f,.5f),false);}\n                Vector2 previous=s.position;'),
('if(effect){PaperVfx.Get()?.Hit(s.position,', 'if(effect){EnemyCombatEffects.Get(world)?.Projectile(s.position,s.velocity,new Color(.68f,.59f,.43f),true);PaperVfx.Get()?.Hit(s.position,')])
for file,kind in [('PaperBossRegion1.cs','new Color(.62f,.58f,.5f)'),('PaperRegion2Encounter.cs','knight?new Color(.75f,.4f,.47f):new Color(.76f,.62f,.32f)')]:
    p='Book/Assets/Unfin/Runtime/'+file
    edit(p,[('public float life,gravity;', 'public float life,gravity,fxCooldown;')] if file=='PaperBossRegion1.cs' else [('public float gravity,life;', 'public float gravity,life,fxCooldown;')])
    edit(p,[('s.shape.Show(s.position,','s.fxCooldown-=dt;if(s.fxCooldown<=0){s.fxCooldown=.055f;EnemyCombatEffects.Get(world)?.Projectile(s.position,s.velocity,'+kind+',false);}\n            s.shape.Show(s.position,')])
print('ENEMY_PROJECTILE_TRAILS_LINKED')
