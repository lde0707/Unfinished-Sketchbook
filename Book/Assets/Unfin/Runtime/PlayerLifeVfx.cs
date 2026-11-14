using UnityEngine;
namespace Unfin
{
    // Authored paper/ink textures animated on the same clock as Death and Revive.
    // Fixed pool, ordinary alpha blending; no changes to the character drawings.
    [DefaultExecutionOrder(100)]
    public sealed class PlayerLifeVfx : MonoBehaviour
    {
        public const int Capacity=26;
        WorldPrototype world;
        readonly Sprite[] art=new Sprite[4];
        readonly SpriteRenderer[] draws=new SpriteRenderer[Capacity];
        int phase,serial;
        Vector2 anchor;
        public int Phase=>phase;
        public int ActiveCount {get{int n=0;foreach(var d in draws)if(d!=null&&d.enabled)n++;return n;}}
        public static PlayerLifeVfx Create(WorldPrototype owner)
        {
            var fx=owner.GetComponent<PlayerLifeVfx>();if(fx!=null)return fx;
            fx=owner.gameObject.AddComponent<PlayerLifeVfx>();fx.Initialize(owner);return fx;
        }
        void Initialize(WorldPrototype owner)
        {
            world=owner;serial=world.ResetSerial;
            var texture=Resources.Load<Texture2D>("PlayerLifeEffects/paper-ink-atlas-v1");
            if(texture==null){Debug.LogError("Missing player life effects atlas");enabled=false;return;}
            // Generated artwork has a taller upper row. Explicit rectangles preserve
            // the full wisps and generous alpha margins without cutting their tips.
            var rects=new[]{new Rect(0,534,627,720),new Rect(627,534,627,720),new Rect(0,0,627,534),new Rect(627,0,627,534)};
            for(int i=0;i<4;i++)art[i]=Sprite.Create(texture,rects[i],new Vector2(.5f,.5f),627,0,SpriteMeshType.FullRect);
            int order=owner.player.GetComponentInChildren<PaperSpritePlayer>().visual.sortingOrder;
            for(int i=0;i<Capacity;i++){
                var go=new GameObject("Life paper effect "+i);go.transform.SetParent(transform,false);
                draws[i]=go.AddComponent<SpriteRenderer>();draws[i].sortingOrder=order+(i<2?-1:1);draws[i].enabled=false;
            }
        }
        void LateUpdate(){Sample();}
        public void Sample()
        {
            if(world==null||draws[0]==null)return;
            int next=world.IsDrowning?1:world.IsReviving?2:0;
            if(next!=phase||serial!=world.ResetSerial){
                phase=next;serial=world.ResetSerial;
                var m=world.Motor;anchor=new Vector2((m.x+GridMotor.Width*.5f)/16,-m.Bottom/16+.5f);
            }
            foreach(var d in draws)d.enabled=false;
            if(phase==0)return;
            float t=world.LifeElapsed;
            if(phase==1)Death(t);else Revive(t);
        }
        void Draw(int index,int sprite,Vector2 offset,Vector2 size,float angle,float alpha)
        {
            if(alpha<.005f)return;
            var d=draws[index];d.enabled=true;d.sprite=art[sprite];
            d.color=new Color(1,1,1,Mathf.Clamp01(alpha*1.65f));
            d.transform.position=anchor+offset;d.transform.rotation=Quaternion.Euler(0,0,angle);
            d.transform.localScale=new Vector3(size.x,size.y,1);
        }
        static float Ease(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}
        void Death(float t)
        {
            float p=Mathf.Clamp01(t/PlayerLifeSprites.DeathDuration),fade=Ease(p/.14f)*(1-Ease((p-.55f)/.45f));
            Draw(0,0,Vector2.up*(p*.15f),Vector2.one*Mathf.Lerp(.65f,2.1f,Ease(p)),p*95,.65f*fade);
            Draw(1,0,new Vector2(.05f,-.17f),new Vector2(1.35f,.65f)*(1+p*.6f),-30-p*60,.3f*fade);
            for(int i=2;i<Capacity;i++){
                float a=(i*137.5f)*Mathf.Deg2Rad,u=Ease((p-(i%5)*.025f)/.85f),r=Mathf.Lerp(.08f,.65f+(i%4)*.19f,u);
                var offset=new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.55f+.24f*u);
                float size=(i%3==0?.24f:.16f)*(1-u*.45f);
                Draw(i,i%3==0?2:3,offset,Vector2.one*size,i*41+p*(i%2==0?170:-150),.8f*fade);
            }
        }
        void Revive(float t)
        {
            float rise=Ease((t-PlayerLifeSprites.ReviveHoldDuration)/.48f),end=1-Ease((t-1.35f)/.36f);
            float gather=Ease(t/1.38f),breathe=.5f+.5f*Mathf.Sin(t*4);
            Draw(0,1,new Vector2(0,-.12f+rise*.15f),new Vector2(1.85f-rise*.65f,1.35f-rise*.3f),-t*55,(.1f+breathe*.08f+rise*.25f)*end);
            Draw(1,1,new Vector2(0,-.38f),new Vector2(1.6f,.3f)*(1+rise*.2f),0,(.12f+rise*.2f)*end);
            for(int i=2;i<Capacity;i++){
                float a=i*137.5f*Mathf.Deg2Rad+t*(i%2==0?1.5f:1.1f),r=Mathf.Lerp(.8f+(i%4)*.15f,.08f,gather);
                var offset=new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.5f-.2f+rise*.2f);
                float size=(i%3==0?.22f:.16f)*(1-gather*.4f);
                float alpha=Ease((t-(i%4)*.06f)/.25f)*(.35f+rise*.4f)*end;
                Draw(i,i%3==0?2:3,offset,Vector2.one*size,i*37-t*100,alpha);
            }
        }
        void OnDestroy(){foreach(var s in art)if(s!=null){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}}
    }
    public sealed partial class WorldPrototype
    {
        public void ReportLifeEffects(){var fx=GetComponent<PlayerLifeVfx>();Debug.Log("LIFE_EFFECTS "+JsonUtility.ToJson(new LifeEffectsSnapshot{death=IsDrowning,revive=IsReviving,elapsed=LifeElapsed,phase=fx!=null?fx.Phase:0,particles=fx!=null?fx.ActiveCount:0}));}
        [System.Serializable] sealed class LifeEffectsSnapshot{public bool death,revive;public float elapsed;public int phase,particles;}
    }
}
