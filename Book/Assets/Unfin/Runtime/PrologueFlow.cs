using UnityEngine;

namespace Unfin
{
    // A playable opening page. Locomotion and whole-drawing player animations are shared.
    public sealed class PrologueFlow : MonoBehaviour
    {
        public const string SceneName="StilldrawnPrologue", WorldSceneName="World12Prototype";
        static PrologueFlow instance;
        public static bool IsTransitioning=>instance!=null&&instance.leaving;
        public bool Leaving=>leaving;
        public float ExitFade=>leaving?Mathf.Clamp01(exitClock/.7f):0;
        WorldPrototype world;
        ProximityStory story;
        float clock,exitClock,lineClock;
        int beat=-1;
        bool leaving,loaded;
        string line="";
        GUIStyle caption,heading;
        public static PrologueFlow Create(WorldPrototype w)
        {
            var flow=w.GetComponent<PrologueFlow>();if(flow!=null)return flow;
            flow=w.gameObject.AddComponent<PrologueFlow>();flow.world=w;flow.story=ProximityStory.Create(w);instance=flow;return flow;
        }
        void Update()
        {
            if(world==null||StilldrawnTitle.Blocking||Time.timeScale==0)return;
            Tick(Time.deltaTime);
            if(leaving&&exitClock>=.7f&&!loaded){loaded=true;StilldrawnTitle.EnterRegionOne();}
        }
        public void Tick(float dt)
        {
            if(dt<=0)return;
            clock+=dt;lineClock+=dt;
            if(leaving){exitClock+=dt;return;}
            float x=world.Motor.WorldBounds.center.x;
            int next=0;
            if(story.Actors[1].phase==ProximityStory.Phase.Gone)next=1;
            if(story.Actors[0].phase!=ProximityStory.Phase.Present)next=2;
            if(story.Actors[2].phase==ProximityStory.Phase.Gone)next=3;
            if(next>beat){beat=next;lineClock=0;line=new[]{
                "펜이 멈춘 뒤, 그림들은 조금씩 지워지고 있었다.",
                "방금까지… 여기 있었는데.",
                "이 책이 끝나지 않으면, 여기 있는 건 전부 지워져.",
                "지워지기 전에, 다음 장으로."}[beat];}
            if(x>=49&&world.Motor.grounded&&!world.flying&&story.Actors.TrueForAll(a=>a.phase==ProximityStory.Phase.Gone))
            {
                leaving=true;world.Motor.vx=world.Motor.vy=0;
                if(Application.isPlaying)WorldSession.Save(world);
            }
        }
        void OnDestroy(){if(instance==this)instance=null;}
        void OnGUI()
        {
            if(world==null||StilldrawnTitle.Blocking)return;
            if(caption==null){caption=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Title/MaruBuri-SemiBold"),alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=26};heading=new GUIStyle(caption){fontSize=34};}
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.depth=-900;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            void Text(Rect r,string value,float opacity,GUIStyle style){style.normal.textColor=new Color(.04f,.035f,.03f,opacity);GUI.Label(new Rect(r.x+1,r.y+2,r.width,r.height),value,style);style.normal.textColor=new Color(.8f,.76f,.65f,opacity);GUI.Label(r,value,style);}
            if(clock<6)Text(new Rect(200,65,880,60),"프롤로그 · 지워지는 첫 장",Mathf.Min(clock,6-clock),heading);
            if(lineClock<5.5f)Text(new Rect(170,586,940,82),line,Mathf.Clamp01(Mathf.Min(lineClock*2,5.5f-lineClock)),caption);
            if(clock<8)Text(new Rect(280,660,720,34),"A · D / ← · →  이동",.55f,caption);
            else if(world.Motor.WorldBounds.center.x>15&&world.Motor.WorldBounds.center.x<20)Text(new Rect(280,660,720,34),"Space  점프",.65f,caption);
            else if(world.Motor.WorldBounds.center.x>44)Text(new Rect(280,660,720,34),story.GoneMask==7?"오른쪽 통로 · 연필 숲으로":"뒤에 남아 있는 그림에 다가가 보세요.",.65f,caption);
            GUI.matrix=matrix;
            if(leaving){GUI.color=new Color(.02f,.03f,.04f,ExitFade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
    }
    [System.Serializable] public sealed class OpeningReport
    {
        public bool prologue,leaving,savedPrologue;public int mask;public float x,y;public string scene,animation;
    }
    public sealed partial class WorldPrototype
    {
        public void ReportOpening(string unused)
        {
            var saved=WorldSession.Read(this);var sprite=player.GetComponentInChildren<PaperSpritePlayer>();
            Debug.Log("STILLDRAWN_OPENING "+JsonUtility.ToJson(new OpeningReport{prologue=Data.isPrologue,leaving=PrologueFlow.IsTransitioning,
                savedPrologue=saved!=null&&saved.inPrologue,mask=GetComponent<ProximityStory>()?.GoneMask??0,x=Motor.x/16,y=Motor.y/16,
                scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,animation=sprite!=null?sprite.CurrentAnimation:""}));
        }
    }

}
