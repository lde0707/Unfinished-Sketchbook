using UnityEngine;
namespace Unfin
{
    // A separate rendering pass follows the live art, including frame changes
    // and collection alpha. It never writes to the artist's sprite/material.
    public sealed class InteractionProximity : MonoBehaviour
    {
        public const float Radius=2.6f;
        public static bool FeedbackEnabled=true;
        static Material material;
        WorldPrototype world;SpriteRenderer source,glow;MaterialPropertyBlock properties;
        float strength;StructureFrameBlend frameBlend;
        public float Strength=>strength;
        public SpriteRenderer Glow=>glow;
        public static Material Material{get{if(material==null){var shader=Resources.Load<Shader>("InteractionGlow");if(shader!=null)material=new Material(shader);}return material;}}
        public static bool Supports(string kind)=>kind=="save"||PaperPickupVisual.Supports(kind)||PaperDeviceVisual.Supports(kind);
        public static bool SpecialTile(int type)=>type==2||type==5||type==6||type==7||type==8||type==13||type==14||type==15||type==16||type==18;
        public static float Weight(float distance)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,Radius,distance));
        public static float Approach(float value,float target,float dt)=>Mathf.MoveTowards(value,target,Mathf.Max(0,dt)*(target>value?3.3f:2.3f));
        public static bool Visible(WorldPrototype w,Vector2 from,Vector2 to)
        {
            int steps=Mathf.CeilToInt(Vector2.Distance(from,to)*8);
            for(int i=1;i<steps;i++){var p=Vector2.Lerp(from,to,(float)i/steps);if(GridMotor.Wall(w.Motor.Get(Mathf.FloorToInt(p.x),Mathf.FloorToInt(-p.y))))return false;}return true;
        }
        public static float Near(WorldPrototype w,Bounds bounds)
        {
            var player=w.Motor.WorldBounds.center;player.z=bounds.center.z;
            var point=bounds.ClosestPoint(player);float distance=Vector2.Distance(player,point);
            return distance>=Radius||!Visible(w,player,point)?0:Weight(distance);
        }
        public static void Attach(MapMarker marker)
        {
            if(marker.enemy||!Supports(marker.kind)||marker.GetComponent<InteractionProximity>()!=null)return;
            var w=FindAnyObjectByType<WorldPrototype>();if(w==null)return;
            var pickup=marker.GetComponent<PaperPickupVisual>();var device=marker.GetComponent<PaperDeviceVisual>();var shrine=marker.GetComponent<SaveShrineVisual>();
            var body=pickup!=null?pickup.InteractionBody:device!=null?device.InteractionBody:shrine!=null?shrine.InteractionBody:marker.GetComponent<SpriteRenderer>();
            if(body==null||Material==null)return;marker.gameObject.AddComponent<InteractionProximity>().Initialize(w,body);
        }
        public void Initialize(WorldPrototype w,SpriteRenderer body)
        {
            world=w;source=body;properties=new MaterialPropertyBlock();
            var go=new GameObject("Nearby interaction glimmer");go.transform.SetParent(body.transform,false);
            glow=go.AddComponent<SpriteRenderer>();glow.sharedMaterial=Material;glow.enabled=false;
        }
        void LateUpdate(){Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(world==null||source==null||glow==null)return;
            bool alive=source.enabled&&source.gameObject.activeInHierarchy&&source.sprite!=null&&source.color.a>.005f;
            if(!world.MapOpen&&!StilldrawnTitle.Blocking)strength=Approach(strength,alive?Near(world,source.bounds):0,dt);
            glow.enabled=alive&&strength>.005f&&FeedbackEnabled;
            if(!glow.enabled)return;
            glow.sprite=source.sprite;glow.flipX=source.flipX;glow.flipY=source.flipY;glow.drawMode=source.drawMode;glow.size=source.size;
            glow.sortingLayerID=source.sortingLayerID;glow.sortingOrder=source.sortingOrder+1;
            glow.color=new Color(1,1,1,strength*source.color.a);
            var r=source.sprite.rect;var tex=source.sprite.texture;
            properties.SetVector("_SpriteRect",new Vector4(r.x/tex.width,r.y/tex.height,r.width/tex.width,r.height/tex.height));
            if(frameBlend==null)frameBlend=source.GetComponent<StructureFrameBlend>();
            if(frameBlend!=null&&frameBlend.Next!=null)frameBlend.CopyTo(properties);else properties.SetFloat("_FrameBlend",0);
            glow.SetPropertyBlock(properties);
        }
    }
}
