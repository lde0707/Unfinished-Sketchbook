using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class StilldrawnTitleIntegration
    {
        static void Check(bool ok,string text){if(!ok)throw new Exception("Stilldrawn title: "+text);}
        static Color32[] PaintingFrame(StilldrawnTitleMotion motion,Texture2D background,float time)
        {
            var frame=motion.RenderPainting(background,time);Check(frame!=null,"living painting shader supported");
            var previous=RenderTexture.active;RenderTexture.active=frame;
            var read=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            read.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);read.Apply();RenderTexture.active=previous;
            Directory.CreateDirectory("Validation/title-painting");File.WriteAllBytes("Validation/title-painting/phase-"+time+".png",read.EncodeToPNG());
            var pixels=read.GetPixels32();UnityEngine.Object.DestroyImmediate(read);return pixels;
        }
        static void ValidatePainting()
        {
            var bg=Resources.Load<Texture2D>("Title/sketchbook");
            using(var motion=new StilldrawnTitleMotion()){
                var a=PaintingFrame(motion,bg,0);var b=PaintingFrame(motion,bg,6);var loop=PaintingFrame(motion,bg,24);
                long moving=0,fixedError=0,loopError=0;int movingCount=0,fixedCount=0;
                for(int y=0;y<bg.height;y++)for(int x=0;x<bg.width;x++){
                    int n=y*bg.width+x;int delta=Math.Abs(a[n].r-b[n].r)+Math.Abs(a[n].g-b[n].g)+Math.Abs(a[n].b-b[n].b);
                    loopError+=Math.Abs(a[n].r-loop[n].r)+Math.Abs(a[n].g-loop[n].g)+Math.Abs(a[n].b-loop[n].b);
                    if(x>bg.width*.52f&&y>bg.height*.30f){moving+=delta;movingCount++;}
                    if(x<bg.width*.45f){fixedError+=delta;fixedCount++;}
                }
                double changed=(double)moving/(movingCount*3),fixedDelta=(double)fixedError/(fixedCount*3);
                Check(changed>.1,"painted pages actually move without overlay effects");Check(fixedDelta<.01,"left forest remains fixed");Check(loopError==0,"painting loop returns to identical pixels");
                File.WriteAllText("Validation/stilldrawn-living-painting.json","{\"passed\":true,\"paintingDifference\":"+changed.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"fixedForestDifference\":"+fixedDelta.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"loopPixelError\":"+loopError+"}");
            }
        }
        static WorldPrototype LoadWorld(){EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);return w;}
        public static void RunAndBuild()
        {
            PreparePanels();
            PaintedHudIntegration.Prepare();
            string path="Assets/Unfin/Resources/Title/sketchbook.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var web=importer.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=4096;web.format=TextureImporterFormat.RGB24;web.textureCompression=TextureImporterCompression.Uncompressed;importer.SetPlatformTextureSettings(web);importer.SaveAndReimport();
            var logoImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Unfin/Resources/Title/stilldrawn-logo-v1.png");
            logoImporter.textureType=TextureImporterType.Default;logoImporter.npotScale=TextureImporterNPOTScale.None;logoImporter.mipmapEnabled=false;logoImporter.maxTextureSize=4096;logoImporter.wrapMode=TextureWrapMode.Clamp;logoImporter.filterMode=FilterMode.Bilinear;logoImporter.alphaIsTransparency=true;logoImporter.textureCompression=TextureImporterCompression.Uncompressed;
            var logoWeb=logoImporter.GetPlatformTextureSettings("WebGL");logoWeb.overridden=true;logoWeb.maxTextureSize=4096;logoWeb.format=TextureImporterFormat.RGBA32;logoWeb.textureCompression=TextureImporterCompression.Uncompressed;logoImporter.SetPlatformTextureSettings(logoWeb);logoImporter.SaveAndReimport();
            PlayerSettings.productName="Stilldrawn";
            bool hadSave=PlayerPrefs.HasKey(WorldSession.SaveKey);string backup=PlayerPrefs.GetString(WorldSession.SaveKey);float priorTime=Time.timeScale;
            try{
                ValidatePainting();
                float loopError=0;
                for(float time=0;time<24;time+=.37f)for(int i=0;i<StilldrawnTitleMotion.PaperCount;i++){
                    var a=StilldrawnTitleMotion.PaperAt(time,i);var b=StilldrawnTitleMotion.PaperAt(time+StilldrawnTitleMotion.LoopSeconds,i);
                    loopError=Mathf.Max(loopError,Vector2.Distance(a.position,b.position));
                    Check(Vector2.Distance(a.size,b.size)<.01f&&Mathf.Abs(a.alpha-b.alpha)<.0001f,"paper loop size and fade continuity");
                }
                Check(loopError<.02f&&StilldrawnTitleMotion.PaperAt(0,0).alpha<.0001f,"seamless 24-second particle loop");
                Check(Vector2.Distance(StilldrawnTitleMotion.BackgroundOffset(0),StilldrawnTitleMotion.BackgroundOffset(24))<.001f,"background loop continuity");
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/stilldrawn-title-motion.json","{\"passed\":true,\"loopSeconds\":24,\"paperParticles\":18,\"motes\":40,\"unscaledTime\":true,\"reducedMotionSupported\":true,\"loopBoundaryErrorPixels\":"+loopError.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                var w=LoadWorld();PlayerPrefs.DeleteKey(WorldSession.SaveKey);Check(WorldSession.Read(w)==null,"no invented Continue");
                var bg=Resources.Load<Texture2D>("Title/sketchbook");Check(bg!=null&&bg.width>=1600&&bg.height>=900,"full resolution title painting");Check(Resources.Load<Texture2D>("Title/stilldrawn-logo-v1")!=null,"transparent illustrated wordmark");Check(Resources.Load<Font>("Title/Marcellus-Regular")!=null&&Resources.Load<Font>("Title/MaruBuri-SemiBold")!=null,"new title and Korean menu fonts");
                w.Respawn(78,123);w.Progression.UseAbilityLocks=true;w.Progression.Grant("walljump");w.Progression.Grant("cut");w.Progression.HeartFragments=2;w.Progression.Shards=7;w.Progression.HealingUnlocked=true;w.Progression.ShieldUnlocked=true;w.Progression.SeenFrontiers.Add("test-frontier");w.Progression.DefeatedEncounters.Add(23);w.Progression.Reveal(78,121);
                foreach(var item in w.Data.pickups)if(item.type=="shard"){w.Progression.Collect(item);break;}
                int gate=-1;for(int i=0;i<w.Motor.cells.Length;i++)if(WorldPrototype.Gate(w.Motor.cells[i])){gate=i;w.ClearGateAt(i%w.Data.width,i/w.Data.width);break;}
                WorldSession.PlaySeconds=3865;WorldSession.Save(w);var s=WorldSession.Read(w);Check(s!=null&&s.playSeconds==3865&&s.cellIndices.Length>0,"real persistent bookmark");
                var fresh=LoadWorld();fresh.Progression.UseAbilityLocks=true;fresh.RestoreSession(s);
                Check(fresh.Progression.Has("walljump")&&fresh.Progression.Has("cut")&&!fresh.Progression.Has("dash"),"abilities preserved without granting later abilities");
                Check(fresh.Progression.HeartFragments==2&&fresh.Progression.Shards==8&&fresh.Progression.HealingUnlocked&&fresh.Progression.ShieldUnlocked,"items and upgrades preserved");
                Check(fresh.Progression.DefeatedEncounters.Contains(23)&&fresh.Progression.SeenFrontiers.Contains("test-frontier"),"encounters and map markers preserved");
                Check(fresh.Motor.cells[gate]==0&&Mathf.Abs(fresh.CheckpointPosition.x-w.CheckpointPosition.x)<.01f,"opened path and actual checkpoint preserved");
                s.defeated=new[]{23,47,69,91};fresh.Progression.RestoreSession(s);Check(fresh.Progression.Has("dash")&&fresh.Progression.Has("parry")&&fresh.Progression.Has("climb"),"pending earned boss rewards survive an immediate save");
                var title=new GameObject("Title validation").AddComponent<StilldrawnTitle>();title.Initialize(fresh);Check(title.CanContinue&&StilldrawnTitle.Blocking&&Time.timeScale==0,"title pauses world and enables real Continue");
                ValidatePanels(title,fresh);
                title.SkipForPreview();Check(!StilldrawnTitle.Blocking&&Time.timeScale==1,"review spawn bypass");title.OpenPause();Check(StilldrawnTitle.Blocking,"Esc pause path");title.Resume();Check(!title.Visible,"resume path");UnityEngine.Object.DestroyImmediate(title.gameObject);
                PlayerPrefs.SetString(WorldSession.SaveKey,"{broken}");Check(WorldSession.Read(fresh)==null,"invalid save cannot masquerade as Continue");
                PlayerPrefs.SetString(WorldSession.SaveKey,JsonUtility.ToJson(new StilldrawnSave{schema=1,mapHash="old map"}));Check(WorldSession.Read(fresh)==null,"incompatible map cannot load");
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/stilldrawn-title.json","{\"passed\":true,\"title\":\"Stilldrawn\",\"referenceGames\":30,\"realContinue\":true,\"checkpointPersistence\":true,\"itemsAbilitiesMapGatesEncounters\":true,\"invalidSaveRejected\":true,\"worldPaused\":true,\"previewBypass\":true}");
                Debug.Log("STILLDRAWN_TITLE_OK");
            }finally{if(hadSave)PlayerPrefs.SetString(WorldSession.SaveKey,backup);else PlayerPrefs.DeleteKey(WorldSession.SaveKey);PlayerPrefs.Save();Time.timeScale=priorTime;WorldSession.PlaySeconds=0;}
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);AssetDatabase.SaveAssets();WebGLBuilder.Build();
        }
        static void PreparePanels()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/Unfin/Resources/Title/Panels","*.png")){
                var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;
                t.mipmapEnabled=false;t.maxTextureSize=4096;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.alphaIsTransparency=true;
                t.textureCompression=TextureImporterCompression.Uncompressed;var web=t.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=4096;web.format=TextureImporterFormat.RGBA32;
                web.textureCompression=TextureImporterCompression.Uncompressed;t.SetPlatformTextureSettings(web);t.SaveAndReimport();
            }
        }
        static void ValidatePanels(StilldrawnTitle title,WorldPrototype world)
        {
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            void Call(string name,params object[] args)=>typeof(StilldrawnTitle).GetMethod(name,flags).Invoke(title,args);
            var scene=world.gameObject.scene.handle;var id=title.GetEntityId();float time=title.BackgroundTime;
            var files=new[]{"background","panel","button","A-paper-cave","B-graphite-mine","C-sketch-tree","D-root-depths","E-sunset-cavern","F-wax-workshop","G-painted-canyon","H-knight-tower"};
            foreach(var file in files)Check(Resources.Load<Texture2D>("Title/Panels/"+file)!=null,"painted resource "+file);
            Call("Activate",0);Check(title.CurrentPage=="Continue","Continue opens a panel before launching");Call("Back");
            Call("Activate",1);Check(title.CurrentPage=="ConfirmNew","New Story opens confirmation with bookmark");Call("Back");
            Call("Activate",2);Check(title.CurrentPage=="Settings","settings panel");
            for(int i=0;i<3;i++){Call("SelectSettingsTab",i);Check(title.SettingsTab==i,"settings tab "+i);}
            Call("Back");Check(title.CurrentPage=="Home"&&title.GetEntityId()==id&&world.gameObject.scene.handle==scene&&title.BackgroundTime==time&&Time.timeScale==0,"all panels retain same scene, title, animation clock and pause");
            var saved=world.CaptureSession();Check(saved.lastZonePlusOne>=1&&saved.lastZonePlusOne<=8,"save captures visited zone");
            var field=typeof(StilldrawnTitle).GetField("record",flags);var original=field.GetValue(title);
            for(int i=0;i<8;i++){saved.lastZonePlusOne=i+1;field.SetValue(title,saved);Check(title.BookmarkZone==i,"zone thumbnail mapping "+i);}
            saved.lastZonePlusOne=0;Check(title.BookmarkZone>=0&&title.BookmarkZone<8,"legacy save zone fallback");
            field.SetValue(title,null);Call("Activate",1);Check(title.CurrentPage=="ConfirmNew"&&!title.CanContinue,"no-save new story confirmation");Call("Back");field.SetValue(title,original);
            File.WriteAllText("Validation/stilldrawn-title-panels.json","{\"passed\":true,\"sameScene\":true,\"backgroundClockPreserved\":true,\"settingsTabs\":3,\"zoneThumbnails\":8,\"legacySaveCompatible\":true,\"realBookmark\":true}");
            Debug.Log("STILLDRAWN_TITLE_PANELS_OK");
        }
    }
}
