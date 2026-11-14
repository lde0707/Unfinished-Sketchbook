using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Unfin
{
    // One menu for the native player and WebGL. No title-only copy of game state.
    [DefaultExecutionOrder(-900)]
    public sealed partial class StilldrawnTitle : MonoBehaviour
    {
        enum Page { Home, Settings, Controls, ConfirmNew, Pause, Continue }
        static StilldrawnTitle instance;
        static int launch; // 0: title, 1: fresh page, 2: saved bookmark
        public static bool Blocking=>instance!=null&&(instance.visible||Time.frameCount<=instance.releaseFrame);
        public bool Visible=>visible;
        public string CurrentPage=>page.ToString();
        public bool CanContinue=>record!=null;
        public int Selection=>selection;
        public bool ReducedMotion=>reducedMotion;
        WorldPrototype world;StilldrawnTitleMotion titleMotion;
        StilldrawnSave record;Texture2D background,logo;Font serif,korean,menuFont;
        bool visible=true,paused,reducedMotion,returnToPause,preview,controlsFromSettings,keyboardNavigation;
        int releaseFrame=-1;float preferenceDelay;
        Page page;int selection=1,settingsTab;float clock,fade,autosave,noticeTime;
        string notice="";Vector2 checkpoint;Vector2 lastPointer;
        readonly Color ink=new Color(.91f,.88f,.79f),muted=new Color(.57f,.61f,.60f),gold=new Color(.78f,.68f,.47f);
        GUIStyle text,display,button,english;
        float master=.7f,effects=.65f,music=.32f,brightness=.9f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register(){instance=null;launch=0;SceneManager.sceneLoaded-=SceneReady;SceneManager.sceneLoaded+=SceneReady;}
        static void SceneReady(Scene scene,LoadSceneMode mode){Boot();}
        static void Boot(){var w=FindFirstObjectByType<WorldPrototype>();if(w==null)return;new GameObject("Stilldrawn Title").AddComponent<StilldrawnTitle>().Initialize(w);}
        public void Initialize(WorldPrototype w)
        {
            instance=this;world=w;titleMotion=new StilldrawnTitleMotion();GraphicsPreferences.Load();
            if(Application.isPlaying&&w.worldCamera.GetComponent<WorldViewport>()==null)w.worldCamera.gameObject.AddComponent<WorldViewport>().Initialize(w.worldCamera);
            background=Resources.Load<Texture2D>("Title/Panels/background");logo=Resources.Load<Texture2D>("Title/stilldrawn-logo-v1");serif=Resources.Load<Font>("Title/Marcellus-Regular");menuFont=Resources.Load<Font>("Title/MaruBuri-SemiBold");korean=Resources.Load<Font>("UI/NotoSansCJKkr-Regular");
            reducedMotion=PlayerPrefs.GetInt("stilldrawn.reducedMotion",0)==1;
            master=PlayerPrefs.GetFloat("audio.master",.7f);effects=PlayerPrefs.GetFloat("audio.sfx",.65f);music=PlayerPrefs.GetFloat("audio.music",.32f);brightness=PlayerPrefs.GetFloat("stilldrawn.brightness",.9f);ApplyBrightness();
            record=WorldSession.Read(w);selection=record==null?1:0;page=Page.Home;clock=0;
            if(launch!=0){
                if(launch==2&&record!=null)w.RestoreSession(record);else if(launch!=3)WorldSession.PlaySeconds=0;
                launch=0;visible=false;fade=1;Time.timeScale=1;checkpoint=w.CheckpointPosition;WorldSession.Save(w);
            }else{visible=true;WorldSession.PlaySeconds=0;Time.timeScale=0;}

        }
        void ApplyBrightness(){var grade=world.worldCamera.GetComponent<WorldCameraGrade>();if(grade!=null)grade.brightness=brightness;}
        void Update()
        {
            clock+=Time.unscaledDeltaTime;fade=Mathf.Max(0,fade-Time.unscaledDeltaTime*1.8f);noticeTime=Mathf.Max(0,noticeTime-Time.unscaledDeltaTime);
            if(preferenceDelay>0){preferenceDelay-=Time.unscaledDeltaTime;if(preferenceDelay<=0)PlayerPrefs.Save();}
            var k=Keyboard.current;var pad=Gamepad.current;
            bool back=(k!=null&&k.escapeKey.wasPressedThisFrame)||(pad!=null&&pad.buttonEast.wasPressedThisFrame);
            if(!visible){
                if(back){if(world.Progression.InteractionChoiceOpen){world.Progression.CancelInteractionChoice();return;}if(world.Progression.ShopOpen){world.Progression.BackFromShop();return;}if(world.Progression.ShopBlocksInput)return;if(world.ExplorationMapOpen){if(world.Progression.NotesEditing)world.Progression.FinishNoteEditing();else world.ToggleExplorationMap();}else OpenPause();return;}
                if(!preview){WorldSession.PlaySeconds+=Time.deltaTime;autosave+=Time.deltaTime;if(autosave>30||checkpoint!=world.CheckpointPosition){SaveNow();}}
                return;
            }
            if(back){Back();return;}
            int dir=0;
            if(k!=null){if(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)dir=1;if(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)dir=-1;}
            if(pad!=null){if(pad.dpad.down.wasPressedThisFrame||pad.leftStick.down.wasPressedThisFrame)dir=1;if(pad.dpad.up.wasPressedThisFrame||pad.leftStick.up.wasPressedThisFrame)dir=-1;}
            if(dir!=0){keyboardNavigation=true;int count=Count();do{selection=(selection+dir+count)%count;}while(page==Page.Home&&selection==0&&record==null);MenuSound(false);}
            bool accept=(k!=null&&(k.enterKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame))||(pad!=null&&pad.buttonSouth.wasPressedThisFrame);
            if(accept)keyboardNavigation=true;
            if(accept&&page!=Page.Settings){Activate(selection);return;}
            if(page==Page.Settings){
                if(k!=null&&k.tabKey.wasPressedThisFrame){SelectSettingsTab((settingsTab+1)%3);keyboardNavigation=true;}
                if(pad!=null&&pad.rightShoulder.wasPressedThisFrame){SelectSettingsTab((settingsTab+1)%3);keyboardNavigation=true;}
                if(pad!=null&&pad.leftShoulder.wasPressedThisFrame){SelectSettingsTab((settingsTab+2)%3);keyboardNavigation=true;}
                int dx=0;if(k!=null){if(k.leftArrowKey.wasPressedThisFrame)dx=-1;if(k.rightArrowKey.wasPressedThisFrame)dx=1;}
                if(pad!=null){if(pad.dpad.left.wasPressedThisFrame)dx=-1;if(pad.dpad.right.wasPressedThisFrame)dx=1;}
                HandlePanelSettingsInput(dx,accept);
            }
        }
        int Count()=>page==Page.Home?(Application.platform==RuntimePlatform.WebGLPlayer?3:4):page==Page.Pause?4:page==Page.Settings?(settingsTab==0?9:settingsTab==1?6:1):page==Page.ConfirmNew||page==Page.Continue?2:1;
        void MenuSound(bool confirm){if(Application.isPlaying)GameAudio.Emit(confirm?SoundCue.MapClose:SoundCue.MapOpen,world.player.position,.35f);}
        void Open(Page next){if(next==Page.Controls){controlsFromSettings=page==Page.Settings;RefreshControlGuide();}if(next==Page.Settings)RefreshControlGuide();page=next;selection=0;GUIUtility.hotControl=0;MenuSound(true);}
        public void OpenPause(){if(visible||world.Progression.ShopBlocksInput)return;visible=paused=true;returnToPause=true;Time.timeScale=0;Open(Page.Pause);}
        public void Resume(){GraphicsPreferences.Revert();visible=paused=false;releaseFrame=Time.frameCount+1;Time.timeScale=1;MenuSound(true);}
        void Back(){GraphicsPreferences.Revert();if(page==Page.Pause){Resume();return;}if(page==Page.Home)return;if(page==Page.Controls){Open(controlsFromSettings?Page.Settings:Page.Pause);selection=controlsFromSettings?5:2;return;}Open(returnToPause?Page.Pause:Page.Home);selection=page==Page.Home?(record==null?1:0):0;}
        void Activate(int index)
        {
            if(page==Page.Home){if(index==0&&record!=null)Open(Page.Continue);else if(index==1)Open(Page.ConfirmNew);else if(index==2)Open(Page.Settings);else if(index==3)Application.Quit();}
            else if(page==Page.Pause){if(index==0)Resume();else if(index==1)Open(Page.Settings);else if(index==2)Open(Page.Controls);else if(index==3)ReturnToTitle();}
            else if(page==Page.ConfirmNew){if(index==0)Back();else StartNewGame();}
            else if(page==Page.Continue){if(index==0)Back();else ContinueGame();}
            else Back();
        }
        public void ContinueGame(){if(record==null)return;GraphicsPreferences.Revert();launch=2;Time.timeScale=1;SceneManager.LoadScene(record.inPrologue?PrologueFlow.SceneName:PrologueFlow.WorldSceneName);}
        public void StartNewGame(){GraphicsPreferences.Revert();PlayerPrefs.DeleteKey(WorldSession.SaveKey);PlayerPrefs.DeleteKey(world.Progression.NoteStorageKey);PlayerPrefs.Save();launch=1;Time.timeScale=1;SceneManager.LoadScene(PrologueFlow.SceneName);}
        public void ReturnToTitle(){GraphicsPreferences.Revert();if(!preview)SaveNow();launch=0;Time.timeScale=1;SceneManager.LoadScene(PrologueFlow.WorldSceneName);}
        public static void EnterRegionOne(){launch=3;Time.timeScale=1;SceneManager.LoadSceneAsync(PrologueFlow.WorldSceneName);}
        public void SkipForPreview(){GraphicsPreferences.Revert();visible=paused=false;preview=true;launch=0;Time.timeScale=1;}
        void SaveNow(){WorldSession.Save(world);checkpoint=world.CheckpointPosition;autosave=0;}
        void OnApplicationPause(bool value){if(value&&!visible&&!preview)SaveNow();}
        void OnApplicationQuit(){if(!visible&&!preview)SaveNow();}
        void OnDisable(){if(instance==this)GraphicsPreferences.Revert();}
        void OnDestroy(){titleMotion?.Dispose();if(instance==this){GraphicsPreferences.Revert();instance=null;Time.timeScale=1;}}
        float Setting(int i)=>i==0?master:i==1?effects:i==2?music:brightness;
        public void ChangeSetting(int i,float value){preferenceDelay=.35f;value=Mathf.Clamp(value,i==3?.7f:0,1);if(i==0){master=value;GameAudio.Get()?.SetMaster(value);}else if(i==1){effects=value;GameAudio.Get()?.SetEffects(value);}else if(i==2){music=value;GameAudio.Get()?.SetMusic(value);}else{brightness=value;PlayerPrefs.SetFloat("stilldrawn.brightness",value);ApplyBrightness();}}
        public void SetReduced(bool value){preferenceDelay=.35f;reducedMotion=value;PlayerPrefs.SetInt("stilldrawn.reducedMotion",value?1:0);}
        void Styles(){if(text!=null)return;text=new GUIStyle(GUI.skin.label){font=korean,fontSize=22,normal={textColor=ink},padding=new RectOffset(0,0,0,0)};display=new GUIStyle(text){font=serif,fontSize=122};button=new GUIStyle(text){font=menuFont,fontSize=31};english=new GUIStyle(text){font=serif,fontSize=15};}
        void Label(float x,float y,float w,float h,string value,int size,Color color,bool title=false,bool menu=false,bool latin=false){var s=menu?button:latin?english:title?display:text;s.fontSize=size;s.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),value,s);}
        void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        void Rule(float x,float y,float width,Color color){Fill(new Rect(x,y,width,1),color);}
        void Diamond(float x,float y,float size,Color color){var matrix=GUI.matrix;GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.Euler(0,0,45),Vector3.one);Fill(new Rect(-size*.5f,-size*.5f,size,size),color);GUI.matrix=matrix;}
        void OnGUI()
        {
            if(world==null)return;Styles();var oldMatrix=GUI.matrix;int oldDepth=GUI.depth;var oldColor=GUI.color;GUI.depth=-1000;
            float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);var offset=new Vector2((Screen.width-1920*scale)*.5f,(Screen.height-1080*scale)*.5f);
            if(visible){
                if(!paused)Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.035f,.045f));else Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.02f,.025f,.43f));GUI.matrix=Matrix4x4.TRS(offset,Quaternion.identity,new Vector3(scale,scale,1));
                var pointer=Event.current.mousePosition;
                if(Vector2.Distance(pointer,lastPointer)>1||Event.current.type==EventType.MouseDown){keyboardNavigation=false;lastPointer=pointer;}
                if(!paused)DrawBackdrop();
                if(page==Page.Home)DrawPaintedHome();else if(page==Page.Pause)DrawPaperPause();else DrawTitlePanel();
                if(page!=Page.Home)Label(150,1010,1000,32,page==Page.Settings?"↑ ↓  이동    ← →  조절    Tab  설정 탭    Esc  뒤로":"↑ ↓  이동     Enter  선택     Esc  뒤로",17,muted);
                if(noticeTime>0)Label(250,955,1300,35,notice,20,gold);
            }
            GUI.matrix=oldMatrix;
            if(!visible&&fade>0)Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.02f,.03f,.04f,fade));
            GUI.color=oldColor;GUI.depth=oldDepth;
        }
        void DrawBackdrop(){titleMotion?.Draw(background,clock,reducedMotion);}
        bool Highlighted(int index,Rect rect)=>keyboardNavigation?selection==index:rect.Contains(Event.current.mousePosition);
    }
}
