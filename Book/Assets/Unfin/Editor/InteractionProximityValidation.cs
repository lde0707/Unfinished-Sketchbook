using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class InteractionProximityValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Interaction proximity: "+message);}
        static void PlayerAt(WorldPrototype w,Vector2 p){w.Motor.x=p.x*16-6;w.Motor.y=-p.y*16-(GridMotor.TopInset+GridMotor.CollisionHeight)*.5f;}
        public static void RunAndBuild(){Run();WebGLBuilder.Build();}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);w.Progression.UseAbilityLocks=true;
            var shader=Resources.Load<Shader>("InteractionGlow");Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"glimmer shader compiles");
            foreach(var p in w.Data.pickups)Check(InteractionProximity.Supports(p.type),"coverage for "+p.type);
            int count=0;
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Where(x=>!x.enemy)){
                typeof(MapMarker).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
                if(!m.gameObject.activeInHierarchy)continue;
                Check(m.GetComponent<InteractionProximity>()!=null,"attached "+m.kind);count++;
            }
            var dog=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).First(m=>m.kind=="sketch"&&m.htmlPosition.x<280*16);
            var feedback=dog.GetComponent<InteractionProximity>();var body=dog.GetComponent<PaperDeviceVisual>().InteractionBody;var b=body.bounds;
            var sprite=body.sprite;var color=body.color;var material=body.sharedMaterial;var scale=body.transform.localScale;
            PlayerAt(w,new Vector2(b.min.x-.5f,b.center.y));feedback.Tick(.5f);
            Check(!w.Progression.Has("cut")&&feedback.Strength>.2f&&feedback.Glow.enabled,"locked friend still responds without granting its ability");
            Check(feedback.Glow.sortingOrder<0&&feedback.Glow.sprite==body.sprite,"same animated frame behind terrain");
            Check(body.sprite==sprite&&body.color==color&&body.sharedMaterial==material&&body.transform.localScale==scale,"source art unchanged");
            float old=feedback.Strength;PlayerAt(w,new Vector2(b.min.x-7,b.center.y));feedback.Tick(.1f);Check(feedback.Strength>0&&feedback.Strength<old,"smooth departure fade");feedback.Tick(1);Check(!feedback.Glow.enabled,"no far glow");
            PlayerAt(w,new Vector2(b.min.x-.5f,b.center.y));feedback.Tick(1);body.color=new Color(1,1,1,0);feedback.Tick(.01f);Check(!feedback.Glow.enabled,"no ghost glow after disappearance");body.color=color;
            foreach(int t in new[]{2,5,6,7,8,13,14,15,16,18})Check(InteractionProximity.SpecialTile(t),"special surface "+t);
            foreach(int t in new[]{0,1,3,4,9})Check(!InteractionProximity.SpecialTile(t),"ordinary terrain and dangerous liquids are not invitation cues");
            // An intervening stone wall must hide an unrelated object's cue.
            int cell=130*w.Data.width+239;byte prior=w.Motor.cells[cell];w.Motor.cells[cell]=1;
            Check(!InteractionProximity.Visible(w,new Vector2(238.5f,-130.5f),new Vector2(240.5f,-130.5f)),"no reveal through walls");w.Motor.cells[cell]=prior;
            InteractionTileProximity.Create(w);var field=w.GetComponent<InteractionTileProximity>();PlayerAt(w,new Vector2(260.4f,-129.5f));field.Tick(1);
            Check(field.GlowingCells>0,"locked gate has a proximity surface cue");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/interaction-proximity.json","{\"passed\":true,\"objects\":"+count+",\"allPickupKinds\":true,\"specialTileKinds\":10,\"lockedFriend\":true,\"lockedGate\":true,\"smoothFade\":true,\"hiddenAfterCollection\":true,\"sourceArtUntouched\":true,\"occludedByWalls\":true}");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
    }
}
