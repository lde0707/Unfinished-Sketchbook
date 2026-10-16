using System;

namespace Unfin
{
    public static class TerrainTopology
    {
        // Exposed cardinal faces: top/right/bottom/left = 1/2/4/8.
        // Concave corners: top-left/top-right/bottom-right/bottom-left = 16/32/64/128.
        // A diagonal matters only when both adjoining cardinal cells are terrain.
        public static int Mask(Func<int, int, int> get, int x, int y)
        {
            int faces = (get(x, y - 1) != 1 ? 1 : 0) | (get(x + 1, y) != 1 ? 2 : 0) |
                (get(x, y + 1) != 1 ? 4 : 0) | (get(x - 1, y) != 1 ? 8 : 0);
            if ((faces & 9) == 0 && get(x - 1, y - 1) != 1) faces |= 16;
            if ((faces & 3) == 0 && get(x + 1, y - 1) != 1) faces |= 32;
            if ((faces & 6) == 0 && get(x + 1, y + 1) != 1) faces |= 64;
            if ((faces & 12) == 0 && get(x - 1, y + 1) != 1) faces |= 128;
            return faces;
        }
        public static bool IsCanonical(int mask) =>
            ((mask & 16) == 0 || (mask & 9) == 0) && ((mask & 32) == 0 || (mask & 3) == 0) &&
            ((mask & 64) == 0 || (mask & 6) == 0) && ((mask & 128) == 0 || (mask & 12) == 0);
    }
}
