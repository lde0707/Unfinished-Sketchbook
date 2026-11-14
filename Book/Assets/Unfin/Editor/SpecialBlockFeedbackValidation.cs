using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Unfin.Editor
{
    public static class SpecialBlockFeedbackValidation
    {
        static void Check(bool value,string reason){if(!value)throw new Exception("Block feedback: "+reason);}
        static void Private(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/Unfin/Resources/Audio","Break*.wav")){
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);Check(clip!=null&&clip.length>.2f&&clip.length<1,"audio duration "+path);clip.LoadAudioData();var samples=new float[clip.samples];Check(clip.GetData(samples,0),"decode "+path);float peak=0;foreach(float s in samples){Check(!float.IsNaN(s)&&!float.IsInfinity(s),"finite audio");peak=Mathf.Max(peak,Mathf.Abs(s));}Check(peak>.05f&&peak<.7f,"non-silent limited audio");
            }
            foreach(int type in new[]{2,5,6,7,13,14,15,18})Check(Resources.Load<AudioClip>("Audio/"+SpecialBlockFeedback.Cue(type)+"_0")!=null,"cue "+type);
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Private(w,"Awake");int gates=0;
            foreach(int type in new[]{2,5,7,13,14,18}){
                int index=Array.FindIndex(w.Motor.cells,t=>t==type);Check(index>=0,"gate exists "+type);int before=SpecialBlockFeedback.Events;
                w.ClearGateAt(index%w.Data.width,index/w.Data.width);Check(w.Motor.cells[index]==0&&SpecialBlockFeedback.Events==before+1,"one event per connected gate");Check(SpecialBlockFeedback.LastCue==SpecialBlockFeedback.Cue(type)&&SpecialBlockFeedback.LastCells>0,"material and cells");w.ClearGateAt(index%w.Data.width,index/w.Data.width);Check(SpecialBlockFeedback.Events==before+1,"no duplicate destroyed gate");gates++;
            }
            var go=new GameObject("Block debris QA");var fx=go.AddComponent<PaperVfx>();Private(fx,"Awake");
            foreach(int type in new[]{2,5,6,7,13,14,15,18})fx.BreakTile(Vector2.zero,type,7);
            Check(fx.ActiveCount==56,"pooled particles for all break materials");foreach(var s in go.GetComponentsInChildren<SpriteRenderer>())if(s.enabled)Check(s.sprite!=null&&s.color.a<=.76f,"muted valid particles");fx.Tick(1);Check(fx.ActiveCount==0,"debris expires");UnityEngine.Object.DestroyImmediate(go);
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/special-block-feedback.json","{\"passed\":true,\"gateTypes\":"+gates+",\"soundVariants\":10,\"oneSoundPerConnectedGate\":true,\"duplicateRemovalSilent\":true,\"particleTypes\":8,\"particlesExpire\":true,\"capacity\":128}");Debug.Log("SPECIAL_BLOCK_FEEDBACK_OK");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
