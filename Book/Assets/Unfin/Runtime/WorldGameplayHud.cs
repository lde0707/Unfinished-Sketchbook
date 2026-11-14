using UnityEngine;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        readonly Color hudInk=new Color(.90f,.87f,.78f),hudMuted=new Color(.60f,.65f,.64f),hudGold=new Color(.69f,.60f,.42f),hudTeal=new Color(.46f,.69f,.65f);
        GUIStyle hudBody,hudHeading,hudTiny,hudNumber;
        Texture2D healthTray;
        readonly MiniMapTracking miniTracking=new MiniMapTracking();
        int miniFrame=-1;
        public Rect MiniView=>miniTracking.View;
        public bool PointerOverHud{
            get{
                var mouse=UnityEngine.InputSystem.Mouse.current;if(mouse==null)return false;
                var screen=mouse.position.ReadValue();float scale=Mathf.Min(Screen.width/UiWidth,Screen.height/UiHeight);
                var p=new Vector2((screen.x-(Screen.width-UiWidth*scale)*.5f)/scale,(Screen.height-screen.y-(Screen.height-UiHeight*scale)*.5f)/scale);
                return new Rect(1505,23,382,255).Contains(p);
            }
        }
        void HudStyles(){
            if(hudBody!=null)return;
            hudBody=new GUIStyle(label){fontSize=20,padding=new RectOffset(),wordWrap=false};
            hudHeading=new GUIStyle(hudBody){font=Resources.Load<Font>("Title/MaruBuri-SemiBold"),fontSize=27};
            hudTiny=new GUIStyle(hudBody){fontSize=16};
            hudNumber=new GUIStyle(hudBody){fontSize=24};
        }
        void HudText(Rect r,string value,Color color,int size=20,bool heading=false,bool number=false){
            var style=heading?hudHeading:number?hudNumber:size<=17?hudTiny:hudBody;style.fontSize=size;style.normal.textColor=color;GUI.Label(r,value,style);
        }
        static Texture2D hudDiamondTexture;
        void HudDiamond(float x,float y,float size,Color color){
            // A cached alpha shape stays in GUI group coordinates; rotating GUI.matrix
            // would rotate the group's origin and displace map markers when clipped.
            if(hudDiamondTexture==null){
                const int n=32;hudDiamondTexture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[n*n];for(int j=0;j<n;j++)for(int i=0;i<n;i++)pixels[j*n+i]=new Color(1,1,1,Mathf.Clamp01((1-Mathf.Abs((i+.5f)/n*2-1)-Mathf.Abs((j+.5f)/n*2-1))*n*.5f));
                hudDiamondTexture.SetPixels(pixels);hudDiamondTexture.Apply(false,true);
            }
            var old=GUI.color;GUI.color=color;float extent=size*1.41421356f;GUI.DrawTexture(new Rect(x-extent*.5f,y-extent*.5f,extent,extent),hudDiamondTexture);GUI.color=old;
        }
        void HudPanel(Rect r,float opacity=.85f){
            var old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*opacity);PaintedHudArt.Draw("ribbon",r);GUI.color=old;
        }
        void HudKey(float x,float y,string key,float width=40){
            PaintedHudArt.Draw("key",new Rect(x-9,y-5,width+18,40));
            var old=hudTiny.alignment;hudTiny.alignment=TextAnchor.MiddleCenter;HudText(new Rect(x,y,width,26),key,new Color(.20f,.18f,.15f),16);hudTiny.alignment=old;
        }
        bool PaintedButton(Rect r,string text){
            bool hover=r.Contains(Event.current.mousePosition);PaintedHudArt.Draw("key",r,hover?Color.white:new Color(.89f,.86f,.80f));
            var old=hudBody.alignment;hudBody.alignment=TextAnchor.MiddleCenter;HudText(new Rect(r.x+12,r.y+8,r.width-24,r.height-12),text,new Color(.19f,.17f,.14f),20);hudBody.alignment=old;
            if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&hover){Event.current.Use();return true;}return false;
        }
        bool HudButton(Rect r,string value,bool active=false){
            bool hover=r.Contains(Event.current.mousePosition);PaintedHudArt.Draw("ribbon",r,hover||active?Color.white:new Color(.72f,.74f,.73f));
            HudText(new Rect(r.x+43,r.y+9,r.width-62,r.height-10),value,hover||active?hudInk:hudMuted,20);
            if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&hover){Event.current.Use();return true;}return false;
        }
        Vector2 HudScreenPoint(Vector3 position){
            var screen=world.worldCamera.WorldToScreenPoint(position);float scale=Mathf.Min(Screen.width/UiWidth,Screen.height/UiHeight);
            return new Vector2((screen.x-(Screen.width-UiWidth*scale)*.5f)/scale,(Screen.height-screen.y-(Screen.height-UiHeight*scale)*.5f)/scale);
        }
        void DrawGameplayHud(){
            DrawVitals();DrawMiniJournal();DrawSocialPrompt();DrawInteractionPrompt();DrawEconomyHud();
            if(toastTime>0){
                var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(toastTime*2));
                var r=new Rect(575,795,790,113);HudPanel(r,1);
                hudBody.wordWrap=true;HudText(new Rect(r.x+105,r.y+35,r.width-150,60),Toast,hudInk,21);hudBody.wordWrap=false;GUI.color=old;
            }
            AreaBanner.Draw();DrawPickupNotice();
        }
        void DrawVitals(){
            int count=Mathf.CeilToInt(MaxHealth);float step=Mathf.Min(54,495f/Mathf.Max(5,count));
            if(healthTray==null)healthTray=Resources.Load<Texture2D>("PaperMenuV2/health-tray");
            float trayWidth=count*step+120;
            if(healthTray!=null)GUI.DrawTexture(new Rect(66,98-trayWidth/3*.46f,trayWidth,trayWidth/3),healthTray,ScaleMode.ScaleToFit,true);
            HudText(new Rect(114,34,400,27),"체력  "+Health.ToString("0.#")+" / "+MaxHealth.ToString("0.#"),Health<=1?new Color(.88f,.52f,.43f):hudMuted,17);
            bool danger=Health<=1;Color tint=danger?Color.Lerp(hudInk,new Color(.85f,.40f,.32f),.4f+.15f*Mathf.Sin(Time.unscaledTime*4)):Color.white;
            for(int i=0;i<count;i++){
                var r=new Rect(112+i*step,65,step+4,66);PaintedHudArt.Draw("leaf-empty",r,new Color(.64f,.61f,.55f),Mathf.Clamp01(MaxHealth-i));
                PaintedHudArt.Draw("leaf",r,tint,Mathf.Clamp01(Health-i));
            }
            PaintedHudArt.Draw("shard",new Rect(113,139,32,40));
            HudText(new Rect(157,146,120,30),"흑연 조각",hudMuted,18);
            HudText(new Rect(280,140,130,38),Shards.ToString("N0"),hudInk,25,number:true);
            if(ShieldUnlocked){PaintedHudArt.Draw("shield",new Rect(427,139,31,38),ShieldReady?Color.white:new Color(.4f,.4f,.4f));HudText(new Rect(467,144,130,30),ShieldReady?"방패 준비":"방패 소모",hudMuted,17);}
            if(HealingUnlocked){
                float x=112+count*step+42;PaintedHudArt.Draw("heal",new Rect(x,48,64,92),HealingCharges>0?Color.white:new Color(.42f,.42f,.42f));
                HudText(new Rect(x+75,62,110,37),HealingCharges+" / "+MaxHealingCharges,hudInk,27,number:true);HudKey(x+80,111,"L",30);
            }
        }
        void DrawMiniJournal(){
            var frame=new Rect(1505,23,382,255);var mini=new Rect(frame.x+43,frame.y+53,314,165);
            PaintedHudArt.Draw("cartographer-panel",frame);
            if(miniFrame!=Time.frameCount){miniFrame=Time.frameCount;miniTracking.Follow(new Vector2((world.Motor.x+6)/16,(world.Motor.y+11)/16),new Vector2(world.Data.width,world.Data.height),Time.deltaTime);}
            var view=miniTracking.View;
            GUI.BeginGroup(mini);DrawMap(new Rect(0,0,mini.width,mini.height),view,false);GUI.EndGroup();
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0&&mini.Contains(e.mousePosition)){
                int note=HitNote(mini,view,e.mousePosition);world.ToggleExplorationMap();if(note>=0)FocusNote(note);e.Use();
            }
            int zone=RegionalTilePalette.ZoneAt(world.Data,Mathf.FloorToInt((world.Motor.x+6)/16),Mathf.FloorToInt((world.Motor.y+11)/16));
            Fill(new Rect(frame.x+53,frame.y+27,15,15),new Color(.23f,.20f,.16f));Fill(new Rect(frame.x+55,frame.y+29,11,11),MapZonePalette.Ink(zone));
            HudText(new Rect(frame.x+77,frame.y+17,275,35),ZoneEntryBanner.Names[zone],new Color(.23f,.20f,.16f),20,true);
        }
        void Legend(float x,float y,Color color,string name){HudDiamond(x+5,y+13,7,color);HudText(new Rect(x+21,y,200,27),name,hudMuted,17);}
        void DrawJournalMap(){
            if(mapRegion<0)mapRegion=world.Motor.x/16<world.Data.regionX?1:2;
            JournalArt.Draw(new Rect(90,45,1740,990));
            HudText(new Rect(230,155,900,55),"지도",hudInk,38,true);
            if(PaintedButton(new Rect(1510,125,170,46),"닫기")){world.ToggleExplorationMap();return;}
            if(HudButton(new Rect(225,265,265,48),"연필 숲",mapRegion==1))mapRegion=1;
            if(HudButton(new Rect(500,265,320,48),"크레파스 언덕",mapRegion==2))mapRegion=2;
            if(HudButton(new Rect(830,265,265,48),"전체",mapRegion==0))mapRegion=0;
            DrawMapNavigation();
            var bounds=journalView.Bounds;
            float width=Mathf.Min(900,350*bounds.width/bounds.height),height=width*bounds.height/bounds.width;
            var rect=new Rect(225+(900-width)*.5f,377+(350-height)*.5f,width,height);
            HandleMapPointer(rect);var view=journalView.View;
            // Clip markers and notes at the map edge; they must not cover the book controls.
            GUI.BeginGroup(rect);DrawMap(new Rect(0,0,rect.width,rect.height),view,true);GUI.EndGroup();
            var e=Event.current;
            if(showMapLegend){
                DrawZoneLegend();
                Legend(240,816,new Color(.85f,.97f,1),"현재 위치");Legend(430,816,new Color(.54f,.85f,.68f),"저장 지점");Legend(640,816,new Color(.76f,.49f,.73f),"이동 지점");
                Legend(240,848,new Color(.94f,.58f,.25f),"잠긴 길");Legend(430,848,new Color(.3f,.94f,.83f),"재방문할 길");Legend(640,848,hudGold,"메모");
                Fill(new Rect(844,823,12,12),new Color(.93f,.78f,.43f));HudText(new Rect(865,816,160,27),"교환소",hudMuted,17);
            }
            foreach(var f in Layout.frontiers)if(SeenFrontiers.Contains(f.id)&&!FrontierCleared(f)&&view.Contains(new Vector2(f.x,f.y))&&Vector2.Distance(MapPoint(rect,view,f.x,f.y),e.mousePosition)<25)HudText(new Rect(230,728,870,25),f.name+" · "+AbilityName(f.ability),hudInk,17);
            DrawNotePage();
        }
    }
}
