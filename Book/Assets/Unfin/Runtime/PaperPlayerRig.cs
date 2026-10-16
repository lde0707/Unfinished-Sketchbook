using UnityEngine;

namespace Unfin
{
    // Clips animate these continuous pose channels. Cloth and limbs deform inside
    // their meshes, so interpolation never swaps or repaints the original artwork.
    [ExecuteAlways, DefaultExecutionOrder(100)]
    public sealed class PaperPlayerRig : MonoBehaviour
    {
        public Material head, body, cape, arm, leg, collar;
        public float bob, lean, squash, frontStep, backStep, frontLift, backLift;
        public float armSwing, armReach, headTilt, cloth, clothWave, collapse;
        public float Facing = 1;
        sealed class Part
        {
            public GameObject go;
            public Mesh mesh;
            public Vector3[] vertices;
            public Vector2 size, pivot;
        }
        Part[] parts;
        const int Columns=12, Rows=18;

        void OnEnable() { Build(); RenderPose(); }
        void LateUpdate() { RenderPose(); }
        void OnDisable()
        {
            if (parts==null) return;
            foreach (var p in parts) if (p!=null) { Release(p.mesh); Release(p.go); }
            parts=null;
        }
        static void Release(Object o)
        {
            if (o==null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
        public void Rebuild() { OnDisable(); Build(); RenderPose(); }
        void Build()
        {
            if (parts!=null || head==null || body==null || cape==null || arm==null || leg==null) return;
            parts=new[] {
                Make("Back leg",leg,new Vector2(.16f,.29f),new Vector2(.5f,1),20),
                Make("Back arm",arm,new Vector2(.115f,.30f),new Vector2(.5f,1),21),
                Make("Flowing paper cloak",cape,new Vector2(.80f,.73f),new Vector2(.86f,.96f),22),
                Make("Front leg",leg,new Vector2(.16f,.29f),new Vector2(.5f,1),23),
                Make("Wraparound cloak",body,new Vector2(.52f,.69f),new Vector2(.5f,1),24),
                Make("Front arm",arm,new Vector2(.115f,.30f),new Vector2(.5f,1),25),
                Make("Head",head,new Vector2(.66f,.615f),new Vector2(.5f,.5f),27)
            };
        }
        Part Make(string name,Material mat,Vector2 size,Vector2 pivot,int order)
        {
            var p=new Part {size=size,pivot=pivot,go=new GameObject(name)};
            p.go.hideFlags=HideFlags.HideAndDontSave;
            p.go.transform.SetParent(transform,false);
            p.vertices=new Vector3[(Columns+1)*(Rows+1)];
            var uv=new Vector2[p.vertices.Length]; var triangles=new int[Columns*Rows*6];
            for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++) uv[y*(Columns+1)+x]=new Vector2((float)x/Columns,(float)y/Rows);
            int t=0;
            for(int y=0;y<Rows;y++) for(int x=0;x<Columns;x++) {
                int i=y*(Columns+1)+x;
                triangles[t++]=i; triangles[t++]=i+Columns+1; triangles[t++]=i+1;
                triangles[t++]=i+1; triangles[t++]=i+Columns+1; triangles[t++]=i+Columns+2;
            }
            p.mesh=new Mesh {name=name+" deformable grid",hideFlags=HideFlags.HideAndDontSave};
            p.mesh.MarkDynamic(); p.mesh.vertices=p.vertices; p.mesh.uv=uv; p.mesh.triangles=triangles;
            p.go.AddComponent<MeshFilter>().sharedMesh=p.mesh;
            var renderer=p.go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=mat; renderer.sortingOrder=order;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
            return p;
        }
        public void RenderPose()
        {
            Build(); if(parts==null) return;
            float sy=1-squash, sx=1+squash*.45f;
            float hip=.29f+bob*.3f;
            Deform(parts[0],new Vector2(-.10f,hip),0,backStep,backLift,0,false);
            Deform(parts[1],new Vector2(-.13f,.80f+bob),-armSwing*.65f,0,0,0,true);
            Deform(parts[2],new Vector2(-.025f,.90f+bob),lean*.3f,cloth,clothWave,clothWave,true);
            Deform(parts[3],new Vector2(.09f,hip),0,frontStep,frontLift,0,false);
            Deform(parts[4],new Vector2(0,.91f+bob),lean,cloth*.18f,clothWave*.08f,clothWave*.1f,true);
            Deform(parts[5],new Vector2(.14f,.78f+bob),armSwing,0,0,0,true);
            for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++) {
                float w=1-(float)y/Rows;
                parts[5].vertices[y*(Columns+1)+x].x+=armReach*w*w;
            }
            Deform(parts[6],new Vector2(.035f+lean*.0014f,1.065f+bob*.8f),headTilt,0,0,0,true);
            // Squash remains visual; the existing 12 x 22 logical collider is untouched.
            foreach(var p in parts) {
                var v=p.vertices;
                float angle=collapse*Mathf.Deg2Rad, cs=Mathf.Cos(angle),sn=Mathf.Sin(angle);
                for(int i=0;i<v.Length;i++) {
                    var q=v[i]; q.x*=sx; q.y*=sy;
                    q=new Vector3(q.x*cs-q.y*sn,q.x*sn+q.y*cs,0);
                    q.x*=Facing<0?-1:1; v[i]=q;
                }
                p.mesh.vertices=v; p.mesh.RecalculateBounds();
            }
        }
        static void Deform(Part p,Vector2 position,float angle,float bend,float lift,float wave,bool curved)
        {
            float a=angle*Mathf.Deg2Rad,cs=Mathf.Cos(a),sn=Mathf.Sin(a);
            for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++) {
                float u=(float)x/Columns,v=(float)y/Rows, w=1-v;
                float px=(u-p.pivot.x)*p.size.x,py=(v-p.pivot.y)*p.size.y;
                // Distal parts follow the root smoothly instead of rotating as a card.
                px+=bend*(curved?w*w:w);
                py+=lift*w+wave*Mathf.Sin(w*Mathf.PI)*Mathf.Sin(u*Mathf.PI);
                p.vertices[y*(Columns+1)+x]=new Vector3(position.x+px*cs-py*sn,position.y+px*sn+py*cs,0);
            }
        }
    }
}
