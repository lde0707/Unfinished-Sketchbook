using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unfin.Editor
{
    public static class WebGLBuilder
    {
        [MenuItem("Unfin/Build Web Player")]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Install Web Build Support for Unity " + Application.unityVersion);

            string output = Environment.GetEnvironmentVariable("UNFIN_WEBGL_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = Path.GetFullPath("../Builds/WebGLSite/dist");
            Directory.CreateDirectory(output);
            PlayerSettings.WebGL.template = "PROJECT:Unfin";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            // Ordinary static hosts do not need Content-Encoding configuration.
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { WorldPrototypeBuilder.ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Web build failed: " + report.summary.result);
            Debug.Log("UNFIN_WEBGL_OK " + output + " bytes=" + report.summary.totalSize);
        }
    }
}
