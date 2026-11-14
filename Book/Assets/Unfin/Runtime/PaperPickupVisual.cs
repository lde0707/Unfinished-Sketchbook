using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    // Animated presentation only: authored pickup positions and progression remain authoritative.
    public sealed class PaperPickupVisual : MonoBehaviour
    {
        // Above room scenery (-20 or lower), below terrain and platforms (1).
        public const int BodySortingOrder=-2,EffectSortingOrder=-1;
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static readonly List<PaperPickupVisual> live=new List<PaperPickupVisual>();
        static Material material;static Sprite fleck;
        MapMarker marker;WorldPrototype world;SpriteRenderer body;Transform art;
        readonly List<SpriteRenderer> motes=new List<SpriteRenderer>();
        Vector3 origin;float clock,phase,collectAge=-1,pulse,artHeight;bool station;
        float sealOpen=1;bool sealedAbility;
        public bool Consuming=>collectAge>=0;public float Pulse=>pulse;
        public SpriteRenderer InteractionBody=>body;
        public static bool Supports(string kind)=>kind=="station"||kind=="heart"||kind=="shard"||kind=="wj"||kind=="cut"||kind=="dash"||kind=="parry"||kind=="climb"||kind=="item_heal"||kind=="healup"||kind=="item_shield"||kind=="item_paint"||kind.StartsWith("sw_");
        public static Sprite Load(string name){
            if(sprites.TryGetValue(name,out var cached)&&cached!=null)return cached;
            var texture=Resources.Load<Texture2D>("PickupArt/"+name);if(texture==null)return null;
            var s=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),256);sprites[name]=s;return s;
        }
        public static void Attach(MapMarker m){if(!Supports(m.kind)||m.GetComponent<PaperPickupVisual>()!=null)return;m.gameObject.AddComponent<PaperPickupVisual>().Initialize(m,FindAnyObjectByType<WorldPrototype>());}
        public void Initialize(MapMarker m,WorldPrototype w){
            marker=m;world=w;station=m.kind=="station";phase=m.sourceIndex*1.73f;
            var s=PaintedHudArt.Pickup(m.kind)??(m.htmlPosition.x/16>=w.Data.regionX?Region2Presentation.PickupFrame(m.kind,0):null)??Load(station?"station":m.kind.StartsWith("sw_")?"pigment":m.kind);if(s==null)return;
            GetComponent<SpriteRenderer>().enabled=false;transform.localScale=Vector3.one;
            art=new GameObject("Paper artwork").transform;art.SetParent(transform,false);body=art.gameObject.AddComponent<SpriteRenderer>();body.sprite=s;body.sortingOrder=BodySortingOrder;
            if(material==null){var shader=Resources.Load<Shader>("PickupArt/PickupRim");if(shader!=null)material=new Material(shader);}
            if(material!=null)body.sharedMaterial=material;
            if(m.htmlPosition.x/16<w.Data.regionX){if(m.kind=="sw_r")body.color=new Color(1,.64f,.61f);if(m.kind=="sw_b")body.color=new Color(.55f,.73f,1);}
            float height=station?3.55f:m.kind=="healup"?.85f:.65f;art.localScale=Vector3.one*(height/s.bounds.size.y);
            // The ribbon extends below the book plinth; anchor the plinth to the floor.
            if(station&&w!=null){var p=System.Array.Find(w.Data.pickups,a=>a.type==m.kind&&Mathf.Abs(a.x-m.htmlPosition.x)<.1f&&Mathf.Abs(a.y-m.htmlPosition.y)<.1f);if(p!=null)transform.position=new Vector3((p.x+p.w*.5f)/16,-p.floor+height*.5f-.37f,0);}
            var placement=WorldPropPlacement.Find(m.kind,m.htmlPosition);if(placement!=null){height=placement.height;transform.position=placement.position;}
            artHeight=height;art.localScale=Vector3.one*(height/s.bounds.size.y);
            origin=transform.position;
            sealedAbility=m.kind=="wj";if(sealedAbility)sealOpen=w.Progression.AbilityPickupUnlocked(m.kind)?1:0;
            if(fleck==null){var tex=new Texture2D(16,16,TextureFormat.RGBA32,false);for(int y=0;y<16;y++)for(int x=0;x<16;x++){float d=Mathf.Abs(x-7.5f)/5+Mathf.Abs(y-7.5f)/7;tex.SetPixel(x,y,new Color(1,.92f,.74f,Mathf.Clamp01((1-d)*2)));}tex.Apply();fleck=Sprite.Create(tex,new Rect(0,0,16,16),new Vector2(.5f,.5f),16);}
            for(int i=0;i<(station?10:5);i++){var go=new GameObject("Paper mote");go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=fleck;r.sortingOrder=EffectSortingOrder;motes.Add(r);}
            live.Add(this);
            // Already collected items must stay absent on any later scene/visual initialization.
            if(!station&&w!=null&&w.Progression.CollectedAt(m.kind,m.htmlPosition)){collectAge=.5f;gameObject.SetActive(false);}
        }
        void OnDestroy(){live.Remove(this);}
        public static void PortalPulse(WorldPrototype w,WorldMarker p){foreach(var v in live)if(v.world==w&&v.station&&Vector2.Distance(v.marker.htmlPosition,new Vector2(p.x,p.y))<.1f)v.pulse=.7f;}
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt){
            if(body==null||world==null||world.MapOpen)return;clock+=dt;pulse=Mathf.Max(0,pulse-dt);
            if(sealedAbility){float target=world.Progression.AbilityPickupUnlocked(marker.kind)?1:0;if(target>sealOpen&&sealOpen==0)pulse=.7f;sealOpen=Mathf.MoveTowards(sealOpen,target,dt*1.7f);body.color=Color.Lerp(new Color(.43f,.48f,.52f),Color.white,sealOpen);}
            if(!station&&collectAge<0&&world.Progression.CollectedAt(marker.kind,marker.htmlPosition))collectAge=0;
            if(collectAge>=0){collectAge+=dt;if(collectAge>=.42f){gameObject.SetActive(false);return;}}
            float t=collectAge<0?0:Mathf.Clamp01(collectAge/.42f),burst=Mathf.Sin(t*Mathf.PI);
            if(marker.htmlPosition.x/16>=world.Data.regionX&&PaintedHudArt.Pickup(marker.kind)==null){
                string name=marker.kind=="parry"||marker.kind=="climb"?"ability":marker.kind=="item_heal"?"healup":marker.kind;
                bool single=name=="heart"||name=="shard"||name=="healup"||name=="ability";
                StructureFrameBlend.Device(body,name,single?0:collectAge<0?(marker.kind.StartsWith("sw_")?0:clock*3):t*3,single?1:4,collectAge<0);
            }
            art.localPosition=station?Vector3.zero:new Vector3(0,Mathf.Sin(clock*2+phase)*.035f,0);
            if(t>0)art.position=Vector3.Lerp(origin,world.player.position+Vector3.up*.25f,t*t)+Vector3.up*burst*.28f;
            float scale=station?1+Mathf.Sin(clock*1.3f)*.004f+Mathf.Sin(pulse/.7f*Mathf.PI)*.035f:1+burst*.16f-t*.85f;
            art.localRotation=Quaternion.Euler(0,0,station?Mathf.Sin(clock*1.4f)*.25f+Mathf.Sin(pulse*14)*pulse*1.5f:Mathf.Sin(clock*1.6f+phase)*1.5f);
            float h=artHeight;art.localScale=Vector3.one*(h/body.sprite.bounds.size.y*scale);
            Color color=body.color;color.a=1-t;body.color=color;
            for(int i=0;i<motes.Count;i++){
                float life=Mathf.Repeat(clock*(station?.3f:.4f)+i*.217f+phase,1),angle=i*2.4f+clock*.24f;
                float radius=(station?1.1f:.34f)*(1+burst*1.3f+pulse*.6f);
                var pos=new Vector3(Mathf.Cos(angle)*radius,(life-.5f)*(station?2.5f:.9f),0);
                if(sealedAbility&&sealOpen<1)pos=Vector3.Lerp(new Vector3(Mathf.Cos(angle)*.47f,Mathf.Sin(angle)*.47f,0),pos,sealOpen);
                if(t>0)pos=Vector3.Lerp(pos,world.player.position-origin,t*t);
                var r=motes[i];r.transform.localPosition=pos;r.transform.localRotation=Quaternion.Euler(0,0,i*47+clock*12);
                r.transform.localScale=Vector3.one*(station?.055f:.035f)*(1+burst*.8f);
                r.color=new Color(1,.9f,.73f,Mathf.Sin(life*Mathf.PI)*(.38f+burst*.4f+pulse*.3f)*(1-t));
            }
        }
    }
}
