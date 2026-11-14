using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    // Painted effect sprites move as whole drawings; scenery stays anchored in the world.
    [DefaultExecutionOrder(200)]
    public sealed class Region2Presentation : MonoBehaviour
    {
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static Region2Presentation instance;
        WorldPrototype world;float ambient,bridgeFocus;int serial;
        class Fleck {public SpriteRenderer draw;public Vector2 velocity;public float age,life,size,spin,alpha;}
        readonly List<Fleck> pool=new List<Fleck>();
        public static Sprite Load(string path){if(cache.TryGetValue(path,out var s)&&s!=null)return s;var t=Resources.Load<Texture2D>("Region2/"+path);if(t==null)return null;s=Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,256,0,SpriteMeshType.FullRect);cache[path]=s;return s;}
        public static Sprite Fx(int i)=>Load("Effects/"+i.ToString("D2"));
        public static Sprite DeviceFrame(string name,int i)=>Load("Props/"+name+"/"+i.ToString("D2"));
        public static Sprite PickupFrame(string kind,int frame){string name=kind=="parry"||kind=="climb"?"ability":kind=="item_heal"?"healup":kind;return DeviceFrame(name,kind=="heart"||kind=="shard"||kind=="healup"||name=="ability"?0:frame%4);}
        public static Region2Presentation Create(WorldPrototype w){if(instance!=null&&instance.world==w)return instance;if(!Application.isPlaying)return null;var go=new GameObject("Crayon hills atmosphere");var p=go.AddComponent<Region2Presentation>();p.Initialize(w);return p;}
        public void Initialize(WorldPrototype w){world=w;instance=this;serial=w.ResetSerial;for(int i=0;i<64;i++){var go=new GameObject("Pooled wax-paper fleck");go.transform.SetParent(transform,false);var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=24;sr.enabled=false;pool.Add(new Fleck{draw=sr});}Region2GateVisual.Create(w);}
        public static void Burst(WorldPrototype w,Vector2 at,string kind,int facing=1){var p=Create(w);if(p==null)return;int effect=kind=="parry"?5:kind=="death"?6:kind=="item"?10:kind=="R201"||kind=="H01"||kind=="H02"||kind=="knight"?3:kind=="R203"||kind=="E03"?11:4;float size=kind=="knight"?2.6f:kind=="R201"||kind=="H01"?1.25f:.75f;p.Spawn(effect,at,new Vector2(facing*.2f,.1f),size,.28f,0,.6f);for(int i=0;i<5;i++)p.Spawn(kind=="death"?6:10,at,new Vector2(Random.Range(-.65f,.65f),Random.Range(.2f,.8f)),Random.Range(.06f,.13f),Random.Range(.3f,.6f),Random.Range(-70,70),.5f);}
        void Spawn(int art,Vector2 at,Vector2 velocity,float size,float life,float spin,float alpha){var f=pool.Find(x=>!x.draw.enabled);var s=Fx(art);if(f==null||s==null)return;f.draw.sprite=s;f.draw.enabled=true;f.draw.transform.position=at;f.draw.transform.rotation=Quaternion.identity;f.age=0;f.life=life;f.velocity=velocity;f.size=size;f.spin=spin;f.alpha=alpha;Render(f);}
        void Render(Fleck f){float t=f.age/f.life;f.draw.transform.localScale=Vector3.one*(f.size/Mathf.Max(f.draw.sprite.bounds.size.x,f.draw.sprite.bounds.size.y))*(1+t*.3f);f.draw.color=new Color(1,1,1,f.alpha*Mathf.Sin(Mathf.Clamp01(t+.08f)*Mathf.PI));}
        void Update(){if(world==null||world.MapOpen)return;float dt=Mathf.Min(Time.deltaTime,.05f);if(serial!=world.ResetSerial){serial=world.ResetSerial;foreach(var f in pool)f.draw.enabled=false;}foreach(var f in pool){if(!f.draw.enabled)continue;f.age+=dt;if(f.age>=f.life){f.draw.enabled=false;continue;}f.draw.transform.position+=(Vector3)f.velocity*dt;f.draw.transform.Rotate(0,0,f.spin*dt);Render(f);}if(world.player.position.x<world.Data.regionX)return;ambient-=dt;if(ambient<=0){ambient=.35f;var at=world.worldCamera.ViewportToWorldPoint(new Vector3(Random.Range(.08f,.92f),Random.Range(.15f,.8f),10));Spawn(7,at,new Vector2(.025f,.07f),Random.Range(.025f,.055f),2.4f,8,.2f);}}
        void LateUpdate(){if(world==null)return;var p=world.player.position;bool bridge=p.x>=455&&p.x<=462&&p.y>=-90&&p.y<=-82;bridgeFocus=Mathf.MoveTowards(bridgeFocus,bridge?1:0,Time.deltaTime*3);world.worldCamera.orthographicSize=Mathf.Lerp(world.Data.orthographicSize,6.5f,bridgeFocus);if(bridgeFocus>0){var c=world.worldCamera.transform.position;c.x=Mathf.Lerp(c.x,(p.x+470.5f)*.5f,bridgeFocus);world.worldCamera.transform.position=c;}}
        void OnDestroy(){if(instance==this)instance=null;}
    }
    public sealed class Region2GateVisual : MonoBehaviour
    {
        WorldPrototype world;SpriteRenderer body;float opened=-1;int serial;Vector2Int gate;
        public static void Create(WorldPrototype w){int l=w.Data.width,r=0,t=w.Data.height,b=0;for(int y=0;y<w.Data.height;y++)for(int x=w.Data.regionX;x<w.Data.width;x++)if(w.Motor.Get(x,y)==18){l=Mathf.Min(l,x);r=Mathf.Max(r,x);t=Mathf.Min(t,y);b=Mathf.Max(b,y);}if(r<l)return;var go=new GameObject("Tricolor wax-paper gate");var v=go.AddComponent<Region2GateVisual>();v.world=w;v.serial=w.ResetSerial;v.gate=new Vector2Int(l,t);v.body=go.AddComponent<SpriteRenderer>();v.body.sortingOrder=PaperPickupVisual.BodySortingOrder;v.body.sprite=Region2Presentation.DeviceFrame("gate",0);float h=b-t+1;go.transform.position=new Vector3((l+r+1)*.5f,-b-1+h*.5f,0);go.transform.localScale=Vector3.one*(h/v.body.sprite.bounds.size.y);for(int y=t;y<=b;y++)for(int x=l;x<=r;x++){w.maps[3].SetTileFlags(WorldData.Cell(x,y),UnityEngine.Tilemaps.TileFlags.None);w.maps[3].SetColor(WorldData.Cell(x,y),Color.clear);}}
        void Update(){if(world.MapOpen)return;if(serial!=world.ResetSerial){serial=world.ResetSerial;if(world.Motor.Get(gate.x,gate.y)==18)opened=-1;}if(world.Motor.Get(gate.x,gate.y)!=18&&opened<0){opened=0;Region2Presentation.Burst(world,transform.position,"item");}if(opened>=0)opened+=Mathf.Min(Time.deltaTime,.05f);StructureFrameBlend.Device(body,"gate",opened<0?0:opened<.18f?opened/.18f:opened<.4f?1+(opened-.18f)/.22f:2+(opened-.4f)/.16f,4,false);}
    }
}
