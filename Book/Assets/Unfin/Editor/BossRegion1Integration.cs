using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class BossRegion1Integration
    {
        static void Check(bool ok,string m){if(!ok)throw new Exception("Region1 boss: "+m);}
        public static void Apply(){
            AssetDatabase.Refresh();var paths=Directory.GetFiles("Assets/Unfin/Resources/BossRegion1","*.png",SearchOption.AllDirectories);Check(paths.Length==132,"120 drawings + 12 effects");
            foreach(var p in paths){var ti=(TextureImporter)AssetImporter.GetAtPath(p);ti.textureType=TextureImporterType.Default;ti.npotScale=TextureImporterNPOTScale.None;ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=1024;var web=ti.GetPlatformTextureSettings("WebGL");web.overridden=true;web.format=TextureImporterFormat.RGBA32;web.maxTextureSize=1024;ti.SetPlatformTextureSettings(web);ti.SaveAndReimport();}
            AssetDatabase.SaveAssets();
        }
        public static void Validate(){
            Directory.CreateDirectory("Validation");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var marker=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Single(m=>m.enemy&&m.sourceIndex==47);
            Check(PaperBossRegion1.Attach(marker),"boss marker replaced");var b=marker.GetComponent<PaperBossRegion1>();var receiver=marker.GetComponent<PaperAttackTarget>();
            Check(marker.GetComponent<ExplorationEncounter>()==null,"no old 16-hit receiver conflict");Check(b.Health==30,"30 health");Check(b.BodyBounds.size.y==2.8f,"heavy boss height");
            foreach(var clip in PaperBossRegion1.Art.clips){Check(clip.durations.Length==12&&PaperBossRegion1.Frames(clip.name).All(s=>s!=null),"twelve drawings "+clip.name);}
            w.Progression.UseAbilityLocks=true;w.Respawn(255,34);w.Simulate(.02f);
            float hp=100;w.Progression.Health=hp;
            int[] active=new int[6];float[] movement=new float[6];int projectilePeak=0;
            for(int p=0;p<6;p++){
                w.Respawn(255,34);w.Simulate(.02f);b.Tick(.001f);w.Progression.Health=100;
                var start=b.transform.position;b.BeginPattern(p);
                for(float t=0;t<PaperBossRegion1.Art.clips[p+1].Duration+.04f;t+=.005f){b.Tick(.005f);active[p]=Math.Max(active[p],b.ActiveShapes);projectilePeak=Math.Max(projectilePeak,b.LiveProjectiles);movement[p]=Math.Max(movement[p],Vector3.Distance(start,b.transform.position));}
                Check(active[p]>0||p==4,"visible active attack "+p);
            }
            Check(movement[1]>1&&movement[2]>.5f,"slam and rush actually move");Check(projectilePeak>=5,"real fan projectiles");
            Check(BossDangerShape.EllipseIntersects(Vector2.zero,new Vector2(2,1),35,new Bounds(new Vector3(1,0),Vector3.one*.1f)),"rotated range inside");
            Check(!BossDangerShape.EllipseIntersects(Vector2.zero,new Vector2(2,1),35,new Bounds(new Vector3(4,4),Vector3.one*.1f)),"range outside");
            // A warning cannot damage: advancing inside the sweep only after its warm-up changes health.
            w.Respawn(260,34);w.Simulate(.02f);b.Tick(.001f);w.Progression.Health=100;b.BeginPattern(0);b.Tick(.1f);Check(b.ActiveShapes==0,"no damaging telegraph");
            var bodyShader=marker.GetComponent<SpriteRenderer>().sharedMaterial.shader.name;receiver.ReceiveHit(1);Check(b.Health==29,"one accepted hit");Check(marker.GetComponent<SpriteRenderer>().sharedMaterial.shader.name==bodyShader,"hit flash preserves silhouette rim");
            // The first encounter is validated without abilities rewarded later.
            w.Progression.Grant("walljump");Check(!w.Progression.Has("dash")&&!w.Progression.Has("parry")&&!w.Progression.Has("climb"),"region1 kit excludes later rewards");
            b.BeginPattern(3);b.Tick(.01f);b.ExposedKnot.ReceiveHit(1);Check(!b.Staggered,"uncut knot does not grant a free stagger");w.Progression.Grant("cut");b.ExposedKnot.ReceiveHit(1);Check(b.Staggered&&b.ActiveShapes==0&&!b.ExposedKnot.enabled,"cut creates earned opening and cancels roots");
            float jumpHeight=PlayerMotion.JumpSpeed*PlayerMotion.JumpSpeed/(2*PlayerMotion.Gravity)/16;Check(jumpHeight>3.4f,"ordinary jump clears low rush and root heights");
            Check(PaperBossRegion1.Art.patterns[1].activeStart*(PlayerMotion.GroundSpeed/16)>PaperBossRegion1.Art.patterns[1].reach+.75f,"fixed slam target allows a ground escape before impact");
            Check(PlayerMotion.GroundSpeed/16>PaperBossRegion1.Art.patterns[4].projectileSpeed,"retreat can separate from forward pellets");
            // Capture authored-room backdrop with all actors hidden, for the animation gallery.
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None)){var sr=m.GetComponent<SpriteRenderer>();if(sr!=null)sr.enabled=false;}
            w.player.gameObject.SetActive(false);b.BeginPattern(4);b.Tick(.001f);b.enabled=false;marker.gameObject.SetActive(false);
            foreach(string method in new[]{"HideRanges","ClearShots"})typeof(PaperBossRegion1).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);
            var camera=w.worldCamera;camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.position=new Vector3(258,-31.3f,-10);camera.aspect=16f/9;
            var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Validation/boss-room-background.png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
            marker.gameObject.SetActive(true);b.enabled=true;w.player.gameObject.SetActive(true);marker.GetComponent<SpriteRenderer>().enabled=true;
            for(int i=0;i<29;i++)receiver.ReceiveHit(1);Check(b.Dead&&!receiver.enabled,"death disables damage receiver");
            w.Respawn(255,34);b.Tick(.005f);Check(b.Dead&&b.Health==0,"simultaneous player respawn cannot undo boss defeat");
            for(int i=0;i<500;i++)b.Tick(.005f);Check(b.FrameIndex==11&&w.Progression.DefeatedEncounters.Contains(47)&&w.Progression.Has("dash"),"death reveals bird and retains dash reward");
            w.Respawn(255,34);b.Tick(.01f);Check(b.Dead,"defeated boss stays defeated");
            File.WriteAllText("Validation/boss-region1.json","{\"passed\":true,\"version\":2,\"bodyDrawings\":120,\"effects\":12,\"patterns\":6,\"bodyHeight\":2.8,\"realSlamAndRushMovement\":true,\"fanProjectiles\":true,\"warningWithoutDamage\":true,\"sharedVisibleDamageGeometry\":true,\"health\":30,\"cutCounter\":true,\"preBossKitOnly\":true,\"escapeGeometryChecks\":true,\"deathAndReward\":true}");
            Debug.Log("UNFIN_BOSS_REGION1_OK 120 drawings / 6 attacks / pre-boss kit / cut counter / bird reward");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        public static void RunAndBuild(){Apply();Validate();WebGLBuilder.Build();}
    }
}
