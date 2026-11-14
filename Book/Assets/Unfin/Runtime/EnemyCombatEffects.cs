using System;
using UnityEngine;
namespace Unfin
{
    // Decorative, pooled whole-sprite effects. Never creates colliders or damage receivers.
    [DefaultExecutionOrder(300)]
    public sealed class EnemyCombatEffects : MonoBehaviour
    {
        public const int Capacity=256;
        static EnemyCombatEffects instance;
        WorldPrototype world;int serial,cursor;
        readonly Particle[] particles=new Particle[Capacity];
        readonly Sprite[] marks=new Sprite[6];
        Sprite graphiteArc,graphiteGround;
        readonly System.Random random=new System.Random(4729);
        public int Emitted {get;private set;}
        public int ActiveCount {get{int n=0;foreach(var p in particles)if(p!=null&&p.life>0)n++;return n;}}
        public int PeakActive {get;private set;}
        public int Windups {get;private set;}public int Strikes {get;private set;}public int Trails {get;private set;}public int Deaths {get;private set;}public int Landings {get;private set;}
        sealed class Particle {public SpriteRenderer draw;public Vector2 velocity,size;public Color tint;public float age,life,duration,gravity,spin,growth;}
        public static EnemyCombatEffects Get(WorldPrototype w){if(w==null)return null;if(instance!=null&&instance.world==w)return instance;if(!Application.isPlaying)return null;var p=new GameObject("Enemy ink and paper effects (pooled)").AddComponent<EnemyCombatEffects>();p.Initialize(w);return p;}
        public void Initialize(WorldPrototype w){
            world=w;instance=this;serial=w.ResetSerial;
            string[] names={"dirt_01","dirt_02","smoke_01","smoke_04","dirt_03","trace_01"};
            for(int i=0;i<marks.Length;i++){var t=Resources.Load<Texture2D>("Effects/Kenney/"+names[i]);if(t==null)throw new Exception("Missing enemy effect texture "+names[i]);marks[i]=Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,t.width,0,SpriteMeshType.FullRect);}
            Sprite Glyph(string name){var t=Resources.Load<Texture2D>("BossRegion1/Effects/"+name);return Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,t.width,0,SpriteMeshType.FullRect);}
            graphiteArc=Glyph("00");graphiteGround=Glyph("11");
            for(int i=0;i<Capacity;i++){var go=new GameObject("Pooled enemy ink mark");go.transform.SetParent(transform,false);var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=26;sr.enabled=false;particles[i]=new Particle{draw=sr};}
        }
        float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        void Add(Sprite sprite,Vector2 at,Vector2 size,Vector2 velocity,Color color,float life,float spin=0,float gravity=0,float growth=.25f,int order=26,float angle=0,bool flip=false){
            if(sprite==null)return;var p=particles[cursor++%Capacity];p.draw.sprite=sprite;p.draw.enabled=true;p.draw.flipX=flip;p.draw.sortingOrder=order;p.draw.transform.SetPositionAndRotation(at,Quaternion.Euler(0,0,angle));p.velocity=velocity;p.size=size;p.tint=color;p.age=0;p.life=p.duration=life;p.spin=spin;p.gravity=gravity;p.growth=growth;Emitted++;Render(p);PeakActive=Mathf.Max(PeakActive,ActiveCount);
        }
        static Color Ink(Color c,float alpha)=>new Color(c.r*.22f,c.g*.22f,c.b*.25f,alpha);
        static Color Paper(Color c,float alpha)=>new Color(Mathf.Lerp(c.r,.86f,.72f),Mathf.Lerp(c.g,.81f,.72f),Mathf.Lerp(c.b,.68f,.72f),alpha);
        public void Windup(Bounds body,Color palette,int facing,bool boss){
            Windups++;float s=Mathf.Clamp(body.size.y,.6f,3);int count=boss?8:4;
            for(int i=0;i<count;i++){var offset=new Vector2(R(-.65f,.65f)*body.size.x,R(-.3f,.4f)*body.size.y);Add(marks[i%2],(Vector2)body.center+offset,Vector2.one*R(.07f,.13f)*s,-offset*1.5f,Paper(palette,.45f),R(.16f,.27f),R(-80,80),0,-.4f,18);}
            Add(marks[5],(Vector2)body.center+Vector2.right*facing*body.size.x*.22f,new Vector2(.28f,.1f)*s,Vector2.zero,Paper(palette,.26f),.18f,0,0,-.45f,18,facing*25);
        }
        public void Strike(Vector2 at,Vector2 radius,float angle,Color palette,int facing,bool boss,bool ground=false){
            Strikes++;float s=Mathf.Clamp(Mathf.Max(radius.x,radius.y),.35f,2.7f);
            // The painted core stays within the authoritative attack ellipse; small fading flecks are non-damaging.
            bool graphite=palette.b<.6f&&Mathf.Abs(palette.r-palette.g)<.07f;
            var art=graphite?(ground?graphiteGround:graphiteArc):ground?Region2Presentation.Fx(4):Region2Presentation.Fx(3);
            Add(art,at,radius*1.65f,Vector2.zero,graphite?new Color(1,1,1,boss?.6f:.48f):new Color(palette.r,palette.g,palette.b,boss?.62f:.48f),.15f,0,0,.1f,25,angle,facing<0);
            Add(marks[4],at,new Vector2(.6f,.35f)*s,Vector2.zero,Paper(palette,.52f),.07f,0,0,.55f,27,angle);
            for(int i=0;i<(boss?18:9);i++){float a=R(-1,1);var velocity=new Vector2(facing*R(.2f,1.7f),a*R(.6f,1.4f))*s;Add(marks[i%2],at+new Vector2(R(-.13f,.13f),R(-.12f,.12f))*s,new Vector2(R(.13f,.3f),R(.08f,.22f))*s,velocity,i%3==0?Ink(palette,.6f):Paper(palette,.78f),R(.2f,.4f),R(-220,220),1.4f,.1f,27);}
        }
        public void Hit(Bounds body,Color palette,int facing,bool boss,bool guarded=false){
            var at=(Vector2)body.center;float s=Mathf.Clamp(body.size.y,.65f,2.6f);
            for(int i=0;i<(guarded?4:boss?15:9);i++)Add(marks[i%2],at+new Vector2(R(-.12f,.12f),R(-.2f,.2f))*s,Vector2.one*R(.12f,.25f)*s,new Vector2(facing*R(.3f,1.6f),R(-.4f,1.2f))*s,Paper(palette,guarded?.45f:.74f),R(.22f,.43f),R(-240,240),1.2f);
            if(!guarded)Add(marks[4],at,Vector2.one*s*.55f,Vector2.zero,Ink(palette,.52f),.2f,0,0,.5f,18);
        }
        public void Trail(SpriteRenderer source,Bounds body,Color palette,int facing){
            if(source==null||source.sprite==null)return;Trails++;
            var size=source.sprite.bounds.size;var scale=source.transform.lossyScale;
            Add(source.sprite,source.transform.position,new Vector2(size.x*Mathf.Abs(scale.x),size.y*Mathf.Abs(scale.y)),new Vector2(-facing*.15f,0),new Color(palette.r*.45f,palette.g*.45f,palette.b*.48f,.18f),.14f,0,0,0,8,source.transform.eulerAngles.z,source.flipX);
            Add(marks[5],(Vector2)body.center-Vector2.right*facing*body.size.x*.35f,new Vector2(body.size.x*.7f,.1f),new Vector2(-facing*.4f,0),Paper(palette,.3f),.16f,0,0,.25f,18);
        }
        public void Landing(Vector2 feet,float width,Color palette,bool boss){
            Landings++;float s=Mathf.Clamp(width,.6f,4);
            Add(marks[4],feet+Vector2.up*.08f,new Vector2(s,.24f),Vector2.zero,Ink(palette,.52f),.34f,0,0,.6f,18);
            bool graphite=palette.b<.6f&&Mathf.Abs(palette.r-palette.g)<.07f;
            Add(graphite?graphiteGround:Region2Presentation.Fx(4),feet+Vector2.up*.14f,new Vector2(s,.32f),Vector2.zero,graphite?new Color(1,1,1,.5f):new Color(palette.r,palette.g,palette.b,.38f),.24f,0,0,.35f,19);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<(boss?9:5);i++)Add(marks[i%3==0?2:i%2],feet,new Vector2(R(.07f,.22f),R(.06f,.15f))*s,new Vector2(side*R(.5f,1.6f),R(.2f,.75f))*s,i%3==0?Ink(palette,.35f):Paper(palette,.65f),R(.3f,.55f),R(-190,190),2,.45f,23);
        }
        public void Death(Bounds body,Color palette,bool boss){
            Deaths++;float s=Mathf.Clamp(body.size.y,.6f,3);int count=boss?40:20;
            Add(marks[4],body.center,new Vector2(body.size.x,body.size.y)*.85f,Vector2.up*.15f,Ink(palette,.38f),.5f,0,0,.35f,18);
            for(int i=0;i<count;i++){var at=(Vector2)body.center+new Vector2(R(-.4f,.4f)*body.size.x,R(-.35f,.35f)*body.size.y);Add(marks[i%4==0?3:i%2],at,new Vector2(R(.12f,.3f),R(.08f,.22f))*s,new Vector2(R(-.75f,.75f),R(.1f,1.1f))*s,i%4==0?Ink(palette,.3f):Paper(palette,.74f),R(.45f,.9f),R(-190,190),.8f,.25f,i%4==0?18:26);}
        }
        public void Projectile(Vector2 at,Vector2 velocity,Color palette,bool impact){
            if(impact){for(int i=0;i<6;i++)Add(marks[i%2],at,Vector2.one*R(.05f,.12f),new Vector2(R(-1,1),R(-.5f,1.2f)),Paper(palette,.6f),R(.18f,.35f),R(-160,160),1);return;}
            Add(marks[5],at,new Vector2(.24f,.065f),-velocity*.08f,Paper(palette,.35f),.15f,0,0,-.2f,18,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
        }
        void Render(Particle p){float t=Mathf.Clamp01(p.age/p.duration),fade=(1-t)*(1-t);var c=p.tint;c.a*=fade;p.draw.color=c;var size=p.draw.sprite.bounds.size;float grow=1+t*p.growth;p.draw.transform.localScale=new Vector3(p.size.x/Mathf.Max(.001f,size.x)*grow,p.size.y/Mathf.Max(.001f,size.y)*grow,1);}
        void Update(){Tick(Mathf.Min(Time.deltaTime,.05f));}
        public void Tick(float dt){if(world==null)return;if(serial!=world.ResetSerial){serial=world.ResetSerial;Clear();}if(world.MapOpen)return;foreach(var p in particles){if(p==null||p.life<=0)continue;p.age+=dt;p.life-=dt;if(p.life<=0){p.draw.enabled=false;continue;}p.velocity+=Vector2.down*p.gravity*dt;p.draw.transform.position+=(Vector3)p.velocity*dt;p.draw.transform.Rotate(0,0,p.spin*dt);Render(p);}}
        public void Clear(){foreach(var p in particles)if(p!=null){p.life=0;p.draw.enabled=false;}}
        void OnDestroy(){foreach(var s in marks)if(s!=null){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}foreach(var s in new[]{graphiteArc,graphiteGround})if(s!=null){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}if(instance==this)instance=null;}
    }
}
