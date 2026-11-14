using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class AnimationCompletionIntegration
    {
        static void Check(bool value,string text){if(!value)throw new Exception("Animation completion: "+text);}
        static void Private(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        public static void Apply(){
            AssetDatabase.Refresh();
            foreach(var folder in new[]{"PlayerReactions","Region1Elites"})foreach(var p in Directory.GetFiles("Assets/Unfin/Resources/"+folder,"*.png",SearchOption.AllDirectories)){
                var ti=(TextureImporter)AssetImporter.GetAtPath(p);ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaIsTransparency=true;ti.npotScale=TextureImporterNPOTScale.None;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Uncompressed;
                var web=ti.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=1024;web.format=folder=="PlayerReactions"?TextureImporterFormat.RGBA32:TextureImporterFormat.DXT5;ti.SetPlatformTextureSettings(web);ti.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
        static WorldPrototype World(){EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Private(w,"Awake");return w;}
        static void Position(WorldPrototype w,int x,int floor){w.Respawn(x,floor);w.Motor.vy=20;w.Motor.Move(.1f);Private(w,"SyncView");w.player.GetComponentInChildren<PaperSpritePlayer>().Tick(.001f);}
        static void Capture(WorldPrototype w,string name){var c=w.worldCamera;var rt=RenderTexture.GetTemporary(1280,720,24);c.targetTexture=rt;c.aspect=16f/9;c.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1280,720,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();Directory.CreateDirectory("Validation/animation-completion");File.WriteAllBytes("Validation/animation-completion/"+name+".png",t.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(t);}
        public static void Validate(){
            var w=World();var player=w.player.GetComponentInChildren<PaperSpritePlayer>();w.Progression.UseAbilityLocks=true;
            var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
            foreach(var marker in markers)Private(marker,"Start");
            int enemyFallback=markers.Count(m=>m.enemy&&m.GetComponent<PaperEnemyAnimation>()==null&&m.GetComponent<PaperBossRegion1>()==null&&m.GetComponent<PaperRegion2Encounter>()==null&&m.GetComponent<PaperRegion1Elite>()==null);
            int propFallback=markers.Count(m=>!m.enemy&&m.gameObject.activeSelf&&m.GetComponent<PaperPickupVisual>()==null&&m.GetComponent<PaperDeviceVisual>()==null&&m.GetComponent<SaveShrineVisual>()==null);
            Check(enemyFallback==0,"no enemy placeholders");Check(propFallback==0,"no active prop placeholders");
            foreach(var name in new[]{"Hurt","Parry","ParrySuccess"}){var clip=PlayerReactionSprites.Find(name);Check(clip.frames.Length==4&&clip.frames.All(f=>f.sprite!=null&&f.duration>0),"four registered whole drawings "+name);}
            Position(w,78,123);var before=w.Motor.x;Check(player.TryReceiveHit(w.player.position,1),"accepted real hit");Check(w.ActionLocked&&player.ReactionAnimation=="Hurt","hurt begins with 100ms player-only pause");Check(!player.RequestAttack()&&!w.RequestDash(1),"hit pause blocks attack/dash");w.Simulate(.05f,1);player.Tick(.05f);Check(Mathf.Abs(w.Motor.x-before)<.001f,"hit pause holds position");Capture(w,"player-hurt");w.Simulate(.06f,1);player.Tick(.06f);Check(!w.ActionLocked&&player.RequestAttack(),"input returns after short hit pause");
            Position(w,78,123);int activation=w.Progression.ParrySerial;w.Progression.Parry();Check(w.Progression.ParrySerial==activation,"parry art respects ability lock");w.Progression.Grant("parry");w.Progression.Parry();player.Tick(.001f);Check(player.ReactionAnimation=="Parry"&&!player.RequestAttack(),"guard visual matches active parry window");Capture(w,"player-parry");int success=w.Progression.ParrySuccessSerial;w.Progression.ConfirmParry();w.Progression.ConfirmParry();player.Tick(.001f);Check(w.Progression.ParrySuccessSerial==success+1&&player.ReactionAnimation=="ParrySuccess","success plays once per guard");Capture(w,"player-parry-success");w.Progression.Tick(.2f,0,0);player.Tick(.2f);Check(player.RequestAttack(),"success transitions directly to attack");
            foreach(var index in new[]{23,44}){
                var elite=markers.Single(m=>m.enemy&&m.sourceIndex==index).GetComponent<PaperRegion1Elite>();Check(elite!=null,"elite controller "+index);var data=w.Data.enemies[index];Position(w,data.tx-4,data.floor);w.Progression.Health=100;elite.Tick(.001f);var origin=elite.transform.position;elite.BeginAttack();
                for(float t=0;t<(index==23?.9f:.65f);t+=.01f)elite.Tick(.01f);
                Check(Vector3.Distance(origin,elite.transform.position)>1,"physical attack movement "+index);Capture(w,index==23?"overseer-rush":"hunter-leap");
                var receiver=elite.GetComponent<PaperAttackTarget>();int fragments=w.Progression.HeartFragments;for(int i=0;i<(index==23?12:14);i++)receiver.ReceiveHit(1);Check(elite.Dead&&elite.StateName=="Death","death drawings "+index);elite.Tick(.3f);elite.GetComponent<SpriteHitFeedback>().Tick(.3f);Capture(w,index==23?"overseer-death":"hunter-death");elite.Tick(1.3f);
                Check(index==23?w.Progression.Has("cut"):w.Progression.HeartFragments==fragments+1,"one-time intended reward "+index);Position(w,data.tx-4,data.floor);elite.Tick(.001f);Check(elite.Dead&&!elite.GetComponent<SpriteRenderer>().enabled,"defeat persists "+index);if(index==44)Check(w.Progression.HeartFragments==fragments+1,"no duplicated heart reward");
            }
            Check(w.worldCamera.GetComponent<WorldCameraGrade>().brightness==.9f,"approved darker camera preserved");
            File.WriteAllText("Validation/animation-completion.json","{\"passed\":true,\"reactionDrawings\":12,\"eliteDrawings\":48,\"eliteEncounters\":2,\"enemyPlacements\":"+markers.Count(m=>m.enemy)+",\"enemyPlaceholders\":"+enemyFallback+",\"activePropPlaceholders\":"+propFallback+",\"hurtActionPause\":0.1,\"parryUnlockRespected\":true,\"realEliteMovement\":true,\"deathRewards\":true,\"cameraBrightness\":0.9}");
            Debug.Log("ANIMATION_COMPLETION_OK: hurt + parry + 2 elites; no remaining active marker placeholders");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        public static void RunAndBuild(){Apply();Validate();WebGLBuilder.Build();}
    }
}
