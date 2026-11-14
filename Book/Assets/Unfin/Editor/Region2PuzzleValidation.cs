using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class Region2PuzzleValidation
    {
        static void Check(bool value,string message){if(!value)throw new Exception("Region2 puzzle: "+message);}
        static void Private(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
        static MapMarker Marker(int source)=>UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).Single(m=>m.enemy&&m.sourceIndex==source);
        static void Advance(WorldPrototype w,EnemyProjectileSystem system,float seconds){for(float t=0;t<seconds;t+=.005f){w.Progression.Tick(.005f,0,0);system.Tick(.005f);}}
        static void Aim(WorldPrototype w,PaperEnemyAnimation enemy,EnemyProjectileSystem system){for(int i=0;i<23;i++){w.Progression.Tick(.05f,0,0);enemy.Tick(.05f);system.Tick(.05f);}}
        public static void Validate(){
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();Private(w,"Awake");w.Progression.UseAbilityLocks=true;foreach(var a in new[]{"walljump","cut","dash","parry"})w.Progression.Grant(a);w.Progression.Health=100;w.worldCamera.aspect=16f/9;
            var system=new GameObject("Projectile puzzle validation").AddComponent<EnemyProjectileSystem>();system.Initialize(w);
            var targetMarker=Marker(75);PaperEnemyAnimation.Attach(targetMarker);var gunner=targetMarker.GetComponent<PaperEnemyAnimation>();int hp=gunner.Health;targetMarker.GetComponent<PaperAttackTarget>().ReceiveHit(1);Check(gunner.Health==hp,"target supplier protected before device opens");
            w.Respawn(412,124);w.Simulate(.001f,1);Aim(w,gunner,system);var from=gunner.ProjectileMuzzle;float travel=Mathf.Abs(from.x-w.player.position.x)/6.25f;Check(system.Fire(gunner,"G03",from,w.player.position,-1),"real pigment shot");Advance(w,system,Mathf.Max(0,travel-.12f));w.Progression.Parry();Advance(w,system,.65f);Check(w.Motor.Get(404,120)==0,"reflected pigment hits visible target and opens door");targetMarker.GetComponent<PaperAttackTarget>().ReceiveHit(1);Check(gunner.Health==hp-1,"supplier protection ends with puzzle");
            system.Clear();var bridgeMarker=Marker(74);PaperEnemyAnimation.Attach(bridgeMarker);var thrower=bridgeMarker.GetComponent<PaperEnemyAnimation>();w.Respawn(458,88);w.Simulate(.001f,1);w.Progression.Health=100;var focus=new GameObject("Puzzle camera validation").AddComponent<Region2Presentation>();focus.Initialize(w);typeof(Region2Presentation).GetField("bridgeFocus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(focus,1f);Private(focus,"LateUpdate");var viewport=w.worldCamera.WorldToViewportPoint(thrower.BodyBounds.center);Check(viewport.x>.03f&&viewport.x<.97f,"far bank caster visible from safe launch edge");
            Aim(w,thrower,system);from=thrower.ProjectileMuzzle;travel=Mathf.Clamp(Vector2.Distance(from,w.player.position)/7,1.2f,3);Check(system.Fire(thrower,"R202",from,w.player.position,-1),"real arcing wax shot crosses gap");Advance(w,system,Mathf.Max(0,travel-.12f));w.Progression.Parry();Check(w.Motor.Get(459,88)!=4,"pressing parry alone does not build bridge");Advance(w,system,1.5f);Check(w.Motor.Get(459,88)==4&&w.Motor.Get(468,88)==4,"reflected wax hits bar and builds all ten platform cells");
            var turtleMarker=Marker(69);PaperRegion2Encounter.Attach(turtleMarker);var turtle=turtleMarker.GetComponent<PaperRegion2Encounter>();w.Respawn(382,94);w.Simulate(.001f);turtle.Tick(.001f);turtle.BeginPattern(1);int peak=0;for(int i=0;i<200;i++){turtle.Tick(.005f);peak=Math.Max(peak,turtle.LiveProjectiles);}Check(peak>=3,"turtle emits three actual wax shards");
            var knightMarker=Marker(91);PaperRegion2Encounter.Attach(knightMarker);var knight=knightMarker.GetComponent<PaperRegion2Encounter>();w.Respawn(538,50);w.Simulate(.001f);w.Progression.Health=100;knight.Tick(.001f);knight.transform.position=new Vector3(537.9f,-50,0);knight.BeginPattern(2);for(int i=0;i<180;i++)knight.Tick(.005f);Check(knight.Staggered,"wall-baited knight charge staggers");var cells=(System.Collections.IList)typeof(PaperRegion2Encounter).GetField("spearCells",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(knight);Check(cells.Count>0,"stuck lance has real one-way collision");var indices=cells.Cast<int>().ToArray();Check(indices.All(i=>w.Motor.cells[i]==4),"lance collider matches platform cells");Private(knight,"ClearSpear");Check(indices.All(i=>w.Motor.cells[i]==0),"temporary lance collision clears safely");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/region2-puzzles.json","{\"passed\":true,\"realTargetReflection\":true,\"realBridgeReflection\":true,\"noDirectParryActivation\":true,\"keeperProtection\":true,\"casterVisibleFromSafeEdge\":true,\"turtleShards\":3,\"wallChargeCounter\":true,\"temporaryLanceCollision\":true}");Debug.Log("UNFIN_REGION2_PUZZLES_OK actual projectiles / visible caster / protected suppliers / spear collision");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
    }
}
