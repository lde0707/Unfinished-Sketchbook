from pathlib import Path
import os

root=Path(__file__).resolve().parents[1]
def edit(file, pairs):
    p=root/file;s=p.read_text(encoding='utf-8')
    for before,after in pairs:
        if before not in s: raise RuntimeError(f'Missing edit in {file}: {before[:75]}')
        s=s.replace(before,after,1)
    tmp=p.with_name(p.name+'.tmp');tmp.write_text(s,encoding='utf-8');os.replace(tmp,p)

edit('Book/Assets/Unfin/Runtime/WorldPrototype.cs',[
('float jumpBuffer, coyote, wallLock, dashTime, dashCooldown, accumulator;', 'float jumpBuffer, coyote, wallLock, dashTime, dashCooldown, accumulator;\n        float actionPause;\n        public bool ActionLocked=>actionPause>0;\n        public void PausePlayerOnHit(float seconds){actionPause=Mathf.Max(actionPause,seconds);MoveInput=0;}'),
('if(HazardInputBlocked||showMap||flying||IsDashing||dashCooldown>0', 'if(HazardInputBlocked||ActionLocked||showMap||flying||IsDashing||dashCooldown>0'),
('            MoveInput=ix;','            if(actionPause>0){actionPause=Mathf.Max(0,actionPause-dt);MoveInput=0;TickChanges(dt);return;}\n            MoveInput=ix;'),
('drowning=recovering=0;releaseAfterHazard=false;','drowning=recovering=actionPause=0;releaseAfterHazard=false;')])
edit('Book/Assets/Unfin/Runtime/WorldProgression.cs',[
('public bool Parrying=>parryTime>0;','public bool Parrying=>parryTime>0;\n        public int ParrySerial {get;private set;}\n        public int ParrySuccessSerial {get;private set;}\n        bool parryConfirmed;'),
('public void ConfirmParry(){if(parryTime>0)parryCooldown=Mathf.Min(parryCooldown,.08f);}', 'public void ConfirmParry(){if(parryTime<=0)return;parryCooldown=Mathf.Min(parryCooldown,.08f);if(parryConfirmed)return;parryConfirmed=true;ParrySuccessSerial++;Region2Presentation.Burst(world,world.player.position+Vector3.right*world.Facing*.4f,"parry",world.Facing);}'),
('if(!Has("parry")||parryCooldown>0||world.HazardInputBlocked||world.MapOpen)return;', 'if(!Has("parry")||parryCooldown>0||world.HazardInputBlocked||world.ActionLocked||world.MapOpen)return;'),
('parryTime=.16f;parryCooldown=.5f;', 'parryTime=.16f;parryCooldown=.5f;parryConfirmed=false;ParrySerial++;')])
edit('Book/Assets/Unfin/Runtime/PaperSpritePlayer.cs',[
('world.Progression.Damage();','bool shielded=world.Progression.ShieldReady;world.Progression.Damage();\n            if(!shielded&&!world.HazardInputBlocked)BeginHurt();'),
('AnimationTime=0;ResetCombat();','AnimationTime=0;ResetCombat();ResetReactions();'),
('            AnimationTime+=dt*','            TickReactions(dt);\n            AnimationTime+=dt*'),
('            RenderCombat(clip,facing);','            RenderCombat(clip,facing);\n            RenderReaction();')])
edit('Book/Assets/Unfin/Runtime/PaperSpriteCombat.cs',[
('public string DisplayedAnimation => AttackAnimation??CurrentAnimation;', 'public string DisplayedAnimation => AttackAnimation??ReactionAnimation??CurrentAnimation;'),
('hitTargets.Clear();attackVfxEmitted=false;', 'reaction=null;hitTargets.Clear();attackVfxEmitted=false;'),
('world.HazardInputBlocked||world.flying||world.MapOpen||world.IsDashing||library==null', 'world.HazardInputBlocked||world.ActionLocked||world.Progression.Parrying||world.flying||world.MapOpen||world.IsDashing||library==null')])
edit('Book/Assets/Unfin/Runtime/MapMarker.cs',[
('if(PaperBossRegion1.Attach(this))return;', 'if(PaperRegion1Elite.Attach(this))return;\n            if(PaperBossRegion1.Attach(this))return;')])
edit('Book/Assets/Unfin/Runtime/PaperAttackTarget.cs',[
('public Bounds HitBounds => TryGetComponent<PaperBossRegion1>', 'public Bounds HitBounds => TryGetComponent<PaperRegion1Elite>(out var elite)?elite.BodyBounds:TryGetComponent<PaperBossRegion1>')])
edit('Book/Assets/Unfin/Runtime/PaperSpriteReactions.cs',[
('guard.sprite=PaperDeviceVisual.Load("target");','guard.sprite=Region2Presentation.Fx(5);')])
print('ANIMATION_COMPLETION_LINKED')
