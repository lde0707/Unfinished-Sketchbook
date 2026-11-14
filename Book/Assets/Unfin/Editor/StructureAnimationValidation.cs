using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class StructureAnimationValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Structure animation: "+message);}
        static void Private(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
        static float Difference(Color32[] a,Color32[] b){float d=0;for(int i=0;i<a.Length;i++)d+=Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b);return d/(a.Length*3*255);}
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();Check(Resources.Load<Shader>("StructureFrameBlend").isSupported,"blend shader supported");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Private(w,"Awake");
            int animated=0;
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None)){
                if(m.enemy||m.htmlPosition.x/16<w.Data.regionX)continue;
                if(m.kind=="save") {var p=Array.Find(w.Data.pickups,a=>a.type==m.kind&&Mathf.Abs(a.x-m.htmlPosition.x)<.1f&&Mathf.Abs(a.y-m.htmlPosition.y)<.1f);if(p==null)continue;var v=m.GetComponent<SaveShrineVisual>()??m.gameObject.AddComponent<SaveShrineVisual>();v.Initialize(w,p);var at=m.transform.position;v.Tick(.05f);Check(v.InteractionBody.GetComponent<StructureFrameBlend>().Blend>0,"shrine interpolates");Check(at==m.transform.position,"shrine stays anchored");w.ToggleExplorationMap();var before=v.InteractionBody.GetComponent<StructureFrameBlend>().Blend;v.Tick(.1f);Check(before==v.InteractionBody.GetComponent<StructureFrameBlend>().Blend,"map pauses drawings");w.ToggleExplorationMap();animated++;}
                if(m.kind=="station"){PaperPickupVisual.Attach(m);var v=m.GetComponent<PaperPickupVisual>();v.Tick(.05f);Check(v.InteractionBody.GetComponent<StructureFrameBlend>().Blend>0,"portal interpolates");animated++;}
            }
            Check(animated>0,"live map props tested");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Animation QA camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.5f;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.075f,.07f,1);
            var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;
            var image=new Texture2D(256,256,TextureFormat.RGB24,false);
            var body=new GameObject("Drawing QA").AddComponent<SpriteRenderer>();
            Color32[] Render(string name,float phase){StructureFrameBlend.Device(body,name,phase,name=="brazier"?3:4);body.transform.localScale=Vector3.one*(2.6f/body.sprite.bounds.size.y);camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();RenderTexture.active=previous;return image.GetPixels32();}
            Directory.CreateDirectory("Validation/structure-animation");
            foreach(string name in new[]{"station","save","brazier","target","diary","gate"}){
                int count=name=="brazier"?3:4;
                for(int i=0;i<count;i++)Check(Region2Presentation.DeviceFrame(name,i).rect.size==Region2Presentation.DeviceFrame(name,0).rect.size,"registered canvas "+name);
                var before=Render(name,.999f);var after=Render(name,1.001f);Check(Difference(before,after)<.004f,"continuous key boundary "+name);
                before=Render(name,count-.001f);after=Render(name,.001f);Check(Difference(before,after)<.004f,"continuous loop seam "+name);
                var a=Render(name,0);var b=Render(name,1);var mid=Render(name,.5f);Check(Difference(a,b)>.0001f&&Difference(a,mid)>.00005f&&Difference(b,mid)>.00005f,"real intermediate pixels "+name);
                File.WriteAllBytes("Validation/structure-animation/"+name+"-mid.png",image.EncodeToPNG());
            }
            StructureFrameBlend.Device(body,"gate",99,4,false);Check(body.sprite==Region2Presentation.DeviceFrame("gate",3)&&StructureFrameBlend.For(body).Blend==0,"gate holds final open pose");
            camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
            File.WriteAllText("Validation/structure-animation.json","{\"passed\":true,\"liveMapProps\":"+animated+",\"gpuClips\":6,\"continuousFrameBoundaries\":true,\"continuousLoopSeams\":true,\"intermediatePixels\":true,\"fixedCanvas\":true,\"pause\":true,\"gateFinalPose\":true}");
            Debug.Log("STRUCTURE_ANIMATION_OK");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
