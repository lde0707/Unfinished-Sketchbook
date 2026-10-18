using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unfin.Editor
{
    public static class RegionalPlatformImporter
    {
        public const string Root="Assets/Unfin/Art/RegionalPlatforms";
        public const string Tiles="Assets/Unfin/Platforms";
        public static readonly string[] Regions={"01-pencil-forest","02-crayon-hills","03-watercolor-lake","04-paper-city","05-ink-abyss"};
        [MenuItem("Unfin/Apply Zone Tile Palettes")]
        public static void ApplyZonesAndRender()
        {
            Generate();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();Apply(world);
            EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            Validate(world);WorldPrototypeBuilder.Validate();MezzanineValidation.RenderRooms();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var d=JsonUtility.FromJson<WorldData>(world.mapJson.text);int[] numbers={1,9,14,18,3,7,18,20};
            for(int z=0;z<8;z++){
                var room=d.rooms.First(a=>a.r==(z<4?1:2)&&a.n==numbers[z]);var camera=world.worldCamera;
                camera.aspect=1920f/1080;camera.orthographicSize=Mathf.Max((room.h+2)*.5f,(room.w+2)*.5f/camera.aspect);
                camera.transform.position=new Vector3(room.x+room.w*.5f,-room.y-room.h*.5f,-10);
                PaperSpriteValidation.Capture(camera,$"Validation/zone-tiles-{RegionalTilePalette.Zones[z]}.png");
            }
            WorldMapPresentation.Render();Debug.Log("UNFIN_ZONE_TILES_OK");
        }
        public static void Generate()
        {
            AssetDatabase.Refresh();Directory.CreateDirectory(Tiles);
            for(int r=0;r<5;r++)for(int v=0;v<3;v++)
            {
                string path=$"{Root}/{Regions[r]}/{(char)('A'+v)}-128.png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null)throw new Exception("Missing platform "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=128;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=128;
                var s=new TextureImporterSettings();importer.ReadTextureSettings(s);s.spriteAlignment=(int)SpriteAlignment.Center;s.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(s);importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                foreach(int type in new[]{4,6,15})
                {
                    string asset=TilePath(r,type,v);var tile=AssetDatabase.LoadAssetAtPath<PrototypeTile>(asset);
                    if(tile==null){tile=ScriptableObject.CreateInstance<PrototypeTile>();AssetDatabase.CreateAsset(tile,asset);}
                    tile.sprite=sprite;tile.htmlType=type;tile.region=r+1;tile.patternVariant=v;tile.exposedEdges=15;
                    tile.colliderType=Tile.ColliderType.Grid;tile.transform=Matrix4x4.identity;tile.flags=TileFlags.LockAll;
                    // Keep temporary-platform types identifiable without changing collision or timers.
                    tile.color=type==6?new Color(.86f,.78f,.70f,1):type==15?new Color(1,.88f,.72f,1):Color.white;
                    EditorUtility.SetDirty(tile);
                }
            }
            for(int z=0;z<8;z++)for(int v=0;v<3;v++)foreach(int type in new[]{4,6,15})
            {
                string asset=ZoneTilePath(z,type,v);var tile=AssetDatabase.LoadAssetAtPath<PrototypeTile>(asset);
                if(tile==null){tile=ScriptableObject.CreateInstance<PrototypeTile>();AssetDatabase.CreateAsset(tile,asset);}
                tile.sprite=Get(RegionalTilePalette.PlatformSources[z],type,v).sprite;
                tile.htmlType=type;tile.region=z<4?1:2;tile.patternVariant=v;tile.exposedEdges=15;
                tile.colliderType=Tile.ColliderType.Grid;tile.transform=Matrix4x4.identity;tile.flags=TileFlags.LockAll;
                var color=RegionalTilePalette.Colors[z];
                if(type==6)color=Color.Lerp(color,new Color(.55f,.43f,.35f),.22f);
                if(type==15)color=Color.Lerp(color,new Color(.78f,.40f,.18f),.22f);
                tile.color=color;EditorUtility.SetDirty(tile);
            }
            var shader=Shader.Find("Unfin/Regional Tiles");if(shader==null||!shader.isSupported)throw new Exception("Regional tile shader unavailable");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Tiles+"/RegionalTiles.mat");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Tiles+"/RegionalTiles.mat");}
            material.shader=shader;EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
        }
        static string TilePath(int r,int type,int v)=>$"{Tiles}/r{r+1}_t{type}_v{v}.asset";
        public static PrototypeTile Get(int r,int type,int v)=>AssetDatabase.LoadAssetAtPath<PrototypeTile>(TilePath(r,type,v));
        static string ZoneTilePath(int z,int type,int v)=>$"{Tiles}/zone{RegionalTilePalette.Zones[z]}_t{type}_v{v}.asset";
        static PrototypeTile ZoneTile(int z,int type,int v)=>AssetDatabase.LoadAssetAtPath<PrototypeTile>(ZoneTilePath(z,type,v));
        public static void Apply(WorldPrototype world)
        {
            var d=JsonUtility.FromJson<WorldData>(world.mapJson.text);var cells=d.Decode();int count=0;
            var material=AssetDatabase.LoadAssetAtPath<Material>(Tiles+"/RegionalTiles.mat");
            foreach(var map in new[]{world.maps[1],world.maps[3],world.maps[4],world.filletMaps[1]})map.GetComponent<TilemapRenderer>().sharedMaterial=material;
            // Tile assets must permit cell colors after a scene reload as well as now.
            // A per-cell flag override alone is replaced by locked Tile.GetTileData on load.
            foreach(var tile in world.terrainTiles.Concat(world.filletTiles))if(tile.region==2){tile.flags&=~TileFlags.LockColor;EditorUtility.SetDirty(tile);}
            for(int y=0;y<d.height;y++)for(int x=0;x<d.width;x++)
            {
                RegionalTilePalette.ApplyTerrainCell(world,d,x,y);
                int type=cells[y*d.width+x];if(!GridMotor.Platform(type))continue;
                int r=x>=d.regionX?1:0,v=(x+y)%3;
                world.maps[r*3+1].SetTile(WorldData.Cell(x,y),ZoneTile(RegionalTilePalette.ZoneAt(d,x,y),type,v));count++;
            }
            Debug.Log("UNFIN_REGIONAL_PLATFORMS_APPLIED "+count);
        }
        public static void Validate(WorldPrototype world)
        {
            var d=JsonUtility.FromJson<WorldData>(world.mapJson.text);var cells=d.Decode();var counts=new int[2];var zoneCounts=new int[8];int coloredTerrain=0;
            for(int r=0;r<5;r++)for(int v=0;v<3;v++)foreach(int t in new[]{4,6,15})
            {
                var tile=Get(r,t,v);if(tile==null||tile.sprite==null||tile.sprite.pixelsPerUnit!=128||tile.sprite.rect.width!=128||tile.htmlType!=t||tile.colliderType!=Tile.ColliderType.Grid)throw new Exception("Platform asset invalid");
            }
            for(int z=0;z<8;z++)for(int v=0;v<3;v++)foreach(int t in new[]{4,6,15}){
                var tile=ZoneTile(z,t,v);if(tile==null||tile.sprite!=Get(RegionalTilePalette.PlatformSources[z],t,v).sprite||tile.htmlType!=t||tile.colliderType!=Tile.ColliderType.Grid)throw new Exception("Zone tile invalid");}
            foreach(var map in new[]{world.maps[1],world.maps[3],world.maps[4],world.filletMaps[1]})
                if(map.GetComponent<TilemapRenderer>().sharedMaterial.shader.name!="Unfin/Regional Tiles")throw new Exception("Missing zone renderer material");
            for(int y=0;y<d.height;y++)for(int x=0;x<d.width;x++)
            {
                int type=cells[y*d.width+x];int r=x>=d.regionX?1:0;var cell=WorldData.Cell(x,y);
                if(r==1&&type==1){var actual=world.maps[3].GetColor(cell);var expected=RegionalTilePalette.TerrainColor(d,x,y);
                    if(Vector4.Distance(actual,expected)>.01f)throw new Exception($"Terrain palette mismatch {x},{y}");coloredTerrain++;}
                if(!GridMotor.Platform(type))continue;int z=RegionalTilePalette.ZoneAt(d,x,y);
                if(world.maps[r*3+1].GetTile(cell)!=ZoneTile(z,type,(x+y)%3))throw new Exception($"Platform mismatch {x},{y}");counts[r]++;zoneCounts[z]++;
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/regional-platforms.json",$"{{\"passed\":true,\"spriteCount\":15,\"baseTileAssets\":45,\"zoneTileAssets\":72,\"world1Cells\":{counts[0]},\"world2Cells\":{counts[1]},\"zones\":\"ABCDEFGH\",\"zonePlatformCounts\":[{string.Join(",",zoneCounts)}],\"coloredWorld2TerrainCells\":{coloredTerrain},\"futureRegionsReady\":3,\"ppu\":128}}");
        }
    }
}
