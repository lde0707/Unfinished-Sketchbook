using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unfin.Editor
{
    public static class TerrainCornerValidation
    {
        static void Check(bool value, string message) { if (!value) throw new Exception("Terrain corners: " + message); }
        public static void Validate(WorldPrototype world)
        {
            Check(world.terrainTiles.Length == 94, "47 variants per region");
            var observed = new HashSet<int>();
            // Exhaust all possible 8-neighbor arrangements, not just today's level.
            var offsets = new[] {new Vector2Int(0,-1),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,0),
                new Vector2Int(-1,-1),new Vector2Int(1,-1),new Vector2Int(1,1),new Vector2Int(-1,1)};
            for (int raw = 0; raw < 256; raw++)
            {
                var grid = Enumerable.Repeat(1, 9).ToArray();
                for (int i = 0; i < 8; i++) if ((raw & (1 << i)) != 0) grid[(offsets[i].y+1)*3+offsets[i].x+1] = 0;
                int mask = TerrainTopology.Mask((x,y) => grid[y*3+x], 1, 1);
                Check((mask & 15) == (raw & 15), "Every cardinal exposure survives");
                Check(TerrainTopology.IsCanonical(mask), "Valid diagonal combination");
                observed.Add(mask);
                foreach (int region in new[] {1,2}) Check(world.terrainTiles.Any(t => t.region == region && t.exposedEdges == mask), "Missing corner asset");
            }
            Check(observed.Count == 47, "Canonical combination count");
            Check(TerrainTopology.Mask((x,y) => x==0&&y==0?0:1, 1, 1) == 16, "Isolated inner top-left corner");
            int surfaces = 0, notches = 0;
            for (int region = 1; region <= 2; region++)
            {
                var tiles = world.terrainTiles.Where(t => t.region == region).OrderBy(t => t.exposedEdges).ToArray();
                var atlas = new Texture2D(8*136, 6*136, TextureFormat.RGBA32, false);
                atlas.SetPixels(Enumerable.Repeat(new Color(.105f,.115f,.14f), atlas.width*atlas.height).ToArray());
                for (int i = 0; i < tiles.Length; i++)
                {
                    var tile = tiles[i]; int mask = tile.exposedEdges;
                    var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tile.sprite)));
                    Check(texture.width == 128 && texture.height == 128, "128px output");
                    // Check all four actual texture faces; this catches the old top-else-right bug.
                    for (int face = 0; face < 4; face++) if ((mask & (1 << face)) != 0)
                    {
                        // Include the material band, not a single dark crack/outline pixel.
                        float light = 0; int samples = 0;
                        for (int depth = 2; depth <= 12; depth++) for (int along = 56; along <= 72; along++)
                        {
                            int px = face==1?127-depth:face==3?depth:along;
                            int py = face==0?127-depth:face==2?depth:along;
                            light += texture.GetPixel(px,py).grayscale; samples++;
                        }
                        Check(light/samples > .04f, $"Uncovered face {region}/{mask}/{face}"); surfaces++;
                    }
                    for (int corner = 0; corner < 4; corner++) if ((mask & (16 << corner)) != 0)
                    {
                        float light = 0;
                        for (int dy = 2; dy <= 8; dy++) for (int dx = 2; dx <= 8; dx++)
                            light += texture.GetPixel(corner==1||corner==2?127-dx:dx, corner<2?127-dy:dy).grayscale;
                        Check(light/49 > .04f, $"Uncovered inner corner {region}/{mask}/{corner}"); notches++;
                    }
                    atlas.SetPixels((i%8)*136+4, (5-i/8)*136+4, 128, 128, texture.GetPixels());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                atlas.Apply(); File.WriteAllBytes($"Validation/world{region}-corner-tiles.png", atlas.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(atlas);
            }
            File.WriteAllText("Validation/terrain-corners.json", $"{{\n  \"neighborConfigurations\": 256,\n  \"variantsPerRegion\": 47,\n  \"regions\": 2,\n  \"exposedFacesChecked\": {surfaces},\n  \"innerCornersChecked\": {notches},\n  \"surfaceCapPixels\": 2,\n  \"status\": \"passed\"\n}}\n");
            Debug.Log($"UNFIN_CORNERS_OK variants=94 faces={surfaces} innerCorners={notches}");
        }

        public static void Render()
        {
            WorldPrototypeBuilder.Validate();
            WorldPrototypeBuilder.RenderPreviews();
            var world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var tiles = world.terrainTiles;
            var tilePaths = tiles.Select(tile => AssetDatabase.GetAssetPath(tile)).ToArray();
            // Descending and ascending steps, a ceiling, overhangs, inner notches,
            // one-cell pillars and floating terrain in one repeatable inspection layout.
            var rows = new[] {
                "000000000000000",
                "011110000111100",
                "010000000001100",
                "000000000000100",
                "000000010000000",
                "000011111000000",
                "000111111100000",
                "001111111110000",
                "111111111111111"};
            for (int region = 1; region <= 2; region++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // NewScene unloads unused native assets; reacquire persistent tiles by path.
                tiles = tilePaths.Select(path => AssetDatabase.LoadAssetAtPath<PrototypeTile>(path)).ToArray();
                var grid = new GameObject("Corner fixture").AddComponent<Grid>();
                var go = new GameObject("Terrain"); go.transform.SetParent(grid.transform);
                var map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>();
                int Get(int x,int y) => x<0||y<0||x>=15||y>=9 ? 0 : rows[y][x]-'0';
                for (int y = 0; y < 9; y++) for (int x = 0; x < 15; x++) if (Get(x,y)==1)
                {
                    int mask = TerrainTopology.Mask(Get,x,y);
                    var tile = tiles.First(t => t.region==region&&t.exposedEdges==mask);
                    Check(tile != null, "Fixture tile asset unloaded");
                    map.SetTile(WorldData.Cell(x,y), tile);
                }
                Check(map.GetUsedTilesCount() > 0, "Fixture terrain populated");
                var camera = new GameObject("Camera").AddComponent<Camera>(); camera.orthographic = true;
                camera.orthographicSize = 4.5f; camera.transform.position = new Vector3(7.5f,-4.5f,-10);
                camera.backgroundColor = new Color(.105f,.115f,.14f); camera.clearFlags = CameraClearFlags.SolidColor;
                // Inspect the saved fixture, matching what a user opens in the editor.
                string previewScene = $"Assets/Scenes/World{region}TerrainCorners.unity";
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), previewScene);
                EditorSceneManager.OpenScene(previewScene);
                camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
                var target = new RenderTexture(1920,1152,24); camera.targetTexture = target; camera.Render();
                RenderTexture.active = target; var image = new Texture2D(1920,1152,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1920,1152),0,0); image.Apply();
                Color[] rendered = image.GetPixels();
                Check(rendered.Max(c => c.grayscale) - rendered.Min(c => c.grayscale) > .1f, "Empty fixture render");
                File.WriteAllBytes($"Validation/world{region}-stairs-and-corners.png",image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            Debug.Log("UNFIN_CORNER_PREVIEWS_OK");
        }
    }
}
