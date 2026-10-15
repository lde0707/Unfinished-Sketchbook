// Unfin 아트 가져오기 설정 자동 — Assets/Art/ 아래 PNG를 넣으면 종류별로 알맞게 설정 (Docs/20 §5-1)
// 🚫 픽셀 아트 아님: Point 필터·Pixel Perfect 금지. 부드럽고 선명한 가장자리 = Trilinear + 밉맵
using UnityEditor;
using UnityEngine;

namespace Unfin.Background.EditorTools
{
    public class UnfinArtImporter : AssetPostprocessor
    {
        public const string Root = "Assets/Art/";
        public const float PPU = 128f;   // 지도 1칸(16 논리 px) = 1유닛 = 그림 128px

        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.StartsWith(Root) || !p.EndsWith(".png")) return;
            var ti = (TextureImporter)assetImporter;
            string f = p.ToLowerInvariant();

            bool wall = f.Contains("/bg/") && (f.Contains("_wall_") || f.Contains("/tall/")) || f.Contains("/link/bg_");
            bool fill = f.Contains("/terrain/") && f.Contains("fill");
            bool edge = f.Contains("/terrain/") && f.Contains("edge");
            bool tall = f.Contains("/bg/tall/");
            bool big = f.Contains("/decor/") || f.Contains("/golden/");

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Trilinear;
            ti.mipmapEnabled = true;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = !(wall || fill);
            ti.alphaSource = (wall || fill) ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            // 지형 속 채움·띠는 Sprite Shape가 반복해서 써서 Repeat, 나머지는 Clamp (가장자리 번짐 방지)
            ti.wrapMode = (fill || edge) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;   // PC: BC7. 그라디언트 띠 생기면 Uncompressed
            ti.crunchedCompression = false;
            ti.maxTextureSize = tall || big ? 4096 : 2048;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = (f.Contains("/bg/") || f.Contains("/link/") || f.Contains("/terrain/")) ? SpriteMeshType.FullRect : SpriteMeshType.Tight;   // 배경·지형은 Tiled 그리기용
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteExtrude = 0;
            ti.SetTextureSettings(s);
        }
    }
}
