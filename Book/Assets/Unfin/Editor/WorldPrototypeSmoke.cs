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
            // Let editor startup (including Search indexing) finish before the
            // play-mode domain reload. Entering immediately can race its callbacks.
            double readyAt = EditorApplication.timeSinceStartup + 5;
            void StartWhenReady()
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                    EditorApplication.timeSinceStartup < readyAt) return;
                EditorApplication.update -= StartWhenReady;
                SessionState.SetBool(ActiveKey, true);
                EditorApplication.EnterPlaymode();
            }
            EditorApplication.update += StartWhenReady;
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
            var patterns = TerrainPatterns.Create(world.Data.width, world.Data.height);
            int gateX = gateIndex % world.Data.width, gateY = gateIndex / world.Data.width;
            int checkedTerrain = 0;
            for (int ty = gateY-3; ty <= gateY+3; ty++) for (int tx = gateX-3; tx <= gateX+3; tx++)
            {
                if (tx<0 || ty<0 || tx>=world.Data.width || ty>=world.Data.height || world.Motor.Get(tx,ty)!=1) continue;
                var tile = world.maps[tx>=world.Data.regionX?3:0].GetTile<PrototypeTile>(WorldData.Cell(tx,ty));
                Check(tile != null && tile.exposedEdges == TerrainTopology.Mask(world.Motor.Get,tx,ty), "Gate refresh corner shape");
                Check(tx < world.Data.regionX && world.region1Terrain != null
                    ? tile == world.region1Terrain.Terrain(tx,ty)
                    : tile.patternVariant == patterns[ty*world.Data.width+tx], "Gate refresh retains material");
                checkedTerrain++;
            }
            Check(checkedTerrain > 0, "Gate terrain fixture covered");
            for(int ty=gateY-3;ty<=gateY+3;ty++)for(int tx=gateX-3;tx<=gateX+3;tx++)
            {
                if(tx<0||ty<0||tx>=world.Data.width||ty>=world.Data.height)continue;
                int mask=TerrainTopology.FilletMask(world.Motor.Get,tx,ty);
                var tile=world.filletMaps[tx>=world.Data.regionX?1:0].GetTile<PrototypeTile>(WorldData.Cell(tx,ty));
                Check((tile!=null)==(mask!=0),"Gate refresh fillet coverage");
                if(tile!=null)Check(tile.exposedEdges==mask&&(tx < world.Data.regionX && world.region1Terrain != null
                    ? tile == world.region1Terrain.Fillet(tx,ty)
                    : tile.patternVariant==patterns[ty*world.Data.width+tx]),"Gate refresh fillet choice");
            }
            yield return Press(Key.R);
            yield return new WaitForSeconds(.25f);
            world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            Check(world.Motor.cells[gateIndex] != 0 && !world.flying, "Reset restores map");
            Check(Mathf.Abs(world.Motor.x / 16 - world.Data.startX) < 1, "Reset restores spawn");
            WorldPrototypeSmoke.Finish(true, "Scene initialization; grounded spawn; keyboard move/jump; World 2 warp; flight; gate opening; refreshed corners and stable regional materials; scene reset.");
        }
    }
}
