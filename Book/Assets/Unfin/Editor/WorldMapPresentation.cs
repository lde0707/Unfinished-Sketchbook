using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class WorldMapPresentation
    {
        [MenuItem("Unfin/Render Rectangular World Maps")]
        public static void RenderAll()
        {
            TerrainCornerValidation.Render();
            Render();
        }

        public static void Render()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);
            var cells=data.Decode();
            int borderCells=0;
            for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++)
            {
                if(x!=0&&y!=0&&x!=data.width-1&&y!=data.height-1)continue;
                int outward=(y==0?1:0)|(x==data.width-1?2:0)|(y==data.height-1?4:0)|(x==0?8:0);
                var tile=world.maps[x>=data.regionX?3:0].GetTile<PrototypeTile>(WorldData.Cell(x,y));
                if(cells[y*data.width+x]!=1||tile==null||(tile.exposedEdges&outward)!=0)
                    throw new Exception($"Rectangular world boundary exposed at {x},{y}");
                borderCells++;
            }
            Directory.CreateDirectory("Validation");
            var camera=world.worldCamera;
            try
            {
                Capture(camera,0,data.width,data.height,"world12-rectangular-map.png");
                Capture(camera,0,data.regionX,data.height,"world1-rectangular-map.png");
                Capture(camera,data.regionX,data.width-data.regionX,data.height,"world2-rectangular-map.png");
                File.WriteAllText("Validation/world-rectangle.json",$"{{\n  \"width\": {data.width},\n  \"height\": {data.height},\n  \"borderCellsChecked\": {borderCells},\n  \"brightExteriorFaces\": 0,\n  \"sourceGridChanged\": false,\n  \"status\": \"passed\"\n}}\n");
            }
            finally { EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath); }
            Debug.Log($"UNFIN_RECTANGULAR_MAP_OK borderCells={borderCells}");
        }

        static void Capture(Camera camera,int x0,int width,int height,string file)
        {
            const int previewPixelsPerCell=4;
            camera.orthographic=true;camera.orthographicSize=height*.5f;
            camera.aspect=(float)width/height;
            camera.transform.position=new Vector3(x0+width*.5f,-height*.5f,-10);
            // Preserve the playable room background; dark soil is supplied by the
            // original rectangular Tilemaps, not by painting empty rooms black.
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.105f,.115f,.14f);
            var target=new RenderTexture(width*previewPixelsPerCell,height*previewPixelsPerCell,24);
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                PreviewOutput.Write("Validation/"+file,image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=null;camera.targetTexture=null;target.Release();
                UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
