using System;
using UnityEngine;
namespace Unfin
{
    // All motion returns to the same pose after 24 seconds, including particle fades.
    // Unscaled time keeps the title alive while the actual game is paused.
    public sealed class StilldrawnTitleMotion : IDisposable
    {
        public const float LoopSeconds=24;
        public const int PaperCount=18;
        readonly Texture2D light,paper,mist;
        Material paintingMaterial;
        RenderTexture paintingFrame;
        public struct PaperPose {public Vector2 position,size;public float rotation,alpha;}
        public StilldrawnTitleMotion()
        {
            light=MakeTexture("Title soft light",64,(x,y)=>{
                float r=x*x+y*y;return new Color(1,1,1,Mathf.Exp(-r*4)*Mathf.Clamp01((1-r)*6));
            });
            paper=MakeTexture("Title paper fleck",32,(x,y)=>{
                float center=.12f*Mathf.Sin(x*Mathf.PI),half=.24f*(1-x*x)+.055f;
                float alpha=Mathf.Clamp01((half-Mathf.Abs(y-center))*18)*Mathf.Clamp01((1-Mathf.Abs(x))*16);
                float fold=.78f+.22f*Mathf.Clamp01((y-center)/half+.5f);return new Color(.92f*fold,.86f*fold,.72f*fold,alpha);
            });
            mist=Resources.Load<Texture2D>("Effects/Kenney/smoke_04");
        }
        static Texture2D MakeTexture(string name,int size,Func<float,float,Color> pixel)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=pixel((x+.5f)/size*2-1,(y+.5f)/size*2-1);
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        static float Angle(float seconds)=>Mathf.Repeat(seconds,LoopSeconds)*(2*Mathf.PI/LoopSeconds);
        public RenderTexture RenderPainting(Texture2D background,float seconds)
        {
            if(background==null)return null;
            if(paintingMaterial==null){
                var shader=Resources.Load<Shader>("Title/StilldrawnLivingBook");
                if(shader==null||!shader.isSupported)return null;
                paintingMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            }
            if(paintingFrame==null||paintingFrame.width!=background.width||paintingFrame.height!=background.height){
                if(paintingFrame!=null){paintingFrame.Release();Release(paintingFrame);}
                paintingFrame=new RenderTexture(background.width,background.height,0,RenderTextureFormat.ARGB32){name="Living title painting",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                paintingFrame.Create();
            }
            paintingMaterial.SetFloat("_Phase",Angle(seconds));
            var previous=RenderTexture.active;
            Graphics.Blit(background,paintingFrame,paintingMaterial);
            RenderTexture.active=previous;
            return paintingFrame;
        }
        public static Vector2 BackgroundOffset(float seconds){float a=Angle(seconds);return new Vector2(Mathf.Sin(a)*5,Mathf.Sin(a*2)*2.5f);}
        public static PaperPose PaperAt(float seconds,int index)
        {
            float phase=Mathf.Repeat(seconds,LoopSeconds)/LoopSeconds,a=phase*2*Mathf.PI;
            float u=Mathf.Repeat(phase*(1+index%3)+index*.13719f,1),seed=index*2.39996f;
            float fade=Mathf.Pow(Mathf.Sin(Mathf.PI*u),2);
            float spread=Mathf.Sin(seed)*210;
            return new PaperPose{
                position=new Vector2(1358+u*spread+Mathf.Sin(a*2+seed)*(12+u*32),715-u*(340+index%5*43)),
                size=new Vector2((10+index%4*4)*(.22f+.78f*Mathf.Abs(Mathf.Cos(a*(2+index%2)+seed))),14+index%3*5),
                rotation=-25+Mathf.Sin(a*2+seed)*35+u*60,
                alpha=fade*(.19f+index%4*.045f)
            };
        }
        void Texture(Texture2D texture,Rect rect,Color tint,float rotation=0)
        {
            if(texture==null)return;var old=GUI.matrix;var color=GUI.color;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(rect.center.x,rect.center.y,0),Quaternion.Euler(0,0,rotation),Vector3.one);
            GUI.color=tint;GUI.DrawTexture(new Rect(-rect.width*.5f,-rect.height*.5f,rect.width,rect.height),texture,ScaleMode.StretchToFill,true);GUI.color=color;GUI.matrix=old;
        }
        public void Draw(Texture2D background,float seconds,bool reduced)
        {
            if(reduced){if(background!=null)GUI.DrawTexture(new Rect(0,0,1920,1080),background,ScaleMode.ScaleAndCrop);return;}
            float a=Angle(seconds);
            if(background!=null){
                if(Event.current.type==EventType.Repaint)RenderPainting(background,seconds);
                GUI.DrawTexture(new Rect(0,0,1920,1080),paintingFrame!=null?(UnityEngine.Texture)paintingFrame:background,ScaleMode.ScaleAndCrop);
            }
            // Very low contrast fog across distant rock layers; never a foreground veil.
            for(int i=0;i<3;i++){
                float p=a+i*2.1f,x=280+i*510+Mathf.Sin(p)*100,y=720+i%2*120+Mathf.Cos(p)*16;
                Texture(mist,new Rect(x-550,y-115,1100,230),new Color(.32f,.43f,.48f,.025f+.012f*Mathf.Sin(p)));
            }
            // The book's existing light breathes, rather than flashing over the screen.
            Texture(light,new Rect(1210,260,300,650),new Color(.56f,.78f,.73f,.055f+.025f*Mathf.Sin(a*2)),-9);
            Texture(light,new Rect(1070,600,600,210),new Color(.65f,.77f,.67f,.07f+.025f*Mathf.Sin(a*2+.4f)));
            for(int i=0;i<PaperCount;i++){
                var p=PaperAt(seconds,i);Texture(paper,new Rect(p.position-p.size*.5f,p.size),new Color(1,1,1,p.alpha),p.rotation);
            }
            // Small motes vanish before wrapping; all periods divide the full loop.
            for(int i=0;i<40;i++){
                float u=Mathf.Repeat(seconds/LoopSeconds*(1+i%3)+i*.07193f,1);
                float x=800+Mathf.Repeat(i*83.71f,1050)+Mathf.Sin(a+i)*14,y=940-u*820;
                float alpha=Mathf.Pow(Mathf.Sin(u*Mathf.PI),2)*(.1f+i%3*.04f);
                Texture(light,new Rect(x,y,5+i%3*2,5+i%3*2),new Color(.82f,.87f,.75f,alpha));
            }
        }
        public void Dispose(){if(paintingFrame!=null){paintingFrame.Release();Release(paintingFrame);}Release(paintingMaterial);Release(light);Release(paper);}
        static void Release(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
