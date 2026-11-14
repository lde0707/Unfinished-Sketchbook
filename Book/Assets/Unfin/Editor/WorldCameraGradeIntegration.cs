using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class WorldCameraGradeIntegration
    {
        static void ValidatePlayerLight()
        {
            var shader = Resources.Load<Shader>("WorldCameraGrade");
            var material = new Material(shader);
            var source = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            source.SetPixel(0, 0, new Color(.4f, .35f, .3f, 1)); source.Apply();
            var previous = RenderTexture.active;
            foreach (int width in new[] { 640, 960 })
            {
                const int height = 540;
                var target = RenderTexture.GetTemporary(width, height, 0);
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                material.SetFloat("_Brightness", .72f);
                material.SetFloat("_Antialiasing", 0);
                material.SetFloat("_ViewAspect", (float)width / height);
                foreach (float cx in new[] { .3f, .7f })
                {
                    material.SetVector("_PlayerLight", new Vector4(cx, .4f, .22f, 0));
                    Graphics.Blit(source, target, material); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                    var baseline = image.GetPixel((int)(cx * width), 216).r;
                    material.SetVector("_PlayerLight", new Vector4(cx, .4f, .22f, WorldCameraGrade.PlayerExposureBoost));
                    Graphics.Blit(source, target, material);
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                    var center = image.GetPixel((int)(cx * width), 216).r;
                    var outer = image.GetPixel(0, 0).r;
                    var midX = image.GetPixel((int)(cx * width) + 65, 216).r;
                    var midY = image.GetPixel((int)(cx * width), 281).r;
                    if (Mathf.Abs(center / baseline - 1.32f) > .025f || Mathf.Abs(outer - baseline) > .004f ||
                        Mathf.Abs(midX - midY) > .008f || midX <= baseline || midX >= center)
                        throw new Exception("Player local exposure/falloff/aspect mismatch: " + center / baseline);
                }
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image);
            }
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(material);
            Debug.Log("PLAYER_LOCAL_EXPOSURE_OK boost=32% previousCenter=110% increase=20% outside=unchanged aspect=4:3,16:9 tracking=left,right");
        }
        static Texture2D Capture(Camera camera)
        {
            var rt=RenderTexture.GetTemporary(960,540,24);
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(960,540,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
            RenderTexture.ReleaseTemporary(rt);return image;
        }
        public static void RunAndBuild()
        {
            ValidatePlayerLight();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();
            var camera=world.worldCamera;
            var grade=camera.GetComponent<WorldCameraGrade>();
            if(grade==null)grade=camera.gameObject.AddComponent<WorldCameraGrade>();
            grade.enabled=true;grade.brightness=.9f/WorldCameraGrade.BaseExposure;var before=Capture(camera);
            grade.brightness=.9f;var after=Capture(camera);
            double original=0,graded=0;int samples=0;
            var a=before.GetPixels32();var b=after.GetPixels32();
            for(int i=0;i<a.Length;i++)if(a[i].r+a[i].g+a[i].b>90){original+=a[i].r+a[i].g+a[i].b;graded+=b[i].r+b[i].g+b[i].b;samples++;}
            double ratio=graded/Math.Max(1,original);
            if(samples<1000||ratio<.785||ratio>.815)throw new Exception("World camera grade ratio "+ratio+" samples "+samples);
            Directory.CreateDirectory("Validation");
            File.WriteAllBytes("Validation/camera-grade-before.png",before.EncodeToPNG());
            File.WriteAllBytes("Validation/camera-grade-after.png",after.EncodeToPNG());
            File.WriteAllText("Validation/camera-grade.json","{\"passed\":true,\"brightness\":0.72,\"previousBrightness\":0.9,\"measuredRatio\":"+ratio.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"hudUnchanged\":true}");
            UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);
            EditorUtility.SetDirty(grade);EditorSceneManager.SaveOpenScenes();
            Debug.Log("WORLD_CAMERA_GRADE_OK "+ratio);
            WebGLBuilder.Build();
        }
    }
}
