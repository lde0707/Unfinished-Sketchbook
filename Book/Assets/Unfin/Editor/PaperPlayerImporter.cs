using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PaperPlayerImporter
    {
        const string Root="Assets/Unfin/Art/PlayerC";
        const string AnimRoot="Assets/Unfin/Animations/PlayerC";
        public static readonly string[] Names={"Idle","Walk","Run","RunStart","RunStop","Jump","AirRise","AirApex","AirFall","Land","Dash","WallSlide","Turn","Attack1","Attack2","Attack3","Hurt","Death"};
        static readonly int[] Frames={120,60,48,12,18,10,30,24,48,18,12,60,12,30,34,40,24,75};
        static readonly HashSet<string> Loops=new HashSet<string>{"Idle","Walk","Run","AirRise","AirApex","AirFall","WallSlide"};
        static readonly string[] Channels={"bob","lean","squash","frontStep","backStep","frontLift","backLift","armSwing","armReach","headTilt","cloth","clothWave","collapse"};

        // Authoring data is deliberately separate from motor and collision rules.
        static float[] Pose(string name,float t)
        {
            var p=new float[Channels.Length];
            float tau=t*Mathf.PI*2,s=Mathf.Sin(tau),lag=Mathf.Sin(tau-.4f),pulse=Mathf.Sin(Mathf.PI*t);
            switch(name) {
                case "Idle": p[0]=.009f*s; p[9]=1.1f*Mathf.Sin(tau-.2f); p[10]=.015f*lag; p[11]=.009f*Mathf.Sin(tau-.6f); break;
                case "Walk": case "Run": {
                    bool run=name=="Run"; float step=run?.20f:.12f;
                    Foot(t,step,out p[3],out p[5]); Foot((t+.5f)%1,step,out p[4],out p[6]);
                    p[0]=.014f*(1-Mathf.Cos(2*tau)); p[1]=run?-7:-3;
                    p[7]=32*s; p[9]=2*Mathf.Sin(tau-.2f);
                    p[10]=(run?-.11f:-.045f)+.028f*lag; p[11]=.025f*Mathf.Sin(tau-.7f); break;
                }
                case "RunStart": p[1]=-10*pulse; p[2]=.045f*pulse; p[3]=.12f*t; p[4]=-.12f*t; p[7]=-25*t; p[10]=-.12f*t; break;
                case "RunStop": p[1]=7*pulse; p[2]=.045f*pulse; p[3]=.12f*(1-t); p[4]=-.12f*(1-t); p[10]=-.10f*Mathf.Cos(t*Mathf.PI/2); p[11]=.035f*pulse; break;
                case "Jump": p[2]=-.055f*pulse; p[3]=.10f; p[4]=-.10f; p[5]=.08f*t; p[6]=.05f*t; p[7]=-20; p[10]=-.07f; p[11]=-.05f*pulse; break;
                case "AirRise": p[2]=-.035f; p[3]=.12f; p[4]=-.08f; p[5]=.08f; p[6]=.06f; p[7]=-22; p[1]=-3; p[10]=-.07f+.012f*s; p[11]=-.04f+.008f*lag; break;
                case "AirApex": p[3]=.09f; p[4]=-.10f; p[5]=.08f; p[6]=.04f; p[7]=-12; p[10]=-.09f+.015f*s; p[11]=.04f+.012f*lag; break;
                case "AirFall": p[1]=4; p[3]=.08f; p[4]=-.07f; p[5]=.02f; p[7]=-40; p[10]=-.08f+.015f*s; p[11]=.17f+.015f*lag; p[9]=-3; break;
                case "Land": p[2]=.10f*Mathf.Pow(1-t,2); p[1]=-6*pulse; p[3]=.06f*(1-t); p[4]=-.06f*(1-t); p[10]=-.06f*(1-t); p[11]=-.08f*pulse; p[9]=-4*pulse; break;
                case "Dash": p[0]=-.05f; p[1]=-20; p[2]=.055f; p[3]=.18f; p[4]=-.18f; p[5]=.08f; p[6]=.10f; p[7]=38; p[9]=-7; p[10]=-.27f-.02f*pulse; p[11]=.22f; break;
                case "WallSlide": p[1]=-5; p[3]=.15f; p[4]=.03f; p[5]=.10f; p[6]=.03f; p[7]=100; p[8]=.05f; p[10]=-.02f+.014f*s; p[11]=.10f+.012f*lag; break;
                case "Turn": p[2]=.04f*pulse; p[1]=6*pulse; p[9]=-5*pulse; p[10]=.09f*pulse; p[11]=.03f*pulse; break;
                case "Attack1": case "Attack2": case "Attack3": {
                    float strength=name=="Attack3"?1.25f:1;
                    // Anticipation 0..0.24, quick swing .24..0.40, follow-through and recovery.
                    float wind=Smooth(0,.24f,t)*(1-Smooth(.24f,.34f,t));
                    float strike=Smooth(.24f,.40f,t)*(1-Smooth(.55f,1,t));
                    float recoil=Smooth(.42f,.62f,t)*(1-Smooth(.62f,1,t));
                    p[1]=8*wind-15*strike*strength; p[2]=.04f*wind+.03f*strike;
                    p[7]=-38*wind+(name=="Attack2"?135:95)*strike;
                    p[8]=.14f*strike*strength; p[3]=.11f*strike; p[4]=-.10f*strike;
                    p[9]=5*wind-7*strike; p[10]=.035f*wind-.15f*recoil; p[11]=.06f*recoil;
                    break;
                }
                case "Hurt": p[1]=16*pulse; p[0]=-.035f*pulse; p[2]=.06f*pulse; p[9]=10*pulse; p[7]=-35*pulse; p[10]=.10f*Mathf.Sin(Mathf.PI*t); break;
                case "Death": p[12]=-76*Smooth(.1f,.85f,t); p[2]=.08f*Smooth(0,.5f,t); p[7]=25*Smooth(0,.7f,t); p[10]=-.1f*pulse; p[9]=12*Smooth(.3f,1,t); break;
            }
            return p;
        }
        static float Smooth(float a,float b,float t) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        static void Foot(float phase,float stride,out float x,out float y)
        {
            phase=Mathf.Repeat(phase,1);
            if(phase<.5f) { x=stride*(1-4*phase); y=0; }
            else { float q=(phase-.5f)*2; x=stride*(2*Mathf.SmoothStep(0,1,q)-1); y=.11f*Mathf.Sin(q*Mathf.PI); }
        }
        static void Generate()
        {
            Directory.CreateDirectory(AnimRoot); AssetDatabase.Refresh();
            foreach(string part in new[]{"head","body","cape","arm","leg","collar"}) {
                string path=Root+"/"+part+".png";
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);
                if(imp==null) throw new Exception("Missing player artwork: "+path);
                imp.textureType=TextureImporterType.Default; imp.sRGBTexture=true; imp.alphaIsTransparency=true;
                imp.wrapMode=TextureWrapMode.Clamp; imp.filterMode=FilterMode.Bilinear;
                imp.mipmapEnabled=false; imp.npotScale=TextureImporterNPOTScale.None;
                imp.maxTextureSize=1024; imp.textureCompression=TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                string mp=Root+"/"+part+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
                if(mat==null) { mat=new Material(Shader.Find("Unfin/Paper Player")); AssetDatabase.CreateAsset(mat,mp); }
                mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path); EditorUtility.SetDirty(mat);
            }
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimRoot+"/PaperPlayer.controller");
            if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(AnimRoot+"/PaperPlayer.controller");
            var machine=controller.layers[0].stateMachine;
            foreach(var old in machine.states) machine.RemoveState(old.state);
            for(int n=0;n<Names.Length;n++) {
                string name=Names[n],path=AnimRoot+"/"+name+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null) { clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,path); }
                clip.ClearCurves(); clip.name=name; clip.frameRate=60;
                for(int c=0;c<Channels.Length;c++) {
                    var keys=new Keyframe[Frames[n]+1];
                    for(int i=0;i<=Frames[n];i++) keys[i]=new Keyframe(i/60f,Pose(name,(float)i/Frames[n])[c]);
                    var curve=new AnimationCurve(keys);
                    for(int i=0;i<keys.Length;i++) {
                        AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
                    }
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(PaperPlayerRig),Channels[c]),curve);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=Loops.Contains(name); settings.loopBlend=false;
                AnimationUtility.SetAnimationClipSettings(clip,settings); EditorUtility.SetDirty(clip);
                var state=machine.AddState(name,new Vector3((n%4)*200,(n/4)*65)); state.motion=clip; state.writeDefaultValues=false;
                if(n==0) machine.defaultState=state;
            }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        }
        public static void Attach(WorldPrototype world)
        {
            var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimRoot+"/PaperPlayer.controller");
            if(controller==null) Generate();
            controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimRoot+"/PaperPlayer.controller");
            var previous=world.player.Find("Paper Player C"); if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var placeholder=world.player.GetComponent<SpriteRenderer>(); if(placeholder!=null) placeholder.enabled=false;
            world.player.localScale=Vector3.one;
            var go=new GameObject("Paper Player C"); go.transform.SetParent(world.player,false); go.transform.localPosition=new Vector3(0,-.6875f,0);
            var rig=go.AddComponent<PaperPlayerRig>();
            rig.head=Mat("head"); rig.body=Mat("body"); rig.cape=Mat("cape"); rig.arm=Mat("arm"); rig.leg=Mat("leg"); rig.collar=Mat("collar"); rig.Rebuild();
            var animator=go.AddComponent<Animator>(); animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var driver=go.AddComponent<PaperPlayerDriver>(); driver.world=world; driver.animator=animator; driver.rig=rig;
            Directory.CreateDirectory("Assets/Unfin/Prefabs");
            // A reusable standalone visual prefab does not serialize a scene reference.
            driver.world=null; PrefabUtility.SaveAsPrefabAsset(go,"Assets/Unfin/Prefabs/PaperPlayerC.prefab"); driver.world=world;
        }
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");

        [MenuItem("Unfin/Build Paper Player Animations and Preview")]
        public static void BuildAndPreview()
        {
            Generate(); EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>(); Attach(world);
            EditorSceneManager.SaveScene(world.gameObject.scene); AssetDatabase.SaveAssets();
            WorldPrototypeBuilder.Validate();
            world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            Validate(world); Render(world);
            Debug.Log("UNFIN_PLAYER_ANIMATIONS_OK clips="+Names.Length);
        }
        static void Validate(WorldPrototype world)
        {
            var rig=world.player.GetComponentInChildren<PaperPlayerRig>();
            if(rig==null || world.player.localScale!=Vector3.one) throw new Exception("Player rig attachment failed");
            foreach(string name in Names) {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimRoot+"/"+name+".anim");
                if(clip==null || clip.length<=0 || clip.frameRate!=60) throw new Exception("Invalid clip: "+name);
                foreach(var binding in AnimationUtility.GetCurveBindings(clip)) {
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);
                    if(Loops.Contains(name) && Mathf.Abs(curve.Evaluate(0)-curve.Evaluate(clip.length))>.0001f) throw new Exception("Loop discontinuity: "+name+"/"+binding.propertyName);
                }
                for(int frame=0;frame<=Mathf.RoundToInt(clip.length*60);frame++) {
                    clip.SampleAnimation(rig.gameObject,frame/60f); rig.RenderPose();
                    foreach(var filter in rig.GetComponentsInChildren<MeshFilter>())
                        foreach(var v in filter.sharedMesh.vertices)
                            if(float.IsNaN(v.x)||float.IsNaN(v.y)||Mathf.Abs(v.x)>3||Mathf.Abs(v.y)>3) throw new Exception("Invalid deformation: "+name);
                }
            }
            ValidateDriver(world);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/player-c-animation.json","{\"clips\":18,\"fps\":60,\"loopSeams\":\"passed\",\"meshBounds\":\"passed\",\"driverTransitions\":\"passed\",\"colliderChanged\":false,\"combat\":\"pose preview only\"}");
        }
        static void ValidateDriver(WorldPrototype world)
        {
            typeof(WorldPrototype).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(world,null);
            var driver=world.player.GetComponentInChildren<PaperPlayerDriver>();
            var animator=driver.animator; animator.Rebind(); animator.Update(0);
            var m=world.Motor;
            void Step(int frames,bool attack=false) {
                for(int i=0;i<frames;i++) { driver.Tick(1f/60,attack && i==0); animator.Update(1f/60); driver.rig.RenderPose(); }
            }
            void Expect(string state) { if(driver.CurrentAnimation!=state) throw new Exception("Driver: expected "+state+", got "+driver.CurrentAnimation); }
            m.grounded=true; Step(20); Expect("Idle");
            m.vx=125; Step(20); Expect("Run");
            m.grounded=false; m.vy=-200; Step(1); Expect("Jump"); Step(12); Expect("AirRise");
            m.vy=0; Step(1); Expect("AirApex"); m.vy=200; Step(1); Expect("AirFall");
            m.hitWall=1; Step(1); Expect("WallSlide");
            m.hitWall=0; m.grounded=true; m.vx=0; m.vy=0; Step(1); Expect("Land"); Step(20); Expect("Idle");
            Step(1,true); Expect("Attack1"); Step(24); Step(1,true); Expect("Attack2"); Step(24); Step(1,true); Expect("Attack3");
            Step(45); Expect("Idle");
            world.Respawn(world.Data.startX,world.Data.startFloor);
        }
        public static void ValidateOnly()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            Validate(UnityEngine.Object.FindAnyObjectByType<WorldPrototype>());
            Debug.Log("UNFIN_PLAYER_DRIVER_OK");
        }
        static void Render(WorldPrototype world)
        {
            string output=Environment.GetEnvironmentVariable("UNFIN_PLAYER_PREVIEW");
            if(string.IsNullOrEmpty(output)) output=Path.GetFullPath("../Art/animations/player-c-v1/frames");
            Directory.CreateDirectory(output);
            var rig=world.player.GetComponentInChildren<PaperPlayerRig>();
            var camera=world.worldCamera;
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimRoot+"/Idle.anim");
            idle.SampleAnimation(rig.gameObject,0); rig.RenderPose();
            Capture(camera,1920,1080,Path.Combine(output,"in-game.png"));
            // All clips use the SAME rig, materials, camera and renderer as the game.
            // Create a clean rig; cloning the scene object would also clone its
            // transient mesh children and leave an unanimated duplicate silhouette.
            var isolated=new GameObject("Animation render rig");
            var preview=isolated.AddComponent<PaperPlayerRig>();
            EditorUtility.CopySerialized(rig,preview); preview.Rebuild();
            if(preview.GetComponentsInChildren<MeshRenderer>().Length!=7) throw new Exception("Preview must contain exactly seven mesh parts");
            foreach(var t in isolated.GetComponentsInChildren<Transform>()) t.gameObject.layer=31;
            var cg=new GameObject("Animation render camera"); var cam=cg.AddComponent<Camera>();
            cam.orthographic=true; cam.orthographicSize=1.03f; cam.aspect=1; cam.cullingMask=1<<31;
            cam.transform.position=new Vector3(0,.66f,-10); cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.035f,.045f,.063f,1);
            var bounds=new Bounds(Vector3.zero,Vector3.zero);
            for(int n=0;n<Names.Length;n++) {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimRoot+"/"+Names[n]+".anim");
                for(int f=0;f<Frames[n];f++) {
                    clip.SampleAnimation(isolated,f/60f); preview.RenderPose();
                    foreach(var renderer in preview.GetComponentsInChildren<MeshRenderer>()) bounds.Encapsulate(renderer.bounds);
                }
            }
            cam.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);
            cam.orthographicSize=Mathf.Max(bounds.extents.x,bounds.extents.y)*1.06f;
            for(int n=0;n<Names.Length;n++) {
                string name=Names[n],folder=Path.Combine(output,name); Directory.CreateDirectory(folder);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimRoot+"/"+name+".anim");
                for(int f=0;f<Frames[n];f++) {
                    clip.SampleAnimation(isolated,f/60f); preview.RenderPose();
                    Capture(cam,384,384,Path.Combine(folder,f.ToString("D3")+".png"));
                }
            }
            UnityEngine.Object.DestroyImmediate(isolated); UnityEngine.Object.DestroyImmediate(cg);
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        static void Capture(Camera camera,int width,int height,string path)
        {
            var rt=new RenderTexture(width,height,24); var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply(); PreviewOutput.Write(path,texture.EncodeToPNG()); }
            finally { camera.targetTexture=null; RenderTexture.active=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
