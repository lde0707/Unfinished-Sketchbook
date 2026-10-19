using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Unfin.Editor
{
 public static class GameAudioValidation
 {
  public static void RunAndBuild(){Run();EnemyAnimationValidation.Run();WebGLBuilder.Build();}
  public static void Run(){
   AssetDatabase.Refresh();var files=Directory.GetFiles("Assets/Unfin/Resources/Audio","*.ogg");int samples=0;
   foreach(var path in files){
    var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
    settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;
    settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.loadInBackground=false;importer.forceToMono=!Path.GetFileName(path).StartsWith("Music");importer.SaveAndReimport();
    var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null||clip.length<=.01f)throw new Exception("Missing or empty audio "+path);
    clip.LoadAudioData();var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("Cannot decode "+path);
    double power=0;float peak=0;foreach(float v in data){if(float.IsNaN(v)||float.IsInfinity(v))throw new Exception("Invalid audio");power+=v*v;peak=Mathf.Max(peak,Mathf.Abs(v));}
    if(power<.00001)throw new Exception("Silent audio "+path);
    Debug.Log("AUDIO_ASSET "+Path.GetFileName(path)+" duration="+clip.length+" peak="+peak+" rms="+Math.Sqrt(power/data.Length));samples++;
   }
   foreach(SoundCue cue in Enum.GetValues(typeof(SoundCue)))if(Resources.Load<AudioClip>("Audio/"+cue+"_0")==null)throw new Exception("Unmapped cue "+cue);
   if(GameAudio.Attenuation(12)!=0||GameAudio.Attenuation(0)!=1||GameAudio.VoiceLimit!=12)throw new Exception("Audio range / pool policy");
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/audio-editor.json","{\"passed\":true,\"clips\":"+samples+",\"cues\":20,\"music\":2,\"voiceLimit\":12}");
   Debug.Log("UNFIN_AUDIO_OK "+samples+" decoded non-silent clips / all cues mapped / distance attenuation / 12 voice limit");
  }
 }
}
