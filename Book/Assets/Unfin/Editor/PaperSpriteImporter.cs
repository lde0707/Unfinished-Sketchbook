using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PaperSpriteImporter
    {
        public const string Root="Assets/Unfin/Art/PlayerSprites";
        public const string AnimRoot="Assets/Unfin/Animations/PlayerSprites";
        [Serializable] class Manifest { public float pixelsPerUnit; public SourceClip[] clips; }
        [Serializable] class SourceClip { public string name; public float anchorX,groundY; public int width,height; public bool loop; public SourceFrame[] frames; }
        [Serializable] class SourceFrame { public string file,sha256; public float duration,headX,headY; }
        public static PaperSpriteLibrary Generate()
        {
            AssetDatabase.Refresh();Directory.CreateDirectory(AnimRoot);
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/manifest.json"));
            var library=AssetDatabase.LoadAssetAtPath<PaperSpriteLibrary>(AnimRoot+"/PlayerSprites.asset");
            if(library==null){library=ScriptableObject.CreateInstance<PaperSpriteLibrary>();AssetDatabase.CreateAsset(library,AnimRoot+"/PlayerSprites.asset");}
            var clips=new List<PaperSpriteClip>();
            foreach(var source in manifest.clips)
            {
                var frames=new List<PaperSpriteFrame>();
                foreach(var f in source.frames)
                {
                    string path=Root+"/"+f.file;
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.spritePixelsPerUnit=manifest.pixelsPerUnit;importer.alphaIsTransparency=true;
                    ConfigureTextureQuality(importer,source.width,source.height);
                    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                    settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(source.anchorX/source.width,1-source.groundY/source.height);
                    settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
                    frames.Add(new PaperSpriteFrame{sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path),duration=f.duration,head=new Vector2(f.headX,f.headY)});
                }
                clips.Add(new PaperSpriteClip{name=source.name,anchorX=source.anchorX,groundY=source.groundY,loop=source.loop,frames=frames.ToArray()});
            }
            PaperSpriteClip Slice(string name,string from,int first,int count,bool reverse=false)
            {
                var original=clips.Find(c=>c.name==from);var frames=original.frames.Skip(first).Take(count);
                if(reverse)frames=frames.Reverse();
                return new PaperSpriteClip{name=name,anchorX=original.anchorX,groundY=original.groundY,frames=frames.ToArray()};
            }
            clips.Add(Slice("RunStart","RunStop",0,4,true));
            clips.Add(Slice("JumpLand","Jump",6,3));clips.Add(Slice("MovingLand","MovingJump",4,2));
            clips.Add(Slice("FallLand","Fall",3,2));
            var fall=clips.Find(c=>c.name=="Fall");fall.frames=fall.frames.Take(3).ToArray();
            fall.frames[0].duration=.065f;fall.frames[1].duration=.09f;fall.frames[2].duration=.14f;
            var landing=clips.Find(c=>c.name=="FallLand");foreach(var f in landing.frames)f.duration=.065f;
            library.pixelsPerUnit=manifest.pixelsPerUnit;library.clips=clips.ToArray();EditorUtility.SetDirty(library);
            var effectPath=Root+"/Effects/thrust.png";var effect=(TextureImporter)AssetImporter.GetAtPath(effectPath);
            effect.textureType=TextureImporterType.Sprite;effect.spriteImportMode=SpriteImportMode.Single;effect.spritePixelsPerUnit=256;
            effect.alphaIsTransparency=true;effect.mipmapEnabled=false;effect.filterMode=FilterMode.Bilinear;effect.wrapMode=TextureWrapMode.Clamp;
            effect.textureCompression=TextureImporterCompression.Uncompressed;effect.maxTextureSize=1024;
            var effectSettings=new TextureImporterSettings();effect.ReadTextureSettings(effectSettings);effectSettings.spriteAlignment=(int)SpriteAlignment.Custom;
            effectSettings.spritePivot=new Vector2(.28f,1-.16f/.3f);effectSettings.spriteMeshType=SpriteMeshType.FullRect;effect.SetTextureSettings(effectSettings);effect.SaveAndReimport();
            library.thrustEffect=AssetDatabase.LoadAssetAtPath<Sprite>(effectPath);
            // Editable sprite-swap clips are also provided for Animator-based consumers.
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimRoot+"/PaperSprites.controller");
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(AnimRoot+"/PaperSprites.controller");
            var machine=controller.layers[0].stateMachine;foreach(var old in machine.states)machine.RemoveState(old.state);
            foreach(var c in clips)
            {
                string path=AnimRoot+"/"+c.name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.ClearCurves();clip.frameRate=1000;
                var keys=new List<ObjectReferenceKeyframe>();float t=0;
                foreach(var f in c.frames){if(f.duration>0)keys.Add(new ObjectReferenceKeyframe{time=t,value=f.sprite});t+=f.duration;}
                keys.Add(new ObjectReferenceKeyframe{time=t,value=c.frames[c.loop?0:c.frames.Length-1].sprite});
                AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=c.loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
                var state=machine.AddState(c.name);state.motion=clip;state.writeDefaultValues=false;if(c.name=="Idle")machine.defaultState=state;
                EditorUtility.SetDirty(clip);
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();return library;
        }
        public static void ConfigureTextureQuality(TextureImporter importer,int width,int height)
        {
            // These drawings are reduced on screen; filtered mip levels prevent
            // fine paper lines and alpha silhouettes from shimmering as they move.
            int size=Mathf.NextPowerOfTwo(Mathf.Max(width,height));
            importer.mipmapEnabled=true;importer.ignoreMipmapLimit=true;
            importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=size;
            var web=importer.GetPlatformTextureSettings("WebGL");
            web.overridden=true;web.maxTextureSize=size;web.format=TextureImporterFormat.RGBA32;
            web.textureCompression=TextureImporterCompression.Uncompressed;web.crunchedCompression=false;
            importer.SetPlatformTextureSettings(web);
        }
        public static void Attach(WorldPrototype world)
        {
            var library=AssetDatabase.LoadAssetAtPath<PaperSpriteLibrary>(AnimRoot+"/PlayerSprites.asset");if(library==null)library=Generate();
            foreach(string name in new[]{"Paper Player C","Paper Sprite Player"}){var old=world.player.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var placeholder=world.player.GetComponent<SpriteRenderer>();if(placeholder!=null)placeholder.enabled=false;
            world.player.localScale=Vector3.one;
            var go=new GameObject("Paper Sprite Player");go.transform.SetParent(world.player,false);go.transform.localPosition=new Vector3(0,-.6875f,0);
            SpriteRenderer Make(string name,int order,float alpha)
            {
                var child=new GameObject(name);child.transform.SetParent(go.transform,false);
                var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=order;renderer.color=new Color(1,1,1,alpha);return renderer;
            }
            var player=go.AddComponent<PaperSpritePlayer>();player.library=library;player.visual=Make("Visual",27,1);
            player.attackEffect=Make("Thrust effect",26,1);player.attackEffect.enabled=false;
            player.trails=new[]{Make("Dash trail near",26,.12f),Make("Dash trail far",25,.06f)};player.Render(1);
            Directory.CreateDirectory("Assets/Unfin/Prefabs");PrefabUtility.SaveAsPrefabAsset(go,"Assets/Unfin/Prefabs/PaperSpritePlayer.prefab");player.world=world;
        }
        [MenuItem("Unfin/Apply Current Player Sprites")]
        public static void Apply()
        {
            Generate();RegionalPlatformImporter.Generate();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();Attach(world);RegionalPlatformImporter.Apply(world);
            EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
            Validate();RegionalPlatformImporter.Validate(world);PaperSpriteValidation.Run();PlayerCombatValidation.Run();WorldPrototypeBuilder.Validate();Debug.Log("UNFIN_SPRITES_APPLIED");
        }
        public static void Validate()
        {
            var library=AssetDatabase.LoadAssetAtPath<PaperSpriteLibrary>(AnimRoot+"/PlayerSprites.asset");
            if(library.clips.Length!=21)throw new Exception("Expected 21 sprite sequences");
            foreach(var c in library.clips)foreach(var f in c.frames)
                if(f.sprite==null||f.duration<0||f.sprite.pixelsPerUnit!=256)throw new Exception("Invalid sprite: "+c.name);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            if(world.player.GetComponentsInChildren<PaperSpritePlayer>().Length!=1)throw new Exception("Legacy rig still attached");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/player-sprites-import.json","{\"passed\":true,\"sourceFrames\":90,\"sequences\":21,\"pixelsPerUnit\":256,\"legacyRigAttached\":false}");
        }
    }
}
