using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class PlayerSurfaceIntegration
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Player paper surface: "+message);}
        [MenuItem("Unfin/Apply Player Paper Surface")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            const string path="Assets/Unfin/Resources/PlayerSurface/paper-fibers.png";
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=TextureImporterType.Default;ti.sRGBTexture=false;ti.npotScale=TextureImporterNPOTScale.None;
            ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.wrapMode=TextureWrapMode.Repeat;
            ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;ti.isReadable=true;
            var web=ti.GetPlatformTextureSettings("WebGL");web.overridden=true;web.format=TextureImporterFormat.RGBA32;web.maxTextureSize=2048;ti.SetPlatformTextureSettings(web);ti.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            double mean=texture.GetPixels32().Average(c=>(double)c.r)/255;
            File.WriteAllText("Assets/Unfin/Resources/PlayerSurface/mean.txt",mean.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var player=UnityEngine.Object.FindAnyObjectByType<PaperSpritePlayer>();
            PaperPlayerSurface.Apply(player.visual);foreach(var trail in player.trails)PaperPlayerSurface.Apply(trail);
            // Persist a normal asset material for scene/prefab consumers too.
            const string matPath="Assets/Unfin/Resources/PlayerSurface/PlayerPaperSurface.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(PaperPlayerSurface.Material);AssetDatabase.CreateAsset(mat,matPath);}
            else mat.CopyPropertiesFromMaterial(PaperPlayerSurface.Material);
            mat.hideFlags=HideFlags.None;EditorUtility.SetDirty(mat);
            player.visual.sharedMaterial=mat;foreach(var trail in player.trails)trail.sharedMaterial=mat;
            var prefab=PrefabUtility.LoadPrefabContents("Assets/Unfin/Prefabs/PaperSpritePlayer.prefab");
            var prefabPlayer=prefab.GetComponent<PaperSpritePlayer>();prefabPlayer.visual.sharedMaterial=mat;foreach(var trail in prefabPlayer.trails)trail.sharedMaterial=mat;
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Unfin/Prefabs/PaperSpritePlayer.prefab");PrefabUtility.UnloadPrefabContents(prefab);
            EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void Validate()
        {
            var library=AssetDatabase.LoadAssetAtPath<PaperSpriteLibrary>(PaperSpriteImporter.AnimRoot+"/PlayerSprites.asset");
            var shader=Resources.Load<Shader>("PlayerSurface/PlayerPaperSurface");
            Check(shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"shader compiles");
            // Include every source drawing, including a drawing omitted by a runtime slice.
            var frames=Directory.GetFiles(PaperSpriteImporter.Root,"*.png",SearchOption.AllDirectories)
                .Where(p=>!p.Replace('\\','/').Contains("/Effects/"))
                .Select(p=>AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/'))).ToList();
            // WallHold reuses WallClimb's first PNG: 91 manifest slots, 90 files.
            Check(frames.Count==90&&frames.All(s=>s!=null),"all 90 unique source drawings loaded");
            foreach(string name in new[]{"Death","Revive"})frames.AddRange(PlayerLifeSprites.Find(name).frames.Select(f=>f.sprite));
            var root=new GameObject("Paper surface render validation");root.layer=31;root.transform.position=new Vector3(10000,10000,0);
            var sr=root.AddComponent<SpriteRenderer>();sr.sortingOrder=32760;
            var cg=new GameObject("Paper surface validation camera");var camera=cg.AddComponent<Camera>();
            camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;
            camera.allowHDR=false;camera.allowMSAA=false;camera.nearClipPlane=.1f;camera.farClipPlane=20;
            var standard=new Material(Shader.Find("Sprites/Default"));int changed=0,alphaMismatches=0;
            Directory.CreateDirectory("Validation");
            foreach(var sprite in frames)
            {
                sr.sprite=sprite;int width=(int)sprite.rect.width,height=(int)sprite.rect.height;
                camera.orthographicSize=height/sprite.pixelsPerUnit/2;camera.aspect=(float)width/height;
                camera.transform.position=sr.bounds.center+Vector3.back*10;
                var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
                Texture2D Render(Material material){sr.sharedMaterial=material;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(width,height,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();RenderTexture.active=previous;return t;}
                var before=Render(standard);var after=Render(PaperPlayerSurface.Material);var a=before.GetPixels32();var b=after.GetPixels32();int rgb=0;
                for(int i=0;i<a.Length;i++){if(a[i].a!=b[i].a)alphaMismatches++;if(a[i].a>128&&(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>3))rgb++;}
                if(rgb>10)changed++;
                if(sprite==library.Find("Idle").frames[0].sprite){File.WriteAllBytes("Validation/player-surface-idle-before.png",before.EncodeToPNG());File.WriteAllBytes("Validation/player-surface-idle-after.png",after.EncodeToPNG());}
                UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
            }
            Check(changed==frames.Count,"all "+frames.Count+" drawings receive material");Check(alphaMismatches==0,"silhouette alpha remains pixel-identical");
            sr.sharedMaterial=PaperPlayerSurface.Material;var flash=SpriteHitFeedback.Get(sr);flash.Trigger(true);Check(sr.sharedMaterial==PaperPlayerSurface.Material,"hit flash retains paper surface");flash.Tick(.4f);Check(sr.sharedMaterial==PaperPlayerSurface.Material,"surface survives hit expiry");
            var enemy=new GameObject("Enemy material isolation");var esr=enemy.AddComponent<SpriteRenderer>();esr.sprite=sr.sprite;SpriteHitFeedback.Get(esr).Trigger(false);Check(esr.sharedMaterial.shader.name=="Unfin/Paper Hit Flash","enemy flash unchanged");
            UnityEngine.Object.DestroyImmediate(enemy);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(standard);
            File.WriteAllText("Validation/player-surface.json","{\"passed\":true,\"drawings\":"+frames.Count+",\"rgbChangedDrawings\":"+changed+",\"alphaMismatchPixels\":"+alphaMismatches+",\"hitFlashPreserved\":true,\"enemyMaterialUnchanged\":true}");
            Debug.Log("UNFIN_PLAYER_SURFACE_OK "+frames.Count+" drawings / exact alpha / hit flash retained");
        }
        public static void RunAndBuild(){Apply();Validate();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
    }
}
