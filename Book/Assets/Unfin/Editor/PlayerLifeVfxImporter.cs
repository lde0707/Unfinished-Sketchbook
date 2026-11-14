using UnityEditor;
namespace Unfin.Editor
{
    public sealed class PlayerLifeVfxImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(assetPath!="Assets/Unfin/Resources/PlayerLifeEffects/paper-ink-atlas-v1.png")return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;
            t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;
            t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;
            t.filterMode=UnityEngine.FilterMode.Bilinear;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;
            var web=t.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=2048;web.format=TextureImporterFormat.RGBA32;t.SetPlatformTextureSettings(web);
        }
    }
}
