from pathlib import Path
import os
root=Path(__file__).resolve().parents[1]
def edit(path,before,after):
    p=root/path;s=p.read_text(encoding='utf-8')
    if before not in s: raise RuntimeError(f'Missing edit {path} {before[:55]}')
    s=s.replace(before,after,1);t=p.with_name(p.name+'.tmp');t.write_text(s,encoding='utf-8');os.replace(t,p)
edit('Book/Assets/Unfin/Runtime/MapMarker.cs','        void Start()\n        {','        void Start()\n        {\n            if(enemy)EnemyCombatVfx.Attach(this);')
for f,needle in [('PaperEnemyAnimation.cs','public int FrameIndex {get;private set;}'),('PaperBossRegion1.cs','public int FrameIndex {get;private set;}'),('PaperRegion2Encounter.cs','public int FrameIndex {get;private set;}'),('PaperRegion1Elite.cs','public int FrameIndex {get;private set;}')]:
    edit('Book/Assets/Unfin/Runtime/'+f,needle,needle+'\n        public float EffectClock=>clock;')
for f in ['PaperBossRegion1.cs','PaperRegion2Encounter.cs']:
    edit('Book/Assets/Unfin/Runtime/'+f,'public float EffectClock=>clock;','public float EffectClock=>clock;\n        public System.Collections.Generic.IReadOnlyList<BossDangerShape> EffectRanges=>zones;')
edit('Book/Assets/Unfin/Runtime/PaperRegion1Elite.cs','public float EffectClock=>clock;','public float EffectClock=>clock;\n        readonly BossDangerShape[] effectRanges=new BossDangerShape[1];\n        public System.Collections.Generic.IReadOnlyList<BossDangerShape> EffectRanges=>effectRanges;')
edit('Book/Assets/Unfin/Runtime/PaperRegion1Elite.cs','danger=new BossDangerShape(transform,Region2Presentation.Fx(3));','danger=new BossDangerShape(transform,Region2Presentation.Fx(3));effectRanges[0]=danger;')
print('ENEMY_COMBAT_VFX_LINKED')
