using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Unfin.Editor
{
    [InitializeOnLoad]
    public static class WorldPrototypeSmoke
    {
        const string ActiveKey = "Unfin.WorldSmoke.Active";
        static WorldPrototypeSmoke()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
                {
                    var go = new GameObject("Automated play verification");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.AddComponent<WorldSmokeDriver>();
                }
            };
        }
        public static void Run()
        {
            WorldPrototypeBuilder.Validate();
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.EnterPlaymode();
        }
        public static void Finish(bool success, string message)
        {
            SessionState.SetBool(ActiveKey, false);
            File.WriteAllText("Validation/world12-playmode.txt", (success ? "PASS\n" : "FAIL\n") + message + "\n");
            Debug.Log((success ? "UNFIN_PLAYMODE_OK " : "UNFIN_PLAYMODE_FAILED ") + message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }

    public sealed class WorldSmokeDriver : MonoBehaviour
    {
        Keyboard keyboard;
        bool failed;
        float started;
        void Awake() { started = Time.realtimeSinceStartup; Application.logMessageReceived += OnLog; }
        void OnDestroy() { Application.logMessageReceived -= OnLog; if (keyboard != null) InputSystem.RemoveDevice(keyboard); }
        void OnLog(string message, string trace, LogType type)
        {
            if (!failed && (type == LogType.Exception || type == LogType.Error))
            { failed = true; WorldPrototypeSmoke.Finish(false, message + "\n" + trace); }
        }
        void Update()
        {
            if (!failed && Time.realtimeSinceStartup - started > 45)
            { failed = true; WorldPrototypeSmoke.Finish(false, "Play verification timeout"); }
        }
        void Check(bool value, string message) { if (!value) throw new Exception("Play verification: " + message); }
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        IEnumerator Press(Key key)
        {
            Keys(key); yield return new WaitForSeconds(.08f); Keys(); yield return new WaitForSeconds(.08f);
        }
        IEnumerator Start()
        {
            // Batch mode has no focused Game view; route synthetic test input explicitly.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return new WaitForSeconds(.25f);
            var world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            Check(world != null && world.Motor != null, "World initialized");
            Check(world.Motor.grounded, "Player starts on floor");
            float x = world.Motor.x;
            Keys(Key.A); yield return new WaitForSeconds(.45f); Keys();
            Check(world.Motor.x < x - 20, $"Keyboard movement: x={x}->{world.Motor.x}, A={keyboard.aKey.isPressed}, current={Keyboard.current == keyboard}");
            yield return new WaitForSeconds(.15f);
            float y = world.Motor.y;
            Keys(Key.Space); yield return new WaitForSeconds(.12f);
            Check(world.Motor.y < y - 20, "Keyboard jump"); Keys();
            yield return new WaitForSeconds(.65f);
            yield return Press(Key.F2);
            Check(world.Motor.x / 16 >= world.Data.regionX, "World 2 warp");
            yield return Press(Key.F);
            Check(world.flying, "Flight toggle");
            y = world.Motor.y; Keys(Key.W); yield return new WaitForSeconds(.2f); Keys();
            Check(world.Motor.y < y - 30, "Flight movement");
            int gateIndex = Array.FindIndex(world.Motor.cells, t => WorldPrototype.Gate(t));
            Check(gateIndex >= 0, "Gate fixture present");
            world.Respawn(gateIndex % world.Data.width - 1, gateIndex / world.Data.width + 1);
            yield return Press(Key.E);
            Check(world.Motor.cells[gateIndex] == 0, "QA gate opens");
            yield return Press(Key.R);
            yield return new WaitForSeconds(.25f);
            world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            Check(world.Motor.cells[gateIndex] != 0 && !world.flying, "Reset restores map");
            Check(Mathf.Abs(world.Motor.x / 16 - world.Data.startX) < 1, "Reset restores spawn");
            WorldPrototypeSmoke.Finish(true, "Scene initialization; grounded spawn; keyboard move/jump; World 2 warp; flight; gate opening; scene reset.");
        }
    }
}
