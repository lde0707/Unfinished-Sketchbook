using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Unfin.Editor
{
    public static class SaveShrineValidation
    {
        static void Check(bool ok,string text){if(!ok)throw new Exception("Save shrine: "+text);}
        public static void RunAndBuild(){
            PickupArtIntegration.Apply();
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Where(m=>!m.enemy&&m.kind=="save").ToArray();Check(markers.Length==10,"ten authored checkpoints preserved");
            foreach(var m in markers){var point=w.Data.pickups.First(p=>p.type=="save"&&Mathf.Abs(p.x-m.htmlPosition.x)<.1f&&Mathf.Abs(p.y-m.htmlPosition.y)<.1f);var v=m.gameObject.AddComponent<SaveShrineVisual>();v.Initialize(w,point);v.Tick(.1f);var r=m.GetComponent<SpriteRenderer>();Check(r.sprite!=null&&r.sprite.texture.name=="save","replacement image present");Check(Mathf.Abs(r.bounds.min.y+point.floor+.035f)<.01f,"base meets floor");}
            var p=w.Data.pickups.First(a=>a.type=="save"&&a.tx==50);w.Respawn(48,p.floor);w.Progression.Health=2;w.Motor.x=p.tx*16+2;w.Motor.y=p.floor*16-GridMotor.CollisionHeight;w.Motor.grounded=true;w.Motor.vy=1;
            for(int i=0;i<30;i++)w.Simulate(1f/120);
            Check(w.Progression.Health==w.Progression.MaxHealth,"touch restores health");var selected=markers.First(a=>Mathf.Abs(a.htmlPosition.x-p.x)<.1f&&Mathf.Abs(a.htmlPosition.y-p.y)<.1f).GetComponent<SaveShrineVisual>();selected.Tick(.1f);Check(selected.Active,"active shrine book responds to saved checkpoint");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/save-shrine.json","{\"passed\":true,\"shrines\":10,\"floorContact\":true,\"restoresHealth\":true,\"activeBookLight\":true}");Debug.Log("UNFIN_SAVE_SHRINE_OK 10 shrines / floor contact / restore / active book light");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
