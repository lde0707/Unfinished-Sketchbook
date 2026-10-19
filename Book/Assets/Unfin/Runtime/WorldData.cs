using System;
using UnityEngine;

namespace Unfin
{
    [Serializable] public class WorldArea
    {
        public string z, name, info;
        public int n, r;
        public int x, y, w, h;
    }
    [Serializable] public class WorldMarker
    {
        public string type, optionsJson;
        public int tx, floor;
        public float x, y, w, h;
    }
    [Serializable] public class WorldData
    {
        public int schema, width, height, htmlTileSize, tilePixels, pixelsPerUnit;
        public int referenceWidth, referenceHeight, regionX, startX, startFloor;
        public float orthographicSize;
        public string source, sourceSha256, gridSha256, mapRle, originalMetadataJson;
        public WorldArea[] rooms, passages, shafts;
        public WorldMarker[] enemies, pickups;

        public byte[] Decode()
        {
            var cells = new byte[width * height];
            int offset = 0;
            foreach (string run in mapRle.Split(','))
            {
                string[] pair = run.Split('.');
                int value = Base36(pair[0]), count = Base36(pair[1]);
                if (value > 255 || count <= 0 || offset + count > cells.Length)
                    throw new FormatException("Invalid world RLE");
                for (int i = 0; i < count; i++) cells[offset++] = (byte)value;
            }
            if (offset != cells.Length) throw new FormatException("Incomplete world RLE");
            return cells;
        }
        static int Base36(string value)
        {
            int result = 0;
            foreach (char c in value)
            {
                int digit = c >= '0' && c <= '9' ? c - '0' : c - 'a' + 10;
                if (digit < 0 || digit >= 36) throw new FormatException("Invalid base36 digit");
                result = checked(result * 36 + digit);
            }
            return result;
        }
        // HTML uses downward Y. Each Unity cell spans [x,x+1] by [-y-1,-y].
        public static Vector3Int Cell(int x, int y) => new Vector3Int(x, -y - 1, 0);
        public Vector3 Center(WorldMarker marker) => new Vector3(
            (marker.x + marker.w / 2) / htmlTileSize,
            -(marker.y + marker.h / 2) / htmlTileSize, 0);
    }

    // Collision math is kept in HTML logical pixels, independent of texture resolution.
    public sealed class GridMotor
    {
        public readonly WorldData data;
        public readonly byte[] cells;
        public float x, y, vx, vy, dropTime;
        public const float Width = 12, Height = 22;
        // Keep the sprite anchor unchanged. The smaller collision box lets the
        // painted feet/body overlap the 1.5–12px recessed terrain edge slightly.
        // One logical pixel is eight terrain pixels; never trace every small bump.
        public const float SideInset = 1, FootInset = 1;
        public const float CollisionHeight = Height - FootInset;
        public float Left => x + SideInset;
        public float Right => x + Width - SideInset;
        public float Bottom => y + CollisionHeight;
        public bool grounded;
        public int hitWall;
        public GridMotor(WorldData data, byte[] cells) { this.data = data; this.cells = cells; }
        public int Get(int tx, int ty) => tx < 0 || ty < 0 || tx >= data.width || ty >= data.height
            ? 1 : cells[ty * data.width + tx];
        public static bool Platform(int t) => t == 4 || t == 6 || t == 15;
        public const float LiquidSurfaceInset = .35f;
        public static bool Liquid(int t) => t == 3 || t == 9;
        public static bool Wall(int t) => t > 0 && !Liquid(t) && !Platform(t);
        public bool InLiquid(float px,float py)
        {
            int tx=Mathf.FloorToInt(px/16),ty=Mathf.FloorToInt(py/16);
            return Liquid(Get(tx,ty)) && (Liquid(Get(tx,ty-1)) || py>= (ty+LiquidSurfaceInset)*16);
        }
        public bool Column(int tx, float py, float h)
        {
            for (int ty = Mathf.FloorToInt(py / 16); ty <= Mathf.FloorToInt((py + h - .01f) / 16); ty++)
                if (Wall(Get(tx, ty))) return true;
            return false;
        }
        public bool Row(int ty, bool platform)
        {
            for (int tx = Mathf.FloorToInt(Left / 16); tx <= Mathf.FloorToInt((Right - .01f) / 16); tx++)
                if (platform ? Platform(Get(tx, ty)) : Wall(Get(tx, ty))) return true;
            return false;
        }
        public int TouchingWall()
        {
            return Column(Mathf.FloorToInt((Right + 1) / 16), y + 3, 16) ? 1 :
                Column(Mathf.FloorToInt((Left - 1) / 16), y + 3, 16) ? -1 : 0;
        }
        public void SnapToWall(int side)
        {
            int column = Mathf.FloorToInt((side > 0 ? Right + 1 : Left - 1) / 16);
            x = side > 0 ? column * 16 - Width + SideInset : (column + 1) * 16 - SideInset;
        }
        public void Move(float dt)
        {
            hitWall = 0; grounded = false; dropTime -= dt;
            x += vx * dt;
            int tx = Mathf.FloorToInt((vx > 0 ? Right - .01f : Left) / 16);
            if (vx != 0 && Column(tx, y, CollisionHeight))
            { hitWall = vx > 0 ? 1 : -1; x = vx > 0 ? tx * 16 - Width + SideInset : (tx + 1) * 16 - SideInset; vx = 0; }
            float previousBottom = Bottom;
            y += vy * dt;
            int ty = Mathf.FloorToInt((vy > 0 ? Bottom - .01f : y) / 16);
            if (vy > 0 && (Row(ty, false) || (dropTime <= 0 && Row(ty, true) && previousBottom <= ty * 16 + .5f)))
            { y = ty * 16 - CollisionHeight; vy = 0; grounded = true; }
            else if (vy < 0 && Row(ty, false)) { y = (ty + 1) * 16; vy = 0; }
        }
    }
}
