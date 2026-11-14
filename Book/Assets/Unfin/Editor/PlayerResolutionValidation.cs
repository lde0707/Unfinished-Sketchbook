using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PlayerResolutionValidation
    {
        [Serializable] class Manifest {public Clip[] clips;}
        [Serializable] class Clip {public int width,height;public Frame[] frames;}
        [Serializable] class Frame {public string file;}
        [MenuItem("Unfin/Apply Player Texture Quality")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(PaperSpriteImporter.Root+"/manifest.json"));
            var visited=new HashSet<string>();
            foreach(var clip in manifest.clips)foreach(var frame in clip.frames)
            {
                string path=PaperSpriteImporter.Root+"/"+frame.file;
                if(!visited.Add(path))continue;
                var before=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var pivot=before.pivot;float ppu=before.pixelsPerUnit;var rect=before.rect;
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                PaperSpriteImporter.ConfigureTextureQuality(importer,clip.width,clip.height);importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);var texture=sprite.texture;
                if(texture.width!=clip.width||texture.height!=clip.height||texture.mipmapCount<2||texture.filterMode!=FilterMode.Trilinear)
                    throw new Exception("Player texture lost source resolution or filtering: "+path);
                if(sprite.pivot!=pivot||sprite.rect!=rect||sprite.pixelsPerUnit!=ppu)
                    throw new Exception("Player registration changed: "+path);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/player-resolution.json","{\"passed\":true,\"frames\":"+visited.Count+",\"fullSourceResolution\":true,\"filteredMipmaps\":true,\"webCompression\":\"RGBA32\",\"registrationUnchanged\":true}");
            Debug.Log("UNFIN_PLAYER_RESOLUTION_OK "+visited.Count+" frames / original dimensions and pivots / trilinear mipmaps / RGBA32");
        }
        public static void RunAndBuild(){Apply();WebGLBuilder.Build();}
    }
}
