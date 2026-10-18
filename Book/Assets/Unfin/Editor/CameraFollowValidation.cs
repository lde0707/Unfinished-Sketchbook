using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class CameraFollowValidation
    {
        static void Check(bool ok, string label) { if (!ok) throw new Exception("Camera follow: " + label); }
        public static void RunAndBuild() { Run(); WebGLBuilder.Build(); }
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var rear = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Unfin/Art/Region1BackgroundV3/Background.mat");
            Check(rear != null && Mathf.Abs(rear.GetFloat("_Opacity") - .52f) < .0001f, "original rear tile opacity restored");
            var w = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(w, null);
            var m = w.Motor;
            // Isolate camera behaviour with the actual game motor; never save this test scene.
            Array.Clear(m.cells, 0, m.cells.Length);
            for (int x = 0; x < w.Data.width; x++) m.cells[80 * w.Data.width + x] = 1;
            const float dt = 1f / 120;
            void Step(int count, int dir = 0)
            {
                for (int i = 0; i < count; i++) { w.Simulate(dt, dir); w.TickCamera(dt); }
            }
            w.Respawn(100, 80); Step(120);
            float baseline = w.worldCamera.transform.position.y, jumpMovement = 0, earlyFollow = 0;
            for (int jump = 0; jump < 4; jump++)
            {
                w.RequestJump();
                for (int i = 0; i < 100; i++)
                {
                    Step(1);
                    if (jump == 0 && i == 11) earlyFollow = w.worldCamera.transform.position.y - baseline;
                    jumpMovement = Mathf.Max(jumpMovement, Mathf.Abs(w.worldCamera.transform.position.y - baseline));
                }
                Check(m.grounded, "repeated jump lands");
            }
            Check(earlyFollow > .35f, "camera follows within first 100ms: " + earlyFollow);
            Check(jumpMovement > 2.5f && jumpMovement < 4.4f, "camera follows jump height smoothly: " + jumpMovement);
            Step(240, 1);
            Check(Mathf.Abs(w.worldCamera.transform.position.x-w.player.position.x)<.0001f, "horizontal tracking has no lag");
            Step(1, -1);
            Check(Mathf.Abs(w.worldCamera.transform.position.x-w.player.position.x)<.0001f, "reversal follows immediately");
            w.RequestDash(-1); Step(12,-1);
            Check(Mathf.Abs(w.worldCamera.transform.position.x-w.player.position.x)<.0001f, "dash follows immediately");
            Step(120);
            // Remove the floor: a real long fall must remain in view and show space below.
            for (int x = 0; x < w.Data.width; x++)
            { m.cells[80 * w.Data.width + x] = 0; m.cells[110 * w.Data.width + x] = 1; }
            float greatestOffset = 0;
            for (int i = 0; i < 210; i++)
            {
                Step(1);
                greatestOffset = Mathf.Max(greatestOffset, Mathf.Abs(w.player.position.y - w.worldCamera.transform.position.y));
            }
            Check(greatestOffset <= w.worldCamera.orthographicSize - .79f, "long fall stays framed");
            Check(m.grounded, "long fall reaches lower platform");
            Step(180);
            Check(Mathf.Abs(w.worldCamera.transform.position.y - w.player.position.y - 1) < .03f, "landing settles on lower platform");
            w.Respawn(250, 110);
            Check(Mathf.Abs(w.worldCamera.transform.position.x - w.player.position.x) < .001f, "teleport resets camera immediately");

            float minFinal = float.MaxValue, maxFinal = float.MinValue;
            foreach (int fps in new[] { 30, 60, 120 })
            {
                var follow = new PlatformCameraFollow(); var start = new Vector2(100, -80);
                follow.Reset(start, 4.21875f, 16f/9, 560, 140);
                for (int i = 1; i <= fps * 2; i++)
                {
                    float t = (float)i / fps;
                    // A sustained vertical climb, beyond the ordinary jump window.
                    var p = start + new Vector2(t*8, t*6);
                    var camera = follow.Tick(p, new Vector2(8,6), false, false, 1f/fps, 4.21875f, 16f/9, 560,140);
                    Check(p.y-camera.y <= 3.42f, "high ascent remains visible at " + fps + " fps");
                }
                minFinal = Mathf.Min(minFinal, follow.Position.y); maxFinal = Mathf.Max(maxFinal, follow.Position.y);
                for (int i = 0; i < fps*2; i++) follow.Tick(start+new Vector2(16,12), Vector2.zero, true,false,1f/fps,4.21875f,16f/9,560,140);
                Check(Mathf.Abs(follow.Position.y - (-67)) < .02f, "upper platform settles");
                follow.Reset(new Vector2(0,0),4.21875f,16f/9,560,140);
                Check(follow.Position.x >= 7.5f && follow.Position.y <= -4.21875f, "world bounds");
            }
            Check(maxFinal-minFinal < .15f, "30/60/120 fps tracking consistency");
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/camera-follow.json", "{\"passed\":true,\"fullJumpCameraTravelTiles\":" + jumpMovement.ToString("F5",System.Globalization.CultureInfo.InvariantCulture) +
                ",\"checks\":[\"rear tile opacity 0.52\",\"first 100ms follows\",\"four full-height jumps\",\"immediate horizontal / reversal / dash\",\"long fall framing\",\"lower and upper landing\",\"respawn reset\",\"high ascent\",\"30/60/120 fps\",\"world bounds\"]}");
            // Discard simulated map state before building the authored scene.
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            Debug.Log("UNFIN_CAMERA_FOLLOW_OK");
        }
    }
}
