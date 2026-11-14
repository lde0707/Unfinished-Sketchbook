using UnityEngine;
namespace Unfin
{
    // Sparse, world-anchored motes. The camera only determines where to recycle
    // the fixed pool; it never drags existing particles along with the player.
    [DefaultExecutionOrder(300)]
    public sealed class WorldAmbientDust : MonoBehaviour
    {
        public const int Capacity=44;
        sealed class Mote {public SpriteRenderer draw;public Vector2 velocity;public float age,life,alpha,phase,spin;public Color color;}
        readonly Mote[] motes=new Mote[Capacity];
        readonly System.Random random=new System.Random(261026);
        WorldPrototype world;WorldAtmosphere atmosphere;Sprite sprite;int serial;bool previewVisible=true;
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        public static void Create(WorldPrototype w)
        {
            if(w.GetComponent<WorldAmbientDust>()!=null)return;
            w.gameObject.AddComponent<WorldAmbientDust>().Initialize(w);
        }
        void Initialize(WorldPrototype w)
        {
            world=w;serial=w.ResetSerial;atmosphere=w.GetComponent<WorldAtmosphere>();
            var texture=Resources.Load<Texture2D>("Effects/Kenney/smoke_01");
            if(texture==null){enabled=false;return;}
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,texture.width);
            for(int i=0;i<Capacity;i++){
                var go=new GameObject("Ambient mote");go.transform.SetParent(transform,false);
                var draw=go.AddComponent<SpriteRenderer>();draw.sprite=sprite;draw.sortingOrder=0;draw.enabled=false;
                draw.sortingOrder=i%4==0?0:-14;
                motes[i]=new Mote{draw=draw};
            }
        }
        bool Open(Vector2 p){int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(-p.y);return x>=0&&y>=0&&x<world.Data.width&&y<world.Data.height&&world.Motor.Get(x,y)==0;}
        void Spawn(Mote p,Rect view)
        {
            p.draw.enabled=false;p.life=0;
            for(int attempt=0;attempt<24;attempt++){
                var position=new Vector2(Range(view.xMin,view.xMax),Range(view.yMin,view.yMax));
                if(!Open(position))continue;
                p.draw.transform.position=position;p.draw.transform.rotation=Quaternion.Euler(0,0,Range(0,360));
                bool near=p.draw.sortingOrder==0;int zone=atmosphere==null?0:atmosphere.Zone;
                float size=near?Range(.075f,.13f):Range(.035f,.075f);p.draw.transform.localScale=new Vector3(size,size*Range(.55f,.9f),1);
                bool warm=zone==4||zone==5;
                p.velocity=new Vector2(Range(.015f,.055f)*(zone%2==0?1:-1),warm?Range(.035f,.08f):Range(-.028f,.018f));
                p.age=0;p.life=Range(19,32);p.alpha=near?Range(.22f,.32f):Range(.13f,.23f);p.phase=Range(0,6.28f);p.spin=Range(-9,9);
                p.color=atmosphere==null?new Color(.74f,.70f,.61f):atmosphere.DustColor;
                p.draw.color=new Color(p.color.r,p.color.g,p.color.b,0);p.draw.enabled=true;return;
            }
        }
        void LateUpdate()
        {
            if(world==null||sprite==null||world.MapOpen||!previewVisible)return;
            var camera=world.worldCamera;float height=camera.orthographicSize*2,width=height*camera.aspect;
            var center=(Vector2)camera.transform.position;var view=new Rect(center.x-width*.5f-.5f,center.y-height*.5f-.5f,width+1,height+1);
            if(serial!=world.ResetSerial){serial=world.ResetSerial;foreach(var p in motes){p.life=0;p.draw.enabled=false;}}
            float dt=Mathf.Min(Time.deltaTime,.05f);
            int budget=GraphicsPreferences.TextureQuality==2?18:Capacity;
            for(int i=0;i<Capacity;i++){
                var p=motes[i];if(i>=budget){p.draw.enabled=false;p.life=0;continue;}
                if(p.life<=0||p.age>=p.life||!view.Contains(p.draw.transform.position)){Spawn(p,view);continue;}
                p.age+=dt;var drift=p.velocity+new Vector2(Mathf.Sin(p.age*.45f+p.phase)*.018f,0);
                p.draw.transform.position+=(Vector3)(drift*dt);
                p.draw.transform.Rotate(0,0,p.spin*dt);
                if(!Open(p.draw.transform.position)){p.life=0;p.draw.enabled=false;continue;}
                float fade=Mathf.SmoothStep(0,1,Mathf.Min(p.age/2.5f,(p.life-p.age)/3f));
                p.draw.color=new Color(p.color.r,p.color.g,p.color.b,p.alpha*fade);
            }
        }
        public void SetPreviewVisible(bool value){previewVisible=value;foreach(var p in motes)if(p!=null){p.life=0;p.draw.enabled=false;}}
        public void ReportAmbientDust(string unused)
        {
            int active=0;float alpha=0;foreach(var p in motes)if(p!=null&&p.draw.enabled){active++;alpha=Mathf.Max(alpha,p.draw.color.a);}
            Debug.Log("AMBIENT_DUST active="+active+" capacity="+Capacity+" maxAlpha="+alpha.ToString("F3")+" paused="+(world!=null&&world.MapOpen));
        }
        void OnDestroy(){if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}}
    }
}
