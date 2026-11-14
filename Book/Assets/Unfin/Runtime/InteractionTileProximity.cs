using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Unfin
{
    public sealed class InteractionTileProximity : MonoBehaviour
    {
        WorldPrototype world;
        readonly Tilemap[] overlays=new Tilemap[6];
        readonly Dictionary<int,float> levels=new Dictionary<int,float>();
        readonly HashSet<int> candidates=new HashSet<int>();
        public int GlowingCells=>levels.Count;
        public static void Create(WorldPrototype w){if(w.GetComponent<InteractionTileProximity>()==null)w.gameObject.AddComponent<InteractionTileProximity>().Initialize(w);}
        public void Initialize(WorldPrototype w)
        {
            world=w;
            for(int i=0;i<overlays.Length;i++){
                var source=w.maps[i];var go=new GameObject("Nearby special surface "+i);go.transform.SetParent(source.transform.parent,false);
                go.transform.localPosition=source.transform.localPosition;go.transform.localRotation=source.transform.localRotation;go.transform.localScale=source.transform.localScale;
                var map=go.AddComponent<Tilemap>();map.tileAnchor=source.tileAnchor;overlays[i]=map;
                var r=go.AddComponent<TilemapRenderer>();var original=source.GetComponent<TilemapRenderer>();r.sortingLayerID=original.sortingLayerID;r.sortingOrder=original.sortingOrder+1;r.sharedMaterial=InteractionProximity.Material;
                // Visual only. No collider, no changes to the source Tilemap.
            }
        }
        void LateUpdate(){Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(world==null)return;
            foreach(var map in overlays)map.GetComponent<TilemapRenderer>().enabled=InteractionProximity.FeedbackEnabled;
            if(world.MapOpen||StilldrawnTitle.Blocking)return;
            int width=world.Data.width,px=Mathf.FloorToInt(world.Motor.WorldBounds.center.x),py=Mathf.FloorToInt(-world.Motor.WorldBounds.center.y);
            candidates.Clear();foreach(int key in levels.Keys)candidates.Add(key);
            for(int y=Mathf.Max(0,py-4);y<=Mathf.Min(world.Data.height-1,py+4);y++)for(int x=Mathf.Max(0,px-4);x<=Mathf.Min(width-1,px+4);x++)
                if(InteractionProximity.SpecialTile(world.Motor.Get(x,y)))candidates.Add(y*width+x);
            foreach(int key in candidates){
                int x=key%width,y=key/width,type=world.Motor.Get(x,y);var cell=WorldData.Cell(x,y);
                int index=(x>=world.Data.regionX?3:0)+WorldPrototype.Layer(type);
                var source=world.maps[index];bool usable=InteractionProximity.SpecialTile(type)&&source.HasTile(cell);
                float height=type==6?.25f:type==15?.3f:1;
                var bounds=new Bounds(new Vector3(x+.5f,-y-height*.5f,0),new Vector3(1,height,.1f));
                float target=usable?InteractionProximity.Near(world,bounds):0;
                levels.TryGetValue(key,out float old);float strength=InteractionProximity.Approach(old,target,dt);
                if(!usable||strength<.005f){foreach(var map in overlays)if(map.HasTile(cell))map.SetTile(cell,null);levels.Remove(key);continue;}
                levels[key]=strength;var overlay=overlays[index];var tile=source.GetTile(cell);
                if(overlay.GetTile(cell)!=tile)overlay.SetTile(cell,tile);
                overlay.SetTileFlags(cell,TileFlags.None);overlay.SetTransformMatrix(cell,source.GetTransformMatrix(cell));
                overlay.SetColor(cell,new Color(1,1,1,strength*source.GetColor(cell).a));
            }
        }
        void OnDestroy(){foreach(var map in overlays)if(map!=null){if(Application.isPlaying)Destroy(map.gameObject);else DestroyImmediate(map.gameObject);}}
    }
    public sealed partial class WorldPrototype
    {
        // Screenshot and regression audit hooks; they do not grant abilities or
        // alter interactions, terrain, pickup state or saved progression.
        public void PreviewInteractionGlow(string enabled){InteractionProximity.FeedbackEnabled=enabled!="off";}
        public void ReportInteractionGlow(string unused)
        {
            int total=0,near=0,lockedFriends=0;float strongest=0;
            foreach(var glow in FindObjectsByType<InteractionProximity>(FindObjectsSortMode.None)){
                total++;if(glow.Strength>.05f)near++;strongest=Mathf.Max(strongest,glow.Strength);
                var m=glow.GetComponent<MapMarker>();if(m!=null&&m.kind=="sketch"&&glow.Strength>.05f&&!Progression.Has(m.originalOptions.Contains("sun")?"climb":"cut"))lockedFriends++;
            }
            Debug.Log("UNFIN_INTERACTION_GLOW total="+total+" near="+near+" lockedFriends="+lockedFriends+" strongest="+strongest.ToString("F3")+" tiles="+(GetComponent<InteractionTileProximity>()?.GlowingCells??0));
        }
    }
}
