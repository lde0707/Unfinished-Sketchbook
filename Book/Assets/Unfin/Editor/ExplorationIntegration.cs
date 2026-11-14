using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Unfin.Editor
{
    public static class ExplorationIntegration
    {
        [MenuItem("Unfin/Apply Exploration Routes")]
        public static void Apply()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var d=JsonUtility.FromJson<WorldData>(w.mapJson.text);var cells=d.Decode();
            var layout=JsonUtility.FromJson<ExplorationLayout>(Resources.Load<TextAsset>("ExplorationLayout").text);
            w.region1Terrain=Region1TerrainImporter.Import(d);var patterns=TerrainPatterns.Create(d.width,d.height);
            w.maps[0].ClearAllTiles();w.filletMaps[0].ClearAllTiles();
            for(int y=0;y<d.height;y++)for(int x=0;x<d.regionX;x++){
                if(cells[y*d.width+x]==1)w.maps[0].SetTile(WorldData.Cell(x,y),w.region1Terrain.Terrain(x,y));
                var fillet=w.region1Terrain.Fillet(x,y);if(fillet!=null)w.filletMaps[0].SetTile(WorldData.Cell(x,y),fillet);
            }
            int Get(int x,int y)=>x<0||y<0||x>=d.width||y>=d.height?1:cells[y*d.width+x];
            foreach(var edit in layout.changes)if(edit.x>=d.regionX){
                var cell=WorldData.Cell(edit.x,edit.y);foreach(var map in w.maps)map.SetTile(cell,null);
            }
            foreach(var edit in layout.changes)for(int y=edit.y-1;y<=edit.y+1;y++)for(int x=edit.x-1;x<=edit.x+1;x++){
                if(x<d.regionX||x>=d.width||y<0||y>=d.height)continue;var cell=WorldData.Cell(x,y);
                if(Get(x,y)==1){int mask=TerrainTopology.Mask(Get,x,y),variant=patterns[y*d.width+x];var tile=Array.Find(w.terrainTiles,t=>t.region==2&&t.htmlType==1&&t.exposedEdges==mask&&t.patternVariant==variant);if(tile==null)throw new Exception("Missing exploration terrain "+mask);w.maps[3].SetTile(cell,tile);}
                int corner=TerrainTopology.FilletMask(Get,x,y);var f=corner==0?null:Array.Find(w.filletTiles,t=>t.region==2&&t.exposedEdges==corner&&t.patternVariant==patterns[y*d.width+x]);w.filletMaps[1].SetTile(cell,f);
            }
            SpecialTileImporter.Apply(w);RegionalPlatformImporter.Apply(w);
            var all=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None);var sample=all.First(m=>!m.enemy&&m.kind=="heart");
            for(int i=0;i<d.pickups.Length;i++){
                var p=d.pickups[i];if(all.Any(m=>!m.enemy&&m.kind==p.type&&Vector2.Distance(m.htmlPosition,new Vector2(p.x,p.y))<.1f))continue;
                // Checkpoint pruning is authored separately; never recreate removed saves.
                if(p.type=="save"&&!CheckpointPlan.Keep(p))continue;
                var go=UnityEngine.Object.Instantiate(sample.gameObject,sample.transform.parent);go.name=$"{i:D3} {p.type} ({p.tx}, {p.floor})";go.transform.position=d.Center(p);go.transform.localScale=new Vector3(p.w/16,p.h/16,1);
                var marker=go.GetComponent<MapMarker>();marker.sourceIndex=i;marker.kind=p.type;marker.htmlPosition=new Vector2(p.x,p.y);marker.originalOptions=p.optionsJson;
            }
            EditorSceneManager.SaveScene(w.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("UNFIN_EXPLORATION_APPLIED "+layout.changes.Length+" changed cells / 3 loops / "+layout.frontiers.Length+" recorded frontiers");
        }
        public static void RunAndBuild(){Apply();ExplorationValidation.Run();MezzanineValidation.ValidateRoutes();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
    }
}
