from pathlib import Path
import os
root=Path(__file__).resolve().parents[1]
def edit(name,old,new):
    p=root/name;s=p.read_text(encoding='utf-8-sig')
    if new in s:return
    if old not in s:raise RuntimeError(name+' missing '+old[:70])
    t=p.with_name(p.name+'.tmp');t.write_text(s.replace(old,new,1),encoding='utf-8');os.replace(t,p)
edit('Book/Assets/Unfin/Runtime/PaperRegion2Encounter.cs','spear.sortingOrder=2;','spear.sortingOrder=-2;')
edit('Book/Assets/Unfin/Runtime/PaperRegion2Encounter.cs','start=facing<0?room.x+1:room.x+room.w-3;','start=facing<0?room.x-1:room.x+room.w-1;')
edit('Book/Assets/Unfin/Editor/Region2Integration.cs','BossRegion1Integration.Validate();Validate();Render();','BossRegion1Integration.Validate();Validate();Region2PuzzleValidation.Validate();Render();')
edit('Book/Assets/Unfin/Runtime/PaperEnemyAnimation.cs','visual.sprite=frames[0];visual.color=Color.white;','visual.sprite=frames[0];visual.color=enraged?new Color(1,.78f,.72f):Color.white;')
edit('Book/Assets/Unfin/Runtime/PaperEnemyAnimation.cs','visual.enabled=true;visual.color=Color.white;receiver.enabled=true;','visual.enabled=true;visual.color=enraged?new Color(1,.78f,.72f):Color.white;receiver.enabled=true;')
print('REGION2_FINAL_NATIVE_CHECKS_READY')
