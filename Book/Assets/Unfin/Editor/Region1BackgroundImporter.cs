using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class Region1BackgroundImporter
    {
        const string Root = "Assets/Unfin/Art/Region1BackgroundV3";
        const string ObjectName = "World 1 - Blended regional back walls";
        static readonly string[] Files = { "A-paper-wall", "B-graphite-wall", "C-wood-wall", "D-root-wall" };
        static readonly string[] Properties = { "_Paper", "_Graphite", "_Wood", "_Root" };

        public static void Apply(WorldData data)
        {
            var shader = Shader.Find("Unfin/Region1 Background");
            if (shader == null || !shader.isSupported) throw new Exception("Background shader unavailable");
            Directory.CreateDirectory(Root);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Background.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Root + "/Background.mat"); }
            material.shader = shader;
            for (int i=0;i<Files.Length;i++)
            {
                string path = Root + "/" + Files[i] + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new Exception("Missing background source: " + path);
                importer.textureType=TextureImporterType.Default; importer.sRGBTexture=true;
                importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Bilinear;
                importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=1024; importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport(); material.SetTexture(Properties[i],AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }
            // Use the terrain's nearest-room distance field so ceilings, shafts
            // and irregular corridors share their zone with the back wall.
            var rooms=data.rooms.Where(r=>r.r==1 && "ABCD".Contains(r.z)).ToArray();
            var zones=new Texture2D(data.regionX+1,data.height+1,TextureFormat.RGBA32,false,true);
            var pixels=new Color[(data.regionX+1)*(data.height+1)];
            for(int y=0;y<=data.height;y++) for(int x=0;x<=data.regionX;x++)
            {
                float[] distances={float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity};
                foreach(var r in rooms)
                {
                    int k="ABCD".IndexOf(r.z,StringComparison.Ordinal);
                    float dx=Mathf.Max(r.x-x,0,x-r.x-r.w),dy=Mathf.Max(r.y-y,0,y-r.y-r.h);
                    distances[k]=Mathf.Min(distances[k],Mathf.Sqrt(dx*dx+dy*dy));
                }
                float nearest=distances.Min(),total=0; var color=new Color();
                for(int k=0;k<4;k++) { color[k]=Mathf.SmoothStep(0,1,1-(distances[k]-nearest)/4f); total+=color[k]; }
                pixels[y*(data.regionX+1)+x]=color/total;
            }
            zones.SetPixels(pixels); zones.Apply();
            PreviewOutput.Write(Root+"/ZoneWeights.png",zones.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(zones);
            AssetDatabase.ImportAsset(Root+"/ZoneWeights.png",ImportAssetOptions.ForceUpdate);
            var zi=(TextureImporter)AssetImporter.GetAtPath(Root+"/ZoneWeights.png");
            zi.sRGBTexture=false; zi.alphaSource=TextureImporterAlphaSource.FromInput;
            zi.alphaIsTransparency=false; zi.mipmapEnabled=false; zi.npotScale=TextureImporterNPOTScale.None;
            zi.wrapMode=TextureWrapMode.Clamp; zi.filterMode=FilterMode.Bilinear;
            zi.textureCompression=TextureImporterCompression.Uncompressed; zi.SaveAndReimport();
            material.SetTexture("_Zones",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/ZoneWeights.png"));
            material.SetVector("_Bounds",new Vector4(data.regionX,data.height,0,0)); material.SetFloat("_Opacity",.52f);
            EditorUtility.SetDirty(material);
            var old=GameObject.Find(ObjectName); if(old!=null) UnityEngine.Object.DestroyImmediate(old);
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name=ObjectName;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position=new Vector3(data.regionX*.5f,-data.height*.5f,5);
            go.transform.localScale=new Vector3(data.regionX,data.height,1);
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.sortingOrder=-100;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Unfin/Apply Region 1 Backgrounds and Validate")]
        public static void ApplyAndValidate()
        {
            AssetDatabase.Refresh(); EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);
            Apply(data); EditorSceneManager.SaveScene(world.gameObject.scene);
            WorldPrototypeBuilder.Validate();
            world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var camera=world.worldCamera; camera.aspect=1920f/1080; camera.orthographicSize=data.orthographicSize;
            foreach(int n in new[]{1,7,13,18})
            {
                var r=data.rooms.First(a=>a.r==1 && a.n==n);
                Capture(camera,new Vector3(r.x+r.w*.5f,-r.y-r.h+3.2f,-10),"zone-"+r.z);
            }
            Capture(camera,new Vector3(data.startX+.5f,-data.startFloor+1.6875f,-10),"start");
            var next=data.rooms.First(r=>r.r==2);
            Capture(camera,new Vector3(next.x+next.w*.5f,-next.y-next.h+3.2f,-10),"world2");
            PreviewOutput.Write("Validation/region1-background-v3.json",System.Text.Encoding.UTF8.GetBytes(
                "{\"version\":3,\"gridSha256\":\""+data.gridSha256+"\",\"zones\":4,\"tileUnits\":8,\"strideUnits\":6,\"overlapUnits\":2,\"opacity\":0.52,\"worldAnchored\":true,\"collidersAdded\":0}"));
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            Debug.Log("UNFIN_BACKGROUND_OK");
        }
        static void Capture(Camera camera,Vector3 position,string name)
        {
            camera.transform.position=position;
            var target=new RenderTexture(1920,1080,24); var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
                PreviewOutput.Write("Validation/background-v3-"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=null; camera.targetTexture=null; target.Release();
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            }
        }

        public static void RenderTransitions()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var camera=world.worldCamera; camera.aspect=1920f/1080;
            var positions=new[]{new Vector3(172.5f,-115,-10),new Vector3(123.5f,-81,-10),
                new Vector3(169,-10,-10),new Vector3(252.5f,-90,-10)};
            string[] names={"A-B","C-A","C-D","D-A"};
            for(int i=0;i<positions.Length;i++)
            {
                Capture(camera,positions[i],"transition-"+names[i]);
                Capture(camera,positions[i]+(i==2?Vector3.right:Vector3.up),"transition-"+names[i]+"-moved");
            }
            var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);
            var next=data.rooms.First(r=>r.r==2);
            GameObject.Find(ObjectName).SetActive(false);
            Capture(camera,new Vector3(next.x+next.w*.5f,-next.y-next.h+3.2f,-10),"world2-baseline");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            Debug.Log("UNFIN_BACKGROUND_TRANSITIONS_OK");
        }
    }
}
