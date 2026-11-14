using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class ProximityStoryIntegration
    {
        static void Check(bool value,string name){if(!value)throw new Exception("Proximity story: "+name);}
        [MenuItem("Unfin/Apply and Validate Opening Proximity Story")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            foreach(var file in Directory.GetFiles("Assets/Unfin/Resources/ProximityStory","*.png"))
            {
                var path=file.Replace('\\','/');var t=(TextureImporter)AssetImporter.GetAtPath(path);
                t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;
                t.alphaIsTransparency=true;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;
                t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=8192;
                foreach(string platform in new[]{"Standalone","WebGL"}){
                    var setting=t.GetPlatformTextureSettings(platform);setting.overridden=true;setting.format=TextureImporterFormat.RGBA32;setting.maxTextureSize=8192;t.SetPlatformTextureSettings(setting);
                }
                t.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Validate();
        }
        public static void RunAndBuild(){Run();WebGLBuilder.Build();}
        static void At(WorldPrototype w,Vector2 feet){w.Motor.x=feet.x*16-6;w.Motor.y=-feet.y*16-GridMotor.CollisionHeight;}
        static void Validate()
        {
            EditorSceneManager.OpenScene(PrologueMapBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=UnityEngine.Object.FindAnyObjectByType<PaperSpritePlayer>();p.Tick(.001f);
            var oldLibrary=p.library;var oldSprite=p.visual.sprite;var oldAnimation=p.CurrentAnimation;
            var story=ProximityStory.Create(w);
            Check(story.Actors.Count==3,"one tree and two creatures");
            Check(story.Actors.All(a=>a.phase==ProximityStory.Phase.Present&&a.visual.enabled&&a.visual.sprite!=null),"all present at start");
            story.Tick(.1f);Check(story.GoneMask==0,"spawn does not auto-trigger");
            Directory.CreateDirectory("Validation/proximity-story");Capture(w,"initial");
            for(int index=0;index<story.Actors.Count;index++)
            {
                story.Restore(0);var actor=story.Actors[index];Vector2 feet=actor.visual.transform.position;
                At(w,feet+Vector2.up*3);story.Tick(.1f);Check(actor.phase==ProximityStory.Phase.Present,"different floor cannot trigger");
                At(w,feet+Vector2.right*.7f);story.Tick(.001f);Check(actor.phase==ProximityStory.Phase.Dissolving&&actor.starts==1,"proximity triggers once");
                float age=actor.age;Time.timeScale=0;
                typeof(ProximityStory).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(story,null);
                Check(actor.age==age,"pause freezes animation");Time.timeScale=1;
                var save=w.CaptureSession();Check((save.proximityStoryGoneMask&(1<<index))!=0,"mid-animation saved");
                At(w,feet+Vector2.right*10);
                var seen=new System.Collections.Generic.HashSet<int>();
                for(int f=0;f<32;f++){seen.Add(actor.frame);story.Tick((index==0?2.4f:1.6f)/32);}
                Check(seen.Count==32,"all 32 frames played");
                Check(actor.phase==ProximityStory.Phase.Gone&&!actor.visual.enabled,"disappears after leaving range, including debris");
                At(w,feet);story.Tick(.5f);Check(actor.starts==1,"no re-entry replay");
                story.Restore(0);w.RestoreSession(save);Check(story.Actors[index].phase==ProximityStory.Phase.Gone,"continue restores gone state");
            }
            Check(p.library==oldLibrary&&p.visual.sprite==oldSprite&&p.CurrentAnimation==oldAnimation,"player sprite animation untouched");
            story.Restore(0);w.Respawn(5,16);
            At(w,new Vector2(25.7f,-16));story.Tick(.8f);Capture(w,"dissolving");
            story.Restore(7);Capture(w,"gone");
            File.WriteAllText("Validation/proximity-story.json","{\"passed\":true,\"actors\":3,\"framesPerClip\":32,\"initiallyVisible\":true,\"approachOnly\":true,\"pause\":true,\"noReplay\":true,\"saveRestore\":true,\"playerAnimationUntouched\":true}");
            Debug.Log("UNFIN_PROXIMITY_STORY_OK");
            EditorSceneManager.OpenScene(PrologueMapBuilder.ScenePath);
        }
        static void Capture(WorldPrototype w,string name)
        {
            var camera=w.worldCamera;camera.transform.position=new Vector3(24,-13.1f,-10);camera.orthographicSize=4.21875f;
            var prior=camera.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes("Validation/proximity-story/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
