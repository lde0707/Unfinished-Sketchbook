using UnityEngine;

namespace Unfin
{
    // Outer paper-white sweep. The authored small sweep remains its inner trail.
    // Whole character drawings and attack timing are untouched.
    public sealed class PaperAttackArc : MonoBehaviour
    {
        const int Segments = 40;
        Mesh mesh;
        MeshRenderer draw;
        readonly Vector3[] vertices = new Vector3[(Segments+1)*2];
        readonly Color[] colors = new Color[(Segments+1)*2];
        readonly Vector2[] uv = new Vector2[(Segments+1)*2];
        Material material;
        public bool Visible => draw != null && draw.enabled;

        public void Initialize(SpriteRenderer source)
        {
            mesh = new Mesh { name = "Paper attack outer arc" }; mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            draw = gameObject.AddComponent<MeshRenderer>();
            material = new Material(source.sharedMaterial) { name = "Paper attack arc" };
            material.mainTexture = Texture2D.whiteTexture; draw.sharedMaterial = material;
            draw.sortingLayerID = source.sortingLayerID; draw.sortingOrder = source.sortingOrder-1;
            draw.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; draw.receiveShadows = false;
            var triangles = new int[Segments*6];
            for(int i=0;i<Segments;i++)
            {
                int v=i*2,t=i*6; triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;
                triangles[t+3]=v+1;triangles[t+4]=v+3;triangles[t+5]=v+2;
            }
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=uv;draw.enabled=false;
        }
        public void Hide(){if(draw!=null)draw.enabled=false;}
        public void Show(Vector2 center, Vector2 radius, float start, float end, float opacity, float ppu)
        {
            for(int i=0;i<=Segments;i++)
            {
                float t=(float)i/Segments,angle=Mathf.Lerp(start,end,t)*Mathf.Deg2Rad;
                float taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.85f);
                // An asymmetric crescent with needle tips, rather than a uniform ring.
                float thickness=(.24f+.12f*t)*taper;
                var outside=new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y);
                var inside=outside*(1-thickness);
                Vector2 a=center+outside,b=center+inside;
                vertices[i*2]=new Vector3((a.x-384)/ppu,(560-a.y)/ppu,0);
                vertices[i*2+1]=new Vector3((b.x-384)/ppu,(560-b.y)/ppu,0);
                colors[i*2]=new Color(1,.96f,.86f,opacity);
                colors[i*2+1]=new Color(.96f,.88f,.72f,opacity*.72f);
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();draw.enabled=true;
        }
        void OnDestroy()
        {
            if(mesh!=null)Destroy(mesh);
            if(material!=null)Destroy(material);
        }
    }
}
