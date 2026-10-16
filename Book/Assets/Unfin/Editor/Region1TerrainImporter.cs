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
    public static class Region1TerrainImporter
    {
        const string Root = "Assets/Unfin/Art/Region1V8";
        public const string CatalogPath = Root + "/Region1Terrain.asset";
        [Serializable] public class Entry { public int x, y, id, mask, zone; public bool overlay; }
        [Serializable] public class Manifest
        {
            public string gridSha256;
            public int width, height, tileSize, atlasSize, columns, pitch, padding, tilesPerAtlas, tileCount;
            public string[] atlases, zones;
            public Entry[] entries;
        }
        public static Region1TerrainCatalog Import(WorldData world)
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/manifest.json"));
            if (manifest.gridSha256 != world.gridSha256 || manifest.width != world.regionX || manifest.height != world.height)
                throw new Exception("Regenerate World 1 tiles with node Tools/export_region1_terrain.cjs after map edits.");
            var sprites = new Dictionary<int, Sprite>();
            for (int a = 0; a < manifest.atlases.Length; a++)
            {
                string path = Root + "/" + manifest.atlases[a];
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 128;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                int count = Math.Min(manifest.tilesPerAtlas, manifest.tileCount - a * manifest.tilesPerAtlas);
                var definitions = new SpriteMetaData[count];
                for (int i = 0; i < count; i++) definitions[i] = new SpriteMetaData
                {
                    name = "tile_" + (a * manifest.tilesPerAtlas + i).ToString("D5"),
                    rect = new Rect(i % manifest.columns * manifest.pitch + manifest.padding,
                        manifest.atlasSize - (i / manifest.columns * manifest.pitch + manifest.padding) - 128, 128, 128),
                    alignment = (int)SpriteAlignment.Center, pivot = new Vector2(.5f, .5f)
                };
#pragma warning disable 0618
                importer.spritesheet = definitions;
#pragma warning restore 0618
                importer.SaveAndReimport();
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    sprites.Add(int.Parse(sprite.name.Substring(5)), sprite);
            }
            if (sprites.Count != manifest.tileCount) throw new Exception("Incomplete World 1 atlas import");
            var catalog = AssetDatabase.LoadAssetAtPath<Region1TerrainCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<Region1TerrainCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            // Stable names keep existing tile subasset references on repeat imports.
            var tiles = AssetDatabase.LoadAllAssetsAtPath(CatalogPath).OfType<PrototypeTile>().ToDictionary(t => t.name);
            catalog.width = manifest.width; catalog.height = manifest.height;
            catalog.gridSha256 = manifest.gridSha256; catalog.uniqueSprites = manifest.tileCount;
            catalog.terrain = new PrototypeTile[manifest.width * manifest.height];
            catalog.fillets = new PrototypeTile[catalog.terrain.Length];
            var used = new HashSet<string>();
            foreach (var e in manifest.entries)
            {
                string key = $"{(e.overlay ? "join" : "ground")}_{e.id:D5}_{e.mask}";
                used.Add(key);
                if (!tiles.TryGetValue(key, out var tile))
                {
                    tile = ScriptableObject.CreateInstance<PrototypeTile>(); tile.name = key;
                    AssetDatabase.AddObjectToAsset(tile, catalog); tiles.Add(key, tile);
                }
                tile.sprite = sprites[e.id]; tile.htmlType = e.overlay ? 0 : 1;
                tile.region = 1; tile.exposedEdges = e.mask; tile.patternVariant = e.zone;
                tile.colliderType = e.overlay ? Tile.ColliderType.None : Tile.ColliderType.Grid;
                (e.overlay ? catalog.fillets : catalog.terrain)[e.y * manifest.width + e.x] = tile;
                EditorUtility.SetDirty(tile);
            }
            foreach (var old in tiles.Where(p => !used.Contains(p.Key)).Select(p => p.Value).ToArray())
                UnityEngine.Object.DestroyImmediate(old, true);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            return catalog;
        }

        [MenuItem("Unfin/Apply Region 1 Colored Tilemaps")]
        public static void BuildAndRender()
        {
            WorldPrototypeBuilder.ReuseLegacyArt = true;
            try { WorldPrototypeBuilder.Build(); }
            finally { WorldPrototypeBuilder.ReuseLegacyArt = false; }
            WorldPrototypeBuilder.RenderPreviews();
            WorldMapPresentation.Render();
            RenderZones();
        }

        public static void RenderZones()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var data = JsonUtility.FromJson<WorldData>(world.mapJson.text);
            int[] rooms = { 1, 7, 13, 18 };
            foreach (int number in rooms)
            {
                var r = data.rooms.First(a => a.r == 1 && a.n == number);
                world.worldCamera.orthographicSize = data.orthographicSize;
                world.worldCamera.aspect = 1920f / 1080;
                world.worldCamera.transform.position = new Vector3(r.x + r.w * .5f, -r.y - r.h + 3.2f, -10);
                var target = new RenderTexture(1920, 1080, 24);
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                try
                {
                    world.worldCamera.targetTexture = target; world.worldCamera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                    PreviewOutput.Write($"Validation/region1-v8-zone-{r.z}.png", image.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = null; world.worldCamera.targetTexture = null; target.Release();
                    UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
                }
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
    }
}
