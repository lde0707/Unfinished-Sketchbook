using UnityEngine;
namespace Unfin
{
    // Painted overlays share the title's lifetime, background and unscaled animation clock.
    // Scene loading is reserved for actually starting/resuming the game.
    public sealed partial class StilldrawnTitle
    {
        Texture2D panelArt,buttonArt;
        readonly Texture2D[] zoneArt=new Texture2D[8];
        static readonly string[] ZoneFiles={"A-paper-cave","B-graphite-mine","C-sketch-tree","D-root-depths","E-sunset-cavern","F-wax-workshop","G-painted-canyon","H-knight-tower"};
        static readonly string[] Tabs={"화면","소리","조작"};
        int draggingSlider=-1;
        public int SettingsTab=>settingsTab;
        public float BackgroundTime=>clock;
        public int BookmarkZone=>record==null?0:record.lastZonePlusOne>=1&&record.lastZonePlusOne<=8?record.lastZonePlusOne-1:
            Mathf.Clamp(RegionalTilePalette.ZoneAt(world.Data,Mathf.FloorToInt(record.checkpointX/16),Mathf.FloorToInt(record.checkpointY/16)),0,7);
        void PanelAssets()
        {
            if(panelArt==null)panelArt=Resources.Load<Texture2D>("Title/Panels/panel");
            if(buttonArt==null)buttonArt=Resources.Load<Texture2D>("Title/Panels/button");
        }
        void CenterText(Rect r,string value,int size,Color color)
        {
            var align=button.alignment;button.alignment=TextAnchor.MiddleCenter;
            Label(r.x,r.y,r.width,r.height,value,size,color,menu:true);button.alignment=align;
        }
        bool PaintedButton(Rect bounds,string value,int index=-1,bool enabled=true,bool active=false,int size=27)
        {
            PanelAssets();bool focus=enabled&&GUI.enabled&&(index>=0?Highlighted(index,bounds):bounds.Contains(Event.current.mousePosition));
            Color old=GUI.color;GUI.color=!enabled?new Color(.45f,.45f,.43f):focus||active?Color.white:new Color(.8f,.78f,.73f);
            if(buttonArt!=null)GUI.DrawTexture(bounds,buttonArt,ScaleMode.ScaleToFit,true);GUI.color=old;
            CenterText(bounds,value,size,!enabled?muted*.65f:focus||active?ink:new Color(.72f,.68f,.60f));
            if(focus&&index>=0&&!keyboardNavigation&&Event.current.type==EventType.Repaint)selection=index;
            if(enabled&&GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&bounds.Contains(Event.current.mousePosition)){
                if(index>=0)selection=index;Event.current.Use();MenuSound(true);return true;
            }
            return false;
        }
        void DrawPaintedHome()
        {
            if(PaintedButton(new Rect(210,475,520,90),"이어하기",0,record!=null))Activate(0);
            if(PaintedButton(new Rect(210,587,520,90),"새 이야기",1))Activate(1);
            if(PaintedButton(new Rect(210,699,520,90),"설정",2))Activate(2);
            if(Application.platform!=RuntimePlatform.WebGLPlayer&&PaintedButton(new Rect(210,804,520,70),"게임 종료",3,size:23))Activate(3);
            float y=Application.platform==RuntimePlatform.WebGLPlayer?835:900;
            if(record!=null){Label(218,y,640,36,(record.inPrologue?"지워지는 첫 장":ZoneEntryBanner.Names[BookmarkZone])+"  ·  "+WorldSession.TimeLabel(record.playSeconds),21,gold,menu:true);Label(218,y+38,640,30,"마지막 기록   "+record.savedAt,17,muted);}
            else Label(218,y,640,40,"새 이야기를 시작하면 책갈피가 남습니다.",20,muted,menu:true);
        }
        void DrawTitlePanel()
        {
            PanelAssets();Fill(new Rect(0,0,1920,1080),new Color(0,0,0,.14f));
            if(panelArt!=null)GUI.DrawTexture(new Rect(40,40,1840,1000),panelArt,ScaleMode.ScaleToFit,true);
            if(page==Page.Continue||page==Page.ConfirmNew)DrawBookmarkPanel();
            else if(page==Page.Settings){DrawSettingsTabs();DrawPaintedSettings();}
            else if(page==Page.Controls){Label(230,190,900,85,"조작 방법",52,ink,menu:true);DrawControlsRows();if(PaintedButton(new Rect(230,868,380,66),"‹  뒤로",0))Back();}
        }
        void PanelHeading(string title)
        {Label(230,215,1200,80,title,51,ink,menu:true);Rule(230,310,1320,new Color(gold.r,gold.g,gold.b,.5f));}
        void DrawBookmarkPanel()
        {
            bool fresh=page==Page.ConfirmNew;int zone=BookmarkZone;
            PanelHeading(fresh?"새 이야기를 펼칠까요?":"마지막 책갈피");
            if(zoneArt[zone]==null)zoneArt[zone]=Resources.Load<Texture2D>("Title/Panels/"+ZoneFiles[zone]);
            var picture=new Rect(230,350,790,445);
            Fill(new Rect(picture.x-4,picture.y-4,picture.width+8,picture.height+8),new Color(.44f,.36f,.25f));
            if(zoneArt[zone]!=null)GUI.DrawTexture(picture,zoneArt[zone],ScaleMode.ScaleToFit,true);
            bool prologue=fresh||(record!=null&&record.inPrologue);
            string chapter=prologue?"프롤로그 · 표지":zone<4?"제1장 · 연필 숲":"제2장 · 크레파스 언덕";
            Label(1080,355,495,45,chapter,28,gold,menu:true);
            Label(1080,412,495,55,prologue?"지워지는 첫 장":ZoneEntryBanner.Names[zone],32,ink,menu:true);
            Rule(1080,485,470,new Color(gold.r,gold.g,gold.b,.4f));
            if(record!=null){
                Label(1080,510,450,35,"머무른 시간",21,muted,menu:true);
                Label(1080,555,450,46,WorldSession.TimeLabel(record.playSeconds)+":"+((int)record.playSeconds%60).ToString("D2"),32,ink,latin:true);
                Label(1080,630,450,35,"마지막 기록",21,muted,menu:true);Label(1080,673,450,45,record.savedAt,25,ink,latin:true);
                Paragraph(1080,750,460,100,fresh?"기존 책갈피와 지도 메모가 새 이야기로 바뀝니다.":"마지막으로 머문 구역입니다.\n탐험은 저장 지점에서 이어집니다.",20);
            }else Paragraph(1080,520,460,200,"아직 남겨진 책갈피가 없습니다.\n\n지워지는 첫 장에서 이야기를 시작합니다.",24);
            if(PaintedButton(new Rect(230,868,380,66),"‹  뒤로",0))Back();
            if(PaintedButton(new Rect(1040,862,530,88),fresh?"새 이야기 시작":"이야기 이어가기",1))Activate(1);
        }
        void SelectSettingsTab(int next)
        {settingsTab=next;selection=0;draggingSlider=-1;GUIUtility.hotControl=0;RefreshControlGuide();MenuSound(false);}
        void DrawSettingsTabs()
        {
            for(int i=0;i<3;i++)if(PaintedButton(new Rect(350+i*390,125,360,64),Tabs[i],active:settingsTab==i))SelectSettingsTab(i);
            PanelHeading("설정");
        }
        void DrawPaintedSettings()
        {
            if(settingsTab==2){DrawControlsRows();if(PaintedButton(new Rect(230,868,380,66),"‹  뒤로",0))Back();return;}
            if(settingsTab==0){
                string[] names={"해상도","안티앨리어싱","텍스처 품질","프레임 제한"};
                string[] values={GraphicsPreferences.ResolutionLabel(GraphicsPreferences.ResolutionIndex),GraphicsPreferences.Antialiasing==0?"끄기":"FXAA",new[]{"높음","보통","낮음"}[GraphicsPreferences.TextureQuality],new[]{"30 FPS","60 FPS","제한 없음"}[GraphicsPreferences.FrameLimit]};
                for(int i=0;i<4;i++){
                    float y=350+i*78;Label(270,y+8,520,48,names[i],27,Highlighted(i,new Rect(250,y,1280,60))?ink:muted,menu:true);
                    if(PaintedButton(new Rect(1010,y,380,64),"‹        "+values[i]+"        ›",i))GraphicsPreferences.Change(i,Event.current.mousePosition.x<1200?-1:1);
                    Rule(250,y+72,1280,new Color(gold.r,gold.g,gold.b,.16f));
                }
                DrawPanelSlider(4,3,675,"화면 밝기");
                if(PaintedButton(new Rect(270,764,610,63),"타이틀 움직임 줄이기    "+(reducedMotion?"켜짐":"꺼짐"),5,active:reducedMotion,size:23))SetReduced(!reducedMotion);
            }else{
                DrawPanelSlider(0,0,380,"전체 음량");DrawPanelSlider(1,1,510,"효과음");DrawPanelSlider(2,2,640,"환경음");
                Label(270,770,1250,40,"설정한 음량을 바로 들으며 조절할 수 있습니다.",20,muted,menu:true);
            }
            int footer=settingsTab==0?6:3;
            if(PaintedButton(new Rect(230,868,380,66),"‹  뒤로",footer))Back();
            if(PaintedButton(new Rect(715,868,380,66),"기본값",footer+1))ResetPanelSettings();
            if(PaintedButton(new Rect(1200,868,380,66),"저장하기",footer+2))ApplyPanelSettings();
            if(GraphicsPreferences.ResolutionUnsaved)Label(270,825,1300,32,"해상도 미리보기 중 · 저장하지 않고 나가면 이전 해상도로 돌아갑니다.",18,gold,menu:true);
        }
        void DrawPanelSlider(int row,int setting,float y,string label)
        {
            Label(270,y,520,50,label,27,Highlighted(row,new Rect(250,y-10,1280,65))?ink:muted,menu:true);
            float value=Setting(setting),min=setting==3?.7f:0;var track=new Rect(970,y+15,390,15);
            float amount=Mathf.InverseLerp(min,1,value);Color old=GUI.color;
            if(buttonArt!=null){GUI.color=new Color(.6f,.55f,.44f);GUI.DrawTexture(new Rect(950,y-12,440,74),buttonArt,ScaleMode.ScaleToFit,true);GUI.color=old;}
            Fill(new Rect(track.x,track.y+6,track.width,3),new Color(.23f,.21f,.17f));Fill(new Rect(track.x,track.y+6,track.width*amount,3),gold);
            Diamond(track.x+amount*track.width,track.y+7,13,gold);Label(1410,y-2,140,45,Mathf.RoundToInt(value*100)+"%",25,ink,latin:true);
            int id=GUIUtility.GetControlID(400+setting,FocusType.Passive);var e=Event.current;
            if(GUI.enabled&&e.type==EventType.MouseDown&&e.button==0&&new Rect(950,y-10,440,65).Contains(e.mousePosition)){draggingSlider=setting;selection=row;GUIUtility.hotControl=id;ChangeSetting(setting,Mathf.Lerp(min,1,(e.mousePosition.x-track.x)/track.width));e.Use();}
            if(GUI.enabled&&draggingSlider==setting&&GUIUtility.hotControl==id){
                if(e.type==EventType.MouseDrag){ChangeSetting(setting,Mathf.Lerp(min,1,(e.mousePosition.x-track.x)/track.width));e.Use();}
                if(e.type==EventType.MouseUp){draggingSlider=-1;GUIUtility.hotControl=0;e.Use();}
            }
            Rule(250,y+70,1280,new Color(gold.r,gold.g,gold.b,.16f));
        }
        void DrawControlsRows()
        {
            float spacing=Mathf.Min(64,460f/Mathf.Max(1,controlRows.Count-1));
            for(int i=0;i<controlRows.Count;i++){
                float y=330+i*spacing;Label(270,y,710,45,controlRows[i].name,25,muted,menu:true);
                CenterText(new Rect(1000,y,500,45),controlRows[i].key,24,ink);
                Rule(250,y+spacing-5,1280,new Color(gold.r,gold.g,gold.b,.14f));
            }
            Label(740,880,830,45,"획득한 능력의 조작만 표시됩니다.",20,muted,menu:true);
        }
        void HandlePanelSettingsInput(int dx,bool accept)
        {
            if(dx!=0){keyboardNavigation=true;if(settingsTab==0){if(selection<4)GraphicsPreferences.Change(selection,dx);else if(selection==4)ChangeSetting(3,brightness+dx*.05f);else if(selection==5)SetReduced(!reducedMotion);}else if(settingsTab==1&&selection<3)ChangeSetting(selection,Setting(selection)+dx*.05f);}
            if(!accept)return;if(settingsTab==2){Back();return;}
            int footer=settingsTab==0?6:3;
            if(selection==footer)Back();else if(selection==footer+1)ResetPanelSettings();else if(selection==footer+2)ApplyPanelSettings();
            else if(settingsTab==0){if(selection<4)GraphicsPreferences.Change(selection,1);else if(selection==5)SetReduced(!reducedMotion);}
        }
        void ResetPanelSettings()
        {
            if(settingsTab==0){GraphicsPreferences.ResetDefaults();ChangeSetting(3,.9f);SetReduced(false);}
            else{ChangeSetting(0,.7f);ChangeSetting(1,.65f);ChangeSetting(2,.32f);}
        }
        void ApplyPanelSettings(){GraphicsPreferences.SaveResolution();notice="설정을 저장했습니다.";noticeTime=1.5f;}
    }
}
