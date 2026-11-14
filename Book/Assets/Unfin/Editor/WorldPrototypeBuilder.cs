using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Unfin.Editor
{
    public static class WorldPrototypeBuilder
    {
        const string Root = "Assets/Unfin";
        public const string ScenePath = "Assets/Scenes/World12Prototype.unity";
        static WorldData data;
        static byte[] cells;
        static byte[] patterns;
        static Region1TerrainCatalog region1Terrain;
        public static bool ReuseLegacyArt;
        static readonly Dictionary<string, PrototypeTile> tileCache = new Dictionary<string, PrototypeTile>();
        static CpuImage[,,] art;
        sealed class CpuImage
        {
            readonly Color[] pixels;
            readonly int width, height;
            public CpuImage(Texture2D texture) { pixels = texture.GetPixels(); width = texture.width; height = texture.height; }
            public Color GetPixelBilinear(float u, float v)
            {
                float px = Mathf.Clamp(u * width - .5f, 0, width - 1), py = Mathf.Clamp(v * height - .5f, 0, height - 1);
                int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py), xx = Mathf.Min(x + 1, width - 1), yy = Mathf.Min(y + 1, height - 1);
                return Color.Lerp(Color.Lerp(pixels[y * width + x], pixels[y * width + xx], px - x),
                    Color.Lerp(pixels[yy * width + x], pixels[yy * width + xx], px - x), py - y);
            }
        }

        [MenuItem("Unfin/Rebuild World 1 + 2 Prototype")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            RegionalPlatformImporter.Generate();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/world12.json");
            data = JsonUtility.FromJson<WorldData>(json.text); cells = data.Decode();
            patterns = TerrainPatterns.Create(data.width, data.height);
            region1Terrain = Region1TerrainImporter.Import(data);
            Require(data.htmlTileSize == 16 && data.tilePixels == 128 && data.pixelsPerUnit == 128, "Tile scale");
            Directory.CreateDirectory(Root + "/Generated"); Directory.CreateDirectory("Assets/Scenes");
            tileCache.Clear();
            LoadArt();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Export every valid blob combination, including shapes absent from today's map.
            var terrainCatalog = new List<PrototypeTile>();
            for (int region = 0; region < 2; region++)
            for (int variant = 0; variant < 3; variant++)
            for (int mask = 0; mask < 256; mask++)
                if (TerrainTopology.IsCanonical(mask)) terrainCatalog.Add(Tile(region, 1, mask, variant));
            var filletCatalog = new List<PrototypeTile>();
            for (int region=0;region<2;region++) for(int variant=0;variant<3;variant++)
                for(int mask=1;mask<16;mask++)filletCatalog.Add(Tile(region,0,mask,variant));
            var grid = new GameObject("World 1 + 2 — HTML coordinates").AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            var controller = new GameObject("World Prototype").AddComponent<WorldPrototype>();
            controller.mapJson = json; controller.maps = new Tilemap[6];
            controller.region1Terrain = region1Terrain;
            controller.terrainTiles = terrainCatalog.ToArray();
            controller.filletTiles=filletCatalog.ToArray(); controller.filletMaps=new Tilemap[2];
            for (int region = 0; region < 2; region++)
            {
                var regionObject = new GameObject(region == 0 ? "World 1 - Graphite" : "World 2 - Crayon");
                regionObject.transform.SetParent(grid.transform, false);
                for (int layer = 0; layer < 3; layer++)
                {
                    var go = new GameObject(new[] { "Solid terrain", "One-way platforms", "Water" }[layer]);
                    go.transform.SetParent(regionObject.transform, false);
                    var map = go.AddComponent<Tilemap>();
                    go.AddComponent<TilemapRenderer>(); controller.maps[region * 3 + layer] = map;
                    int x0 = region == 0 ? 0 : data.regionX, width = region == 0 ? data.regionX : data.width - data.regionX;
                    var bounds = new BoundsInt(x0, -data.height, 0, width, data.height, 1);
                    var tiles = new TileBase[width * data.height];
                    for (int y = 0; y < data.height; y++)
                    for (int x = x0; x < x0 + width; x++)
                    {
                        int type = Get(x, y);
                        if (type == 0 || WorldPrototype.Layer(type) != layer) continue;
                        int mask = type == 1 ? Mask(x, y) : 15;
                        tiles[(data.height - y - 1) * width + x - x0] = region == 0 && type == 1
                            ? region1Terrain.Terrain(x,y) : Tile(region, type, mask, type == 1 ? patterns[y * data.width + x] : 0);
                    }
                    map.SetTilesBlock(bounds, tiles);
                    if (layer < 2)
                    {
                        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
                        var composite = go.AddComponent<CompositeCollider2D>();
                        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                        var collider = go.AddComponent<TilemapCollider2D>();
                        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                        if (layer == 1)
                        {
                            composite.usedByEffector = true;
                            var effector = go.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.useOneWayGrouping = true;
                        }
                    }
                }
            }
            for(int region=0;region<2;region++)
            {
                var go=new GameObject($"World {region+1} rounded inner corners (visual only)");
                go.transform.SetParent(grid.transform,false);
                var map=go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>().sortingOrder=1;
                controller.filletMaps[region]=map;
                int x0=region==0?0:data.regionX, width=region==0?data.regionX:data.width-data.regionX;
                var tiles=new TileBase[width*data.height];
                for(int y=0;y<data.height;y++)for(int x=x0;x<x0+width;x++)
                {
                    int mask=TerrainTopology.FilletMask(Get,x,y);
                    if(mask!=0)tiles[(data.height-y-1)*width+x-x0]=region==0 ? region1Terrain.Fillet(x,y) : Tile(region,0,mask,patterns[y*data.width+x]);
                }
                map.SetTilesBlock(new BoundsInt(x0,-data.height,0,width,data.height,1),tiles);
            }
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = data.orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.105f, .115f, .14f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.transform.position = new Vector3(data.startX + .5f, -data.startFloor + 1.6875f, -10);
            camera.gameObject.AddComponent<AudioListener>(); controller.worldCamera = camera;
            Sprite markerSprite = MarkerSprite();
            var player = new GameObject("Player — 12 x 22 HTML units");
            var playerRenderer = player.AddComponent<SpriteRenderer>(); playerRenderer.sprite = markerSprite;
            playerRenderer.color = new Color(.82f, .94f, 1); playerRenderer.sortingOrder = 20;
            player.transform.localScale = new Vector3(.75f, 1.375f, 1);
            player.transform.position = new Vector3(data.startX + .5f, -data.startFloor + .6875f, 0);
            controller.player = player.transform;
            PaperSpriteImporter.Attach(controller);
            RegionalPlatformImporter.Apply(controller);SpecialTileImporter.Apply(controller);
            MakeMarkers("Enemy placement (no AI)", data.enemies, true, markerSprite);
            MakeMarkers("Item and device placement", data.pickups, false, markerSprite);
            MakeAreas("Rooms", data.rooms); MakeAreas("Passages", data.passages); MakeAreas("Shafts", data.shafts);
            controller.overview = Overview();
            Region1BackgroundImporter.Apply(data);
            RoomBackdropImporter.Apply(data);
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("UNFIN_BUILD_OK " + ScenePath);
        }
        // Requires a graphics device; validates and renders both world and corner scenes.
        static void LoadArt()
        {
            art = new CpuImage[2, 3, 2];
            for(int region=0;region<2;region++)for(int variant=0;variant<3;variant++)for(int side=0;side<2;side++)
            {
                var texture=new Texture2D(2,2);
                string name=(region==0?"01-graphite":"02-crayon")+"-"+(char)('a'+variant)+(side==0?"-floor.png":"-wall.png");
                texture.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"../../Art/samples/regional-variants-v6",name)));
                art[region,variant,side]=new CpuImage(texture);UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        public static void RefreshFilletsAndRender()
        {
            tileCache.Clear(); LoadArt();
            for(int region=0;region<2;region++)for(int variant=0;variant<3;variant++)
                for(int mask=1;mask<16;mask++)Tile(region,0,mask,variant);
            AssetDatabase.SaveAssets(); TerrainCornerValidation.Render();
        }
        public static void BuildAndRender()
        {
            Build();
            TerrainCornerValidation.Render();
        }
        public static void RebuildLayoutAndRender()
        {
            Build();
            RenderPreviews();
            WorldMapPresentation.Render();
        }
        static int Get(int x, int y) => x < 0 || y < 0 || x >= data.width || y >= data.height ? 1 : cells[y * data.width + x];
        static int Mask(int x, int y) => TerrainTopology.Mask(Get, x, y);
        static Color Tint(int type) => type switch
        {
            2 => new Color(.55f,.3f,.33f), 3 => new Color(.55f,.12f,.36f), 4 => new Color(.55f,.61f,.65f),
            5 => new Color(.3f,.48f,.33f), 6 => new Color(.6f,.47f,.34f), 7 => new Color(.63f,.5f,.28f),
            8 => new Color(.7f,.51f,.24f), 9 => new Color(.2f,.47f,.68f,.65f), 13 => new Color(.57f,.33f,.7f),
            14 => new Color(.62f,.54f,.36f), 15 => new Color(.78f,.41f,.2f), 16 => new Color(.55f,.44f,.23f),
            18 => new Color(.5f,.32f,.5f), _ => new Color(.44f,.46f,.5f)
        };
        static PrototypeTile Tile(int region, int type, int mask, int variant = 0)
        {
            string key = $"r{region + 1}_t{type}_e{mask}";
            if (variant != 0) key += $"_v{variant}";
            if (tileCache.TryGetValue(key, out var cached)) return cached;
            if (ReuseLegacyArt)
            {
                var existing = AssetDatabase.LoadAssetAtPath<PrototypeTile>(Root + "/Generated/" + key + ".asset");
                if (existing != null) { tileCache[key] = existing; return existing; }
            }
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                Color color;
                if (type == 0)
                    color=TerrainSurface.Fillet(mask,x,y,art[region,variant,0].GetPixelBilinear,art[region,variant,1].GetPixelBilinear);
                else if (type == 1)
                {
                    color = TerrainSurface.Pixel(mask, x, y, art[region, variant, 0].GetPixelBilinear, art[region, variant, 1].GetPixelBilinear);
                }
                else
                {
                    Color tint = Tint(type);
                    if (GridMotor.Platform(type) && y < 110) color = Color.clear;
                    else
                    {
                        float rim = Mathf.Clamp01(1 - Mathf.Min(x, 127 - x, y, 127 - y) / 10f);
                        color = Color.Lerp(tint * .42f, tint, rim); color.a = tint.a;
                        if (type == 3)
                        {
                            // Smooth ink ripples retain the hazard color without jagged marks.
                            float wave = 114 + 3 * Mathf.Sin(x * Mathf.PI / 32);
                            float ripple = 1 - Mathf.SmoothStep(0, 1, Mathf.Abs(y - wave) / 4);
                            color = Color.Lerp(color, tint, ripple * .6f);
                        }
                        if (WorldPrototype.Gate(type) && Mathf.Abs(x - 64) < 3) color = tint * 1.3f;
                    }
                }
                pixels[y * 128 + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply();
            string png = Root + "/Generated/" + key + ".png";
            File.WriteAllBytes(png, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            Sprite sprite = ImportSprite(png);
            string asset = Root + "/Generated/" + key + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<PrototypeTile>(asset);
            if (tile == null) { tile = ScriptableObject.CreateInstance<PrototypeTile>(); AssetDatabase.CreateAsset(tile, asset); }
            tile.sprite = sprite; tile.htmlType = type; tile.region = region + 1; tile.exposedEdges = mask;
            tile.patternVariant = variant;
            tile.flags=region==1&&(type==0||type==1)?TileFlags.LockTransform:TileFlags.LockAll;
            tile.colliderType = type == 0 || GridMotor.Liquid(type) ? UnityEngine.Tilemaps.Tile.ColliderType.None : UnityEngine.Tilemaps.Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile); tileCache[key] = tile;
            return tile;
        }
        static Sprite ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 128; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite MarkerSprite()
        {
            var texture = new Texture2D(128, 128);
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float d = Mathf.Min(x, y, 127-x, 127-y);
                pixels[y*128+x] = d < 5 ? new Color(.3f,.34f,.4f,1) : Color.white;
            }
            texture.SetPixels(pixels); texture.Apply();
            string path = Root + "/Generated/marker.png"; File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); return ImportSprite(path);
        }
        static void MakeMarkers(string title, WorldMarker[] markers, bool enemy, Sprite sprite)
        {
            var parent = new GameObject(title);
            for (int i = 0; i < markers.Length; i++)
            {
                var marker = markers[i]; var go = new GameObject($"{i:D3} {marker.type} ({marker.tx}, {marker.floor})");
                go.transform.SetParent(parent.transform, false); go.transform.position = data.Center(marker);
                go.transform.localScale = new Vector3(marker.w / 16, marker.h / 16, 1);
                var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                renderer.color = enemy ? new Color(.7f,.26f,.3f,.8f) : marker.type == "save" ? new Color(.35f,.83f,.8f,.85f) : new Color(.85f,.68f,.28f,.8f);
                renderer.sortingOrder = enemy?5:PaperPickupVisual.BodySortingOrder;
                var metadata = go.AddComponent<MapMarker>(); metadata.enemy = enemy; metadata.sourceIndex = i;
                metadata.kind = marker.type; metadata.originalOptions = marker.optionsJson; metadata.htmlPosition = new Vector2(marker.x, marker.y);
            }
        }
        static void MakeAreas(string title, WorldArea[] areas)
        {
            var parent = new GameObject(title + " (HTML bounds)");
            foreach (var area in areas)
            {
                var go = new GameObject($"{area.z} {area.n} {area.name} [{area.w} x {area.h}]");
                go.transform.SetParent(parent.transform, false); go.transform.position = new Vector3(area.x, -area.y, 0);
            }
        }
        static Texture2D Overview()
        {
            var texture = new Texture2D(data.width, data.height, TextureFormat.RGBA32, false);
            var pixels = new Color[data.width * data.height];
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                int type = Get(x, y);
                pixels[(data.height - y - 1) * data.width + x] = type == 0 ? new Color(.52f,.55f,.59f) : type == 1 ? new Color(.025f,.028f,.035f) : Tint(type);
            }
            texture.SetPixels(pixels); texture.Apply();
            string path = Root + "/Generated/world12-overview.png"; File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception("World validation failed: " + message); }

        [MenuItem("Unfin/Validate World 1 + 2 Prototype")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            data = JsonUtility.FromJson<WorldData>(world.mapJson.text); cells = data.Decode();
            patterns = TerrainPatterns.Create(data.width, data.height);
            Require(EditorBuildSettings.scenes.Length == 1 && EditorBuildSettings.scenes[0].path == ScenePath &&
                EditorBuildSettings.scenes[0].guid.ToString() == AssetDatabase.AssetPathToGUID(ScenePath), "Build scene reference");
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(cells)).Replace("-", "").ToLowerInvariant();
            Require(hash == data.gridSha256, "Decoded grid hash");
            int occupied = 0;
            for (int y = 0; y < data.height; y++) for (int x = 0; x < data.width; x++)
            {
                int type = cells[y * data.width + x], seen = 0;
                for (int layer = 0; layer < world.maps.Length; layer++)
                {
                    var tile = world.maps[layer].GetTile<PrototypeTile>(WorldData.Cell(x, y));
                    if (tile == null) continue;
                    seen++; Require(tile.htmlType == type, $"Tile type at {x},{y}");
                    Require(layer == (x >= data.regionX ? 3 : 0) + WorldPrototype.Layer(type), "Tile layer");
                    if(type==1||type==4)Require(tile.sprite.rect.width==128&&tile.sprite.rect.height==128&&tile.sprite.pixelsPerUnit==128,"Terrain sprite scale");
                    else Require(tile.sprite.pixelsPerUnit==128&&Mathf.Abs(tile.sprite.bounds.size.x*tile.transform.m00-1)<.001f,"Special tile spans one cell");
                    if (type == 1) Require(tile.exposedEdges == Mask(x, y), $"Terrain neighbors at {x},{y}");
                    if (type == 1 && x >= data.regionX) Require(tile.patternVariant == patterns[y * data.width + x], $"Terrain pattern at {x},{y}");
                    if (type == 1 && x < data.regionX) Require(world.region1Terrain != null && tile == world.region1Terrain.Terrain(x,y), $"Colored terrain at {x},{y}");
                }
                Require(seen == (type == 0 ? 0 : 1), $"Cell coverage at {x},{y}");
                if (type != 0) occupied++;
            }
            Require(Mathf.Abs(1080 / (world.worldCamera.orthographicSize * 2) - 128) < .001f, "Screen tile size");
            var markers = UnityEngine.Object.FindObjectsByType<MapMarker>();
            Require(markers.Count(m => m.enemy) == data.enemies.Length && markers.Count(m => !m.enemy) == data.pickups.Length, "Marker counts");
            foreach (var marker in markers)
            {
                var source = (marker.enemy ? data.enemies : data.pickups)[marker.sourceIndex];
                Require(Vector3.Distance(marker.transform.position, data.Center(source)) < .0001f, "Marker position");
            }
            ValidateMotor();
            Directory.CreateDirectory("Validation");
            TerrainCornerValidation.Validate(world);
            File.WriteAllText("Validation/world12-validation.json", "{\n" +
                $"  \"gridSha256\": \"{hash}\",\n  \"cellsCompared\": {cells.Length},\n  \"occupiedCells\": {occupied},\n" +
                $"  \"rooms\": {data.rooms.Length},\n  \"shafts\": {data.shafts.Length},\n  \"enemies\": {data.enemies.Length},\n  \"pickups\": {data.pickups.Length},\n" +
                "  \"tileTextureSize\": 128,\n  \"pixelsPerUnit\": 128,\n  \"screenTilePixelsAt1080\": 128,\n  \"orthographicSize\": 4.21875,\n  \"gridAndMarkerValidation\": \"passed\",\n  \"motorValidation\": \"passed\"\n}\n");
            Debug.Log($"UNFIN_VALIDATION_OK cells={cells.Length} occupied={occupied} markers={markers.Length}");
        }
        static void ValidateMotor()
        {
            var fixture = new WorldData { width = 12, height = 12 };
            var grid = new byte[144];
            for (int x = 0; x < 12; x++) grid[8 * 12 + x] = 1;
            for (int x = 2; x < 8; x++) grid[5 * 12 + x] = 4;
            var motor = new GridMotor(fixture, grid) { x = 50, y = 40, vy = 300 };
            for (int i = 0; i < 40 && !motor.grounded; i++) motor.Move(1f / 120);
            Require(motor.grounded && Mathf.Abs(motor.y - 58) < .001f, "Land on platform");
            motor.dropTime = .5f; motor.vy = 200;
            for (int i = 0; i < 50 && motor.y < 100; i++) { motor.vy = 200; motor.Move(1f / 120); }
            Require(motor.y > 90, "Drop through platform");
            motor.y = 90; motor.vy = -200; motor.Move(.05f);
            Require(motor.vy < 0 && motor.y < 90, "Pass upward through platform");
            motor.y = 100; motor.vy = 300; motor.Move(.04f);
            Require(motor.grounded && Mathf.Abs(motor.y - 106) < .001f, "Solid ground");
            grid[6 * 12 + 5] = 1; motor.x = 67; motor.y = 96; motor.vx = 330; motor.vy = 0; motor.Move(1f / 120);
            Require(motor.hitWall == 1 && Mathf.Abs(motor.x - 68) < .001f, "Dash wall collision");
            Require(!GridMotor.Wall(9) && !GridMotor.Wall(6) && !GridMotor.Wall(15) && !GridMotor.Wall(3), "Liquid and platform collision semantics");
        }

        // Run with a graphics device (omit -nographics) for actual Unity-rendered previews.
        public static void RenderPreviews()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var d = JsonUtility.FromJson<WorldData>(world.mapJson.text);
            Directory.CreateDirectory("Validation");
            for (int region = 0; region < 2; region++)
            {
                var p = region == 0 ? new Vector2(d.startX + .5f, -d.startFloor + .6875f) :
                    new Vector2(d.pickups.First(m => m.type == "save" && m.tx >= d.regionX).tx + .5f,
                        -d.pickups.First(m => m.type == "save" && m.tx >= d.regionX).floor + .6875f);
                world.player.position = p;
                world.worldCamera.transform.position = new Vector3(p.x, p.y + 1, -10);
                var target = new RenderTexture(1920, 1080, 24); world.worldCamera.targetTexture = target;
                world.worldCamera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                PreviewOutput.Write($"Validation/world{region + 1}-1920x1080.png", image.EncodeToPNG());
                world.worldCamera.targetTexture = null; RenderTexture.active = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            Debug.Log("UNFIN_PREVIEWS_OK");
        }
    }
}
