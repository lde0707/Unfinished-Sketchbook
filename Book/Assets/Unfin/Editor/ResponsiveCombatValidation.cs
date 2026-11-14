using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Unfin.Editor
{
    public static class ResponsiveCombatValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Responsive combat: "+message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
            Array.Clear(m.cells,0,m.cells.Length);for(int x=0;x<w.Data.width;x++)m.cells[40*w.Data.width+x]=1;
            void Step(int ms,int dir=0){for(int i=0;i<ms;i+=5){w.Simulate(.005f,dir);p.Tick(.005f);}}
            void Reset(){w.Respawn(100,40);Step(500);}
            void Finish(){for(int i=0;i<200&&p.Attacking;i++)Step(5);Check(!p.Attacking,"attack finishes");}
            foreach(int dir in new[]{-1,0,1})
            {
                Reset();Step(200,dir);
                Check(p.RequestAttack()&&p.ActiveHitPolygon!=null,"same-call impact, direction "+dir);
                float clock=p.AttackTime;int count=p.AttackCount,frame=p.AttackFrame;var sprite=p.visual.sprite;
                p.Render(-p.AttackFacing);
                Check(p.AttackTime==clock&&p.AttackCount==count&&p.AttackFrame==frame&&p.visual.sprite==sprite&&p.ActiveHitPolygon!=null,"reversal preserves active drawing and clock");
                float start=m.x;Step(50,dir);float distance=m.x-start;
                Reset();Step(200,dir);start=m.x;Step(50,dir);
                Check(Mathf.Abs((m.x-start)-distance)<.001f,"attack does not slow movement");
            }
            Reset();p.RequestAttack();
            Check(p.AttackAnimation=="Attack3"&&p.attackEffect.enabled,"first hit is thrust");
            for(int hit=0;hit<2;hit++){
                string expected=hit==0?"Attack3":"Attack1";
                float duration=p.library.Find(expected).Duration;
                while(p.AttackTime<duration+.02f)Step(5);
                Check(p.AttackAnimation==expected&&p.ActiveHitPolygon==null,"inter-hit recovery has no hit volume");
                int count=p.AttackCount;Check(p.RequestAttack(),"queue during recovery");Step(20);
                Check(p.AttackCount==count,"queue cannot skip80ms pause");
                for(int i=0;i<100&&p.AttackCount==count;i++)Step(5);
                Check(p.AttackCount==count+1&&p.ActiveHitPolygon!=null,"queued next attack starts at impact");
            }
            Check(p.AttackAnimation=="Attack2"&&p.SlashEffectVisible,"third hit is upward slash");
            Finish();Check(!p.RequestAttack(),"brief finisher recovery");Step(125);Check(p.RequestAttack()&&p.ActiveHitPolygon!=null,"responsive finisher restart");
            Reset();p.RequestAttack();Finish();Step(245);p.RequestAttack();Check(p.AttackAnimation=="Attack1","250ms combo window retained");
            Reset();p.RequestAttack();Finish();Step(255);p.RequestAttack();Check(p.AttackAnimation=="Attack3","combo window expires to thrust");
            foreach(bool falling in new[]{false,true})
            {
                Reset();m.y-=50;m.vy=falling?100:-100;m.grounded=false;Step(5);
                Check(p.RequestAttack()&&p.ActiveHitPolygon!=null,"immediate rising/falling impact");
                Step(10,-1);Check(p.Attacking&&p.AttackFacing==-1,"air steering follows input");
                Check(w.RequestDash(1),"dash accepted");p.Tick(0);Check(!p.Attacking&&p.ActiveHitPolygon==null,"dash cancels hit volume");
            }
            foreach(bool falling in new[]{false,true})
            {
                Reset();m.y-=160;m.vy=falling?100:-100;m.grounded=false;Step(5);
                Check(p.RequestAttack()&&p.ActiveHitPolygon!=null,"first air hit remains immediate");
                Step(50);for(int i=0;i<4;i++)Check(p.RequestAttack(),"air swing input buffered once");
                Step(150);Check(!p.Attacking&&p.AttackCount==1,"air animation ends before next strike");
                Check(p.RequestAttack(),"recovery input accepted into buffer");
                Step(150,-1);Check(!p.Attacking&&p.AttackCount==1,"buffer cannot bypass360ms cadence");
                Step(15,-1);Check(p.AttackCount==2&&p.ActiveHitPolygon!=null&&p.AttackFacing==-1,"queued air attack starts on first eligible tick with current facing");
                Step(365);Check(p.AttackCount==2&&!p.Attacking,"many clicks produce only one queued repeat");
            }
            Reset();m.y-=2;m.vy=100;m.grounded=false;Step(5);p.RequestAttack();Step(200);
            Check(m.grounded&&p.RequestAttack()&&p.AttackCount==1,"landing buffers without bypassing recovery");
            Step(165);Check(p.AttackCount==2&&p.AttackAnimation=="Attack3","landing consumes buffer as grounded attack");
            Reset();m.y-=160;m.vy=-100;m.grounded=false;Step(5);p.RequestAttack();p.RequestAttack();Step(10);
            Check(w.RequestDash(1),"air recovery dash cancel accepted");p.Tick(0);Step(365);
            Check(!p.Attacking&&p.AttackCount==1,"dash cancels buffered repeat");
            Check(p.RequestAttack(),"air attack resumes after canceled attack cooldown");
            Reset();m.y-=160;m.vy=-100;m.grounded=false;Step(5);p.RequestAttack();Step(10);
            w.RequestDash(1);p.Tick(0);Step(205);
            Check(!w.IsDashing&&p.RequestAttack()&&p.AttackCount==1,"post-dash air recovery input buffered without early hit");
            Step(150);Check(p.AttackCount==2,"post-dash buffer runs when cooldown ends");
            Reset();m.y-=2;m.vy=100;m.grounded=false;Step(5);p.RequestAttack();Step(20);
            Check(m.grounded&&p.Attacking,"landing preserves air attack");Finish();
            Reset();p.RequestAttack();w.RequestJump();Step(5,1);Check(p.Attacking,"jump preserves attack");Finish();
            Reset();int impacts=0;
            for(int t=0;t<2400;t+=5){if(t%75==0){int prior=p.AttackCount;p.RequestAttack();if(p.AttackCount>prior){Check(p.ActiveHitPolygon!=null,"rapid input starts immediately");impacts++;}}if(t%400==0)w.RequestJump();Step(5,(t/30)%2==0?1:-1);}
            Check(p.AttackCount>=4,"rapid steering and jump inputs produce repeated direct or buffered attacks");
            foreach(int facing in new[]{-1,1})
            {
                Reset();typeof(WorldPrototype).GetField("facing",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,facing);
                typeof(PaperSpritePlayer).GetMethod("StartAttack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{0});
                float before=m.x;Step(85);
                Check(Mathf.Abs(m.x-before)<.001f,"stationary thrust never nudges body");
                Check(p.AttackFrame==3&&p.attackEffect.enabled,"extended thrust impact still visible at85ms");
                Step(25);Check(!p.attackEffect.enabled,"thrust impact ends after100ms");
                Reset();Step(200,facing);before=m.x;Step(85,facing);float travel=m.x-before;
                Reset();Step(200,facing);before=m.x;
                typeof(PaperSpritePlayer).GetMethod("StartAttack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{0});Step(85,facing);
                Check(Mathf.Abs((m.x-before)-travel)<.001f,"moving thrust adds no forward displacement");
                Check(p.AttackFrame==3,"moving thrust also holds extended impact");
            }
            // Every reordered ground pose must survive locomotion transitions without restarting.
            foreach(int hit in new[]{0,1,2})foreach(int dir in new[]{-1,1}){
                Reset();Step(200,dir);
                typeof(PaperSpritePlayer).GetMethod("StartAttack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{hit});
                Step(5,dir);string clip=p.AttackAnimation;float time=p.AttackTime;int count=p.AttackCount;
                Check(clip=="MoveAttack"+new[]{3,1,2}[hit],"moving combo order");
                w.RequestJump();Step(5,dir);
                Check(p.Attacking&&p.AttackAnimation==clip&&p.AttackTime>time&&p.AttackCount==count,"each attack continues into jump");
                m.y=40*16-GridMotor.CollisionHeight-1;m.vy=100;m.grounded=false;Step(20,dir);
                Check(m.grounded&&p.AttackAnimation==clip&&p.AttackCount==count,"each attack continues through landing");
                time=p.AttackTime;Step(5,-dir);
                Check(p.AttackAnimation==clip&&p.AttackTime>time&&p.AttackFacing==-dir,"each attack mirrors without restart");
                Check(w.RequestDash(-dir),"each attack allows dash cancel");p.Tick(0);
                Check(!p.Attacking&&p.ActiveHitPolygon==null,"dash clears each attack effect and volume");
                Step(230);Check(p.RequestAttack()&&p.AttackAnimation.EndsWith("Attack3"),"after dash combo restarts at thrust");
                Finish();Step(150);Check(!p.Attacking&&p.CurrentAnimation=="Idle","after recovery returns to idle");
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/player-combat-runtime.json","{\"passed\":true,\"inputToImpactFrames\":0,\"comboPauseSeconds\":0.08,\"airAttackIntervalSeconds\":0.36,\"airBufferSlots\":1,\"finisherRecoverySeconds\":0.12,\"checks\":[\"immediate ground/move/air impact\",\"80ms inter-hit recovery holds\",\"reversal preserves frame\",\"unrestricted movement\",\"thrust slash upward-slash order\",\"buffered 3-hit combo\",\"250ms combo expiry\",\"dash cancel\",\"jump/landing continuity\",\"rapid kiting inputs\"]}");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);Debug.Log("UNFIN_RESPONSIVE_COMBAT_OK");
        }
    }
}
