using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Unfin.Editor
{
    public static class PrologueMapBuilder
    {
        public const string ScenePath="Assets/Scenes/StilldrawnPrologue.unity";
        const string Root="Assets/Unfin/Resources/Prologue";
        static string Base36(int value){const string chars="0123456789abcdefghijklmnopqrstuvwxyz";string s="";do{s=chars[value%36]+s;value/=36;}while(value>0);return s;}
        [MenuItem("Unfin/Build Playable Prologue")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);
            var data=new WorldData{isPrologue=true,schema=1,width=56,height=22,regionX=56,startX=5,startFloor=16,htmlTileSize=16,tilePixels=128,pixelsPerUnit=128,referenceWidth=1920,referenceHeight=1080,orthographicSize=4.21875f,
                rooms=new[]{new WorldArea{n=0,z="A",name="지워지는 첫 장",x=3,y=7,w=50,h=9,r=1}},passages=Array.Empty<WorldArea>(),shafts=Array.Empty<WorldArea>(),enemies=Array.Empty<WorldMarker>(),pickups=Array.Empty<WorldMarker>(),source="STILLDRAWN playable opening page"};
            var cells=new byte[data.width*data.height];
            for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++)if(y<7||y>=16||x<3||x>=53)cells[y*data.width+x]=1;
            for(int x=18;x<=20;x++)cells[15*data.width+x]=1;
            // Optional elevated paper ledge; the main route needs only the basic jump.
            for(int x=30;x<=32;x++)cells[13*data.width+x]=4;
            var rle=new StringBuilder();for(int i=0;i<cells.Length;){int count=1;while(i+count<cells.Length&&cells[i+count]==cells[i])count++;if(rle.Length>0)rle.Append(',');rle.Append(Base36(cells[i])).Append('.').Append(Base36(count));i+=count;}data.mapRle=rle.ToString();
            using(var hash=SHA256.Create())data.gridSha256=BitConverter.ToString(hash.ComputeHash(cells)).Replace("-","").ToLowerInvariant();
            File.WriteAllText(Root+"/map.json",JsonUtility.ToJson(data,true));AssetDatabase.ImportAsset(Root+"/map.json");
            var sourceScene=EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var source=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var originalData=JsonUtility.FromJson<WorldData>(source.mapJson.text);var originalCells=originalData.Decode();
            int OriginalCell(int x,int y)=>x<0||y<0||x>=originalData.width||y>=originalData.height?1:originalCells[y*originalData.width+x];
            var paperShapes=new System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<TileBase>>();
            for(int y=0;y<originalData.height;y++)for(int x=0;x<originalData.regionX;x++)if(OriginalCell(x,y)==1&&x>=220&&x<=270&&y>=118&&y<=138&&RegionalTilePalette.ZoneAt(originalData,x,y)==0){
                int mask=TerrainTopology.Mask(OriginalCell,x,y);var tile=source.region1Terrain.Terrain(x,y);if(tile==null)continue;
                if(!paperShapes.TryGetValue(mask,out var choices)){choices=new System.Collections.Generic.List<TileBase>();paperShapes.Add(mask,choices);}if(choices.Count<24)choices.Add(tile);
            }
            var decorations=UnityEngine.Object.FindObjectsByType<RoomBackdropLayer>(FindObjectsSortMode.None).Where(x=>x.roomBounds.x==230&&x.roomBounds.z==260).OrderBy(x=>x.name).Take(3).ToArray();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var root=new GameObject("World Prototype");var world=root.AddComponent<WorldPrototype>();EditorUtility.CopySerialized(source,world);
            world.mapJson=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/map.json");world.region1Terrain=null;world.overview=null;world.maps=new Tilemap[6];world.filletMaps=new Tilemap[2];
            var grid=new GameObject("Prologue terrain").AddComponent<Grid>();
            for(int i=0;i<8;i++){var go=new GameObject(i<6?"Terrain layer "+i:"Rounded corners "+(i-6));go.transform.SetParent(grid.transform,false);var map=go.AddComponent<Tilemap>();go.AddComponent<TilemapRenderer>().sortingOrder=i<6?0:1;if(i<6)world.maps[i]=map;else world.filletMaps[i-6]=map;}
            int Get(int x,int y)=>x<0||y<0||x>=data.width||y>=data.height?1:cells[y*data.width+x];
            var canonical=source.terrainTiles.Where(t=>t!=null&&t.region==1&&t.htmlType==1).ToArray();
            for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++){
                int value=Get(x,y);if(value==0)continue;TileBase tile;
                int mask=TerrainTopology.Mask(Get,x,y);
                if(value==4)tile=source.maps[1].GetTile(WorldData.Cell(236,125));
                else if(x==2&&y>=7&&y<15)tile=source.maps[0].GetTile(WorldData.Cell(261,Mathf.Min(125,119+y-7)));
                else if(x==53&&y>=7&&y<15)tile=source.maps[0].GetTile(WorldData.Cell(260,123+(y-7)%3));
                else if(y>=16&&x>=4&&x<52&&!(x>=17&&x<=21&&y==16))tile=source.maps[0].GetTile(WorldData.Cell(238+x%6,131+Mathf.Min(5,y-16)));
                else if(y<=6&&x>=4&&x<52)tile=source.maps[0].GetTile(WorldData.Cell(238+x%6,121-Mathf.Min(5,6-y)));
                else if(paperShapes.TryGetValue(mask,out var choices))tile=choices[(x+y)%choices.Count];
                else tile=canonical.FirstOrDefault(t=>t.exposedEdges==mask&&t.patternVariant==x%3);
                if(tile==null)tile=canonical.First(t=>t.exposedEdges==mask);
                world.maps[value==4?1:0].SetTile(WorldData.Cell(x,y),tile);
            }
            var black=AssetDatabase.LoadAssetAtPath<Material>(Root+"/OuterInk.mat");if(black==null){black=new Material(Shader.Find("Unlit/Color")){color=Color.black};AssetDatabase.CreateAsset(black,Root+"/OuterInk.mat");}
            foreach(float side in new[]{1f,55f}){var fill=GameObject.CreatePrimitive(PrimitiveType.Quad);fill.name="Unlit outer page fill";UnityEngine.Object.DestroyImmediate(fill.GetComponent<Collider>());fill.transform.position=new Vector3(side,-11,-.1f);fill.transform.localScale=new Vector3(2,22,1);fill.GetComponent<MeshRenderer>().sharedMaterial=black;fill.GetComponent<MeshRenderer>().sortingOrder=1;}
            var player=UnityEngine.Object.Instantiate(source.player.gameObject);player.name=source.player.name;world.player=player.transform;player.transform.position=new Vector3(5.5f,-15.3f,0);
            player.GetComponentInChildren<PaperSpritePlayer>().world=world;
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=data.orthographicSize;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.065f,.075f);camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transform.position=new Vector3(7.5f,-13,-10);camera.gameObject.AddComponent<AudioListener>();world.worldCamera=camera;
            var oldMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Unfin/Art/Region1BackgroundV3/Background.mat");var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Background.mat");if(material==null){material=new Material(oldMaterial);AssetDatabase.CreateAsset(material,Root+"/Background.mat");}else EditorUtility.CopySerialized(oldMaterial,material);
            var zones=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Zones.asset");if(zones==null){zones=new Texture2D(1,1,TextureFormat.RGBA32,false,true);zones.SetPixel(0,0,Color.red);zones.Apply();AssetDatabase.CreateAsset(zones,Root+"/Zones.asset");}material.SetTexture("_Zones",zones);material.SetVector("_Bounds",new Vector4(56,22,0,0));EditorUtility.SetDirty(material);
            var back=GameObject.CreatePrimitive(PrimitiveType.Quad);back.name="Prologue paper cave";UnityEngine.Object.DestroyImmediate(back.GetComponent<Collider>());back.transform.position=new Vector3(28,-11,5);back.transform.localScale=new Vector3(56,22,1);back.GetComponent<MeshRenderer>().sharedMaterial=material;back.GetComponent<MeshRenderer>().sortingOrder=-100;
            for(int i=0;i<decorations.Length;i++){var copy=UnityEngine.Object.Instantiate(decorations[i].gameObject);var layer=copy.GetComponent<RoomBackdropLayer>();layer.trackingCamera=camera;layer.roomBounds=new Vector4(3,-16,53,-7);layer.restPosition=new Vector3(9+i*17,decorations[i].restPosition.y+111.5f,decorations[i].restPosition.z);copy.transform.position=layer.restPosition;layer.Configure();}
            var gate=new GameObject("Torn page passage to Pencil Forest").AddComponent<SpriteRenderer>();gate.sprite=PaperPickupVisual.Load("station");gate.sortingOrder=-2;gate.transform.position=new Vector3(49,-14.3f,0);gate.transform.localScale=Vector3.one*(3.5f/gate.sprite.bounds.size.y);
            EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(sourceScene,true);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(WorldPrototypeBuilder.ScenePath,true),new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();Validate();
        }
        static void Check(bool value,string text){if(!value)throw new Exception("Prologue: "+text);}
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath);var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(w,null);w.Progression.UseAbilityLocks=true;
            var story=ProximityStory.Create(w);var flow=PrologueFlow.Create(w);
            Check(w.Data.isPrologue&&w.Data.startX==5&&w.Data.enemies.Length==0,"separate peaceful map");Check(story.Actors.Count==3&&story.GoneMask==0,"all apparitions initially present");
            var player=UnityEngine.Object.FindAnyObjectByType<PaperSpritePlayer>();player.Tick(.001f);
            Directory.CreateDirectory("Validation/prologue");Capture(w,"start",7.5f);
            // Follow the real motor to the exit. Holding jump is unnecessary; request only at the step.
            bool jumped=false;for(int i=0;i<4200;i++){
                if(w.Motor.x/16>16&&w.Motor.x/16<19&&w.Motor.grounded){w.RequestJump();jumped=true;}
                w.Simulate(1f/120,1);story.Tick(1f/120);flow.Tick(1f/120);
                if(flow.Leaving)break;
            }
            Check(jumped&&w.Motor.x/16>48&&flow.Leaving,"basic movement and jump reach exit without unlocks");Check(story.GoneMask==7,"all disappearance beats completed before exit");
            var save=w.CaptureSession();Check(save.inPrologue,"opening bookmark distinguished");story.Restore(0);w.RestoreSession(save);Check(story.GoneMask==7,"opening resume preserves effects");
            Capture(w,"exit",48);Check(EditorBuildSettings.scenes.Length>=2,"both scenes in native build");
            File.WriteAllText("Validation/prologue.json","{\"passed\":true,\"separateScene\":true,\"basicMovementRoute\":true,\"allDisappearances\":true,\"saveRestore\":true,\"exitFade\":true}");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);Debug.Log("UNFIN_PROLOGUE_OK");
        }
        static void Capture(WorldPrototype w,string name,float x)
        {
            var camera=w.worldCamera;camera.transform.position=new Vector3(x,-13.1f,-10);var rt=RenderTexture.GetTemporary(1920,1080,24);var previous=RenderTexture.active;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes("Validation/prologue/"+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
