using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
 public enum SoundCue {StepStone,StepSoft,StepWood,Jump,Land,Dash,Swing,Thrust,Hit,Death,Wall,EnemyMove,EnemyAttack,Gate,Crumble,Restore,Hazard,MapOpen,MapClose,Respawn,BreakGraphite,BreakVine,BreakWood,BreakSeal,BreakWax}
 public sealed class GameAudio : MonoBehaviour
 {
  public const int VoiceLimit=12;
  static GameAudio instance;
  readonly Dictionary<SoundCue,AudioClip[]> clips=new Dictionary<SoundCue,AudioClip[]>();
  readonly Dictionary<SoundCue,float> last=new Dictionary<SoundCue,float>();
  readonly Dictionary<SoundCue,int> variants=new Dictionary<SoundCue,int>();
  readonly AudioSource[] voices=new AudioSource[VoiceLimit],music=new AudioSource[2];
  readonly float[] gains=new float[VoiceLimit];
  AudioClip[] themes;WorldPrototype world;int current,region=-1;float stepDistance;Vector2 priorPosition;
  public const float AmbienceCrossfadeSeconds=5;
  readonly float[] ambienceLevel=new float[2];
  int resetSerial;bool focused=true;float master=.7f,sfx=.65f,bgm=.32f;
  public int PlayedCount {get;private set;}
  public int MusicRegion=>region;
  public int ActiveVoices {get {int n=0;foreach(var v in voices)if(v!=null&&v.isPlaying)n++;return n;}}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(FindAnyObjectByType<WorldPrototype>()!=null)Get();}
  public static GameAudio Get(){if(!Application.isPlaying)return null;if(instance==null)new GameObject("Game Audio").AddComponent<GameAudio>();return instance;}
  public static void Emit(SoundCue cue,Vector2 position,float strength=1){Get()?.Play(cue,position,strength);}
  void Awake(){instance=this;world=FindAnyObjectByType<WorldPrototype>();Initialize();}
  public void Initialize(){
   foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue))){var found=new List<AudioClip>();for(int i=0;i<4;i++){var c=Resources.Load<AudioClip>("Audio/"+cue+"_"+i);if(c!=null)found.Add(c);}clips[cue]=found.ToArray();}
   for(int i=0;i<voices.Length;i++)voices[i]=Source("Effect "+i);
   for(int i=0;i<2;i++)music[i]=Source("Ambience "+i);
   themes=new[]{Resources.Load<AudioClip>("Audio/AmbienceGraphite_0"),Resources.Load<AudioClip>("Audio/AmbienceCrayon_0")};
   for(int i=0;i<2;i++){music[i].clip=themes[i];music[i].loop=true;}
   if(world!=null){resetSerial=world.ResetSerial;priorPosition=world.player.position;}
   master=PlayerPrefs.GetFloat("audio.master",.7f);sfx=PlayerPrefs.GetFloat("audio.sfx",.65f);bgm=PlayerPrefs.GetFloat("audio.music",.32f);
  }
  AudioSource Source(string name){var go=new GameObject(name);go.transform.SetParent(transform);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.spatialBlend=0;a.dopplerLevel=0;a.volume=0;return a;}
  public static float Attenuation(float distance)=>Mathf.Clamp01(1-Mathf.Max(0,distance-2)/10);
  static float Level(SoundCue c){switch(c){case SoundCue.StepStone:case SoundCue.StepSoft:case SoundCue.StepWood:return .2f;case SoundCue.EnemyMove:return .12f;case SoundCue.Wall:return .16f;case SoundCue.MapOpen:case SoundCue.MapClose:return .23f;case SoundCue.Hit:return .42f;case SoundCue.Death:return .38f;default:return .3f;}}
  public bool Play(SoundCue cue,Vector2 position,float strength=1){
   if(!focused||master<=0||sfx<=0)return false;
   if(world!=null&&world.MapOpen&&cue!=SoundCue.MapOpen&&cue!=SoundCue.MapClose)return false;
   float distance=world!=null?Vector2.Distance(position,world.player.position):0,gain=Attenuation(distance)*Level(cue)*Mathf.Clamp(strength,.2f,1.5f);
   if(gain<.005f||!clips.TryGetValue(cue,out var bank)||bank.Length==0)return false;
   float now=Time.unscaledTime,interval=cue==SoundCue.EnemyMove?.18f:.055f;
   if(last.TryGetValue(cue,out var time)&&now-time<interval)return false;
   int slot=-1;for(int i=0;i<voices.Length;i++)if(!voices[i].isPlaying){slot=i;break;}
   if(slot<0)return false; // Drop excess sounds instead of amplifying a crowd.
   last[cue]=now;variants.TryGetValue(cue,out int index);variants[cue]=index+1;
   var voice=voices[slot];voice.clip=bank[index%bank.Length];voice.pitch=1+((index%5)-2)*.025f;
   voice.panStereo=world!=null?Mathf.Clamp((position.x-world.player.position.x)/14,-.4f,.4f):0;
   gains[slot]=gain;voice.volume=gain*master*sfx;voice.Play();PlayedCount++;return true;
  }
  void Update(){
   if(world==null||!focused)return;
   if(resetSerial!=world.ResetSerial){resetSerial=world.ResetSerial;stepDistance=0;priorPosition=world.player.position;}
   Vector2 p=world.player.position;float moved=Vector2.Distance(p,priorPosition);priorPosition=p;
   if(!world.flying&&!world.MapOpen&&!world.IsDashing&&(world.Motor.grounded||world.WallMoving)&&moved<1){
    stepDistance+=moved;if(stepDistance>.85f){stepDistance=0;var m=world.Motor;int type=m.Get(Mathf.FloorToInt((m.x+6)/16),Mathf.FloorToInt((m.y+23)/16));
     Emit(world.OnWall?SoundCue.Wall:type==16?SoundCue.StepSoft:GridMotor.Platform(type)?SoundCue.StepWood:SoundCue.StepStone,p);}
   }else stepDistance=0;
   int wanted=world.player.position.x>=world.Data.regionX?1:0;
   TickMusic(Time.unscaledDeltaTime,wanted);
   for(int i=0;i<voices.Length;i++)voices[i].volume=gains[i]*master*sfx;
  }
  public void TickMusic(float dt,int wanted){
   region=current=Mathf.Clamp(wanted,0,1);
   float volume=master*bgm*.45f*(world!=null&&world.MapOpen?.65f:1);
   for(int i=0;i<2;i++){
    // Keep phase through region changes. The authored clips loop at their exact
    // silent seam; changing regions does not restart or bunch up the rare cues.
    if(music[i].clip!=null&&!music[i].isPlaying)music[i].Play();
    ambienceLevel[i]=Mathf.MoveTowards(ambienceLevel[i],i==current?1:0,Mathf.Max(0,dt)/AmbienceCrossfadeSeconds);
    music[i].volume=volume*ambienceLevel[i];
   }
  }
  public void SetMaster(float value){master=Mathf.Clamp01(value);PlayerPrefs.SetFloat("audio.master",master);}
  public void SetEffects(float value){sfx=Mathf.Clamp01(value);PlayerPrefs.SetFloat("audio.sfx",sfx);}
  public void SetMusic(float value){bgm=Mathf.Clamp01(value);PlayerPrefs.SetFloat("audio.music",bgm);}
  public void ReportAudio(string unused){Debug.Log("UNFIN_AUDIO_RUNTIME played="+PlayedCount+" voices="+ActiveVoices+" region="+region+" music="+(music[current].isPlaying?"playing":"stopped")+" ambience="+(music[current].clip!=null?music[current].clip.name:"missing")+" loop="+music[current].loop+" level0="+ambienceLevel[0].ToString("F3")+" level1="+ambienceLevel[1].ToString("F3"));}
  void OnApplicationFocus(bool value){focused=value;foreach(var a in voices)if(a!=null)a.Stop();foreach(var a in music)if(a!=null){if(value)a.UnPause();else a.Pause();}}
  void OnDestroy(){if(instance==this)instance=null;}
 }
}
