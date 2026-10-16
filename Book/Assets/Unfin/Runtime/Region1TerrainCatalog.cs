using UnityEngine;

namespace Unfin
{
    // Map-specific 128px tiles sampled from continuous four-cell painted modules.
    // Geometry comes from the unchanged HTML grid; special tiles remain separate.
    public sealed class Region1TerrainCatalog : ScriptableObject
    {
        public string gridSha256;
        public int width, height, uniqueSprites;
        public PrototypeTile[] terrain;
        public PrototypeTile[] fillets;
        public PrototypeTile Terrain(int x, int y) => x < 0 || y < 0 || x >= width || y >= height ? null : terrain[y * width + x];
        public PrototypeTile Fillet(int x, int y) => x < 0 || y < 0 || x >= width || y >= height ? null : fillets[y * width + x];
    }
}
