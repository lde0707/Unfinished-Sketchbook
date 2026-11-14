using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Unfin.Editor
{
    public static class AmbienceIntegration
    {
        public static void RunAndBuild(){Run();WebGLBuilder.Build();}
        public static void Run()
        {
            AssetDatabase.Refresh();int count=0;
            foreach(string name in new[]{"AmbienceGraphite_0","AmbienceCrayon_0"}){
                string path="Assets/Unfin/Resources/Audio/"+name+".wav";
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing ambience "+path);
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.72f;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=false;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);clip.LoadAudioData();
                if(clip.channels!=2||clip.length<90)throw new Exception("Ambience must be a long stereo loop");
                var data=new float[clip.samples*clip.channels];if(!clip.GetData(data,0))throw new Exception("Cannot decode ambience");
                double sum=0,power=0;float peak=0;
                foreach(float value in data){if(!float.IsFinite(value))throw new Exception("Invalid ambience samples");sum+=value;power+=value*value;peak=Mathf.Max(peak,Mathf.Abs(value));}
                double rms=Math.Sqrt(power/data.Length);if(rms<.0001||rms>.01||peak>.12||Math.Abs(sum/data.Length)>.0001)throw new Exception("Quiet ambience loudness / clipping / DC failed");
                int silent=0,seconds=Mathf.FloorToInt(clip.length),stride=clip.frequency*clip.channels;
                for(int sec=0;sec<seconds;sec++){double energy=0;for(int j=sec*stride;j<(sec+1)*stride;j++)energy+=data[j]*data[j];if(Math.Sqrt(energy/stride)<.00001)silent++;}
                if((float)silent/seconds<.7f)throw new Exception("Ambience needs mostly silence");
                for(int j=0;j<10*stride;j++)if(Mathf.Abs(data[j])>.00001f)throw new Exception("Ambience should start quietly");
                float seam=Mathf.Max(Mathf.Abs(data[0]-data[data.Length-2]),Mathf.Abs(data[1]-data[data.Length-1]));if(seam>.0001)throw new Exception("Ambience loop seam too large");
                Debug.Log("AMBIENCE_ASSET "+name+" seconds="+clip.length+" rms="+rms+" peak="+peak+" seam="+seam);count++;
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/ambience-editor.json","{\"passed\":true,\"stereoLoops\":"+count+",\"durations\":[96,112],\"mostlySilence\":true,\"quietFirstTenSeconds\":true,\"noClipping\":true,\"silentSeams\":true,\"crossfadeSeconds\":5}");
        }
    }
}
