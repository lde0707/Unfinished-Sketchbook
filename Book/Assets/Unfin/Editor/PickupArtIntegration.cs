using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class PickupArtIntegration
    {
        const string Root="Assets/Unfin/Resources/PickupArt";
        static void Check(bool ok,string message){if(!ok)throw new Exception("Pickup art: "+message);}
        public static void Apply(){
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Root,"*.png")){
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=256;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;
                var web=importer.GetPlatformTextureSettings("WebGL");web.overridden=true;web.format=TextureImporterFormat.RGBA32;web.maxTextureSize=1024;importer.SetPlatformTextureSettings(web);importer.SaveAndReimport();
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);int count=0;
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None)){
                if(!m.enemy)m.GetComponent<SpriteRenderer>().sortingOrder=PaperPickupVisual.BodySortingOrder;
                if(m.enemy||(!PaperPickupVisual.Supports(m.kind)&&m.kind!="save"))continue;
                string name=m.kind.StartsWith("sw_")?"pigment":m.kind;var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+name+".png");Check(sprite!=null,"missing "+name);
                var r=m.GetComponent<SpriteRenderer>();r.sprite=sprite;r.color=m.kind=="sw_r"?new Color(1,.64f,.61f):m.kind=="sw_b"?new Color(.55f,.73f,1):Color.white;
                float h=m.kind=="save"?SaveShrineVisual.Height:m.kind=="station"?3.55f:m.kind=="healup"?.85f:.65f;m.transform.localScale=Vector3.one*(h/sprite.bounds.size.y);
                if(m.kind=="save"){var p=Array.Find(data.pickups,a=>a.type==m.kind&&Mathf.Abs(a.x-m.htmlPosition.x)<.1f&&Mathf.Abs(a.y-m.htmlPosition.y)<.1f);Check(p!=null,"save placement");m.transform.position=SaveShrineVisual.Position(p);}
                if(m.kind=="station"){var p=Array.Find(data.pickups,a=>a.type==m.kind&&Mathf.Abs(a.x-m.htmlPosition.x)<.1f&&Mathf.Abs(a.y-m.htmlPosition.y)<.1f);Check(p!=null,"station source placement");m.transform.position=new Vector3((p.x+p.w*.5f)/16,-p.floor+h*.5f-.37f,0);}
                count++;
            }
            EditorSceneManager.SaveScene(UnityEngine.Object.FindAnyObjectByType<WorldPrototype>().gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("UNFIN_PICKUP_ART_APPLIED "+count);
        }
        public static void Validate(){
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            Check(Resources.Load<Shader>("PickupArt/PickupRim")!=null,"rim shader included");
            var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Where(m=>!m.enemy&&PaperPickupVisual.Supports(m.kind)).ToArray();
            foreach(var m in markers)PaperPickupVisual.Attach(m);
            int tested=0;
            foreach(var group in markers.Where(m=>m.kind!="station").GroupBy(m=>m.kind)){
                var m=group.First();var p=w.Data.pickups.First(a=>a.type==m.kind&&Mathf.Abs(a.x-m.htmlPosition.x)<.1f&&Mathf.Abs(a.y-m.htmlPosition.y)<.1f);
                var v=m.GetComponent<PaperPickupVisual>();Check(v!=null,"visual "+m.kind);w.Progression.Collect(p);v.Tick(.1f);Check(v.Consuming&&m.gameObject.activeSelf,"pickup remains visible during consumption "+m.kind);
                v.Tick(.4f);Check(!m.gameObject.activeSelf,"consumption finishes "+m.kind);float health=w.Progression.MaxHealth;int shards=w.Progression.Shards;w.Progression.Collect(p);Check(health==w.Progression.MaxHealth&&shards==w.Progression.Shards,"single reward "+m.kind);tested++;
            }
            var source=w.Data.pickups.First(p=>p.type=="station"&&p.optionsJson.Contains("r2d"));var target=w.Data.pickups.First(p=>p.type=="station"&&p.optionsJson.Contains("r1s"));
            w.TravelTo(source.tx,source.floor);float oldX=w.Motor.x,hp=w.Progression.Health;var checkpoint=w.CheckpointPosition;
            typeof(WorldProgression).GetMethod("Travel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w.Progression,new object[]{"r2d"});Check(w.Progression.PortalTravelling,"entry starts delayed travel");
            w.Simulate(.1f,1);Check(Mathf.Abs(w.Motor.x-oldX)<.01f,"entry briefly holds player");w.Simulate(.13f);Check(!w.Progression.PortalTravelling&&Mathf.Abs((w.Motor.x+6)/16-target.tx)<1,"arrival at paired portal");
            Check(w.Progression.Health==hp&&w.CheckpointPosition==checkpoint,"travel preserves health and checkpoint");
            var arrival=markers.First(m=>m.kind=="station"&&m.originalOptions.Contains("r1s")).GetComponent<PaperPickupVisual>();Check(arrival.Pulse>0,"arrival animation triggered");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/pickup-art.json","{\"passed\":true,\"markers\":"+markers.Length+",\"pickupKinds\":"+tested+",\"consumeSeconds\":0.42,\"portalEntrySeconds\":0.22,\"arrivalPulse\":true}");
            Debug.Log("UNFIN_PICKUP_ART_OK "+markers.Length+" visuals / "+tested+" collectible types / consume + paired portal animations");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        public static void RunAndBuild(){Apply();Validate();WebGLBuilder.Build();}
    }
}
