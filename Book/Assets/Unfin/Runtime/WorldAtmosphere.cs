using System.Collections.Generic;
using UnityEngine;

namespace Unfin
{
    // One shared, paused clock. Fog samples world coordinates; only its coverage
    // quad follows the camera. Decoration anchors and gameplay geometry never move.
    [DefaultExecutionOrder(250)]
    public sealed class WorldAtmosphere : MonoBehaviour
    {
        public static readonly Color[] HazeColors={
            new Color(.64f,.60f,.49f),new Color(.40f,.51f,.62f),
            new Color(.48f,.57f,.40f),new Color(.43f,.37f,.51f),
            new Color(.70f,.42f,.25f),new Color(.67f,.55f,.32f),
            new Color(.36f,.48f,.65f),new Color(.57f,.39f,.44f)};
        static readonly float[] Rays={.85f,.40f,.75f,.20f,.95f,.70f,.40f,.60f};
        static readonly int[] HangingArt={0,0,1,1,2,2,3,3};
        public int Zone {get;private set;}
        public float Clock {get;private set;}
        public bool VisualEnabled {get;private set;}=true;
        public int DecorationCount=>decorations.Count;
        public Color DustColor=>Color.Lerp(HazeColors[Zone],new Color(.85f,.81f,.70f),.48f);
        WorldPrototype world;Mesh mesh;Material air,cloth;MeshRenderer airDraw;
        readonly List<SpriteRenderer> decorations=new List<SpriteRenderer>();
        readonly List<Sprite> sprites=new List<Sprite>();
        readonly int[] zoneDecorationCounts=new int[8];
        Color haze;float rays;Vector4 roomBounds;Vector2 lastPlayer;int resetSerial;
        bool ready;
        static readonly int ClockId=Shader.PropertyToID("_StilldrawnAtmosphereTime"),StrengthId=Shader.PropertyToID("_StilldrawnAtmosphereStrength");
        public static WorldAtmosphere Create(WorldPrototype w)
        {
            var value=w.GetComponent<WorldAtmosphere>();if(value!=null)return value;
            value=w.gameObject.AddComponent<WorldAtmosphere>();value.Initialize(w);return value;
        }
        public void Initialize(WorldPrototype w)
        {
            world=w;resetSerial=w.ResetSerial;
            var shader=Resources.Load<Shader>("Atmosphere/WorldAir");
            if(shader==null){enabled=false;return;}
            air=new Material(shader){hideFlags=HideFlags.DontSave};
            mesh=new Mesh{name="Atmosphere coverage"};
            mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            var go=new GameObject("World anchored air and light");go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;airDraw=go.AddComponent<MeshRenderer>();airDraw.sharedMaterial=air;airDraw.sortingOrder=-12;
            airDraw.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;airDraw.receiveShadows=false;
            BuildDecorations();
        }
        WorldArea NearestRoom(Vector2 p)
        {
            WorldArea best=null;float distance=float.MaxValue;int region=p.x>=world.Data.regionX?2:1;
            foreach(var r in world.Data.rooms){if(r.r!=region)continue;
                float dx=Mathf.Max(r.x-p.x,0,p.x-r.x-r.w),dy=Mathf.Max(r.y+p.y,0,-p.y-r.y-r.h);
                float d=dx*dx+dy*dy;if(d<distance){distance=d;best=r;}}
            return best;
        }
        bool Clear(int x,int y)=>x>=1&&x<world.Data.width-1&&y>=1&&y<world.Data.height-1&&world.Motor.Get(x,y)==0;
        void BuildDecorations()
        {
            var texture=Resources.Load<Texture2D>("Atmosphere/hanging-atlas-v1");var shader=Resources.Load<Shader>("Atmosphere/PaperWind");
            if(texture==null||shader==null)return;
            cloth=new Material(shader){hideFlags=HideFlags.DontSave};
            // Exact alpha bounds of the generated atlas, normalized to its 1254px original.
            var crops=new[]{new Rect(190,0,307,620),new Rect(740,0,452,627),new Rect(201,635,302,611),new Rect(821,630,316,615)};
            foreach(var c in crops){float scale=texture.width/1254f;var rect=new Rect(c.x*scale,(1254-c.y-c.height)*scale,c.width*scale,c.height*scale);
                sprites.Add(Sprite.Create(texture,rect,new Vector2(.5f,1),rect.height,0,SpriteMeshType.FullRect));}
            foreach(var room in world.Data.rooms){
                int zone=RegionalTilePalette.Zones.IndexOf(room.z);if(zone<0||room.w<7||room.h<5)continue;
                var random=new System.Random(room.n*613+room.r*8011);int count=room.w>24?2:1;
                for(int j=0;j<count;j++){
                    int x=room.x+Mathf.RoundToInt(room.w*(j==0?.22f:.74f))+(int)(random.NextDouble()*3)-1;
                    for(int y=room.y;y<Mathf.Min(room.y+room.h-3,room.y+18);y++){
                        if(world.Motor.Get(x,y-1)!=1||!Clear(x,y)||!Clear(x-1,y)||!Clear(x+1,y))continue;
                        int free=0;while(free<7&&Clear(x,y+free)&&Clear(x-1,y+free)&&Clear(x+1,y+free))free++;
                        if(free<4)continue;float height=Mathf.Min(free-1.5f,3.3f+(float)random.NextDouble()*2);
                        var anchor=new Vector2(x+.5f,-y+.12f);bool occupied=false;
                        foreach(var p in world.Data.pickups)if(Vector2.Distance(anchor+Vector2.down*height*.5f,new Vector2(p.tx+.5f,-p.floor+1))<3){occupied=true;break;}
                        if(occupied)continue;
                        var go=new GameObject("Hanging paper · "+room.z+" · "+room.n);go.transform.SetParent(transform,false);go.transform.position=new Vector3(anchor.x,anchor.y,3);
                        go.transform.localScale=new Vector3((random.Next(2)==0?-1:1)*height,height,1);
                        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprites[HangingArt[zone]];sr.sharedMaterial=cloth;sr.sortingOrder=-16;
                        sr.color=Color.Lerp(new Color(.43f,.42f,.39f),HazeColors[zone]*.7f,.2f);
                        decorations.Add(sr);zoneDecorationCounts[zone]++;break;
                    }
                }
            }
        }
        void LateUpdate()
        {
            if(world==null||air==null)return;
            var camera=world.worldCamera;var p=(Vector2)world.player.position;
            var r=NearestRoom(p);if(r==null)return;
            int zone=Mathf.Max(0,RegionalTilePalette.Zones.IndexOf(r.z));
            bool snap=!ready||Vector2.Distance(lastPlayer,p)>12||resetSerial!=world.ResetSerial;
            if(!world.MapOpen)Clock+=Mathf.Min(Time.deltaTime,.05f);
            float blend=snap?1:1-Mathf.Exp(-Time.deltaTime*1.8f);
            Zone=zone;haze=Color.Lerp(haze,HazeColors[zone],blend);rays=Mathf.Lerp(rays,Rays[zone],blend);
            roomBounds=Vector4.Lerp(roomBounds,new Vector4(r.x,-r.y-r.h,r.x+r.w,-r.y),blend);
            ready=true;lastPlayer=p;resetSerial=world.ResetSerial;
            var t=airDraw.transform;t.position=new Vector3(camera.transform.position.x,camera.transform.position.y,4);
            t.localScale=new Vector3(camera.orthographicSize*2*camera.aspect+.2f,camera.orthographicSize*2+.2f,1);
            airDraw.enabled=VisualEnabled;
            air.SetColor("_Haze",haze);air.SetVector("_Room",roomBounds);air.SetFloat("_Rays",rays);air.SetFloat("_Quality",GraphicsPreferences.TextureQuality==2?0:1);
            Shader.SetGlobalFloat(ClockId,Clock);Shader.SetGlobalFloat(StrengthId,VisualEnabled?1:0);
        }
        public void SetAtmospherePreview(string state)
        {
            VisualEnabled=state!="off";foreach(var d in decorations)d.enabled=VisualEnabled;
            var dust=GetComponent<WorldAmbientDust>();if(dust!=null)dust.SetPreviewVisible(VisualEnabled);
            Shader.SetGlobalFloat(StrengthId,VisualEnabled?1:0);
        }
        public void ReportAtmosphere(string unused)
        {
            int visible=0;float anchors=0;foreach(var d in decorations){if(d.enabled&&d.isVisible)visible++;anchors+=d.transform.position.x*.13f+d.transform.position.y*.07f;}
            Debug.Log("WORLD_ATMOSPHERE "+JsonUtility.ToJson(new Snapshot{zone=Zone,clock=Clock,decorations=decorations.Count,visibleDecorations=visible,zoneDecorations=zoneDecorationCounts,anchorChecksum=anchors,paused=world.MapOpen,enabled=VisualEnabled,lowQuality=GraphicsPreferences.TextureQuality==2,colliders=GetComponentsInChildren<Collider>().Length+GetComponentsInChildren<Collider2D>().Length}));
        }
        [System.Serializable] class Snapshot{public int zone,decorations,visibleDecorations,colliders;public int[] zoneDecorations;public float clock,anchorChecksum;public bool paused,enabled,lowQuality;}
        void OnDestroy(){Shader.SetGlobalFloat(StrengthId,0);foreach(var s in sprites)Destroy(s);if(air!=null)Destroy(air);if(cloth!=null)Destroy(cloth);if(mesh!=null)Destroy(mesh);}
    }
}
