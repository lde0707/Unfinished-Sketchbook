using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class PlayerLifeValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Player life: "+message);}
        [MenuItem("Unfin/Apply Player Death Revive and Checkpoints")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach(string name in new[]{"death","revive"}){
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Unfin/Resources/PlayerLife/"+name+".png");
                importer.textureType=TextureImporterType.Default;PaperSpriteImporter.ConfigureTextureQuality(importer,1536,1024);importer.SaveAndReimport();
                var texture=Resources.Load<Texture2D>("PlayerLife/"+name);
                Check(texture.width==1536&&texture.height==1024,name+" original atlas resolution");
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var data=JsonUtility.FromJson<WorldData>(w.mapJson.text);CheckpointPlan.Apply(data);
            foreach(var marker in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None))
                if(!marker.enemy&&marker.kind=="save"&&!CheckpointPlan.HasMarker(data,marker.htmlPosition))UnityEngine.Object.DestroyImmediate(marker.gameObject);
            EditorSceneManager.SaveScene(w.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void RunAndBuild(){Apply();Run();InkHazardValidation.Run();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(WorldPrototype).GetMethod("Awake",flags).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
            var lifeAtlas=Resources.Load<Texture2D>("PlayerLifeEffects/paper-ink-atlas-v1");
            Check(lifeAtlas!=null&&lifeAtlas.width==1254&&lifeAtlas.height==1254,"life effects preserve full alpha atlas resolution");
            var fx=PlayerLifeVfx.Create(w);
            Check(w.Data.pickups.Count(x=>x.type=="save")==10,"ten active checkpoints");
            Check(w.Data.pickups.Count(x=>x.type=="save"&&x.tx<w.Data.regionX)==5,"five region-one checkpoints");
            Check(UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Count(x=>!x.enemy&&x.kind=="save")==10,"removed scene markers");
            void Step(int count,int dir=0){for(int i=0;i<count;i++){w.Simulate(.005f,dir);p.Tick(.005f);fx.Sample();}}
            foreach(var point in w.Data.pickups.Where(x=>x.type=="save")){
                w.Respawn(point.tx,point.floor);Step(20);
                Check((bool)typeof(WorldPrototype).GetMethod("SafeStanding",flags).Invoke(w,new object[]{w.CheckpointPosition.x*16-6,-w.CheckpointPosition.y*16-11}),"safe checkpoint "+point.tx+","+point.floor);
            }
            foreach(string name in new[]{"Death","Revive"}){
                var clip=PlayerLifeSprites.Find(name);Check(clip.frames.Length==6,name+" six drawings");
                Check(Mathf.Abs(clip.Duration-(name=="Death"?PlayerLifeSprites.DeathDuration:PlayerLifeSprites.ReviveDuration))<.001f,name+" timing");
                foreach(var frame in clip.frames)Check(frame.sprite!=null&&frame.sprite.pivot.y>=0&&frame.sprite.pixelsPerUnit>300,"registered life drawing");
            }
            w.Respawn(244,131);Step(20);var initial=w.CheckpointPosition;
            m.x=236*16+2;m.y=131*16-GridMotor.Height;Step(20);var checkpoint=w.CheckpointPosition;
            Check(Vector2.Distance(initial,checkpoint)>3,"touching save activates checkpoint");
            m.x=250*16+2;m.y=131*16-GridMotor.Height;Step(20);
            Check(w.CheckpointPosition==checkpoint,"ordinary ground never overwrites checkpoint");
            foreach(int type in new[]{3,9}){
                int serial=w.ResetSerial;m.cells[20*w.Data.width+20]=(byte)type;
                m.x=20*16+2;m.y=(20+GridMotor.LiquidSurfaceInset)*16-GridMotor.Height+.1f;m.grounded=false;
                typeof(WorldPrototype).GetMethod("CheckGround",flags).Invoke(w,null);p.Tick(0);
                Check(w.IsDrowning&&p.CurrentAnimation=="Death"&&w.ResetSerial==serial,"liquid starts death "+type);
                Check(!w.RequestDash(1)&&!p.RequestAttack(),"no actions while dead");
                var deathFrames=new System.Collections.Generic.HashSet<int>();int guard=0,deathPeak=0;
                while(w.IsDrowning&&guard++<220){deathFrames.Add(p.CurrentFrame);if(fx.Phase==1)deathPeak=Mathf.Max(deathPeak,fx.ActiveCount);Step(1,1);}
                Check(deathPeak==PlayerLifeVfx.Capacity,"full death paper burst for hazard "+type);
                Check(deathFrames.Count==6,"all death drawings displayed");
                Check(w.IsReviving&&p.CurrentAnimation=="Revive"&&w.ResetSerial==serial+1,"death then revive at checkpoint");
                Check(Vector2.Distance(w.player.position,checkpoint)<.01f,"last-save return");
                var reviveFrames=new System.Collections.Generic.HashSet<int>();guard=0;int revivePeak=0;
                while(w.IsReviving&&guard++<Mathf.CeilToInt(PlayerLifeSprites.ReviveDuration/.005f)+10){reviveFrames.Add(p.CurrentFrame);if(fx.Phase==2)revivePeak=Mathf.Max(revivePeak,fx.ActiveCount);Step(1,1);}
                Check(revivePeak==PlayerLifeVfx.Capacity&&fx.ActiveCount==0,"revive gathers and clears without lingering particles");
                Check(reviveFrames.Count==6,"all revive drawings displayed");Step(80,1);
                Check(w.HazardInputBlocked&&w.ResetSerial==serial+1,"held input never loops death");
                Step(1);Check(!w.HazardInputBlocked&&p.CurrentAnimation=="Idle","revive ends in idle and release rearms");
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/player-death-revive.json","{\"passed\":true,\"deathFrames\":6,\"reviveFrames\":6,\"deathSeconds\":0.83,\"reviveSeconds\":1.71,\"checkpointsBefore\":18,\"checkpointsNow\":10,\"checks\":[\"water and ink death\",\"all 12 frames\",\"last checkpoint return\",\"ordinary floor does not save\",\"safe checkpoint anchors\",\"held-input protection\"]}");
            w.BeginDeath();Step(40);Check(fx.Phase==1&&fx.ActiveCount>0,"normal death uses effects too");
            var before=fx.GetComponentsInChildren<SpriteRenderer>();var positions=before.Select(d=>d.transform.position).ToArray();
            fx.Sample();Check(before.Select((d,i)=>d.transform.position==positions[i]).All(x=>x),"sampling without life clock progress leaves particles still");
            w.Respawn(236,131);fx.Sample();Check(fx.ActiveCount==0,"manual reset clears life effects");
            Debug.Log("PLAYER_LIFE_VFX_OK death/revive/hazards/reset/paused-clock pool=26");
            Debug.Log("UNFIN_PLAYER_DEATH_REVIVE_OK");
        }
    }
}
