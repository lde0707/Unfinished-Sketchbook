using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class ReviveHoldIntegration
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Revive/ranges: "+message);}
        public static void RunAndBuild()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);
            var parent=new GameObject("Range validation");
            var shape=new BossDangerShape(parent.transform,Region2Presentation.Fx(3));
            var center=new Vector2(5,5);var radius=new Vector2(3,1);
            var inside=new Bounds(center,Vector3.one*.2f);var outside=new Bounds(new Vector3(20,20,0),Vector3.one);
            shape.Show(center,radius,20,false);
            Check(shape.Visible&&!shape.Damaging&&!shape.Overlaps(inside),"warning has no damage");
            Check(!parent.GetComponentInChildren<SpriteRenderer>().enabled,"no filled range warning");
            Check(parent.GetComponentsInChildren<LineRenderer>(true).Length==0,"no range rims");
            shape.Show(center,radius,20,true);
            Check(parent.GetComponentInChildren<SpriteRenderer>().enabled,"actual attack art remains visible");
            Check(shape.Overlaps(inside)&&!shape.Overlaps(outside),"damage ellipse unchanged");
            shape.Hide();Check(!shape.Visible&&!shape.Overlaps(inside),"hidden damage disabled");shape.Dispose();UnityEngine.Object.DestroyImmediate(parent);
            w.Respawn(78,123);var p=w.player.GetComponentInChildren<PaperSpritePlayer>();
            w.BeginDeath();p.Tick(0);
            void Step(){w.Simulate(.005f);p.Tick(.005f);}
            int guard=0;while(w.IsDrowning&&guard++<200)Step();
            Check(w.IsReviving&&p.CurrentAnimation=="Revive","death enters revive");
            var position=w.player.position;
            for(int i=0;i<200;i++){
                Check(p.CurrentFrame==0,"lying frame held for one second");
                Check(!w.RequestDash(1)&&!p.RequestAttack(),"actions blocked while lying");
                Step();Check(Vector3.Distance(position,w.player.position)<.001f,"lying body stays at checkpoint");
            }
            var frames=new HashSet<int>{0};guard=0;
            while(w.IsReviving&&guard++<200){frames.Add(p.CurrentFrame);Step();}
            Check(!w.IsReviving&&frames.Count==6,"all six original get-up drawings finish");
            Check(Mathf.Abs(PlayerLifeSprites.Find("Revive").Duration-PlayerLifeSprites.ReviveDuration)<.001f,"animation and input lock agree");
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/revive-hold-ranges.json","{\"passed\":true,\"lyingHoldAddedSeconds\":1,\"reviveSeconds\":1.71,\"originalReviveFrames\":6,\"rangeRims\":false,\"rangeWarnings\":false,\"attackArtVisible\":true,\"damageGeometryUnchanged\":true,\"actionsBlockedDuringHold\":true}");
            Debug.Log("REVIVE_HOLD_RANGES_OK");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
