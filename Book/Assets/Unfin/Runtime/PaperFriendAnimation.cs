using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    // Whole, individually drawn poses; no skeletal deformation.
    public sealed class PaperFriendAnimation
    {
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        readonly WorldPrototype world;readonly WorldMarker point;readonly Transform root,art;readonly SpriteRenderer body;readonly SpriteRenderer[] motes;readonly Sprite[] frames;readonly float height;
        float clock,depart=-1;public string StateName {get;private set;}="Idle";public int FrameIndex {get;private set;}
        public static bool Supports(WorldMarker p)=>p.type=="sketch"||p.type=="npc";
        public static Sprite[] Load(string name){if(cache.TryGetValue(name,out var a)&&a.Length>0&&a[0]!=null)return a;a=new Sprite[12];for(int i=0;i<12;i++){var t=Resources.Load<Texture2D>("FriendAnimations/"+name+"/"+i.ToString("D2"));if(t==null)throw new System.InvalidOperationException("Missing friend frame "+name+" "+i);a[i]=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),256,0,SpriteMeshType.FullRect);}cache[name]=a;return a;}
        public PaperFriendAnimation(WorldPrototype w,WorldMarker p,Transform parent,Transform picture,SpriteRenderer renderer,SpriteRenderer[] particles,float h){world=w;point=p;root=parent;art=picture;body=renderer;motes=particles;height=h;frames=Load(PaperDeviceVisual.ArtName(p));if(w.Progression.Collected(p)){StateName="Gone";root.gameObject.SetActive(false);}else Tick(0);}
        public void Tick(float dt){
            if(StateName=="Gone"||world.MapOpen)return;clock+=dt;float alpha=1,progress=0;
            if(world.Progression.Collected(point)){
                if(depart<0)depart=0;depart+=dt;StateName="Depart";
                FrameIndex=depart<.14f?8:depart<.28f?9:depart<.46f?10:11;progress=Mathf.Clamp01(depart/.72f);alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.72f,depart));
                if(depart>=.72f){StateName="Gone";root.gameObject.SetActive(false);return;}
            }else if(world.Progression.SocialTarget==point&&world.Progression.SocialProgress>0){StateName="Interact";progress=world.Progression.SocialProgress;FrameIndex=4+Mathf.Min(3,Mathf.FloorToInt(progress*4));}
            else {StateName="Idle";float t=Mathf.Repeat(clock,1.62f);FrameIndex=t<.52f?0:t<.94f?1:t<1.10f?2:3;}
            int next=FrameIndex;float blend=0;
            if(StateName=="Idle"){float t=Mathf.Repeat(clock,1.62f);float start=FrameIndex==0?0:FrameIndex==1?.52f:FrameIndex==2?.94f:1.10f;float duration=FrameIndex==0?.52f:FrameIndex==1?.42f:FrameIndex==2?.16f:.52f;next=(FrameIndex+1)%4;blend=(t-start)/duration;}
            else if(StateName=="Interact"){float f=Mathf.Min(3,progress*4);next=Mathf.Min(7,FrameIndex+1);blend=f-Mathf.Floor(f);}
            else if(StateName=="Depart"){float start=FrameIndex==8?0:FrameIndex==9?.14f:FrameIndex==10?.28f:.46f;float duration=FrameIndex==10?.18f:FrameIndex==11?.26f:.14f;next=Mathf.Min(11,FrameIndex+1);blend=(depart-start)/duration;}
            StructureFrameBlend.For(body).Set(frames[FrameIndex],frames[next],blend);body.color=new Color(1,1,1,alpha);art.localRotation=Quaternion.identity;art.localScale=Vector3.one*(height/frames[0].bounds.size.y);
            art.localPosition=Vector3.up*(StateName=="Depart"?Mathf.Sin(progress*Mathf.PI)*.12f:0);
            for(int i=0;i<motes.Length;i++){
                float life=Mathf.Repeat(clock*.4f+i*.317f,1),radius=StateName=="Depart"?.2f+progress*.4f:StateName=="Interact"?.4f:.28f;
                var r=motes[i];r.transform.localPosition=new Vector3(Mathf.Cos(i*2.4f+clock*.7f)*radius,(life-.5f)*height,0);r.transform.localScale=Vector3.one*(StateName=="Idle"?.03f:.045f);
                r.color=new Color(1,.9f,.72f,Mathf.Sin(life*Mathf.PI)*(StateName=="Idle"?.25f:.5f)*alpha);
            }
        }
    }
}
