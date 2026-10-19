using UnityEngine;
namespace Unfin {
 // Render a dark shaft over buried terrain, stopping before any other playable room.
 public sealed class InkHazardVisual:MonoBehaviour {
  Material material;Mesh mesh;
  public static void Create(WorldPrototype world){
   var shader=Resources.Load<Shader>("InkDepth");if(shader==null)return;
   var d=world.Data;var m=world.Motor;
   for(int y=0;y<d.height;y++)for(int x=0;x<d.width;x++){
    if(m.Get(x,y)!=3||m.Get(x,y-1)==3)continue;
    int start=x;while(x+1<d.width&&m.Get(x+1,y)==3&&m.Get(x+1,y-1)!=3)x++;
    // Each column stops at an underground room rather than painting across it.
    for(int col=start;col<=x;col++){
     float depth=0;for(int row=y;row<Mathf.Min(d.height,y+11);row++){
      int t=m.Get(col,row);if(row>y&&t!=1&&t!=3)break;depth=row-y+1-GridMotor.LiquidSurfaceInset;
     }
     var go=new GameObject("Deep ink "+col+","+y);go.transform.SetParent(world.transform,false);
     go.transform.position=new Vector3(col,-y-GridMotor.LiquidSurfaceInset,0);
     var v=go.AddComponent<InkHazardVisual>();v.material=new Material(shader);
     var tile=world.maps[(col>=d.regionX?3:0)].GetTile<UnityEngine.Tilemaps.Tile>(WorldData.Cell(col,y));
     if(tile!=null&&tile.sprite!=null){var s=tile.sprite;var r=s.rect;v.material.SetTexture("_MainTex",s.texture);v.material.SetVector("_PaintRect",new Vector4(r.x/s.texture.width,r.y/s.texture.height,r.width/s.texture.width,r.height/s.texture.height));}
     v.material.SetFloat("_Depth",depth);v.material.SetFloat("_Offset",col);
     v.mesh=new Mesh {vertices=new[]{new Vector3(0,.035f,0),new Vector3(1,.035f,0),new Vector3(0,-depth,0),new Vector3(1,-depth,0)},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,depth+.035f),new Vector2(1,depth+.035f)},triangles=new[]{0,1,2,2,1,3}};
     go.AddComponent<MeshFilter>().sharedMesh=v.mesh;
     var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=v.material;renderer.sortingOrder=30;
    }
   }
  }
  void OnDestroy(){if(material!=null)Destroy(material);if(mesh!=null)Destroy(mesh);}
 }
}
