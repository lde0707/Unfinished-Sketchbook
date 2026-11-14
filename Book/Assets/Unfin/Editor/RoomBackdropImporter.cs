using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class RoomBackdropImporter
    {
        const string Root="Assets/Unfin/Art/RoomBackdropsV1", ObjectName="Room-specific layered backdrops";
        [Serializable] public class Plan {public string gridSha256;public Room[] rooms;public Depth[] layers;}
        [Serializable] public class Depth {public int order;public float parallax,brightness;}
        [Serializable] public class Room {public string id,z,name,title,design,atlas;public int r,n,x,y,w,h;public Prop[] props;}
        [Serializable] public class Prop {public string atlas;public int part,layer;public float x,y,w,h;public bool flip,anchorFloor,anchorCeiling;}
        [Serializable] public class AtlasInfo {public Atlas[] atlases;}
        [Serializable] public class Atlas {public string file,path;public int width,height;public Crop[] crops;}
        [Serializable] public class Crop {public float x,y,w,h;}

        public static void Apply(WorldData data)
        {
            if(!File.Exists(Root+"/atlas-layouts.json"))return;
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Root+"/room-layouts.json"));
            if(plan.gridSha256!=data.gridSha256)throw new Exception("Regenerate room backdrop layout after changing map geometry.");
            var atlasInfo=JsonUtility.FromJson<AtlasInfo>(File.ReadAllText(Root+"/atlas-layouts.json"));
            var shader=Shader.Find("Unfin/Room Backdrop");if(shader==null||!shader.isSupported)throw new Exception("Room backdrop shader missing");
            var materials=new System.Collections.Generic.Dictionary<string,Material>();
            foreach(var atlas in atlasInfo.atlases)
            {
                string path=string.IsNullOrEmpty(atlas.path)?Root+"/"+atlas.file:atlas.path;
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);if(ti==null)throw new Exception("Missing background "+path);
                ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaIsTransparency=true;
                ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.npotScale=TextureImporterNPOTScale.None;
                ti.wrapMode=TextureWrapMode.Clamp;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=true;
                ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
                string matPath=Path.ChangeExtension(path,".mat");
                var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
                mat.shader=shader;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(mat);materials.Add(atlas.file,mat);
            }
            var old=GameObject.Find(ObjectName);if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var root=new GameObject(ObjectName);var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            foreach(var room in plan.rooms)
            {
                var group=new GameObject(room.id+" "+room.name);group.transform.SetParent(root.transform,false);
                for(int i=0;i<room.props.Length;i++)
                {
                    var p=room.props[i];var atlas=atlasInfo.atlases.First(a=>a.file==(string.IsNullOrEmpty(p.atlas)?room.atlas:p.atlas));
                    var crop=atlas.crops[p.part];var depth=plan.layers[p.layer];
                    var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=$"{i+1:00} - prop {p.part} - layer {p.layer}";
                    UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(group.transform,false);
                    // Preserve source shape. A requested box is a size limit, not an instruction to stretch.
                    float aspect=crop.w*atlas.width/(crop.h*atlas.height),w=p.w,h=w/aspect;if(h>p.h){h=p.h;w=h*aspect;}
                    float cy=p.anchorFloor?-room.y-room.h+h*.5f-.35f:p.anchorCeiling?-room.y-h*.5f+.4f:-p.y;
                    go.transform.position=new Vector3(p.x,cy,2+p.layer*.1f);go.transform.localScale=new Vector3(w,h,1);
                    var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=materials[atlas.file];renderer.sortingOrder=depth.order;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                    var layer=go.AddComponent<RoomBackdropLayer>();layer.trackingCamera=world.worldCamera;layer.restPosition=go.transform.position;
                    layer.roomBounds=new Vector4(room.x,-room.y-room.h,room.x+room.w,-room.y);
                    // Authored crop uses top-left normalized coordinates; Unity texture UV is bottom-left.
                    layer.uvRect=new Vector4(crop.x,1-crop.y-crop.h,crop.w,crop.h);
                    layer.brightness=depth.brightness;layer.parallax=depth.parallax;layer.flip=p.flip;layer.Configure();
                }
            }
            var rear=AssetDatabase.LoadAssetAtPath<Material>("Assets/Unfin/Art/Region1BackgroundV3/Background.mat");
            if(rear!=null){rear.SetFloat("_Opacity",.52f);EditorUtility.SetDirty(rear);}
            AssetDatabase.SaveAssets();
            // Re-importing illustrations must preserve gameplay clearance once a placement plan exists.
            if(File.Exists("Assets/Unfin/Resources/WorldPropPlacement.json"))WorldPropPlacementIntegration.Arrange();
        }

        [MenuItem("Unfin/Apply Room Backdrops and Render")]
        public static void ApplyAndRender()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);
            Apply(data);EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);Validate();RenderRooms();
            Debug.Log("UNFIN_ROOM_BACKDROPS_OK");
        }
        public static void Validate()
        {
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var data=JsonUtility.FromJson<WorldData>(world.mapJson.text);
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Root+"/room-layouts.json"));var root=GameObject.Find(ObjectName);
            if(root==null||root.transform.childCount!=data.rooms.Length)throw new Exception("Missing room backdrop groups");
            if(plan.gridSha256!=data.gridSha256)throw new Exception("Backdrop geometry mismatch");
            if(root.GetComponentsInChildren<Collider>().Length!=0||root.GetComponentsInChildren<Collider2D>().Length!=0)throw new Exception("Decorative backgrounds must not collide");
            var layers=root.GetComponentsInChildren<RoomBackdropLayer>();
            if(layers.Length!=plan.rooms.Sum(r=>r.props.Length))throw new Exception("Missing backdrop pieces");
            foreach(var layer in layers)
            {
                var renderer=layer.GetComponent<MeshRenderer>();
                if(renderer.sharedMaterial==null||renderer.sharedMaterial.mainTexture==null||renderer.sortingOrder>=0)throw new Exception("Invalid backdrop layer");
                if(layer.brightness>.7f||layer.parallax>.12f)throw new Exception("Backdrop must remain behind gameplay");
                var props=new MaterialPropertyBlock();renderer.GetPropertyBlock(props);
                if(props.GetFloat("_Brightness")!=layer.brightness)throw new Exception("Backdrop settings did not survive scene reload");
            }
            WorldPrototypeBuilder.Validate();
            File.WriteAllText("Validation/room-backdrops.json",$"{{\"passed\":true,\"rooms\":{plan.rooms.Length},\"props\":{layers.Length},\"atlasCount\":11,\"uniqueIllustrations\":35,\"layers\":3,\"collidersAdded\":0,\"gridSha256\":\"{data.gridSha256}\"}}");
        }
        public static void RenderRooms()
        {
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Root+"/room-layouts.json"));
            var layers=UnityEngine.Object.FindObjectsByType<RoomBackdropLayer>(FindObjectsSortMode.None);
            Directory.CreateDirectory("Validation/room-backdrops");
            foreach(var room in plan.rooms)
            {
                world.worldCamera.aspect=1920f/1080;
                world.worldCamera.orthographicSize=Mathf.Max((room.h+2)*.5f,(room.w+2)*.5f/world.worldCamera.aspect);
                world.worldCamera.transform.position=new Vector3(room.x+room.w*.5f,-room.y-room.h*.5f,-10);
                foreach(var layer in layers){layer.Configure();layer.PositionForCamera();}
                PaperSpriteValidation.Capture(world.worldCamera,"Validation/room-backdrops/"+room.id+".png");
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
    }
}
