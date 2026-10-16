using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Unfin
{
    public sealed class WorldPrototype : MonoBehaviour
    {
        public TextAsset mapJson;
        public Tilemap[] maps; // region * 3 + walls / one-way platforms / water
        public PrototypeTile[] terrainTiles; // 47 shapes x 3 patterns per region
        byte[] terrainPatterns;
        public Camera worldCamera;
        public Transform player;
        public Texture2D overview;
        public WorldData Data { get; private set; }
        public GridMotor Motor { get; private set; }
        public bool flying;
        bool showMap, previousJump;
        float jumpBuffer, coyote, wallLock, dashTime, dashCooldown, accumulator;
        int facing = 1;
        Vector2 safe;
        readonly Dictionary<int, Change> changes = new Dictionary<int, Change>();
        readonly List<int> changeKeys = new List<int>();
        readonly Dictionary<int, PrototypeTile> terrainLookup = new Dictionary<int, PrototypeTile>();
        sealed class Change { public int original; public float time; public bool gone; public TileBase tile; }

        public static int Layer(int type) => type == 9 ? 2 : GridMotor.Platform(type) ? 1 : 0;
        public static bool Gate(int type) => type == 2 || type == 5 || type == 7 || type == 13 || type == 14 || type == 18;
        void Awake()
        {
            Data = JsonUtility.FromJson<WorldData>(mapJson.text);
            Motor = new GridMotor(Data, Data.Decode());
            terrainPatterns = TerrainPatterns.Create(Data.width, Data.height);
            foreach (var tile in terrainTiles)
                terrainLookup[TerrainPatterns.Key(tile.region - 1, tile.patternVariant, tile.exposedEdges)] = tile;
            Respawn(Data.startX, Data.startFloor);
            worldCamera.orthographicSize = Data.orthographicSize;
        }
        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.mKey.wasPressedThisFrame) showMap = !showMap;
            if (k.fKey.wasPressedThisFrame) { flying = !flying; Motor.vx = Motor.vy = 0; }
            if (k.rKey.wasPressedThisFrame) RestoreWorld();
            if (k.f1Key.wasPressedThisFrame) Respawn(Data.startX, Data.startFloor);
            if (k.f2Key.wasPressedThisFrame)
                foreach (var p in Data.pickups) if (p.type == "save" && p.tx >= Data.regionX) { Respawn(p.tx, p.floor); break; }
            if (k.eKey.wasPressedThisFrame) OpenNearbyGate();
            if (k.spaceKey.wasPressedThisFrame) jumpBuffer = .12f;
            if ((k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame) && dashCooldown <= 0)
            { dashTime = .17f; dashCooldown = .45f; }
            bool jump = k.spaceKey.isPressed;
            if (previousJump && !jump && Motor.vy < 0) Motor.vy *= .45f;
            previousJump = jump;
            int ix = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            int iy = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0);
            accumulator += Mathf.Min(Time.deltaTime, .1f);
            const float dt = 1f / 120;
            while (accumulator >= dt) { Step(dt, ix, iy); accumulator -= dt; }
            SyncView();
        }
        void Step(float dt, int ix, int iy)
        {
            dashCooldown -= dt; wallLock -= dt; jumpBuffer -= dt;
            if (ix != 0) facing = ix;
            if (flying)
            {
                Motor.x = Mathf.Clamp(Motor.x + ix * 260 * dt, 0, Data.width * 16 - GridMotor.Width);
                Motor.y = Mathf.Clamp(Motor.y + iy * 260 * dt, 0, Data.height * 16 - GridMotor.Height);
                Motor.vx = Motor.vy = 0;
            }
            else
            {
                if (Motor.grounded) coyote = .09f; else coyote -= dt;
                if (dashTime > 0) { dashTime -= dt; Motor.vx = facing * 330; Motor.vy = 0; }
                else
                {
                    int under = Mathf.FloorToInt((Motor.y + GridMotor.Height + 1) / 16);
                    bool wax = Motor.grounded && Motor.Get(Mathf.FloorToInt((Motor.x + 6) / 16), under) == 16;
                    if (wallLock <= 0) Motor.vx = Mathf.Lerp(Motor.vx, ix * (Motor.grounded ? 125 : 150) * (wax ? .5f : 1), dt * (Motor.grounded ? 18 : 10));
                    Motor.vy = Mathf.Min(430, Motor.vy + 1350 * dt);
                    int wall = Motor.Column(Mathf.FloorToInt((Motor.x + 13) / 16), Motor.y + 3, 16) ? 1 :
                        Motor.Column(Mathf.FloorToInt((Motor.x - 1) / 16), Motor.y + 3, 16) ? -1 : 0;
                    if (wall != 0 && !Motor.grounded && ix == wall && Motor.vy > 60) Motor.vy = 60;
                    if (jumpBuffer > 0 && iy > 0 && Motor.grounded && Motor.Row(under, true) && !Motor.Row(under, false))
                    { Motor.dropTime = .22f; Motor.y += 2; coyote = 0; jumpBuffer = 0; }
                    if (jumpBuffer > 0 && coyote > 0)
                    { Motor.vy = -425 * (wax ? .84f : 1); if (ix != 0) Motor.vx = ix * 155; jumpBuffer = 0; coyote = 0; }
                    else if (jumpBuffer > 0 && wall != 0)
                    { Motor.vy = -420; Motor.vx = -wall * 175; wallLock = .17f; jumpBuffer = 0; }
                }
                Motor.Move(dt);
                CheckGround();
            }
            TickChanges(dt);
        }
        void CheckGround()
        {
            int left = Mathf.FloorToInt(Motor.x / 16), right = Mathf.FloorToInt((Motor.x + GridMotor.Width - .01f) / 16);
            int bottom = Mathf.FloorToInt((Motor.y + GridMotor.Height + 1) / 16);
            bool hazard = false, unstable = false;
            for (int x = left; x <= right; x++)
            {
                int t = Motor.Get(x, bottom);
                hazard |= t == 3;
                unstable |= t == 6 || t == 15;
                if (Motor.grounded && (t == 6 || t == 15)) Schedule(x, bottom, t);
                for (int y = Mathf.FloorToInt((Motor.y + 4) / 16); y <= Mathf.FloorToInt((Motor.y + GridMotor.Height - 2) / 16); y++)
                    hazard |= Motor.Get(x, y) == 9;
            }
            if (hazard) { Motor.x = safe.x; Motor.y = safe.y; Motor.vx = Motor.vy = 0; dashTime = 0; }
            else if (Motor.grounded && !unstable) safe = new Vector2(Motor.x, Motor.y);
        }
        Tilemap Map(int x, int type) => maps[(x >= Data.regionX ? 3 : 0) + Layer(type)];
        void Schedule(int x, int y, int type)
        {
            int index = y * Data.width + x;
            if (!changes.ContainsKey(index)) changes.Add(index, new Change { original = type, time = type == 15 ? 1.5f : .45f, tile = Map(x, type).GetTile(WorldData.Cell(x, y)) });
        }
        void TickChanges(float dt)
        {
            changeKeys.Clear(); changeKeys.AddRange(changes.Keys);
            foreach (int index in changeKeys)
            {
                Change change = changes[index]; change.time -= dt;
                if (change.time > 0) continue;
                int x = index % Data.width, y = index / Data.width;
                if (!change.gone)
                {
                    Motor.cells[index] = 0; Map(x, change.original).SetTile(WorldData.Cell(x, y), null);
                    RefreshTerrain(x, y);
                    change.gone = true; change.time = change.original == 15 ? 3 : 2.6f;
                }
                else if (!(Motor.x < (x + 1) * 16 && Motor.x + 12 > x * 16 && Motor.y < (y + 1) * 16 && Motor.y + 22 > y * 16))
                {
                    Motor.cells[index] = (byte)change.original; Map(x, change.original).SetTile(WorldData.Cell(x, y), change.tile); changes.Remove(index);
                    RefreshTerrain(x, y);
                }
            }
        }
        // Explicit QA interaction: remove one connected gate; does not implement combat/progression.
        void OpenNearbyGate()
        {
            int cx = Mathf.FloorToInt((Motor.x + 6) / 16), cy = Mathf.FloorToInt((Motor.y + 11) / 16);
            for (int radius = 0; radius <= 3; radius++)
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int type = Motor.Get(x, y);
                if (!Gate(type) || x < 0 || y < 0 || x >= Data.width || y >= Data.height) continue;
                var pending = new Queue<Vector2Int>(); pending.Enqueue(new Vector2Int(x, y));
                while (pending.Count > 0)
                {
                    var p = pending.Dequeue();
                    if (p.x < 0 || p.y < 0 || p.x >= Data.width || p.y >= Data.height || Motor.Get(p.x, p.y) != type) continue;
                    Motor.cells[p.y * Data.width + p.x] = 0; Map(p.x, type).SetTile(WorldData.Cell(p.x, p.y), null);
                    RefreshTerrain(p.x, p.y);
                    pending.Enqueue(p + Vector2Int.up); pending.Enqueue(p + Vector2Int.down);
                    pending.Enqueue(p + Vector2Int.left); pending.Enqueue(p + Vector2Int.right);
                }
                return;
            }
        }
        public void Respawn(int tx, int floor)
        {
            Motor.x = tx * 16 + 2; Motor.y = floor * 16 - 22; Motor.vx = Motor.vy = 0; Motor.grounded = false;
            safe = new Vector2(Motor.x, Motor.y); dashTime = jumpBuffer = coyote = wallLock = 0;
            SyncView();
        }
        void RefreshTerrain(int cx, int cy)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
            for (int x = cx - 1; x <= cx + 1; x++)
            {
                if (x < 0 || y < 0 || x >= Data.width || y >= Data.height || Motor.Get(x, y) != 1) continue;
                int mask = TerrainTopology.Mask(Motor.Get, x, y);
                int key = TerrainPatterns.Key(x >= Data.regionX ? 1 : 0, terrainPatterns[y * Data.width + x], mask);
                Map(x, 1).SetTile(WorldData.Cell(x, y), terrainLookup[key]);
            }
        }
        void RestoreWorld() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        void SyncView()
        {
            player.position = new Vector3((Motor.x + 6) / 16, -(Motor.y + 11) / 16, 0);
            float halfH = worldCamera.orthographicSize, halfW = halfH * worldCamera.aspect;
            worldCamera.transform.position = new Vector3(Mathf.Clamp(player.position.x, halfW, Data.width - halfW), Mathf.Clamp(player.position.y + 1, -Data.height + halfH, -halfH), -10);
        }
        void OnGUI()
        {
            if (Data == null) return;
            GUI.Box(new Rect(12, 12, 620, 76), "WORLD 1 + 2  |  128 px / tile at 1920 x 1080\nA/D: move   Space: jump / wall jump   S+Space: drop   Shift: dash\nF: free flight   E: open nearby gate (QA)   F1/F2: region   M: map   R: reset");
            GUI.Label(new Rect(24, 91, 700, 28), $"{(Motor.x / 16 < Data.regionX ? "World 1" : "World 2")}   tile ({Motor.x / 16:F1}, {Motor.y / 16:F1})   {(flying ? "FLIGHT" : "WALK")}   Enemies / items: placement markers");
            if (!showMap) return;
            float width = Mathf.Min(Screen.width - 40, 1120), height = width * Data.height / Data.width;
            var rect = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
            GUI.DrawTexture(rect, overview, ScaleMode.StretchToFill);
            var old = GUI.color; GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(rect.x + player.position.x / Data.width * width - 3, rect.y - player.position.y / Data.height * height - 3, 6, 6), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(rect.x, rect.yMax + 4, width, 25), "Original HTML layout. Click an open cell to inspect in free flight; M closes map.");
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                Vector2 p = Event.current.mousePosition - rect.position;
                int tx = Mathf.FloorToInt(p.x / width * Data.width), ty = Mathf.FloorToInt(p.y / height * Data.height);
                if (Motor.Get(tx, ty) == 0)
                { flying = true; Respawn(tx, ty + 1); showMap = false; Event.current.Use(); }
            }
        }
    }
}
