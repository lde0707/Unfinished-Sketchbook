using System;
using System.IO;
using UnityEngine;
namespace Unfin.Editor
{
    public static class ResolutionPreviewValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Resolution preview: "+message);}
        public static void RunAndBuild()
        {
            string[] keys={"graphics.resolution","graphics.aa","graphics.texture","graphics.fps"};
            var exists=new bool[keys.Length];var values=new int[keys.Length];
            for(int i=0;i<keys.Length;i++){exists[i]=PlayerPrefs.HasKey(keys[i]);values[i]=PlayerPrefs.GetInt(keys[i]);}
            int fps=Application.targetFrameRate,mips=QualitySettings.globalTextureMipmapLimit,aa=QualitySettings.antiAliasing,vsync=QualitySettings.vSyncCount;
            try{
                PlayerPrefs.SetInt(keys[0],1);GraphicsPreferences.Load();
                foreach(int index in new[]{0,2,3,4,5,6,7}){
                    GraphicsPreferences.ChooseResolution(index);
                    Check(GraphicsPreferences.ResolutionIndex==index&&GraphicsPreferences.ResolutionUnsaved,"preview switches immediately "+index);
                    Check(PlayerPrefs.GetInt(keys[0])==1,"preview never overwrites saved resolution "+index);
                }
                GraphicsPreferences.Change(1,1);PlayerPrefs.Save();
                Check(PlayerPrefs.GetInt(keys[0])==1,"unrelated preference save cannot commit preview");
                GraphicsPreferences.Revert();Check(GraphicsPreferences.ResolutionIndex==1&&!GraphicsPreferences.ResolutionUnsaved,"cancel multiple changes returns to original");
                GraphicsPreferences.ChooseResolution(2);GraphicsPreferences.SaveResolution();
                Check(!GraphicsPreferences.ResolutionUnsaved&&PlayerPrefs.GetInt(keys[0])==2,"explicit save commits");
                GraphicsPreferences.ChooseResolution(4);GraphicsPreferences.Revert();Check(GraphicsPreferences.ResolutionIndex==2,"save becomes new rollback baseline");
                GraphicsPreferences.ResetDefaults();Check(GraphicsPreferences.ResolutionIndex==1&&PlayerPrefs.GetInt(keys[0])==2,"defaults are preview only");
                GraphicsPreferences.Revert();Check(GraphicsPreferences.ResolutionIndex==2,"cancel defaults");
                GraphicsPreferences.ChooseResolution(0);GraphicsPreferences.Load();Check(GraphicsPreferences.ResolutionIndex==2,"reload discards unsaved preview");
                GraphicsPreferences.ChooseResolution(0);GraphicsPreferences.SaveResolution();GraphicsPreferences.Load();Check(GraphicsPreferences.ResolutionIndex==0,"reload restores explicit save");
                GraphicsPreferences.Change(0,-1);Check(GraphicsPreferences.ResolutionIndex==7,"left arrow wraps");GraphicsPreferences.Change(0,1);Check(GraphicsPreferences.ResolutionIndex==0&&!GraphicsPreferences.ResolutionUnsaved,"return to saved choice clears pending state");
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/resolution-preview.json","{\"passed\":true,\"immediatePreview\":true,\"explicitSaveOnly\":true,\"unrelatedSaveIsolated\":true,\"multipleChangesRollback\":true,\"defaultsRollback\":true,\"reloadSavedOnly\":true}");
                Debug.Log("RESOLUTION_PREVIEW_OK");
            }finally{
                for(int i=0;i<keys.Length;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}
                PlayerPrefs.Save();GraphicsPreferences.Load();Application.targetFrameRate=fps;QualitySettings.globalTextureMipmapLimit=mips;QualitySettings.antiAliasing=aa;QualitySettings.vSyncCount=vsync;
            }
            WebGLBuilder.Build();
        }
    }
}
