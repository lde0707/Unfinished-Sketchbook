using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class JournalUIIntegration
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Journal UI: "+message);}
        static void Prepare()
        {
            foreach(string path in Directory.GetFiles("Assets/Unfin/Resources/PaperMenuV2","*.png")){
                var art=(TextureImporter)AssetImporter.GetAtPath(path);art.textureType=TextureImporterType.Default;art.alphaIsTransparency=true;art.npotScale=TextureImporterNPOTScale.None;
                art.mipmapEnabled=false;art.maxTextureSize=4096;art.wrapMode=TextureWrapMode.Clamp;art.filterMode=FilterMode.Bilinear;art.textureCompression=TextureImporterCompression.Uncompressed;
                var platform=art.GetPlatformTextureSettings("WebGL");platform.overridden=true;platform.maxTextureSize=4096;platform.format=TextureImporterFormat.RGBA32;art.SetPlatformTextureSettings(platform);art.SaveAndReimport();
            }
            var i=(TextureImporter)AssetImporter.GetAtPath("Assets/Unfin/Resources/JournalUI/open-journal.png");
            i.textureType=TextureImporterType.Default;i.alphaIsTransparency=true;i.npotScale=TextureImporterNPOTScale.None;
            i.mipmapEnabled=false;i.maxTextureSize=2048;i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.textureCompression=TextureImporterCompression.Uncompressed;
            var web=i.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=2048;web.format=TextureImporterFormat.RGBA32;i.SetPlatformTextureSettings(web);i.SaveAndReimport();
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Unfin/Art/Region1BackgroundV3"})){
                var path=AssetDatabase.GUIDToAssetPath(guid);var bg=AssetImporter.GetAtPath(path) as TextureImporter;
                if(bg==null||bg.textureType!=TextureImporterType.Default)continue;
                bg.mipmapEnabled=true;bg.ignoreMipmapLimit=false;bg.SaveAndReimport();
            }
        }
        public static void RunAndBuild()
        {
            Prepare();
            string[] keys={"graphics.resolution","graphics.aa","graphics.texture","graphics.fps"};
            var values=new int[4];var exists=new bool[4];for(int i=0;i<4;i++){exists[i]=PlayerPrefs.HasKey(keys[i]);values[i]=PlayerPrefs.GetInt(keys[i]);}
            float time=Time.timeScale;int fps=Application.targetFrameRate,mips=QualitySettings.globalTextureMipmapLimit,aa=QualitySettings.antiAliasing,vsync=QualitySettings.vSyncCount;
            string noteKey=null,noteBackup=null;bool noteExists=false;StilldrawnTitle title=null;
            try{
                var mini=new MiniMapTracking();var worldSize=new Vector2(560,140);
                var initialMini=mini.Follow(new Vector2(100,70),worldSize,1f/60);
                var movingMini=mini.Follow(new Vector2(110,75),worldSize,1f/60);
                Check(movingMini.center.x>100&&movingMini.center.x<110&&movingMini.center.y>70&&movingMini.center.y<75,"minimap follows smoothly without snapping");
                for(int step=0;step<90;step++)movingMini=mini.Follow(new Vector2(110,75),worldSize,1f/60);
                Check(Vector2.Distance(movingMini.center,new Vector2(110,75))<.01f&&movingMini.size==initialMini.size,"minimap converges with stable scale");
                var teleportMini=mini.Follow(new Vector2(490,35),worldSize,1f/60);
                Check(Vector2.Distance(teleportMini.center,new Vector2(490,35))<.01f,"minimap snaps on region teleport");
                var edgeMini=mini.Follow(Vector2.zero,worldSize,1f/60);Check(edgeMini.x>=0&&edgeMini.y>=0,"minimap stays in map bounds");
                var navigation=new JournalMapView();var mapBounds=new Rect(280,0,280,140);navigation.Reset(mapBounds);
                var pointer=new Vector2(.7f,.65f);var anchor=navigation.WorldAt(pointer);navigation.ZoomAt(2,pointer);
                Check(Vector2.Distance(anchor,navigation.WorldAt(pointer))<.0001f,"cursor anchored map zoom");
                var start=navigation.View;navigation.PanFrom(start,new Vector2(90,45),new Vector2(900,450));
                Check(Vector2.Distance(navigation.View.position,start.position-new Vector2(14,7))<.0001f,"drag follows pointer in world coordinates");
                navigation.ZoomAt(100,Vector2.one*.5f);Check(navigation.Zoom==6,"zoom maximum");
                navigation.PanFrom(navigation.View,new Vector2(-100000,-100000),new Vector2(900,450));
                Check(navigation.View.xMax<=mapBounds.xMax+.0001f&&navigation.View.yMax<=mapBounds.yMax+.0001f,"pan bounded at far edge");
                navigation.PanFrom(navigation.View,new Vector2(100000,100000),new Vector2(900,450));
                Check(navigation.View.xMin>=mapBounds.xMin&&navigation.View.yMin>=mapBounds.yMin,"pan bounded at near edge");
                navigation.ZoomAt(.0001f,Vector2.zero);Check(navigation.Zoom==1&&navigation.View==mapBounds,"zoom minimum restores full bounds");
                navigation.Focus(new Vector2(420,70));Check(navigation.Zoom==3&&Vector2.Distance(navigation.View.center,new Vector2(420,70))<.0001f,"locate player");
                navigation.Reset(new Rect(0,0,280,140));Check(navigation.Zoom==1&&navigation.View.x==0,"region tab resets viewport");
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/journal-map-navigation.json","{\"passed\":true,\"cursorAnchoredZoom\":true,\"zoomRange\":[1,6],\"boundedPan\":true,\"playerFocus\":true,\"regionReset\":true}");
                var sizes=new HashSet<Vector2Int>();foreach(var r in GraphicsPreferences.Resolutions){
                    Check(sizes.Add(r),"unique resolutions");var v=WorldViewport.Fit(r.x,r.y);
                    Check(Mathf.Abs(v.width*r.x/(v.height*r.y)-16f/9)<.0001f,"aspect preserved "+r);
                    Check(v.x>=0&&v.y>=0&&v.xMax<=1.0001f&&v.yMax<=1.0001f,"viewport bounded "+r);
                }
                Check(sizes.Count==8,"eight resolutions");
                foreach(var key in keys)PlayerPrefs.DeleteKey(key);GraphicsPreferences.Load();
                for(int q=0;q<3;q++){Check(QualitySettings.globalTextureMipmapLimit==q,"real texture mip quality "+q);GraphicsPreferences.Change(2,1);}
                GraphicsPreferences.Change(1,1);Check(GraphicsPreferences.Antialiasing==1,"FXAA toggle");
                GraphicsPreferences.Change(3,-1);Check(Application.targetFrameRate==30,"real 30 FPS limit");
                GraphicsPreferences.ChooseResolution(7);Check(GraphicsPreferences.ResolutionUnsaved,"live preview begins");GraphicsPreferences.Revert();Check(GraphicsPreferences.ResolutionIndex==1,"revert last saved resolution");
                GraphicsPreferences.ChooseResolution(0);GraphicsPreferences.SaveResolution();GraphicsPreferences.Load();Check(GraphicsPreferences.ResolutionIndex==0,"saved resolution persists");
                Check(Resources.Load<Shader>("WorldCameraGrade").isSupported,"FXAA grade shader supported");
                Check(Resources.Load<Texture2D>("JournalUI/open-journal").width==1536,"full journal bitmap imported");
                EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();
                typeof(WorldPrototype).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);
                var p=w.Progression;noteKey=p.NoteStorageKey;noteExists=PlayerPrefs.HasKey(noteKey);noteBackup=PlayerPrefs.GetString(noteKey);PlayerPrefs.DeleteKey(noteKey);p.LoadNotes();
                p.SelectNote(78,123);p.SetNoteDraft("닫힌 길 · M 메모\n능력을 얻고 돌아오기");Check(p.SaveNote()&&p.NoteCount==1,"create Korean multiline note");
                p.LoadNotes();Check(p.NoteCount==1,"notes survive reload");
                p.SelectNote(78,123,0);p.SetNoteDraft("수정한 메모");Check(p.SaveNote()&&p.NoteCount==1,"edit without duplicate");
                Check(w.CaptureSession().notes[0].text=="수정한 메모","session fallback includes note");
                p.DeleteNote();p.LoadNotes();Check(p.NoteCount==0,"delete persists");
                PlayerPrefs.SetString(noteKey,"{broken}");p.LoadNotes();Check(p.NoteCount==0,"malformed notes isolated");PlayerPrefs.DeleteKey(noteKey);
                p.LoadNotes(new[]{new MapNote{x=10,y=10,text="이전 저장"}});Check(p.NoteCount==1,"save fallback");
                p.SelectNote(490,35);p.SetNoteDraft("2지역에 다시 올 곳");p.SaveNote();p.FocusNote(1);
                Check(p.SelectedNoteIndex==1&&p.SelectedNoteText=="2지역에 다시 올 곳"&&p.JournalZoom>=3&&p.JournalView.Contains(new Vector2(490,35))&&p.JournalView.x>=w.Data.regionX,"note list selects text and focuses other region");
                p.FocusNote(0);Check(p.SelectedNoteIndex==0&&p.SelectedNoteText=="이전 저장"&&p.JournalView.Contains(new Vector2(10,10))&&p.JournalView.x<w.Data.regionX,"note list returns to first region");
                File.WriteAllText("Validation/map-follow-notes.json","{\"passed\":true,\"smoothFollow\":true,\"fixedScale\":true,\"teleportSnap\":true,\"mapBounds\":true,\"crossRegionNoteFocus\":true,\"selectedNoteText\":true}");
                title=new GameObject("Journal validation title").AddComponent<StilldrawnTitle>();title.Initialize(w);title.SkipForPreview();
                w.ToggleExplorationMap();Check(w.ExplorationMapOpen&&Time.timeScale==0,"map pauses world");w.ToggleExplorationMap();Check(!w.ExplorationMapOpen&&Time.timeScale==1,"map resumes world");
                title.OpenPause();Check(title.Visible&&Time.timeScale==0&&StilldrawnTitle.Blocking,"pause stops simulation and input");title.Resume();Check(!title.Visible&&Time.timeScale==1,"resume");
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/journal-ui.json","{\"passed\":true,\"resolutions\":8,\"letterboxAspect\":\"16:9\",\"actualTextureLimits\":[0,1,2],\"fxaaShaderSupported\":true,\"fpsLimit\":true,\"resolutionPreviewSaveRollback\":true,\"koreanNotesCreateEditDeleteReload\":true,\"legacySaveFallback\":true,\"mapAndPauseFreeze\":true}");
                Debug.Log("JOURNAL_UI_OK");
                // End the pause fixture: its one-frame input guard intentionally
                // blocks world interaction until the next frame in real play.
                UnityEngine.Object.DestroyImmediate(title.gameObject);title=null;
                InteractionMenuValidation.Run(w);
            }finally{
                if(title!=null)UnityEngine.Object.DestroyImmediate(title.gameObject);
                if(noteKey!=null){if(noteExists)PlayerPrefs.SetString(noteKey,noteBackup);else PlayerPrefs.DeleteKey(noteKey);}
                for(int i=0;i<4;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}
                PlayerPrefs.Save();Time.timeScale=time;Application.targetFrameRate=fps;QualitySettings.globalTextureMipmapLimit=mips;QualitySettings.antiAliasing=aa;QualitySettings.vSyncCount=vsync;
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);AssetDatabase.SaveAssets();WebGLBuilder.Build();
        }
    }
}
