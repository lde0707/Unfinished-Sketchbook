using UnityEditor;
using UnityEngine;
namespace Unfin.Editor
{
    public sealed class AtmosphereTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Unfin/Resources/Atmosphere/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaIsTransparency=true;
            t.mipmapEnabled=true;t.sRGBTexture=true;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;
            t.textureCompression=TextureImporterCompression.Uncompressed;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;
        }
    }
}
