using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unfin
{
    // The eight existing map zones, not eight new world regions.
    public static class RegionalTilePalette
    {
        public const string Zones="ABCDEFGH";
        public static readonly string[] Names={"종이굴","흑연 광산","나무 속","뿌리 심부","노을 굴","밀랍 공방","지하 협곡","기사의 탑"};
        public static readonly int[] PlatformSources={0,2,0,4,1,0,2,3};
        public static readonly Color[] Colors={
            new Color(.66f,.61f,.50f),new Color(.41f,.51f,.62f),
            new Color(.53f,.37f,.22f),new Color(.46f,.28f,.35f),
            new Color(.74f,.38f,.18f),new Color(.66f,.59f,.39f),
            new Color(.30f,.42f,.66f),new Color(.62f,.27f,.30f)};

        static float Distance(WorldArea r,float x,float y)
        {
            float dx=Mathf.Max(r.x-x,0,x-r.x-r.w),dy=Mathf.Max(r.y-y,0,y-r.y-r.h);
            return Mathf.Sqrt(dx*dx+dy*dy);
        }
        public static int ZoneAt(WorldData data,int x,int y)
        {
            int region=x>=data.regionX?2:1,best=region==1?0:4;float distance=float.PositiveInfinity;
            foreach(var room in data.rooms)if(room.r==region){int z=Zones.IndexOf(room.z);if(z<0)continue;
                float d=Distance(room,x+.5f,y+.5f);if(d<distance){distance=d;best=z;}}
            return best;
        }
        public static Color TerrainColor(WorldData data,int x,int y)
        {
            if(x<data.regionX)return Color.white; // World 1 already has four painted, blended materials.
            float a=float.PositiveInfinity,b=a,c=a,d=a;
            foreach(var room in data.rooms)if(room.r==2){float v=Distance(room,x+.5f,y+.5f);
                switch(room.z){case "E":a=Mathf.Min(a,v);break;case "F":b=Mathf.Min(b,v);break;case "G":c=Mathf.Min(c,v);break;case "H":d=Mathf.Min(d,v);break;}}
            float min=Mathf.Min(a,b,c,d);
            float Weight(float v)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(1-(v-min)/4));
            a=Weight(a);b=Weight(b);c=Weight(c);d=Weight(d);
            var color=(Colors[4]*a+Colors[5]*b+Colors[6]*c+Colors[7]*d)/(a+b+c+d);color.a=1;return color;
        }
        public static void ApplyTerrainCell(WorldPrototype world,WorldData data,int x,int y)
        {
            if(x<data.regionX)return;
            var cell=WorldData.Cell(x,y);var color=TerrainColor(data,x,y);
            void Apply(Tilemap map){if(map==null||!map.HasTile(cell))return;
                map.SetTileFlags(cell,map.GetTileFlags(cell)&~TileFlags.LockColor);map.SetColor(cell,color);}
            var tile=world.maps[3].GetTile<PrototypeTile>(cell);
            if(tile!=null&&tile.htmlType==1)Apply(world.maps[3]);
            Apply(world.filletMaps[1]);
        }
    }
}
