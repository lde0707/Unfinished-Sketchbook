using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class FriendAnimationIntegration
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Friend animation: "+message);}
        public static void RunAndBuild(){
            AssetDatabase.Refresh();var files=Directory.GetFiles("Assets/Unfin/Resources/FriendAnimations","*.png",SearchOption.AllDirectories);Check(files.Length==36,"36 individual whole drawings");
            foreach(var path in files){var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.npotScale=TextureImporterNPOTScale.None;ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Trilinear;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=1024;var web=ti.GetPlatformTextureSettings("WebGL");web.overridden=true;web.format=TextureImporterFormat.RGBA32;web.maxTextureSize=1024;ti.SetPlatformTextureSettings(web);ti.SaveAndReimport();}
            DeviceArtIntegration.Apply();Validate();WebGLBuilder.Build();
        }
        static void Validate(){
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);w.Progression.UseAbilityLocks=true;
            var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Where(m=>!m.enemy&&(m.kind=="sketch"||m.kind=="npc")).OrderBy(m=>m.kind=="npc"?2:m.originalOptions.Contains("sun")?1:0).ToArray();Check(markers.Length==3,"dog, sun, keeper");
            foreach(var m in markers){
                var p=w.Data.pickups.First(a=>a.type==m.kind&&Vector2.Distance(new Vector2(a.x,a.y),m.htmlPosition)<.1f);PaperDeviceVisual.Attach(m);var v=m.GetComponent<PaperDeviceVisual>();var animation=v.FriendAnimation;Check(animation!=null,"frame animator attached");Check(PaperFriendAnimation.Load(PaperDeviceVisual.ArtName(p)).Length==12,"12 frames per character");
                animation.Tick(.7f);Check(animation.FrameIndex==1,"idle advances through individual drawings");
                var position=w.Progression.SocialPosition(p);w.Motor.x=position.x*16-6;w.Motor.y=-position.y*16-11;w.Motor.vx=w.Motor.vy=0;w.Motor.grounded=true;
                void Hold(float seconds,int ix=0,int iy=-1){for(int i=0;i<Mathf.CeilToInt(seconds/.01f);i++)w.Progression.Tick(.01f,ix,iy);}
                if(p.type=="sketch"){Hold(.5f);Check(w.Progression.SocialProgress==0&&!w.Progression.Collected(p),"friend completion requires learned tool");w.Progression.Grant(p.optionsJson.Contains("sun")?"climb":"cut");}
                Check(w.Progression.BeginSocial(p),"E begins interaction");Hold(.3f);animation.Tick(.01f);Check(animation.StateName=="Interact"&&animation.FrameIndex>=4,"interaction drawings continue after key release");Hold(.01f,1,-1);Check(w.Progression.SocialProgress==0&&!w.Progression.Collected(p),"moving cancels incomplete interaction");
                int count=w.Progression.FoundFriends,shards=w.Progression.Shards;w.Progression.BeginSocial(p);Hold(p.type=="npc"?.7f:1.25f);Check(w.Progression.Collected(p),"one E press completes interaction");Check(w.Progression.FoundFriends==count+(p.type=="sketch"?1:0)&&w.Progression.Shards==shards,"one friend record, no invented currency reward");
                animation.Tick(.05f);Check(animation.StateName=="Depart"&&m.gameObject.activeSelf,"departure remains visible briefly");animation.Tick(.8f);Check(!m.gameObject.activeSelf&&animation.StateName=="Gone","disappearance animation completes");Hold(1.5f);Check(w.Progression.FoundFriends==count+(p.type=="sketch"?1:0),"held input does not award twice");
                w.Progression.Restore();Check(w.Progression.Collected(p),"restoring player does not respawn collected companion");
                var clone=UnityEngine.Object.Instantiate(m.gameObject);clone.SetActive(true);clone.GetComponent<PaperDeviceVisual>().Initialize(w,clone.GetComponent<MapMarker>(),p);Check(!clone.activeSelf,"reinitialized collected character remains gone");UnityEngine.Object.DestroyImmediate(clone);
            }
            Check(w.Progression.FoundFriends==2,"both picture friends counted");Directory.CreateDirectory("Validation");File.WriteAllText("Validation/friend-animation.json","{\"passed\":true,\"characters\":3,\"drawings\":36,\"abilityLocks\":true,\"cancelOnMove\":true,\"singleAward\":true,\"departure\":true,\"noRespawnAfterRestore\":true}");Debug.Log("UNFIN_FRIEND_ANIMATION_OK 3 characters / 36 drawings / gated E interaction / cancellation / one-time award / departure");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
    }
}
