using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class EnemySpecialtyValidation
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string message){if(!ok)throw new Exception("Enemy specialty: "+message);}
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
        static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
        static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
        static void Clock(PaperEnemyAnimation e,float time)=>typeof(PaperEnemyAnimation).GetProperty("clock",Private).SetValue(e,time);
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Call(w,"Awake");
            foreach(var title in UnityEngine.Object.FindObjectsByType<StilldrawnTitle>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(title);
            Array.Clear(w.Motor.cells,0,w.Motor.cells.Length);for(int x=0;x<w.Data.width;x++)w.Motor.cells[80*w.Data.width+x]=1;
            w.Respawn(105,80);w.flying=true;w.player.position=new Vector3(105,-79.3f,0);
            PaperEnemyAnimation Actor(string id){var g=new GameObject("Specialty validation "+id);g.AddComponent<SpriteRenderer>();var e=g.AddComponent<PaperEnemyAnimation>();e.Initialize(PaperEnemyAnimation.Manifest.enemies.First(a=>a.id==id));e.transform.position=new Vector3(100,-80,0);Set(e,"origin",e.transform.position);Set(e,"patrolOrigin",e.transform.position);return e;}
            void Destroy(PaperEnemyAnimation e)=>UnityEngine.Object.DestroyImmediate(e.gameObject);
            void Step(PaperEnemyAnimation e,float seconds){for(float t=0;t<seconds-.0001f;t+=.005f)e.Tick(.005f);}
            Check(PaperEnemyAnimation.Manifest.enemies.Length==33,"33 defined species");
            foreach(var spec in PaperEnemyAnimation.Manifest.enemies){
                string original=JsonUtility.ToJson(spec);var e=Actor(spec.id);var p=e.Specialty;var art=e.Art;
                Check(!string.IsNullOrWhiteSpace(p.strength)&&!string.IsNullOrWhiteSpace(p.counter),spec.id+" strength and counter");
                Check(art.clips[1].durations.All(t=>t>0),spec.id+" positive frames");
                Check(art.clips[1].durations.Length==spec.clips[1].durations.Length,spec.id+" existing whole frames retained");
                Check(Mathf.Abs(art.clips[1].durations.Sum()-(p.windup+p.active+p.recovery)*1.12f)<.001f,spec.id+" phase synchronized");
                if(art.attackWindows!=null)Check(art.attackWindows.All(t=>t>=0&&t<=art.clips[1].durations.Sum()),spec.id+" damage windows in clip");
                Call(e,"SetState",EnemyAnimationState.Attack);float before=e.EffectClock;w.ToggleExplorationMap();e.Tick(.05f);Check(e.EffectClock==before,spec.id+" paused");w.ToggleExplorationMap();
                float elapsed=0;while(e.StateName=="Attack"&&elapsed<8){e.Tick(.005f);elapsed+=.005f;}
                Check(e.StateName=="Move"&&Mathf.Abs(elapsed-(p.windup+p.active+p.recovery))<.012f,spec.id+" attack finishes");
                Check(Mathf.Abs(Read<float>(e,"cooldown")-p.cooldown)<.001f,spec.id+" individual cooldown");
                Check(JsonUtility.ToJson(spec)==original,spec.id+" shared asset not mutated");
                e.ResetEnemy();int hp=e.Health;var receiver=e.GetComponent<PaperAttackTarget>();
                if(p.guard!=EnemyGuard.None){
                    receiver.ReceiveHit(-1,e.BodyBounds.center);Check(e.Health==hp&&e.BlockedHits>0,spec.id+" guarding blocks actual receiver damage");
                    if(p.guard==EnemyGuard.Front){receiver.ReceiveHit(1,e.BodyBounds.center);Check(e.Health==hp-1,spec.id+" rear is vulnerable");}
                    Call(e,"SetState",EnemyAnimationState.Attack);Clock(e,(p.windup+p.active+.05f)*1.12f);hp=e.Health;
                    receiver.ReceiveHit(-1,e.BodyBounds.center);Check(e.Health==hp-1&&!e.Guarding,spec.id+" recovery exposes body");
                }
                e.ResetEnemy();Call(e,"SetState",EnemyAnimationState.Attack);Clock(e,(p.windup+p.active+.05f)*1.12f);hp=e.Health;
                receiver.ReceiveHit(1,e.BodyBounds.center);Check(e.Health==hp-1&&e.StateName=="Attack",spec.id+" vulnerable hit retains animation");
                for(int i=0;i<8&&e.Health>0;i++)receiver.ReceiveHit(1,e.BodyBounds.center);
                Check(e.StateName=="Death"&&!receiver.enabled,spec.id+" death removes target");Step(e,art.clips[2].durations.Sum()+1);
                Check(!e.GetComponent<SpriteRenderer>().enabled,spec.id+" death fade");e.ResetEnemy();Check(e.Health==Math.Max(3,spec.health)&&receiver.enabled,spec.id+" reset");
                Destroy(e);
            }
            // Verify trajectories, not just configured values.
            foreach(string id in new[]{"C03","E03","B03","C01","COMMON02"}){
                var e=Actor(id);e.transform.position=new Vector3(100,-75,0);w.player.position=new Vector3(102,-78,0);Call(e,"SetState",EnemyAnimationState.Attack);
                Vector3 start=e.transform.position;Step(e,e.Specialty.windup+e.Specialty.active*.75f);var d=e.transform.position-start;
                if(e.Specialty.lane==EnemyLane.Vertical)Check(Mathf.Abs(d.x)<.01f&&d.y<-.3f,id+" vertical drop");
                else if(e.Specialty.lane==EnemyLane.Horizontal)Check(d.x>1&&Mathf.Abs(d.y)<.01f,id+" horizontal flight");
                else Check(d.x>.2f&&d.y<-.2f,id+" aimed dive");Destroy(e);
            }
            w.player.position=new Vector3(105,-79.3f,0);var dog=Actor("D01");var worm=Actor("R101");Set(dog,"cooldown",10f);Set(worm,"cooldown",10f);Step(dog,.4f);Step(worm,.4f);Check(dog.transform.position.x>worm.transform.position.x+.25f,"dog distinctly faster pursuit");Destroy(dog);Destroy(worm);
            var shield=Actor("G02");Call(shield,"Face",1f);Call(shield,"Face",-1f);Check(Read<int>(shield,"direction")==1,"shield cannot instantly deny flank");Destroy(shield);
            var counter=Actor("H02");counter.GetComponent<PaperAttackTarget>().ReceiveHit(-1);Check(counter.StateName=="Attack"&&counter.EffectClock==0,"shield counter has full telegraph");Destroy(counter);
            foreach(string id in new[]{"D03","F01"}){var e=Actor(id);var b=e.AttackBounds;Check(b.size.x>e.BodyBounds.size.x+1&&b.max.y<e.BodyBounds.min.y+.5f,id+" broad ground sweep remains jumpable");Destroy(e);}
            var caster=Actor("R202");w.player.position=new Vector3(102,-79.3f,0);Set(caster,"cooldown",10f);Step(caster,.4f);Check(caster.transform.position.x<99.7f,"thrower retreats on approach");Destroy(caster);
            // Ink occupies ground; wax trail slows without damage; precision shot is faster than turret shot.
            var system=new GameObject("Specialty projectile validation").AddComponent<EnemyProjectileSystem>();system.Initialize(w);
            var owner=new GameObject("Shot owner");system.Fire(owner,"G03",new Vector2(100,-79),new Vector2(106,-79),1);Check(Mathf.Abs(system.Capture()[0].velocity.x-9)<.001f,"precision projectile speed");system.Clear();
            system.Fire(owner,"COMMON03",new Vector2(100,-79),new Vector2(103,-79),1);system.Tick(1.2f);var puddle=system.Capture().Single();Check(puddle.kind==EnemyProjectileSystem.Kind.Puddle&&puddle.width>1.4f&&puddle.life>1,"ink has meaningful area and duration");system.Clear();
            system.Fire(owner,"D02",new Vector2(100,-79),new Vector2(103,-79),1);Check(!system.Fire(owner,"D02",new Vector2(100,-79),new Vector2(103,-79),1),"seed cannot spam");system.Tick(.7f);Check(system.BounceCount==1,"seed bounces");system.Tick(1);Check(system.ActiveCount==0,"seed expires after next impact");
            for(int y=0;y<80;y++)w.Motor.cells[y*w.Data.width+103]=1;system.Fire(owner,"G03",new Vector2(100,-79),new Vector2(106,-79),1);system.Tick(.8f);Check(system.ActiveCount==0,"fast shot never tunnels through wall");
            UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(system.gameObject);
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/enemy-specialties.json","{\"passed\":true,\"species\":33,\"checks\":[\"authored phase and hit-window synchronization\",\"all species pause attack death reset\",\"real receiver front guard and rear/recovery opening\",\"vertical horizontal aimed flight\",\"pursuit speed differentiation\",\"shield facing commitment\",\"telegraphed shield counter\",\"jumpable ground sweeps\",\"ranged retreat\",\"ink area duration\",\"seed bounce cap\",\"fast projectile wall sweep\"]}");
            Debug.Log("ENEMY_SPECIALTIES_OK");Time.timeScale=1;EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
