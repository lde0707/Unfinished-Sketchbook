using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
namespace Unfin.Editor
{
    public static class PropRenderOrderValidation
    {
        public static void RunAndBuild(){
            PickupArtIntegration.Apply();
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            int tileOrder=UnityEngine.Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None).Min(r=>r.sortingOrder),count=0;
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None)){
                if(m.enemy)continue;
                PaperPickupVisual.Attach(m);
                if(m.kind=="save"){
                    var p=w.Data.pickups.First(a=>a.type=="save"&&Mathf.Abs(a.x-m.htmlPosition.x)<.1f&&Mathf.Abs(a.y-m.htmlPosition.y)<.1f);
                    m.gameObject.AddComponent<SaveShrineVisual>().Initialize(w,p);
                }
                foreach(var r in m.GetComponentsInChildren<SpriteRenderer>()){
                    if(r.sortingLayerID!=0||r.sortingOrder>=tileOrder||r.sortingOrder<=-20)throw new Exception("Prop must sit between room scenery and terrain: "+m.name+" / "+r.name);
                    count++;
                }
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/prop-render-order.json","{\"passed\":true,\"renderers\":"+count+",\"bodyOrder\":-2,\"effectOrder\":-1,\"terrainOrder\":"+tileOrder+"}");
            Debug.Log("UNFIN_PROP_RENDER_ORDER_OK "+count+" prop and effect renderers behind terrain");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
