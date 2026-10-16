namespace Unfin
{
    public static class TerrainPatterns
    {
        public const int Count = 3;
        public static int Key(int region, int variant, int mask) => (region * Count + variant) * 256 + mask;

        // Coordinate-stable coloring: adjacent cells never repeat the same pattern.
        // Independent of occupancy so opening a gate does not reshuffle nearby art.
        public static byte[] Create(int width, int height)
        {
            var result = new byte[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                uint hash = unchecked((uint)x * 374761393u + (uint)y * 668265263u + 1234567u);
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                int first = (int)((hash ^ (hash >> 16)) % Count);
                for (int i = 0; i < Count; i++)
                {
                    int candidate = (first + i) % Count;
                    if (x > 0 && result[y * width + x - 1] == candidate) continue;
                    if (y > 0 && result[(y - 1) * width + x] == candidate) continue;
                    result[y * width + x] = (byte)candidate; break;
                }
            }
            return result;
        }
    }
}
