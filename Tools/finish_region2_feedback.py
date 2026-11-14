from pathlib import Path
import os
root=Path(__file__).resolve().parents[1]
def edit(p,old,new):
    p=root/p;s=p.read_text(encoding='utf-8-sig')
    if new in s:return
    if old not in s:raise RuntimeError('Missing '+old[:80])
    t=p.with_name(p.name+'.tmp');t.write_text(s.replace(old,new,1),encoding='utf-8');os.replace(t,p)
p='Book/Assets/Unfin/Runtime/EnemyProjectileSystem.cs'
edit(p,'s.draw.transform.localScale=new Vector3(s.puddleWidth/.45f,.2f,1);','s.draw.transform.localScale=s.wax?new Vector3(s.puddleWidth/s.draw.sprite.bounds.size.x,.12f/s.draw.sprite.bounds.size.y,1):new Vector3(s.puddleWidth/.45f,.2f,1);')
edit(p,'s.draw.transform.rotation=Quaternion.Euler(0,0,angle);s.draw.color=','s.draw.transform.rotation=Quaternion.Euler(0,0,angle);if(s.kind==Kind.Wax||s.kind==Kind.Pigment||s.kind==Kind.Seal)s.draw.transform.localScale=Vector3.one*(s.radius*2/Mathf.Max(s.draw.sprite.bounds.size.x,s.draw.sprite.bounds.size.y));s.draw.color=')
p='Book/Assets/Unfin/Runtime/Region2Presentation.cs'
edit(p,'w.maps[3].SetColor(WorldData.Cell(x,y),Color.clear);','{w.maps[3].SetTileFlags(WorldData.Cell(x,y),UnityEngine.Tilemaps.TileFlags.None);w.maps[3].SetColor(WorldData.Cell(x,y),Color.clear);}')
p='Book/Assets/Unfin/Editor/Region2PuzzleValidation.cs'
edit(p,'static void Aim(WorldPrototype w,PaperEnemyAnimation enemy){','static void Aim(WorldPrototype w,PaperEnemyAnimation enemy,EnemyProjectileSystem system){')
edit(p,'w.Progression.Tick(.05f,0,0);enemy.Tick(.05f);','w.Progression.Tick(.05f,0,0);enemy.Tick(.05f);system.Tick(.05f);')
edit(p,'Aim(w,gunner);','Aim(w,gunner,system);')
edit(p,'Aim(w,thrower);','Aim(w,thrower,system);')
print('REGION2_VISUAL_RANGES_MATCHED')
