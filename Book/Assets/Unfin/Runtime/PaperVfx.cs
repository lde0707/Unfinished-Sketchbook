using UnityEngine;
namespace Unfin
{
    // CC0 Kenney textures, normal alpha blending: never additive or emissive.
    public sealed class PaperVfx : MonoBehaviour
    {
        public const int Capacity=128;
        static PaperVfx instance;
        readonly Sprite[] sprites=new Sprite[6];
        readonly Particle[] particles=new Particle[Capacity];
        readonly System.Random random=new System.Random(712);
        int cursor,serial;
        WorldPrototype world;
        public int ActiveCount {get {int n=0;foreach(var p in particles)if(p!=null&&p.life>0)n++;return n;}}
        sealed class Particle {public SpriteRenderer draw;public Vector2 velocity,scale;public float life,duration,spin,growth;public Color color;}
        static readonly string[] Names={"dirt_01","dirt_02","smoke_01","smoke_04","dirt_03","trace_01"};
        public static PaperVfx Get()
        {
            if(!Application.isPlaying)return null;
            if(instance==null)instance=new GameObject("Paper action effects (CC0)").AddComponent<PaperVfx>();
            return instance;
        }
        void Awake()
        {
            instance=this;world=FindFirstObjectByType<WorldPrototype>();serial=world!=null?world.ResetSerial:0;
            for(int i=0;i<Names.Length;i++){
                var texture=Resources.Load<Texture2D>("Effects/Kenney/"+Names[i]);
                if(texture!=null)sprites[i]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width);
            }
            for(int i=0;i<Capacity;i++){
                var go=new GameObject("Pooled paper particle");go.transform.SetParent(transform,false);
                var draw=go.AddComponent<SpriteRenderer>();draw.sortingOrder=25;draw.enabled=false;
                particles[i]=new Particle{draw=draw};
            }
        }
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        void Add(int sprite,Vector2 position,Vector2 velocity,Vector2 scale,float duration,float alpha,float rotation=0)
        {
            var p=particles[cursor++%Capacity];p.draw.sprite=sprites[sprite];p.draw.enabled=true;
            p.draw.transform.position=position;p.draw.transform.rotation=Quaternion.Euler(0,0,rotation);
            p.velocity=velocity;p.scale=scale;p.life=p.duration=duration;p.spin=Range(-60,60);p.growth=sprite<4?.8f:.1f;
            p.color=new Color(.64f,.60f,.52f,alpha);p.draw.color=p.color;p.draw.transform.localScale=new Vector3(scale.x,scale.y,1);
        }
        public void Dust(Vector2 feet,float strength,int direction=0)
        {
            int count=strength<.5f?2:5;
            for(int i=0;i<count;i++)Add(i%2+2,feet+new Vector2(Range(-.12f,.12f),.09f),new Vector2(Range(-1,1)*strength-direction*.5f,Range(.15f,.55f)),new Vector2(.4f,.23f)*Mathf.Lerp(.7f,1.7f,strength),Range(.22f,.4f),.28f);
            Add(0,feet+Vector2.up*.12f,new Vector2(-direction*.3f,.25f),new Vector2(.55f,.35f)*Mathf.Max(.4f,strength),.24f,.38f);
        }
        public void Hit(Vector2 point,int facing)
        {
            Add(4,point,Vector2.zero,new Vector2(.65f,.5f),.12f,.48f,Range(0,180));
            for(int i=0;i<5;i++)Add(i%2,point,new Vector2(facing*Range(.4f,2),Range(-.8f,1.2f)),Vector2.one*Range(.24f,.48f),Range(.18f,.3f),.52f,Range(0,360));
        }
        public void Swing(Vector2 point,int facing)
        {
            Add(5,point,new Vector2(facing*1.5f,0),new Vector2(.85f,.35f),.12f,.38f,facing<0?90:-90);
            Add(1,point,new Vector2(facing*.8f,.15f),new Vector2(.55f,.45f),.18f,.35f);
        }
        public void Death(Bounds body)
        {
            // Muted paper dust and fragments, sharing our existing CC0 textures.
            // Spread across the body as its final drawing fades, without a flash.
            for(int i=0;i<12;i++){
                var point=(Vector2)body.center+new Vector2(Range(-.4f,.4f)*body.size.x,Range(-.4f,.4f)*body.size.y);
                Add(i%3==0?3:i%2,point,new Vector2(Range(-.45f,.45f),Range(.15f,.65f)),Vector2.one*Range(.18f,.4f),Range(.55f,.95f),.32f,Range(0,360));
            }
        }
        public void Dash(Vector2 point,int facing)
        {
            for(int i=0;i<3;i++)Add(3,point+Vector2.left*facing*i*.2f,new Vector2(-facing*1.4f,.1f),new Vector2(.65f,.32f),.25f,.26f);
        }
        void Update()
        {
            if(world!=null&&serial!=world.ResetSerial){serial=world.ResetSerial;Clear();}
            if(world!=null&&world.MapOpen)return;
            Tick(Mathf.Min(Time.deltaTime,.1f));
        }
        public void Tick(float dt)
        {
            foreach(var p in particles){if(p.life<=0)continue;p.life-=dt;if(p.life<=0){p.draw.enabled=false;continue;}
                float age=1-p.life/p.duration;var t=p.draw.transform;t.position+=(Vector3)(p.velocity*dt);t.Rotate(0,0,p.spin*dt);
                t.localScale=new Vector3(p.scale.x*(1+age*p.growth),p.scale.y*(1+age*p.growth),1);
                var color=p.color;color.a*=1-age*age;p.draw.color=color;
            }
        }
        public void Clear(){foreach(var p in particles){p.life=0;p.draw.enabled=false;}}
        void OnDestroy(){foreach(var sprite in sprites)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}if(instance==this)instance=null;}
    }
}
