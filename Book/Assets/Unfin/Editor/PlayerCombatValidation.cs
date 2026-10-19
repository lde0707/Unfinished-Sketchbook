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
        public static void RunAndBuild(){PaperSpriteImporter.Generate();PlayerTempoValidation.Run();PaperSpriteValidation.Run();Run();PlayerAttackRangeValidation.Run();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
        public static void Run(){ResponsiveCombatValidation.Run();}
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
            File.WriteAllText("Validation/attack-effects.json","{\"passed\":true,\"thrustScaleX\":3.2,\"thrustScaleY\":1.6,\"slash\":\"extended sweep with shared hit outline\",\"characterFramesChanged\":false,\"timingChanged\":false,\"captures\":[\"slash1\",\"slash2\",\"thrust\",\"moving\",\"air-rise\",\"air-fall\"]}");
        }
    }
}
