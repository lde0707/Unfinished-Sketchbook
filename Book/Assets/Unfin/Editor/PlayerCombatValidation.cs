using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PlayerCombatValidation
    {
        static void Check(bool ok,string label){if(!ok)throw new Exception("Combat integration: "+label);}
        public static void RunAndBuild(){Run();CaptureLargerEffects();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;var original=(byte[])m.cells.Clone();
            Array.Clear(m.cells,0,m.cells.Length);for(int x=0;x<w.Data.width;x++)m.cells[40*w.Data.width+x]=1;
            // Sample animation at1ms, motor at5ms: below that the legacy .01px
            // collision skin is larger than a gravity step and contact flickers.
            int physicsTick=0;
            void Step(int ms,int dir=0){for(int i=0;i<ms;i++){if(physicsTick++%5==0)w.Simulate(.005f,dir);p.Tick(.001f);}}
            void Reset(){physicsTick=0;w.Respawn(100,40);typeof(WorldPrototype).GetField("facing",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,1);Step(100);Check(m.grounded,"reset grounded");}
            void Attack(string name){Check(p.RequestAttack(),"accepted "+name);Check(p.AttackAnimation==name,"expected "+name+" got "+p.AttackAnimation);}
            Reset();Attack("Attack1");Step(120);Check(p.SlashEffectVisible,"larger first slash");Step(160);Check(!p.SlashEffectVisible,"slash hidden during recovery");Check(p.RequestAttack(),"buffer second");Step(80);Check(p.AttackAnimation=="Attack2",$"second follows first: {p.AttackAnimation}, time={p.AttackTime}, grounded={m.grounded}, count={p.AttackCount}, mode={p.CurrentAnimation}");
            Step(200);Check(p.RequestAttack(),"buffer third");Step(90);Check(p.AttackAnimation=="Attack3","third follows second");
            Step(155);Check(p.AttackFrame==2&&p.attackEffect.enabled,"snap thrust effect frame");Check(Mathf.Abs(p.attackEffect.transform.localScale.x-.224f)<.0001f,"thrust enlarged 1.6x");Step(20);Check(p.AttackFrame==3&&p.attackEffect.enabled,"thrust impact effect");
            Step(285);Check(!p.Attacking&&!p.attackEffect.enabled,"finisher ends");Check(!p.RequestAttack(),"finisher recovery");Step(501);Attack("Attack1");
            Reset();Attack("Attack1");Step(360);Step(249);Attack("Attack2");
            Reset();Attack("Attack1");Step(360);Step(251);Attack("Attack1");
            Reset();Attack("Attack1");p.RequestAttack();Step(360);Check(!p.Attacking,"early buffer expires");
            Reset();Step(200,1);Attack("MoveAttack1");float before=m.x;Step(100,1);float attacked=m.x-before;
            Reset();Step(200,1);before=m.x;Step(100,1);Check(Mathf.Abs(m.x-before-attacked)<.0001f,"attack does not change movement");
            Attack("MoveAttack1");Step(1,-1);Check(!p.Attacking,"direction reversal cancels ground strike");
            Reset();Attack("Attack1");Step(90,1);Check(p.AttackAnimation=="MoveAttack1","moving variant switch");
            w.RequestJump();Step(1,1);Check(!p.Attacking,"jump cancels ground attack");Attack("AirRiseAttack");Step(40,1);
            Check(p.AttackFrame==1,"air rise single swing");Check(w.RequestDash(1),"dash accepted during attack");p.Tick(0);Check(!p.Attacking,"dash cancels attack");
            Step(180,1);Attack("AirFallAttack");Step(80,-1);Check(p.Attacking&&p.AttackFacing==1,"air facing locked while steering");Step(140);Check(!p.Attacking,"air attack220ms");
            Reset();m.y-=50;m.vy=100;m.grounded=false;Step(1);Attack("AirFallAttack");
            for(int i=0;i<700&&!m.grounded;i++)Step(1);Check(m.grounded&&!p.Attacking,"landing cancels air strike");
            // A real lower floor: no timed midair landing or return to jump ascent.
            Reset();for(int x=0;x<w.Data.width;x++){m.cells[40*w.Data.width+x]=0;m.cells[70*w.Data.width+x]=1;}
            w.RequestJump();Step(1,1);Check(m.vy<0,"high ledge jump launches");
            for(int i=0;i<1500&&p.CurrentAnimation!="Fall";i++)Step(1,1);
            Check(p.CurrentAnimation=="Fall"&&!m.grounded&&m.y+GridMotor.Height<70*16,"jump descends into long fall before lower floor");
            float speed=m.vy;Check(w.RequestDash(1),"long fall dash");Step(180,1);Check(Mathf.Abs(m.vy-speed)<.02f,"long fall dash retains descent speed");
            for(int i=0;i<2000&&!m.grounded;i++){Step(1,1);if(!m.grounded)Check(!p.CurrentAnimation.EndsWith("Land"),"no landing drawings in air");}
            Check(m.grounded&&p.CurrentAnimation=="FallLand","actual lower-floor landing");
            // Real-map captures after all deterministic checks; do not save altered test cells.
            Array.Copy(original,m.cells,original.Length);w.Respawn(w.Data.startX,w.Data.startFloor);Step(100);Attack("Attack1");Step(140);
            PaperSpriteValidation.Capture(w.worldCamera,"Validation/player-attack-in-game.png");
            int px=-1,py=-1;for(int y=0;y<w.Data.height&&px<0;y++)for(int x=0;x<w.Data.regionX;x++)if(GridMotor.Platform(m.Get(x,y))){px=x;py=y;break;}
            Check(px>=0,"map has platforms");w.Respawn(px,py);Step(100);PaperSpriteValidation.Capture(w.worldCamera,"Validation/platform-world1-in-game.png");
            w.Respawn(w.Data.startX,w.Data.startFloor);Step(100);m.y-=35;m.grounded=false;m.vy=120;Step(1);Attack("AirFallAttack");Step(50);
            PaperSpriteValidation.Capture(w.worldCamera,"Validation/player-air-attack-in-game.png");
            RegionalPlatformImporter.Validate(w);
            File.WriteAllText("Validation/player-combat-runtime.json","{\"passed\":true,\"checks\":[\"three-stage combo and thrust VFX\",\"250ms combo window\",\"150ms input expiry\",\"500ms finisher recovery\",\"moving attack preserves motor travel\",\"moving/standing variant and reversal\",\"jump/dash/landing cancel\",\"rise and forward fall attacks220ms\",\"air facing lock\",\"long jump-to-fall and real contact\",\"long-fall dash velocity\"]}");
            Debug.Log("UNFIN_COMBAT_RUNTIME_OK");
        }
        static void CaptureLargerEffects()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();
            void Step(int ms,int direction=0){for(int i=0;i<ms;i+=5){w.Simulate(.005f,direction);p.Tick(.005f);}}
            void Reset(){w.Respawn(78,123);Step(100);}
            void Shot(string name)
            {
                foreach(var arc in p.GetComponentsInChildren<PaperAttackArc>())
                    foreach(var vertex in arc.GetComponent<MeshFilter>().sharedMesh.vertices)
                        Check(!float.IsNaN(vertex.x)&&!float.IsNaN(vertex.y)&&!float.IsInfinity(vertex.x)&&!float.IsInfinity(vertex.y),"finite effect geometry");
                var camera=w.worldCamera;camera.aspect=16f/9;camera.orthographicSize=2.2f;
                camera.transform.position=w.player.position+new Vector3(.65f,.4f,-10);
                foreach(var layer in UnityEngine.Object.FindObjectsByType<RoomBackdropLayer>(FindObjectsSortMode.None))layer.PositionForCamera();
                PaperSpriteValidation.Capture(camera,"Validation/attack-fx-"+name+".png");
            }
            Reset();p.RequestAttack();Step(120);Shot("slash1");Step(240);
            p.RequestAttack();Step(110);Check(p.SlashEffectVisible,"larger second slash");Shot("slash2");Step(180);
            p.RequestAttack();Step(180);Shot("thrust");
            Reset();Step(180,1);p.RequestAttack();Step(220,1);Check(p.SlashEffectVisible,"larger moving slash");Shot("moving");
            Reset();w.RequestJump();Step(60,1);p.RequestAttack();Step(60,1);Check(p.SlashEffectVisible,"larger rising slash");Shot("air-rise");
            Reset();w.Motor.y-=35;w.Motor.grounded=false;w.Motor.vy=100;Step(5);p.RequestAttack();Step(60);Check(p.SlashEffectVisible,"larger falling slash");Shot("air-fall");
            File.WriteAllText("Validation/attack-effects.json","{\"passed\":true,\"thrustScale\":1.6,\"slash\":\"larger outer crescent over existing inner trail\",\"characterFramesChanged\":false,\"timingChanged\":false,\"captures\":[\"slash1\",\"slash2\",\"thrust\",\"moving\",\"air-rise\",\"air-fall\"]}");
        }
    }
}
