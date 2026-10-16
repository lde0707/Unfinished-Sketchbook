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
            Check(world.terrainTiles.Length == 282, "47 shapes x 3 patterns per region");
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
                foreach (int region in new[] {1,2}) for (int variant=0;variant<3;variant++)
                    Check(world.terrainTiles.Any(t => t.region == region && t.patternVariant == variant && t.exposedEdges == mask), "Missing corner asset");
            }
            Check(observed.Count == 47, "Canonical combination count");
            Check(TerrainTopology.Mask((x,y) => x==0&&y==0?0:1, 1, 1) == 16, "Isolated inner top-left corner");
            int surfaces = 0, notches = 0;
            for (int region = 1; region <= 2; region++)
            {
                var tiles = world.terrainTiles.Where(t => t.region == region).OrderBy(t => t.patternVariant).ThenBy(t => t.exposedEdges).ToArray();
                var atlas = new Texture2D(8*136, 18*136, TextureFormat.RGBA32, false);
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
                    atlas.SetPixels((i%47%8)*136+4, (17-i/47*6-i%47/8)*136+4, 128, 128, texture.GetPixels());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                atlas.Apply(); PreviewOutput.Write($"Validation/world{region}-corner-tiles.png", atlas.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(atlas);
            }
            File.WriteAllText("Validation/terrain-corners.json", $"{{\n  \"neighborConfigurations\": 256,\n  \"shapesPerPattern\": 47,\n  \"patternsPerRegion\": 3,\n  \"variantsPerRegion\": 141,\n  \"regions\": 2,\n  \"exposedFacesChecked\": {surfaces},\n  \"innerCornersChecked\": {notches},\n  \"maximumRimRecessPixels\": 3,\n  \"status\": \"passed\"\n}}\n");
            ValidatePatterns(world);
            ValidateRoundedCorners(world);
            Debug.Log($"UNFIN_CORNERS_OK variants=282 faces={surfaces} innerCorners={notches}");
        }

        static void ValidateRoundedCorners(WorldPrototype world)
        {
            Check(world.filletTiles.Length==90,"15 fillet combinations x 3 patterns x 2 regions");
            Check(world.filletMaps.Length==2,"Both regions have fillet layers");
            foreach(var map in world.filletMaps) Check(map.GetComponent<Collider2D>()==null,"Fillet layer has no collider");
            foreach(var tile in world.filletTiles)
            {
                Check(tile.colliderType==Tile.ColliderType.None,"Fillet tile has no collision");
                var image=new Texture2D(2,2); image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tile.sprite)));
                Check(image.GetPixel(64,64).a==0,"Fillet leaves the empty cell center clear");
                for(int c=0;c<4;c++)
                {
                    int x=c==1||c==2?127:0,y=c<2?127:0;
                    Check(image.GetPixel(x,y).a==((tile.exposedEdges&(1<<c))!=0?1:0),"Fillet corner coverage");
                }
                UnityEngine.Object.DestroyImmediate(image);
            }
            int[] faces={9,3,6,12};
            for(int c=0;c<4;c++)
            {
                int X(int d)=>c==1||c==2?127-d:d;
                int Y(int d)=>c<2?127-d:d;
                Color White(float u,float v)=>Color.white;
                Check(TerrainSurface.Pixel(faces[c],X(4),Y(4),White,White).a==0,"Outer corner is visibly rounded");
                Check(TerrainSurface.Pixel(faces[c],X(16),Y(16),White,White).a==1,"Rounded corner keeps its interior");
                Check(TerrainSurface.Pixel(faces[c],X(32),Y(0),White,White).a==1,"Arc returns to the straight face");
            }
            // All 256 neighbor arrangements: empty corners only get supported fillets.
            var offsets=new[]{new Vector2Int(0,-1),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,0),
                new Vector2Int(-1,-1),new Vector2Int(1,-1),new Vector2Int(1,1),new Vector2Int(-1,1)};
            for(int raw=0;raw<256;raw++)
            {
                var grid=new int[9];for(int i=0;i<8;i++)if((raw&(1<<i))!=0)grid[(offsets[i].y+1)*3+offsets[i].x+1]=1;
                int actual=TerrainTopology.FilletMask((x,y)=>grid[y*3+x],1,1);
                int expected=((raw&25)==25?1:0)|((raw&35)==35?2:0)|((raw&70)==70?4:0)|((raw&140)==140?8:0);
                Check(actual==expected,"Supported concave topology");
                grid[4]=1;Check(TerrainTopology.FilletMask((x,y)=>grid[y*3+x],1,1)==0,"No fillet on occupied cells");
            }
            var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);var cells=data.Decode();
            int Get(int x,int y)=>x<0||y<0||x>=data.width||y>=data.height?1:cells[y*data.width+x];
            var patterns=TerrainPatterns.Create(data.width,data.height);int count=0;
            for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++)
            {
                int mask=TerrainTopology.FilletMask(Get,x,y),region=x>=data.regionX?1:0;
                var tile=world.filletMaps[region].GetTile<PrototypeTile>(WorldData.Cell(x,y));
                Check((tile!=null)==(mask!=0),"Fillet map coverage");
                Check(world.filletMaps[1-region].GetTile(WorldData.Cell(x,y))==null,"Fillet region separation");
                if(tile!=null){Check(tile.exposedEdges==mask&&tile.patternVariant==patterns[y*data.width+x],"Fillet map choice");count++;}
            }
            File.WriteAllText("Validation/terrain-rounded-corners.json",$"{{\n  \"outerRadiusPixels\": {TerrainSurface.OuterRadius},\n  \"innerRadiusPixels\": {TerrainSurface.InnerRadius},\n  \"filletAssets\": 90,\n  \"filletCells\": {count},\n  \"neighborConfigurations\": 256,\n  \"collisionGridChanged\": false,\n  \"status\": \"passed\"\n}}\n");
        }

        static void ValidatePatterns(WorldPrototype world)
        {
            var data = JsonUtility.FromJson<WorldData>(world.mapJson.text);
            var patterns = TerrainPatterns.Create(data.width, data.height);
            var again = TerrainPatterns.Create(data.width, data.height);
            Check(patterns.SequenceEqual(again), "Deterministic patterns");
            var counts = new int[3];
            for (int y=0;y<data.height;y++) for (int x=0;x<data.width;x++)
            {
                int p=patterns[y*data.width+x]; counts[p]++;
                Check(x==0 || p!=patterns[y*data.width+x-1], "Horizontal pattern repetition");
                Check(y==0 || p!=patterns[(y-1)*data.width+x], "Vertical pattern repetition");
            }
            float maximumSeamError=0;
            for (int region=1;region<=2;region++)
            {
                var floors = new Texture2D[3];
                var walls = new Texture2D[3];
                for (int v=0;v<3;v++)
                {
                    floors[v]=Load(1); walls[v]=Load(2);
                    Texture2D Load(int mask)
                    {
                        var tile=world.terrainTiles.Single(t=>t.region==region&&t.patternVariant==v&&t.exposedEdges==mask);
                        var image=new Texture2D(2,2); image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(tile.sprite)));
                        return image;
                    }
                    int minRim=128,maxRim=0;
                    for(int x=8;x<120;x++)
                    {
                        int depth=0; while(depth<8&&floors[v].GetPixel(x,127-depth).a<.5f)depth++;
                        minRim=Math.Min(minRim,depth);maxRim=Math.Max(maxRim,depth);
                    }
                    Check(maxRim-minRim>=1 && maxRim<=3,"Shallow irregular walkable rim");
                    float previous=1;
                    for(int band=0;band<8;band++)
                    {
                        float sum=0;
                        for(int d=band*16;d<(band+1)*16;d++)for(int x=12;x<116;x++)sum+=floors[v].GetPixel(x,127-d).grayscale;
                        float mean=sum/(16*104);
                        Check(mean<previous,"Gradual full-depth shading"); previous=mean;
                    }
                }
                for(int a=0;a<3;a++)for(int b=0;b<3;b++)for(int d=0;d<128;d++)
                {
                    maximumSeamError=Math.Max(maximumSeamError, Error(floors[a].GetPixel(127,d),floors[b].GetPixel(0,d)));
                    maximumSeamError=Math.Max(maximumSeamError, Error(walls[a].GetPixel(d,127),walls[b].GetPixel(d,0)));
                }
                Check(maximumSeamError<.025f,"A/B/C seam value discontinuity");
                for(int v=1;v<3;v++) Check(!floors[0].GetPixels32().SequenceEqual(floors[v].GetPixels32()),"Distinct pattern artwork");
                var preview=new Texture2D(768,256,TextureFormat.RGBA32,false);
                int[] order={0,1,2,0,2,1};
                for(int i=0;i<order.Length;i++)
                {
                    preview.SetPixels(i*128,128,128,128,floors[order[i]].GetPixels());
                    preview.SetPixels(i*128,0,128,128,walls[order[i]].GetPixels());
                }
                preview.Apply();PreviewOutput.Write($"Validation/world{region}-patterns-abc.png",preview.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(preview);
                foreach(var texture in floors.Concat(walls))UnityEngine.Object.DestroyImmediate(texture);
            }
            File.WriteAllText("Validation/terrain-patterns.json",$"{{\n  \"patternsPerRegion\": 3,\n  \"totalTerrainTiles\": 282,\n  \"patternCellCounts\": [{counts[0]}, {counts[1]}, {counts[2]}],\n  \"adjacentRepeats\": 0,\n  \"maximumSeamError\": {maximumSeamError.ToString(System.Globalization.CultureInfo.InvariantCulture)},\n  \"maximumRimRecessPixels\": 3,\n  \"depthBandValidation\": \"passed\"\n}}\n");
        }
        static float Error(Color a, Color b) => Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b),Mathf.Abs(a.a-b.a));

        public static void Render()
        {
            WorldPrototypeBuilder.Validate();
            WorldPrototypeBuilder.RenderPreviews();
            var world = UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var tiles = world.terrainTiles;
            var tilePaths = tiles.Select(tile => AssetDatabase.GetAssetPath(tile)).ToArray();
            var filletPaths = world.filletTiles.Select(tile=>AssetDatabase.GetAssetPath(tile)).ToArray();
            // Interior corner fixture enclosed in rectangular earth. Treat outside as
            // solid, exactly like the real world, so no bright exterior rim is generated.
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
                var fillets = filletPaths.Select(path=>AssetDatabase.LoadAssetAtPath<PrototypeTile>(path)).ToArray();
                var grid = new GameObject("Corner fixture").AddComponent<Grid>();
                var go = new GameObject("Terrain"); go.transform.SetParent(grid.transform);
                var map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>();
                var filletObject=new GameObject("Rounded inner corners (visual only)");filletObject.transform.SetParent(grid.transform);
                var filletMap=filletObject.AddComponent<Tilemap>();filletObject.AddComponent<TilemapRenderer>().sortingOrder=1;
                int Get(int x,int y) => x<=0||y<=0||x>=14||y>=8 ? 1 : rows[y][x]-'0';
                var patterns = TerrainPatterns.Create(15,9);
                for (int y = 0; y < 9; y++) for (int x = 0; x < 15; x++) if (Get(x,y)==1)
                {
                    int mask = TerrainTopology.Mask(Get,x,y);
                    var tile = tiles.First(t => t.region==region&&t.exposedEdges==mask&&t.patternVariant==patterns[y*15+x]);
                    Check(tile != null, "Fixture tile asset unloaded");
                    map.SetTile(WorldData.Cell(x,y), tile);
                }
                Check(map.GetUsedTilesCount() > 0, "Fixture terrain populated");
                for(int y=0;y<9;y++)for(int x=0;x<15;x++)
                {
                    int mask=TerrainTopology.FilletMask(Get,x,y);
                    if(mask!=0)filletMap.SetTile(WorldData.Cell(x,y),fillets.First(t=>t.region==region&&t.exposedEdges==mask&&t.patternVariant==patterns[y*15+x]));
                }
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
                PreviewOutput.Write($"Validation/world{region}-stairs-and-corners.png",image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            Debug.Log("UNFIN_CORNER_PREVIEWS_OK");
        }
    }
}
