using UnityEngine;
namespace Unfin
{
    // Damage geometry stays active independently of the painted attack sprite.
    public sealed class BossDangerShape
    {
        public Vector2 Center,Radius;
        public float Angle;
        public bool Damaging {get;private set;}
        public bool Visible {get;private set;}
        readonly SpriteRenderer effect;
        readonly GameObject root;
        public BossDangerShape(Transform parent,Sprite art,int order=23)
        {
            root=new GameObject("Painted boss range");root.transform.SetParent(parent,false);
            effect=root.AddComponent<SpriteRenderer>();effect.sprite=art;effect.sortingOrder=order;
            Hide();
        }
        public void Show(Vector2 center,Vector2 radius,float angle,bool active,bool rim=true)
        {
            Center=center;Radius=radius;Angle=angle;Visible=true;Damaging=active;
            root.SetActive(true);root.transform.position=center;root.transform.rotation=Quaternion.Euler(0,0,angle);
            var size=effect.sprite.bounds.size;root.transform.localScale=Vector3.one;
            effect.transform.localScale=Vector3.one;
            root.transform.localScale=new Vector3(radius.x*2/size.x,radius.y*2/size.y,1);
            // Telegraph through the boss pose; show painted effects only during the attack.
            // Keep Visible/Damaging logical so collision and combat VFX retain their timing.
            effect.enabled=active;
            effect.color=new Color(.94f,.9f,.81f,.85f);
        }
        public void Hide(){Visible=Damaging=false;root.SetActive(false);}
        public void SetArt(Sprite art){effect.sprite=art;}
        public void Dispose(){if(root==null)return;if(Application.isPlaying)Object.Destroy(root);else Object.DestroyImmediate(root);}
        public bool Overlaps(Bounds bounds)=>Visible&&Damaging&&EllipseIntersects(Center,Radius,Angle,bounds);
        public static bool EllipseIntersects(Vector2 center,Vector2 radius,float angle,Bounds bounds)
        {
            if(radius.x<=0||radius.y<=0)return false;
            if(center.x>=bounds.min.x&&center.x<=bounds.max.x&&center.y>=bounds.min.y&&center.y<=bounds.max.y)return true;
            float a=-angle*Mathf.Deg2Rad,co=Mathf.Cos(a),si=Mathf.Sin(a);
            Vector2 Unit(float x,float y){var p=new Vector2(x,y)-center;return new Vector2((p.x*co-p.y*si)/radius.x,(p.x*si+p.y*co)/radius.y);}
            var corners=new[]{Unit(bounds.min.x,bounds.min.y),Unit(bounds.max.x,bounds.min.y),Unit(bounds.max.x,bounds.max.y),Unit(bounds.min.x,bounds.max.y)};
            for(int i=0;i<4;i++){var p=corners[i];var d=corners[(i+1)%4]-p;float t=d.sqrMagnitude>0?Mathf.Clamp01(-Vector2.Dot(p,d)/d.sqrMagnitude):0;if((p+d*t).sqrMagnitude<=1)return true;}
            return false;
        }
    }
}
