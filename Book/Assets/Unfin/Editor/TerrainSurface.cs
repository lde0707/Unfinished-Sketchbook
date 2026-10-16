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
        public const float OuterRadius = 28;
        public const float InnerRadius = 24;
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
                float weight = Mathf.Clamp01(1 - (delta.magnitude - nearest) / 18f);
                normal += delta.normalized * weight * weight;
            }
            // The material depth and normal follow the same arc as the silhouette.
            // Merely enlarging an alpha cut leaves straight bright bands inside it.
            float silhouette = 1;
            for (int corner = 0; corner < 4; corner++)
            {
                int faces = corner == 0 ? 9 : corner == 1 ? 3 : corner == 2 ? 6 : 12;
                if ((mask & faces) != faces) continue;
                float cx = corner == 1 || corner == 2 ? 128-px : px;
                float cy = corner < 2 ? 128-py : py;
                if (cx >= OuterRadius || cy >= OuterRadius) continue;
                var radial = new Vector2(OuterRadius-cx, OuterRadius-cy);
                float depth = OuterRadius - radial.magnitude;
                silhouette = Mathf.Min(silhouette, Mathf.Clamp01(depth + .5f));
                if (depth < nearest) { nearest = Mathf.Max(0, depth); normal = radial.normalized; }
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
            result.a = surface.a * silhouette;
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
        // Decorative concave fillet in an EMPTY cell. This layer has no collider.
        // Corner bits TL/TR/BR/BL. Each quarter arc meets both adjoining faces tangentially.
        public static Color Fillet(int mask, int x, int y, Func<float,float,Color> floor, Func<float,float,Color> wall)
        {
            float px=x+.5f, py=y+.5f;
            for (int corner=0;corner<4;corner++) if ((mask & (1<<corner)) != 0)
            {
                float cx=corner==1||corner==2?128-px:px, cy=corner<2?128-py:py;
                if(cx>=InnerRadius||cy>=InnerRadius)continue;
                var radial=new Vector2(InnerRadius-cx,InnerRadius-cy);
                float depth=radial.magnitude-InnerRadius;
                if(depth<=-.5f)continue;
                float weight=radial.x/(radial.x+radial.y);
                // Sample below the irregular transparent rim; its hidden RGB may be
                // bright and must not be made opaque along this new curve.
                float materialDepth=Mathf.Max(0,depth)+4;
                Color color=Color.Lerp(Periodic(floor,px,materialDepth,false),Periodic(wall,py,materialDepth,true),weight);
                color.a=Mathf.Clamp01(depth+.5f);
                return color;
            }
            return Color.clear;
        }
    }
}
