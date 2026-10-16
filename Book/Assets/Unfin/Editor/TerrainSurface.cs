using System;
using UnityEngine;

namespace Unfin.Editor
{
    // Distance to exposed faces and diagonal notches produces one continuous material
    // shell. No face wins an if/else priority, including on one-cell steps and pillars.
    public static class TerrainSurface
    {
        public static readonly Color Interior = new Color(.016f, .017f, .023f, 1);
        public const float SurfaceDepth = 128;
        static readonly Vector2Int[] Neighbors = {
            new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(-1,0),
            new Vector2Int(-1,1), new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        public static Color Pixel(int mask, int x, int y, Func<float,float,Color> floor, Func<float,float,Color> wall)
        {
            if (mask == 0) return Interior;
            float px = x + .5f, py = y + .5f, nearest = 1000;
            // Find the closest point on the union of neighboring empty cells.
            for (int i = 0; i < 8; i++) if ((mask & (1 << i)) != 0)
                nearest = Mathf.Min(nearest, Delta(px, py, Neighbors[i]).magnitude);
            if (nearest >= SurfaceDepth) return Interior;
            Vector2 normal = Vector2.zero;
            for (int i = 0; i < 8; i++) if ((mask & (1 << i)) != 0)
            {
                Vector2 delta = Delta(px, py, Neighbors[i]);
                float weight = Mathf.Clamp01(1 - (delta.magnitude - nearest) / 3f);
                normal += delta.normalized * weight * weight;
            }
            float horizontal = Mathf.Abs(normal.x), vertical = Mathf.Abs(normal.y);
            // Opposite faces can cancel on thin platforms; blend both material axes there.
            float wallWeight = horizontal + vertical < .001f ? .5f : horizontal / (horizontal + vertical);
            Color surface = Color.Lerp(Periodic(floor, px, nearest, false), Periodic(wall, py, nearest, true), wallWeight);
            // Masters already shade gradually across their whole depth. Only join the
            // final eight pixels into the interior, instead of blacking out mid-tile.
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((nearest - 120) / 8));
            Color result = Color.Lerp(Interior, surface, fade);
            // Artwork supplies a shallow irregular rim; no ruler-straight cap overlay.
            result.a = surface.a * RoundedAlpha(mask, px, py);
            return result;
        }
        static Vector2 Delta(float x, float y, Vector2Int neighbor)
        {
            float x0 = neighbor.x * 128, y0 = neighbor.y * 128;
            return new Vector2(x - Mathf.Clamp(x, x0, x0 + 128), y - Mathf.Clamp(y, y0, y0 + 128));
        }
        static Color Periodic(Func<float,float,Color> sample, float tangent, float depth, bool wall)
        {
            float t = tangent / 128, d = 1 - depth / 128;
            return wall ? sample(d, t) : sample(t, d);
        }
        static float RoundedAlpha(int mask, float x, float y)
        {
            const float radius = 3;
            if ((mask & 9) == 9 && x < radius && y > 128 - radius) return CornerAlpha(x, 128-y, radius);
            if ((mask & 3) == 3 && x > 128-radius && y > 128-radius) return CornerAlpha(128-x, 128-y, radius);
            if ((mask & 6) == 6 && x > 128-radius && y < radius) return CornerAlpha(128-x, y, radius);
            if ((mask & 12) == 12 && x < radius && y < radius) return CornerAlpha(x, y, radius);
            return 1;
        }
        static float CornerAlpha(float x, float y, float radius) => Mathf.Clamp01(radius + .5f - new Vector2(x-radius, y-radius).magnitude);
    }
}
