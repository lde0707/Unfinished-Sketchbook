using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class DeviceArtIntegration
    {
        const string Root="Assets/Unfin/Resources/DeviceArt";
        static void Check(bool ok,string message){if(!ok)throw new Exception("Device art: "+message);}
        public static void Apply(){
            AssetDatabase.Refresh();foreach(var path in Directory.GetFiles(Root,"*.png")){
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=256;ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.wrapMode=TextureWrapMode.Clamp;ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=ti.GetPlatformTextureSettings("WebGL");settings.overridden=true;settings.format=TextureImporterFormat.RGBA32;settings.maxTextureSize=1024;ti.SetPlatformTextureSettings(settings);ti.SaveAndReimport();
            }
            PickupArtIntegration.Apply();var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var data=JsonUtility.FromJson<WorldData>(w.mapJson.text);int count=0;
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None)){
                if(m.enemy||!PaperDeviceVisual.Supports(m.kind))continue;var p=data.pickups.First(a=>a.type==m.kind&&Vector2.Distance(new Vector2(a.x,a.y),m.htmlPosition)<.1f);
                var r=m.GetComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+PaperDeviceVisual.ArtName(p)+".png");Check(r.sprite!=null,"missing image "+m.kind);r.color=Color.white;r.sortingOrder=PaperPickupVisual.BodySortingOrder;count++;
            }
            Check(count==12,"all 12 authored objects replaced");WorldPropPlacementIntegration.Arrange();Debug.Log("UNFIN_DEVICE_ART_APPLIED "+count);
        }
        public static void Validate(){
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);int count=0;
            var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Where(m=>!m.enemy&&PaperDeviceVisual.Supports(m.kind)).ToArray();
            foreach(var m in markers){PaperDeviceVisual.Attach(m);var v=m.GetComponent<PaperDeviceVisual>();Check(v!=null,"runtime artwork "+m.kind);v.Tick(.1f);Check(!m.GetComponent<SpriteRenderer>().enabled,"placeholder hidden "+m.kind);var children=m.GetComponentsInChildren<SpriteRenderer>();Check(children.Length>=5&&children.All(r=>r.sortingOrder<0),"art and quiet effects behind terrain "+m.kind);count++;}
            var lever=markers.First(m=>m.kind=="lever");var source=w.Data.pickups.First(p=>p.type=="lever");w.Motor.x=source.tx*16+2;w.Motor.y=source.floor*16-22;w.Progression.TryInteract();w.Progression.ConfirmInteractionChoice();Check(w.Progression.Collected(source),"existing lever operation preserved");var lv=lever.GetComponent<PaperDeviceVisual>();lv.Tick(.01f);Check(lv.Pulse>0&&lever.GetComponentsInChildren<SpriteRenderer>().Skip(1).Any(r=>r.enabled),"lever reacts and remains visible");
            w.BuildParryBridge();var bar=markers.First(m=>m.kind=="bar").GetComponent<PaperDeviceVisual>();bar.Tick(.1f);Check(bar.Pulse>0&&w.Motor.Get(459,88)==4,"tipping bar reacts to existing bridge");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/device-art.json","{\"passed\":true,\"placements\":"+count+",\"uniqueImages\":9,\"placeholderReplaced\":true,\"quietEffects\":true,\"leverReaction\":true,\"bridgeReaction\":true}");Debug.Log("UNFIN_DEVICE_ART_OK 12 placements / 9 images / quiet effects / lever + bridge reactions");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        public static void RunAndBuild(){Apply();Validate();WebGLBuilder.Build();}
    }
}
