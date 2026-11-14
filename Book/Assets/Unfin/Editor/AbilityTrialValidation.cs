using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class AbilityTrialValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Ability trial: "+message);}
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Call(w,"Awake");
            foreach(var title in UnityEngine.Object.FindObjectsByType<StilldrawnTitle>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(title);
            var p=w.Progression;p.UseAbilityLocks=true;var fresh=w.CaptureSession();
            var hand=w.Data.pickups.First(m=>m.type=="wj");
            var marker=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None).First(m=>m.enemy&&m.sourceIndex==AbilityTrials.PaperGuardian);
            Check(PaperRegion1Elite.Attach(marker),"guardian replaces ordinary enemy before normal attachment");
            var guardian=marker.GetComponent<PaperRegion1Elite>();var receiver=marker.GetComponent<PaperAttackTarget>();
            Check(guardian.IsPaperGuardian&&guardian.Health==9,"independent guardian health");
            Check(PaperRegion1Elite.Manifest.actors.First(a=>a.sourceIndex==23).health==12,"later elite unchanged");
            void PlaceHand(){w.Respawn(hand.tx,hand.floor);var at=p.SocialPosition(hand);w.Motor.x=at.x*16-6;w.Motor.y=-at.y*16-11;w.Motor.grounded=true;guardian.Tick(0);}
            PlaceHand();Check(p.NearbyInteraction()==hand,"hand remains reachable without wall jump");
            p.TryInteract();p.Collect(hand);Check(!p.HasAcquired("walljump")&&!p.Collected(hand),"both E and collection enforce seal");
            foreach(string ability in new[]{"cut","dash","parry","climb"}){p.Collect(new WorldMarker{type=ability});Check(!p.HasAcquired(ability),"loose pickup cannot skip boss "+ability);}
            // Simulate an actual base jump in the real room, with every mobility ability locked.
            w.Respawn(204,128);guardian.Tick(0);for(int i=0;i<120;i++)w.Simulate(1f/120);
            Check(w.Motor.grounded&&!p.Has("walljump")&&!p.Has("dash"),"base kit grounded in trial room");
            float bottom=w.Motor.WorldBounds.min.y,highest=bottom;w.RequestJump();
            for(int i=0;i<120;i++){w.Simulate(1f/120);highest=Mathf.Max(highest,w.Motor.WorldBounds.min.y);}
            Check(highest-bottom>guardian.BodyBounds.size.y+.3f,"base jump clears guardian body");
            Check(w.Motor.grounded,"base jump returns safely to floor");
            guardian.BeginAttack();Vector3 origin=guardian.transform.position;
            guardian.Tick(.47f);Check(Vector3.Distance(origin,guardian.transform.position)<.001f,"readable stationary windup");
            for(int i=0;i<60;i++)guardian.Tick(.008f);
            float travel=Vector3.Distance(origin,guardian.transform.position);Check(travel>2.8f&&travel<3.4f,"short committed charge");
            receiver.ReceiveHit(1);Check(guardian.Health==8,"guardian takes attacks");
            w.Respawn(198,128);guardian.Tick(0);Check(guardian.Health==9,"unbeaten guardian resets on death/retry");
            for(int i=0;i<9;i++)receiver.ReceiveHit(1);
            Check(guardian.Dead&&p.DefeatedEncounters.Contains(AbilityTrials.PaperGuardian),"kill records encounter");
            Check(p.AbilityPickupUnlocked("wj")&&!p.HasAcquired("walljump"),"kill unseals but still requires claiming hand");
            var earned=w.CaptureSession();p.RestoreSession(earned);w.Respawn(198,128);guardian.Tick(0);
            Check(guardian.Dead&&!receiver.enabled&&p.AbilityPickupUnlocked("wj"),"save during death keeps encounter cleared");
            PlaceHand();Check(p.TryInteract()&&p.PickupNoticeOpen&&!p.InteractionChoiceOpen&&p.HasAcquired("walljump")&&p.Collected(hand),"E claims earned ability before notice");p.ClosePickupNotice();
            p.RestoreSession(fresh);p.Grant("walljump");w.Respawn(198,128);guardian.Tick(0);
            Check(guardian.Dead&&!receiver.enabled,"old wall-jump saves are grandfathered");
            p.RestoreSession(fresh);w.Respawn(198,128);guardian.Tick(0);
            Check(!guardian.Dead&&guardian.Health==9&&!p.AbilityPickupUnlocked("wj"),"fresh run restores challenge");
            foreach(var pair in new[]{("cut",23),("dash",47),("parry",69),("climb",91)}){
                p.RestoreSession(fresh);p.DefeatedEncounters.Add(pair.Item2);var save=w.CaptureSession();p.RestoreSession(save);
                Check(p.HasAcquired(pair.Item1),"existing boss reward persists "+pair.Item1);
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/ability-trial.json","{\"passed\":true,\"guardianHealth\":9,\"windup\":0.48,\"charge\":0.48,\"baseJumpHeight\":"+(highest-bottom).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"checks\":[\"sealed direct and E pickup\",\"no mobility ability required\",\"actual base jump clears guardian\",\"committed short charge\",\"reset before defeat\",\"save before claim\",\"E claim after kill\",\"legacy save compatibility\",\"fresh run locked\",\"other four boss rewards preserved\"]}");
            Debug.Log("ABILITY_TRIAL_OK");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
