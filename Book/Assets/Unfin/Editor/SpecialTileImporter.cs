using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Unfin.Editor
{
 public static class SpecialTileImporter
 {
  public const string Art="Assets/Unfin/Art/SpecialTilesV1",Assets="Assets/Unfin/SpecialTilesV1";
  public static readonly int[] Types={2,3,5,6,7,8,9,13,14,15,16,18};
  static readonly string[] Names={"erasable-scribble","hazard-ink","cuttable-vines","crumbling-platform","lever-gate","climbable-wax","deep-liquid","parry-lock","oneway-hatch","melting-platform","sticky-wax-floor","three-pigment-door"};
  public static Sprite SpriteFor(int type)=>AssetDatabase.LoadAssetAtPath<Sprite>($"{Assets}/sprite-{type}.asset");
  public static Matrix4x4 TransformFor(int type,Sprite sprite,bool interior=false){
   bool liquid=GridMotor.Liquid(type)&&!interior;
   float h=liquid?1-GridMotor.LiquidSurfaceInset:type==6?.25f:type==15?.3f:1;
   return Matrix4x4.TRS(new Vector3(0,liquid?(h-1)*.5f:(1-h)*.5f,0),Quaternion.identity,new Vector3(1/sprite.bounds.size.x,h/sprite.bounds.size.y,1));}
  public static void Generate(){
   Directory.CreateDirectory(Assets);AssetDatabase.Refresh();
   for(int i=0;i<Types.Length;i++){
    int type=Types[i];string path=$"{Art}/{type:D2}-{Names[i]}.png";
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing special tile "+path);
    importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
    importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=texture.GetPixels32();int x0=texture.width,y0=texture.height,x1=0,y1=0,clear=0;
    for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++){
     if(px[y*texture.width+x].a<16){clear++;continue;}x0=Math.Min(x0,x);x1=Math.Max(x1,x);y0=Math.Min(y0,y);y1=Math.Max(y1,y);
    }
    if(clear<texture.width||x1<=x0||y1<=y0)throw new Exception("Missing usable alpha "+path);
    var rect=new Rect(x0,y0,x1-x0+1,y1-y0+1);SaveSprite(type,texture,rect,false);
    if(type==3||type==9)SaveSprite(type,texture,new Rect(rect.x,rect.y,rect.width,rect.height*.65f),true);
    for(int region=1;region<=2;region++){
     MakeTile(region,type,false);if(type==3||type==9)MakeTile(region,type,true);
    }
   }
   AssetDatabase.SaveAssets();
  }
  static void SaveSprite(int type,Texture2D texture,Rect rect,bool interior){
   string path=$"{Assets}/sprite-{type}{(interior?"-fill":"")}.asset";
   var next=Sprite.Create(texture,rect,new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);next.name=$"Special {type} {(interior?"fill":"surface")}";
   var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   if(existing==null)AssetDatabase.CreateAsset(next,path);else{EditorUtility.CopySerialized(next,existing);UnityEngine.Object.DestroyImmediate(next);EditorUtility.SetDirty(existing);}
  }
  static string TilePath(int region,int type,bool interior)=>$"{Assets}/r{region}-t{type}{(interior?"-fill":"")}.asset";
  static void MakeTile(int region,int type,bool interior){
   string path=TilePath(region,type,interior);var tile=AssetDatabase.LoadAssetAtPath<PrototypeTile>(path);
   if(tile==null){tile=ScriptableObject.CreateInstance<PrototypeTile>();AssetDatabase.CreateAsset(tile,path);}
   tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>($"{Assets}/sprite-{type}{(interior?"-fill":"")}.asset");
   tile.htmlType=type;tile.region=region;tile.patternVariant=interior?1:0;tile.exposedEdges=15;tile.color=Color.white;tile.flags=TileFlags.None;
   tile.colliderType=GridMotor.Liquid(type)?Tile.ColliderType.None:Tile.ColliderType.Grid;tile.transform=TransformFor(type,tile.sprite,interior);EditorUtility.SetDirty(tile);
  }
  public static void Apply(WorldPrototype world){
   var d=JsonUtility.FromJson<WorldData>(world.mapJson.text);var cells=d.Decode();int count=0;
   for(int y=0;y<d.height;y++)for(int x=0;x<d.width;x++){
    int type=cells[y*d.width+x];if(type==0||type==1||GridMotor.Platform(type))continue;
    int r=x>=d.regionX?2:1;bool interior=(type==3||type==9)&&y>0&&cells[(y-1)*d.width+x]==type;
    var tile=AssetDatabase.LoadAssetAtPath<PrototypeTile>(TilePath(r,type,interior));if(tile==null)throw new Exception("Unmapped type "+type);
    var map=world.maps[(r-1)*3+WorldPrototype.Layer(type)];var cell=WorldData.Cell(x,y);map.SetTile(cell,tile);map.SetTileFlags(cell,TileFlags.None);map.SetTransformMatrix(cell,tile.transform);map.SetColor(cell,Color.white);count++;
   }
   Debug.Log("UNFIN_SPECIAL_TILES_APPLIED "+count);
  }
  public static void ApplyValidateAndBuild(){
   RegionalPlatformImporter.Generate();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
   var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();RegionalPlatformImporter.Apply(world);Apply(world);
   EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
   Validate();WorldPrototypeBuilder.Validate();Validate();WebGLBuilder.Build();
  }
  public static void Validate(){
   var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);var cells=data.Decode();int n=0,platforms=0;
   for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++){
    int type=cells[y*data.width+x];if(type==0||type==1)continue;int r=x>=data.regionX?1:0;
    var tile=world.maps[r*3+WorldPrototype.Layer(type)].GetTile<PrototypeTile>(WorldData.Cell(x,y));
    if(tile==null||tile.sprite==null||tile.htmlType!=type)throw new Exception("Tile mapping mismatch");
    if(type==4){platforms++;if(Mathf.Abs(tile.transform.m11-1.25f)>.001f)throw new Exception("Platform thickness");}
    else if(!AssetDatabase.GetAssetPath(tile.sprite).StartsWith(Assets))throw new Exception("Old special sprite remains");
    var actual=world.maps[r*3+WorldPrototype.Layer(type)].GetTransformMatrix(WorldData.Cell(x,y));
    if(actual!=tile.transform)throw new Exception($"Saved cell transform mismatch {x},{y} type {type}");
    var top=actual.MultiplyPoint3x4(new Vector3(0,tile.sprite.bounds.max.y,0));
    // Ordinary sprites have transparent padding: their canvas top is the stable anchor.
    float expectedTop=GridMotor.Liquid(type)&&tile.patternVariant==0?.5f-GridMotor.LiquidSurfaceInset:.5f;
    if(Mathf.Abs(top.y-expectedTop)>.001f)throw new Exception("Changed top contact anchor");n++;
   }
   RegionalPlatformImporter.Validate(world);
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/special-tiles.json",$"{{\"passed\":true,\"types\":12,\"cells\":{n},\"normalPlatforms\":{platforms},\"thicknessScale\":1.25,\"gridSha256\":\"{data.gridSha256}\"}}");
   Debug.Log("UNFIN_SPECIAL_TILES_OK 12 types / actual cell transforms / lowered liquid surfaces / original grid");
  }
  public static void ValidateBehaviour(){
   EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
   Validate();
   var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   typeof(WorldPrototype).GetMethod("Awake",flags).Invoke(world,null);
   int liquidIndex=Array.IndexOf(world.Motor.cells,(byte)3),lx=liquidIndex%world.Data.width,ly=liquidIndex/world.Data.width;
   var probe=new GridMotor(world.Data,world.Data.Decode()){x=lx*16+2,y=ly*16-GridMotor.Height-.2f,vy=80};
   probe.Move(.05f);
   if(probe.grounded||probe.InLiquid(probe.x+6,probe.y+GridMotor.Height))throw new Exception("Liquid blocks falling or triggers above surface");
   probe.Move(.03f);
   if(probe.grounded||!probe.InLiquid(probe.x+6,probe.y+GridMotor.Height))throw new Exception("Liquid fails to trigger after entering lowered surface");
   var schedule=typeof(WorldPrototype).GetMethod("Schedule",flags);var tick=typeof(WorldPrototype).GetMethod("TickChanges",flags);
   foreach(int type in new[]{6,15}){
    int index=Array.IndexOf(world.Motor.cells,(byte)type),x=index%world.Data.width,y=index/world.Data.width;
    var map=world.maps[(x>=world.Data.regionX?3:0)+1];var cell=WorldData.Cell(x,y);var tile=map.GetTile<PrototypeTile>(cell);
    float delay=type==6?.45f:1.5f,restore=type==6?2.6f:3;
    schedule.Invoke(world,new object[]{x,y,type});tick.Invoke(world,new object[]{delay*.5f});
    if(world.Motor.Get(x,y)!=type||map.GetTile(cell)!=tile||map.GetColor(cell).a>=1)throw new Exception("Missing platform warning");
    if(Mathf.Abs(map.GetTransformMatrix(cell).m13-tile.transform.m13)>.001f)throw new Exception("Warning moved contact height");
    tick.Invoke(world,new object[]{delay*.5f+.01f});
    if(world.Motor.Get(x,y)!=0||map.GetTile(cell)!=null)throw new Exception("Platform failed to disappear");
    tick.Invoke(world,new object[]{restore-.1f});if(map.GetTile(cell)!=null)throw new Exception("Platform restored too early");
    tick.Invoke(world,new object[]{.11f});
    if(world.Motor.Get(x,y)!=type||map.GetTile(cell)!=tile||map.GetColor(cell)!=Color.white||map.GetTransformMatrix(cell)!=tile.transform)throw new Exception("Platform restore mismatch");
   }
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/special-platform-behaviour.json","{\"passed\":true,\"types\":[6,15],\"warning\":true,\"fixedContactHeight\":true,\"disappear\":true,\"restore\":true}");
   File.WriteAllText("Validation/special-liquid-geometry.json","{\"passed\":true,\"surfaceInsetTiles\":0.35,\"noSolidLiquidCollision\":true,\"triggersBelowSurfaceOnly\":true,\"savedSceneTransformsVerified\":true}");
   Debug.Log("UNFIN_SPECIAL_PLATFORM_BEHAVIOUR_OK / LIQUID_ENTRY_OK");
  }
 }
}
