using UnityEngine;
namespace Unfin
{
    public sealed class SaveShrineVisual : MonoBehaviour
    {
        public const float Height=2.15f;
        WorldPrototype world;SpriteRenderer body,glow;SpriteRenderer[] motes;
        WorldMarker point;float clock,pulse;bool wasActive;
        public bool Active {get;private set;}
        public SpriteRenderer InteractionBody=>body;
        public static Vector3 Position(WorldMarker p)=>new Vector3((p.tx+.5f),-p.floor+Height*.5f-.035f,0);
        public void Initialize(WorldPrototype w,WorldMarker p){
            world=w;point=p;body=GetComponent<SpriteRenderer>();body.sprite=(p.tx>=w.Data.regionX?Region2Presentation.DeviceFrame("save",0):null)??PaperPickupVisual.Load("save");body.color=Color.white;body.sortingOrder=PaperPickupVisual.BodySortingOrder;
            transform.position=Position(p);transform.localScale=Vector3.one*(Height/body.sprite.bounds.size.y);
            var placement=WorldPropPlacement.Find("save",new Vector2(p.x,p.y));if(placement!=null){transform.position=placement.position;transform.localScale=Vector3.one*(placement.height/body.sprite.bounds.size.y);}
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;tex.SetPixel(x,y,new Color(1,.88f,.65f,Mathf.Pow(Mathf.Clamp01(1-d),2)));}tex.Apply();
            var soft=Sprite.Create(tex,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);
            SpriteRenderer Make(string name){var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=soft;r.sortingOrder=PaperPickupVisual.EffectSortingOrder;return r;}
            glow=Make("Warm book light");glow.transform.localPosition=new Vector3(0,-body.sprite.bounds.size.y*.16f,0);glow.transform.localScale=new Vector3(1.1f,.65f,1);
            motes=new[]{Make("Book dust 1"),Make("Book dust 2"),Make("Book dust 3")};Tick(0);
        }
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt){
            if(world==null||world.MapOpen)return;clock+=dt;if(point.tx>=world.Data.regionX)StructureFrameBlend.Device(body,"save",clock*3);
            Active=Mathf.Abs(world.CheckpointPosition.x-(point.tx+.5f))<.6f&&Mathf.Abs(world.CheckpointPosition.y-(-point.floor+.625f))<.7f;
            if(Active&&!wasActive)pulse=.6f;wasActive=Active;pulse=Mathf.Max(0,pulse-dt);
            float warm=Active?1:.82f;body.color=new Color(warm,warm,warm,1);
            glow.color=new Color(1,.89f,.68f,(Active?.18f:.06f)+Mathf.Sin(clock*1.5f)*.02f+pulse*.12f);
            for(int i=0;i<motes.Length;i++){
                float life=Mathf.Repeat(clock*.35f+i*.33f,1);var r=motes[i];
                r.transform.localPosition=new Vector3(Mathf.Sin(clock*.6f+i*2.1f)*.4f,-.35f+life*.9f,0);r.transform.localScale=Vector3.one*.07f;
                r.color=new Color(1,.9f,.72f,Mathf.Sin(life*Mathf.PI)*(Active?.55f:.22f));
            }
        }
    }
}
