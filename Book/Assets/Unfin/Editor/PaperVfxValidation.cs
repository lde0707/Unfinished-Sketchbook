using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Unfin.Editor
{
 public static class PaperVfxValidation
 {
  public static void RunAndBuild(){Run();EnemyAnimationValidation.Run();WebGLBuilder.Build();}
  public static void Run(){
   AssetDatabase.Refresh();
   var files=Directory.GetFiles("Assets/Unfin/Resources/Effects/Kenney","*.png");
   if(files.Length!=6)throw new Exception("Six CC0 effect textures required");
   foreach(var path in files){
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
    importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
    importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=512;
    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   }
   var go=new GameObject("VFX validation");var fx=go.AddComponent<PaperVfx>();
   // Edit-mode AddComponent does not invoke Awake.
   typeof(PaperVfx).GetMethod("Awake",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(fx,null);
   for(int i=0;i<80;i++){fx.Hit(Vector2.zero,1);fx.Dust(Vector2.zero,1);fx.Dust(Vector2.zero,.7f,1,true);fx.Dash(Vector2.zero,-1);fx.Swing(Vector2.zero,1);}
   if(fx.ActiveCount!=PaperVfx.Capacity)throw new Exception("VFX pool limit");
   foreach(var sr in go.GetComponentsInChildren<SpriteRenderer>())if(sr.sprite==null||sr.color.r>.65f||sr.color.a>.53f)throw new Exception("Missing texture or excessive brightness");
   fx.Tick(1);if(fx.ActiveCount!=0)throw new Exception("VFX expiry");
   fx.Hit(Vector2.zero,-1);fx.Clear();if(fx.ActiveCount!=0)throw new Exception("VFX reset");
   UnityEngine.Object.DestroyImmediate(go);
   Debug.Log("UNFIN_VFX_OK six CC0 textures / muted alpha / 128 pooled particles / expiry / clear");
  }
 }
}
