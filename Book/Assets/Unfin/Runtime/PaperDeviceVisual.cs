using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    public sealed class PaperDeviceVisual : MonoBehaviour
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();static Sprite fleck;static Material rim;
        MapMarker marker;WorldPrototype world;WorldMarker point;Transform art;SpriteRenderer body;SpriteRenderer[] motes;
        float height,clock,pulse,fall=-1,extinguish=-1;bool used,bridgeBefore;
        PaperFriendAnimation friend;
        public PaperFriendAnimation FriendAnimation=>friend;
        public float Pulse=>pulse;
        public SpriteRenderer InteractionBody=>body;
        public static bool Supports(string kind)=>kind=="lever"||kind=="target"||kind=="hatch"||kind=="sketch"||kind=="npc"||kind=="diary"||kind=="brazier"||kind=="bar";
        public static string ArtName(WorldMarker p)=>p.type=="sketch"?(p.optionsJson.Contains("sun")?"sun":"dog"):p.type;
        public static bool Grounded(WorldMarker p)=>p.type=="lever"||p.type=="target"||p.type=="npc"||p.type=="brazier"||p.type=="bar"||(p.type=="sketch"&&!p.optionsJson.Contains("sun"));
        public static float DefaultHeight(WorldMarker p){switch(ArtName(p)){case "lever":return 1.1f;case "target":return 1.25f;case "hatch":return .6f;case "dog":return 1.05f;case "sun":return 1.2f;case "npc":return 1.5f;case "diary":return .6f;case "brazier":return .9f;default:return 2.5f;}}
        public static Sprite Load(string name){if(cache.TryGetValue(name,out var s)&&s!=null)return s;var t=Resources.Load<Texture2D>("DeviceArt/"+name);if(t==null)return null;s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),256);cache[name]=s;return s;}
        public static void Attach(MapMarker m){if(!Supports(m.kind)||m.GetComponent<PaperDeviceVisual>()!=null)return;var w=FindAnyObjectByType<WorldPrototype>();if(w==null)return;var p=System.Array.Find(w.Data.pickups,a=>a.type==m.kind&&Vector2.Distance(new Vector2(a.x,a.y),m.htmlPosition)<.1f);if(p!=null)m.gameObject.AddComponent<PaperDeviceVisual>().Initialize(w,m,p);}
        public void Initialize(WorldPrototype w,MapMarker m,WorldMarker p){
            world=w;marker=m;point=p;height=DefaultHeight(p);transform.localScale=Vector3.one;
            transform.position=Grounded(p)?new Vector3((p.x+p.w*.5f)/16,-p.floor+height*.5f-.025f,0):w.Data.Center(p);
            var entry=WorldPropPlacement.Find(p.type,m.htmlPosition);if(entry!=null){transform.position=entry.position;height=entry.height;}
            GetComponent<SpriteRenderer>().enabled=false;art=new GameObject("Device artwork").transform;art.SetParent(transform,false);body=art.gameObject.AddComponent<SpriteRenderer>();body.sprite=(p.tx>=w.Data.regionX?Region2Presentation.DeviceFrame(ArtName(p),0):null)??Load(ArtName(p));body.sortingOrder=PaperPickupVisual.BodySortingOrder;art.localScale=Vector3.one*(height/body.sprite.bounds.size.y);
            if(rim==null)rim=new Material(Resources.Load<Shader>("PickupArt/PickupRim"));body.sharedMaterial=rim;
            if(fleck==null){var t=new Texture2D(16,16,TextureFormat.RGBA32,false);for(int y=0;y<16;y++)for(int x=0;x<16;x++)t.SetPixel(x,y,new Color(1,.9f,.7f,Mathf.Clamp01(1-Mathf.Abs(x-7.5f)/6-Mathf.Abs(y-7.5f)/7)));t.Apply();fleck=Sprite.Create(t,new Rect(0,0,16,16),new Vector2(.5f,.5f),16);}
            motes=new SpriteRenderer[p.type=="brazier"?5:3];for(int i=0;i<motes.Length;i++){var go=new GameObject("Quiet paper mote");go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=fleck;r.sortingOrder=PaperPickupVisual.EffectSortingOrder;motes[i]=r;}
            if(PaperFriendAnimation.Supports(p)&&!(p.type=="npc"&&p.tx>=w.Data.regionX))friend=new PaperFriendAnimation(w,p,transform,art,body,motes,height);
            if(p.type=="brazier"){var hit=art.gameObject.AddComponent<PaperAttackTarget>();hit.onHit.AddListener(()=>world.Progression.ExtinguishBrazier(point));}
            bridgeBefore=w.Motor.Get(459,88)==4;Tick(0);
        }
        void TickResident(){bool finished=world.Progression.DefeatedEncounters.Contains(91),social=world.Progression.SocialTarget==point;StructureFrameBlend.Device(body,"npc",!finished&&social?world.Progression.SocialProgress*3:clock*2,4,finished||!social,finished?8:social?4:0);body.color=Color.white;art.localScale=Vector3.one*(height/body.sprite.bounds.size.y);art.localPosition=Vector3.zero;art.localRotation=Quaternion.identity;for(int m=0;m<motes.Length;m++){float t=Mathf.Repeat(clock*.3f+m*.33f,1);motes[m].transform.localPosition=new Vector3(Mathf.Sin(m*2.4f+clock*.2f)*.25f,(t-.5f)*height);motes[m].transform.localScale=Vector3.one*.025f;motes[m].color=new Color(1,.87f,.65f,Mathf.Sin(t*Mathf.PI)*.2f);}}
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt){
            if(body==null||world.MapOpen)return;clock+=dt;pulse=Mathf.Max(0,pulse-dt);
            if(point.type=="npc"&&point.tx>=world.Data.regionX){TickResident();return;}
            if(friend!=null){friend.Tick(dt);return;}
            if(point.tx>=world.Data.regionX){
                if(point.type=="brazier"){
                    if(world.Progression.BrazierLit(point)){extinguish=-1;StructureFrameBlend.Device(body,"brazier",clock*6,3);}
                    else {if(extinguish<0)extinguish=0;extinguish+=dt;StructureFrameBlend.For(body).Set(Region2Presentation.DeviceFrame("brazier",2),Region2Presentation.DeviceFrame("brazier",3),extinguish/.16f);}
                }else if(point.type=="bar")body.sprite=Region2Presentation.DeviceFrame("bar",0);
                else StructureFrameBlend.Device(body,ArtName(point),clock*3);
            }
            bool leverUsed=marker.kind=="lever"&&world.Progression.CollectedAt(marker.kind,marker.htmlPosition);if(leverUsed&&!used){used=true;pulse=.55f;}
            if(marker.kind=="target"&&world.Progression.Parrying&&Vector2.Distance(world.player.position,transform.position)<6)pulse=.3f;
            bool bridge=world.Motor.Get(459,88)==4;if(marker.kind=="bar"&&bridge&&!bridgeBefore){fall=0;pulse=.4f;}if(marker.kind=="bar"&&!bridge&&bridgeBefore)fall=-1;bridgeBefore=bridge;
            if(fall>=0)fall+=dt;
            if(point.type=="diary"&&world.Progression.Collected(point)&&!used){used=true;pulse=.6f;}
            float alpha=point.type=="diary"&&used?Mathf.Clamp01(pulse/.3f):fall<0?1:Mathf.Clamp01(1-(fall-.25f)/.18f);float tilt=marker.kind=="bar"&&fall>=0?-Mathf.SmoothStep(0,85,fall/.35f):marker.kind=="sketch"?Mathf.Sin(clock*1.5f)*.6f:0;
            art.localRotation=Quaternion.Euler(0,0,tilt);art.localPosition=Vector3.up*((marker.kind=="sketch"&&ArtName(point)=="sun"||marker.kind=="hatch")?Mathf.Sin(clock*1.5f)*.025f:0);
            if(marker.kind=="bar"&&fall>=0){var pivot=Vector3.up*(height*.5f);art.localPosition=art.localRotation*pivot-pivot;}
            float breath=marker.kind=="npc"||marker.kind=="sketch"?1+Mathf.Sin(clock*1.6f)*.006f:1;art.localScale=Vector3.one*(height/body.sprite.bounds.size.y*breath);
            float warmth=marker.kind=="brazier"?.94f+Mathf.Sin(clock*8)*.025f:used?1:.94f;body.color=new Color(warmth,warmth,warmth,alpha);
            for(int i=0;i<motes.Length;i++){
                float life=Mathf.Repeat(clock*(marker.kind=="brazier"?.7f:.3f)+i*.317f,1);var r=motes[i];r.enabled=point.type!="brazier"||world.Progression.BrazierLit(point);float radius=marker.kind=="brazier"?.17f:.32f;
                r.transform.localPosition=new Vector3(Mathf.Sin(clock*.7f+i*2.3f)*radius,(life-.5f)*height*.9f,0);r.transform.localScale=Vector3.one*(marker.kind=="brazier"?.04f:.03f);
                r.color=new Color(1,marker.kind=="brazier"?.7f:.9f,marker.kind=="brazier"?.4f:.72f,Mathf.Sin(life*Mathf.PI)*(.28f+pulse*.4f)*alpha);
            }
        }
    }
}
