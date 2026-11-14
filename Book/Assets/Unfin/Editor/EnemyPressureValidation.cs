using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class EnemyPressureValidation
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string message){if(!ok)throw new Exception("Enemy pressure: "+message);}
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Private).Invoke(o,args);
        static T Read<T>(object o,string name)=>(T)o.GetType().GetField(name,Private).GetValue(o);
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Call(w,"Awake");
            Array.Clear(w.Motor.cells,0,w.Motor.cells.Length);
            for(int x=0;x<w.Data.width;x++)w.Motor.cells[80*w.Data.width+x]=1;
            w.Respawn(100,80);w.flying=true;
            PaperEnemyAnimation Actor(string id){
                var go=new GameObject("Pressure validation "+id);go.AddComponent<SpriteRenderer>();
                var e=go.AddComponent<PaperEnemyAnimation>();e.Initialize(PaperEnemyAnimation.Manifest.enemies.First(s=>s.id==id));
                e.transform.position=new Vector3(98,-80,0);return e;
            }
            foreach(var spec in PaperEnemyAnimation.Manifest.enemies){
                var e=Actor(spec.id);int hp=e.Health;
                Call(e,"SetState",EnemyAnimationState.Attack);
                float before=e.EffectClock;w.ToggleExplorationMap();e.Tick(.05f);Check(e.EffectClock==before,"map pauses "+spec.id);w.ToggleExplorationMap();
                float elapsed=0;while(e.StateName=="Attack"&&elapsed<8){e.Tick(.005f);elapsed+=.005f;}
                Check(e.StateName=="Move","attack ends "+spec.id);
                Check(Mathf.Abs(elapsed-e.Art.clips[1].durations.Sum()/1.12f)<.011f,"shared attack playback "+spec.id);
                Check(Mathf.Abs(Read<float>(e,"cooldown")-e.Specialty.cooldown)<.001f,"recovery cooldown "+spec.id);
                Check(e.Health==hp,"health unchanged "+spec.id);
                UnityEngine.Object.DestroyImmediate(e.gameObject);
            }
            var flyer=Actor("COMMON02");Call(flyer,"SetState",EnemyAnimationState.Attack);
            w.player.position=new Vector3(96,-78,0);flyer.Tick(.05f);
            Check(Read<Vector2>(flyer,"attackAim").x<0,"early windup tracks player crossing sides");
            for(int i=0;i<9;i++)flyer.Tick(.05f);
            Vector2 locked=Read<Vector2>(flyer,"attackAim");w.player.position=new Vector3(101,-79,0);
            flyer.Tick(.05f);Check(Read<Vector2>(flyer,"attackAim")==locked,"late windup aim locked");
            for(int i=0;i<6;i++)flyer.Tick(.05f);
            Check(Read<Vector2>(flyer,"attackAim")==locked,"active lunge cannot home");
            UnityEngine.Object.DestroyImmediate(flyer.gameObject);
            w.Respawn(100,80);w.flying=true;
            var crawler=Actor("R101");Check((float)Call(crawler,"MeleeTriggerReach")<2,"short melee waits until in reach");
            float start=crawler.transform.position.x;crawler.Tick(.05f);Check(crawler.transform.position.x>start,"nearby melee approaches player");
            // Ground enemies still respect holes and walls while closing in.
            for(int x=99;x<105;x++)w.Motor.cells[80*w.Data.width+x]=0;
            start=crawler.transform.position.x;for(int i=0;i<120;i++)crawler.Tick(.05f);
            Check(crawler.transform.position.x<99,"pursuit cannot walk off a ledge");
            UnityEngine.Object.DestroyImmediate(crawler.gameObject);
            for(int x=0;x<w.Data.width;x++)w.Motor.cells[80*w.Data.width+x]=1;
            w.Respawn(100,80);w.flying=false;
            var soldier=Actor("R201");var body=w.Motor.WorldBounds;
            soldier.transform.position=body.center+Vector3.right*2-Vector3.up*(soldier.BodyBounds.size.y*.5f);
            var from=soldier.BodyBounds;from.center-=Vector3.right*4;
            float health=w.Progression.Health;
            Call(soldier,"ContactPlayer",from,0);
            Check(w.Progression.Health<health,"extended melee sweep hits between endpoints");
            health=w.Progression.Health;Call(soldier,"ContactPlayer",from,0);
            Check(w.Progression.Health==health,"player hit grace prevents repeated damage");
            UnityEngine.Object.DestroyImmediate(soldier.gameObject);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/enemy-pressure.json","{\"passed\":true,\"species\":"+PaperEnemyAnimation.Manifest.enemies.Length+",\"attackPlayback\":1.12,\"cooldownSeconds\":0.6,\"checks\":[\"all species attack completion and unchanged health\",\"pause freezes action\",\"early aim follows, late aim locks\",\"short melee closes distance\",\"pursuit respects ledges\",\"melee sweep hits between endpoints\",\"player damage grace retained\"]}");
            Debug.Log("ENEMY_PRESSURE_OK");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
