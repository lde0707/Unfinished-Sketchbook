using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class RuntimeOptimizationValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Runtime optimization: "+message);}
        static void CheckTimings(float[] times,Func<float,int> frame,Func<int,int> index)
        {
            float time=0;
            for(int i=0;i<times.Length;i++)
            {
                if(times[i]>0)Check(frame(time+times[i]*.5f)==index(i),"boss/elite frame centre "+i);
                time+=times[i];
            }
            Check(frame(time+1)==index(times.Length-1),"non-looping last frame");
        }
        public static void RunAndBuild()
        {
            var machine=new FiniteStateMachine<PlayerCombatState>(PlayerCombatState.Ready);
            machine.Change(PlayerCombatState.GroundAttack);machine.Advance(.125f);
            Check(!machine.Change(PlayerCombatState.GroundAttack)&&machine.Time==.125f,"same state preserves clock");
            Check(machine.Change(PlayerCombatState.GroundAttack,true)&&machine.Time==0,"explicit restart");
            machine.Change(PlayerCombatState.Ready);Check(machine.Time==0,"state exit resets clock");
            int samples=0;
            var lib=AssetDatabase.LoadAssetAtPath<PaperSpriteLibrary>("Assets/Unfin/Animations/PlayerSprites/PlayerSprites.asset");
            foreach(var clip in lib.clips)
            {
                Check(ReferenceEquals(lib.Find(clip.name),clip),"cached clip identity "+clip.name);
                float sum=0;foreach(var frame in clip.frames)sum+=frame.duration;
                Check(Mathf.Abs(clip.Duration-sum)<.000001f,"duration "+clip.name);
                // Check the centre of every frame over multiple cycles, plus dense samples
                // away from exact floating-point boundaries of the legacy subtractive clock.
                for(int i=0;i<4000;i++)
                {
                    float t=i*.001371f,legacy=clip.loop?Mathf.Repeat(t,sum):t;
                    int expected=clip.frames.Length-1;bool boundary=false;
                    for(int f=0;f<clip.frames.Length;f++)
                    {
                        if(Mathf.Abs(legacy-clip.frames[f].duration)<.00001f)boundary=true;
                        if(legacy<clip.frames[f].duration){expected=f;break;}legacy-=clip.frames[f].duration;
                    }
                    if(!boundary)Check(clip.FrameAt(t)==expected,"frame timing "+clip.name+" @ "+t);
                    samples++;
                }
            }
            foreach(var clip in PaperBossRegion1.Art.clips)CheckTimings(clip.durations,clip.Frame,i=>i);
            foreach(var actor in PaperRegion1Elite.Manifest.actors)
                foreach(var clip in actor.clips)CheckTimings(clip.durations,clip.Frame,i=>i);
            foreach(var actor in PaperRegion2Encounter.Catalog.actors)
                foreach(var clip in actor.clips)CheckTimings(clip.durations,clip.Frame,i=>clip.frames[i]);
            var polygon=new[]{new Vector2(-2,-2),new Vector2(2,-2),new Vector2(2,2),new Vector2(-2,2)};
            var inside=new Bounds(Vector3.zero,Vector3.one);
            Check(PaperSpritePlayer.OverlapsAttack(polygon,inside),"target contained by attack");
            Check(PaperSpritePlayer.OverlapsAttack(polygon,new Bounds(Vector3.zero,Vector3.one*8)),"attack contained by target");
            Check(PaperSpritePlayer.OverlapsAttack(polygon,new Bounds(new Vector3(2.5f,0,0),Vector3.one)),"touching edges");
            Check(!PaperSpritePlayer.OverlapsAttack(polygon,new Bounds(new Vector3(8,0,0),Vector3.one)),"distant target rejected");
            var cross=new[]{new Vector2(-4,-.1f),new Vector2(4,-.1f),new Vector2(4,.1f),new Vector2(-4,.1f)};
            Check(PaperSpritePlayer.OverlapsAttack(cross,inside),"edge crossing without contained vertices");
            // Warm up before measuring the steady-state hot path (not asset loading).
            for(int i=0;i<100;i++){lib.Find("Run").FrameAt(i*.01f);PaperSpritePlayer.OverlapsAttack(polygon,inside);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++){lib.Find("Run").FrameAt(i*.01f);PaperSpritePlayer.OverlapsAttack(polygon,inside);}
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Check(allocated==0,"cached lookup/timing/overlap managed allocation: "+allocated);
            PaperSpriteValidation.Run();
            ResponsiveCombatValidation.Run();
            PlayerWallValidation.Run();
            PlayerLifeValidation.Run();
            EnemyProjectileValidation.Run();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var world=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(world,null);
            int enemies=0;
            foreach(var marker in UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None))
            {
                if(!marker.enemy||!PaperEnemyAnimation.Attach(marker))continue;
                var enemy=marker.GetComponent<PaperEnemyAnimation>();
                enemy.Tick(.016f);enemy.ResetEnemy();
                Check(enemy.StateName=="Move"&&enemy.FrameIndex==0,"enemy reset "+enemy.SpeciesId);
                enemies++;
            }
            Check(enemies>0,"enemy instances initialized");
            foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)==0,"missing script "+go.name);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/runtime-optimization.json","{\"passed\":true,\"timingSamples\":"+samples+",\"enemies\":"+enemies+",\"movementCombatWallLifeProjectiles\":true,\"bossEliteTiming\":true,\"hotPathIterations\":10000,\"hotPathAllocatedBytes\":"+allocated+",\"missingScripts\":0}");
            Debug.Log("RUNTIME_OPTIMIZATION_OK samples="+samples+" enemies="+enemies);
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            WebGLBuilder.Build();
        }
    }
}
