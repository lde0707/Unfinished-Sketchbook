using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Unfin.Editor
{
    // Keep navigation and all progression coordinates intact; fit artwork into available space.
    public static class WorldPropPlacementIntegration
    {
        [Serializable] class Change {public string name,action;public Vector3 before,after;public float scale;}
        [Serializable] class Report {public bool passed;public int devices,buildings,movedBuildings,hiddenBuildings;public string gridSha256;public Change[] changes;}
        static WorldData data;static byte[] grid;
        static readonly List<Rect> occupied=new List<Rect>();static readonly List<Change> changes=new List<Change>();
        static Rect Box(Vector3 p,float width,float height)=>new Rect(p.x-width*.5f,p.y-height*.5f,width,height);
        static bool Intersects(Rect a,Rect b,float gap=.08f)=>a.xMin<b.xMax+gap&&a.xMax>b.xMin-gap&&a.yMin<b.yMax+gap&&a.yMax>b.yMin-gap;
        static bool Free(Rect rect){
            int x0=Mathf.FloorToInt(rect.xMin-.06f),x1=Mathf.FloorToInt(rect.xMax+.06f),y0=Mathf.FloorToInt(-rect.yMax-.06f),y1=Mathf.FloorToInt(-rect.yMin+.06f);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
                if(x<0||y<0||x>=data.width||y>=data.height)return false;
                if(grid[y*data.width+x]!=0&&Intersects(rect,new Rect(x,-y-1,1,1),.035f))return false;
            }
            return !occupied.Any(r=>Intersects(rect,r));
        }
        static Rect DeviceBox(WorldMarker p,Vector3 pos,float height,float aspect){
            var rect=Box(pos,height*aspect,height);
            // The grounded bases may tuck into the irregular floor; hanging portal ribbon is not traversable artwork.
            if(p.type=="station"||p.type=="save"||PaperDeviceVisual.Grounded(p))rect.yMin=-p.floor+.12f;
            else {rect.yMin-=.05f;rect.yMax+=.05f;}
            return rect;
        }
        public static void Arrange(){
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();data=JsonUtility.FromJson<WorldData>(w.mapJson.text);CheckpointPlan.Apply(data);grid=data.Decode();occupied.Clear();changes.Clear();
            var entries=new List<PropPlacement>();
            foreach(var p in data.pickups.Where(p=>p.type=="save"||PaperPickupVisual.Supports(p.type)||PaperDeviceVisual.Supports(p.type)).OrderBy(p=>p.type=="station"?0:p.type=="save"?1:PaperDeviceVisual.Supports(p.type)?2:3)){
                bool device=PaperDeviceVisual.Supports(p.type),grounded=device&&PaperDeviceVisual.Grounded(p);
                string art=device?PaperDeviceVisual.ArtName(p):p.type.StartsWith("sw_")?"pigment":p.type;var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Unfin/Resources/"+(device?"DeviceArt/":"PickupArt/")+art+".png");float aspect=sprite.bounds.size.x/sprite.bounds.size.y;
                float original=device?PaperDeviceVisual.DefaultHeight(p):p.type=="station"?3.55f:p.type=="save"?SaveShrineVisual.Height:p.type=="healup"?.85f:.65f;
                Vector3 Origin(float h)=>p.type=="station"?new Vector3((p.x+p.w*.5f)/16,-p.floor+h*.5f-.37f*(h/3.55f),0):p.type=="save"||grounded?new Vector3((p.x+p.w*.5f)/16,-p.floor+h*.5f-.035f,0):data.Center(p);
                float min=p.type=="station"?1.35f:p.type=="save"?1.1f:.32f;Vector3 result=Origin(original);float height=original;bool found=false;
                for(float h=original;h>=min-.001f&&!found;h-=.08f){foreach(float dy in p.type=="station"||p.type=="save"||grounded?new[]{0f}:new[]{0f,.12f,.24f,.5f,.75f,1f,1.25f,1.5f,1.75f,2f})foreach(float dx in new[]{0f,-.2f,.2f,-.4f,.4f}){
                    if(found)break;
                    var pos=Origin(h)+new Vector3(dx,dy,0);if(!Free(DeviceBox(p,pos,h,aspect)))continue;result=pos;height=h;found=true;break;
                }}
                if(!found)throw new Exception("No clear pickup placement within interaction reach: "+p.type+" "+p.tx+","+p.floor);
                occupied.Add(DeviceBox(p,result,height,aspect));entries.Add(new PropPlacement{kind=p.type,html=new Vector2(p.x,p.y),position=result,height=height});
                if(height<original-.01f||Vector3.Distance(result,Origin(original))>.01f)changes.Add(new Change{name=p.type+" "+p.tx+","+p.floor,action="fit device",before=Origin(original),after=result,scale=height/original});
            }
            // Keep decorative architecture clear of all authored interactions, including NPCs, diaries and levers.
            foreach(var p in data.pickups.Where(p=>p.type!="save"&&!PaperPickupVisual.Supports(p.type)&&!PaperDeviceVisual.Supports(p.type)))occupied.Add(Box(data.Center(p),p.w/16,p.h/16));
            int buildings=0,moved=0,hidden=0;
            foreach(var layer in UnityEngine.Object.FindObjectsByType<RoomBackdropLayer>(FindObjectsSortMode.None).OrderByDescending(l=>l.GetComponent<MeshRenderer>().sortingOrder).ThenBy(l=>l.name)){
                buildings++;var before=layer.restPosition;var size=layer.transform.localScale;Vector3 best=before;float bestScale=1;bool found=false;
                foreach(float factor in new[]{1f,.85f,.7f,.55f,.4f,.3f}){
                    float width=size.x*factor,height=size.y*factor;if(width<1.2f||height<1.2f)continue;
                    float left=layer.roomBounds.x+width*.5f+.15f,right=layer.roomBounds.z-width*.5f-.15f,bottom=layer.roomBounds.y+height*.5f+.15f,top=layer.roomBounds.w-height*.5f-.15f;
                    float score=float.MaxValue;
                    void Try(float x,float y){var pos=new Vector3(x,y,before.z);if(x<left||x>right||y<bottom||y>top||!Free(Box(pos,width,height)))return;float d=(pos-before).sqrMagnitude;if(d<score){score=d;best=pos;bestScale=factor;found=true;}}
                    Try(before.x,before.y);for(float y=bottom;y<=top;y+=.65f)for(float x=left;x<=right;x+=.65f)Try(x,y);
                    if(found)break;
                }
                var renderer=layer.GetComponent<MeshRenderer>();
                if(!found){renderer.enabled=false;hidden++;changes.Add(new Change{name=layer.transform.parent.name+"/"+layer.name,action="omit crowded duplicate",before=before,after=before,scale=0});continue;}
                renderer.enabled=true;layer.restPosition=best;layer.transform.position=best;layer.transform.localScale=size*bestScale;occupied.Add(Box(best,size.x*bestScale,size.y*bestScale));
                if(Vector3.Distance(before,best)>.01f||bestScale<.99f){moved++;changes.Add(new Change{name=layer.transform.parent.name+"/"+layer.name,action="place building in clear space",before=before,after=best,scale=bestScale});}
            }
            File.WriteAllText("Assets/Unfin/Resources/WorldPropPlacement.json",JsonUtility.ToJson(new PropPlacementPlan{entries=entries.ToArray()},true));AssetDatabase.Refresh();WorldPropPlacement.ClearCache();
            foreach(var m in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None)){
                if(m.enemy)continue;var entry=entries.FirstOrDefault(e=>e.kind==m.kind&&Vector2.Distance(e.html,m.htmlPosition)<.01f);if(entry==null)continue;m.transform.position=entry.position;var s=m.GetComponent<SpriteRenderer>().sprite;m.transform.localScale=Vector3.one*(entry.height/s.bounds.size.y);
            }
            EditorSceneManager.SaveScene(w.gameObject.scene);AssetDatabase.SaveAssets();Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/prop-placement.json",JsonUtility.ToJson(new Report{passed=true,devices=entries.Count,buildings=buildings,movedBuildings=moved,hiddenBuildings=hidden,gridSha256=data.gridSha256,changes=changes.ToArray()},true));
            Debug.Log("UNFIN_PROP_PLACEMENT_OK "+entries.Count+" devices / "+moved+" buildings adjusted / "+hidden+" crowded duplicates omitted / map topology preserved");
        }
        public static void RunAndBuild(){PickupArtIntegration.Apply();Arrange();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
    }
}
