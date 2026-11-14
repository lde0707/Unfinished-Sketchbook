using System.Runtime.InteropServices;
using UnityEngine;
namespace Unfin
{
    public static class GraphicsPreferences
    {
        // Steam September 2026, Windows primary display top eight (excluding Other).
        public static readonly Vector2Int[] Resolutions={new Vector2Int(1366,768),new Vector2Int(1920,1080),new Vector2Int(1920,1200),new Vector2Int(2560,1080),new Vector2Int(2560,1440),new Vector2Int(2560,1600),new Vector2Int(3440,1440),new Vector2Int(3840,2160)};
        public static int ResolutionIndex {get;private set;}=1;
        public static int Antialiasing {get;private set;}
        public static int TextureQuality {get;private set;}
        public static int FrameLimit {get;private set;}=1;
        static int savedResolution=1;
        public static bool ResolutionUnsaved=>ResolutionIndex!=savedResolution;
        public static string ResolutionLabel(int index)=>Resolutions[index].x+" × "+Resolutions[index].y;
        [DllImport("__Internal")] static extern void Stilldrawn_SetResolution(int width,int height);
        public static void Load()
        {
            ResolutionIndex=Mathf.Clamp(PlayerPrefs.GetInt("graphics.resolution",1),0,7);
            savedResolution=ResolutionIndex;
            Antialiasing=Mathf.Clamp(PlayerPrefs.GetInt("graphics.aa",0),0,1);
            TextureQuality=Mathf.Clamp(PlayerPrefs.GetInt("graphics.texture",0),0,2);
            FrameLimit=Mathf.Clamp(PlayerPrefs.GetInt("graphics.fps",1),0,2);
            ApplyQuality();SetResolution(ResolutionIndex);
        }
        static void SetResolution(int index)
        {
            ResolutionIndex=Mathf.Clamp(index,0,7);var r=Resolutions[ResolutionIndex];
            if(!Application.isPlaying)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            Stilldrawn_SetResolution(r.x,r.y);
#elif !UNITY_EDITOR
            Screen.SetResolution(r.x,r.y,Screen.fullScreenMode);
#endif
        }
        public static void ChooseResolution(int index)
        {
            index=Mathf.Clamp(index,0,7);if(index==ResolutionIndex)return;
            // Live preview only. Other preferences may save while this is pending;
            // never put an uncommitted resolution into PlayerPrefs.
            SetResolution(index);
        }
        public static void SaveResolution(){savedResolution=ResolutionIndex;PlayerPrefs.SetInt("graphics.resolution",savedResolution);PlayerPrefs.Save();}
        public static void Revert(){if(ResolutionUnsaved)SetResolution(savedResolution);}
        public static void Change(int row,int delta)
        {
            if(row==0){ChooseResolution((ResolutionIndex+delta+8)%8);return;}
            if(row==1)Antialiasing=(Antialiasing+delta+2)%2;
            if(row==2)TextureQuality=(TextureQuality+delta+3)%3;
            if(row==3)FrameLimit=(FrameLimit+delta+3)%3;
            ApplyQuality();SaveQuality();
        }
        public static void Preset(bool low)
        {
            Antialiasing=low?0:1;TextureQuality=low?2:0;FrameLimit=low?0:1;
            ApplyQuality();SaveQuality();ChooseResolution(low?0:1);
        }
        public static void ResetDefaults()
        {
            Antialiasing=0;TextureQuality=0;FrameLimit=1;ApplyQuality();SaveQuality();ChooseResolution(1);
        }
        static void SaveQuality(){PlayerPrefs.SetInt("graphics.aa",Antialiasing);PlayerPrefs.SetInt("graphics.texture",TextureQuality);PlayerPrefs.SetInt("graphics.fps",FrameLimit);PlayerPrefs.Save();}
        static void ApplyQuality()
        {
            // A real mip upload limit, not a blur filter. Non-mipped/UI art stays intact.
            QualitySettings.globalTextureMipmapLimit=TextureQuality;
            QualitySettings.antiAliasing=0; // FXAA runs in the existing world grading pass.
            QualitySettings.vSyncCount=0;Application.targetFrameRate=FrameLimit==0?30:FrameLimit==1?60:-1;
        }
    }
}
