using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class EconomyValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Economy: "+message);}
        static void Private(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Private(w,"Awake");var p=w.Progression;
            var drops=EnemyRewardDrops.Get(w);var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
            var normal=markers.First(m=>m.enemy&&PaperEnemyAnimation.Manifest.placements.Any(e=>e.sourceIndex==m.sourceIndex)&&m.sourceIndex!=23&&m.sourceIndex!=44&&m.sourceIndex!=47&&m.sourceIndex!=69&&m.sourceIndex!=91&&m.sourceIndex!=74&&m.sourceIndex!=75);
            Check(PaperEnemyAnimation.Attach(normal),"normal enemy attach");var enemy=normal.GetComponent<PaperEnemyAnimation>();
            for(int i=enemy.Health;i>0;i--)Private(enemy,"OnHit");Check(drops.SpawnEvents==1&&drops.PendingValue>0,"normal death emits");Private(enemy,"OnHit");Check(drops.SpawnEvents==1,"dead enemy cannot emit twice");
            EnemyRewardDrops.Discard(w);enemy.ResetEnemy();for(int i=enemy.Health;i>0;i--)Private(enemy,"OnHit");Check(drops.SpawnEvents==2,"respawned normal enemy can reward again");EnemyRewardDrops.Discard(w);
            foreach(int index in new[]{23,44,47,69,91}){
                var marker=markers.First(m=>m.enemy&&m.sourceIndex==index);int before=drops.SpawnEvents;
                if(index==23||index==44){PaperRegion1Elite.Attach(marker);var e=marker.GetComponent<PaperRegion1Elite>();for(int i=e.Health;i>0;i--)Private(e,"Hit");Private(e,"Hit");}
                else if(index==47){PaperBossRegion1.Attach(marker);var e=marker.GetComponent<PaperBossRegion1>();for(int i=e.Health;i>0;i--)Private(e,"Hit");Private(e,"Hit");}
                else {PaperRegion2Encounter.Attach(marker);var e=marker.GetComponent<PaperRegion2Encounter>();for(int i=e.Health;i>0;i--)Private(e,"Damage",1);Private(e,"Damage",1);}
                Check(drops.SpawnEvents==before+1,"single elite/boss reward "+index);Check(drops.PendingValue==(index==23?25:index==44?12:index==47?60:index==69?35:75),"boss amount "+index);EnemyRewardDrops.Discard(w);
            }
            w.Respawn(236,131);w.Motor.grounded=true;drops.Tick(0);
            drops.Spawn(w.Motor.WorldBounds.center,7);var save=w.CaptureSession();Check(save.inkDust==7&&p.InkDust==0,"pending reward included in save without early credit");
            w.ToggleExplorationMap();drops.Tick(.5f);Check(drops.PendingValue==7,"drops pause on map");w.ToggleExplorationMap();
            for(int i=0;i<120;i++)drops.Tick(1f/60);Check(p.InkDust==7&&drops.PendingValue==0,"nearby drops collected exactly once");
            p.RestoreSession(JsonUtility.FromJson<StilldrawnSave>(JsonUtility.ToJson(save)));Check(p.InkDust==7&&drops.PendingValue==0,"pending rewards restored once");
            drops.Spawn(Vector2.zero,5);w.Respawn(236,131);w.Motor.grounded=true;drops.Tick(0);Check(p.InkDust==12&&drops.PendingValue==0,"death/reset retains earned drops");
            Check(p.OpenShop()&&w.MapOpen&&Time.timeScale==0,"nearby checkpoint shop pauses game");
            Check(!p.BuyOffer(0)&&p.InkDust==12,"cannot overspend");p.AddInkDust(1000);int wallet=p.InkDust;
            Check(!p.BuyOffer(1)&&p.InkDust==wallet,"healing ability remains exploration-gated");
            Check(p.BuyOffer(0)&&p.InkDust==wallet-40&&p.MaxHealth==6&&p.OfferPrice(0)==90,"health purchase and next price");
            Check(p.BuyOffer(0)&&p.BuyOffer(0)&&p.MaxHealth==8,"health cap");wallet=p.InkDust;Check(!p.BuyOffer(0)&&p.InkDust==wallet,"maxed purchase cannot charge");
            p.HealingUnlocked=true;Check(p.BuyOffer(1)&&p.BuyOffer(1)&&p.MaxHealingCharges==5,"permanent bottle capacity");Check(!p.BuyOffer(1),"bottle cap");
            Check(p.BuyOffer(2)&&p.BuyOffer(2)&&p.BuyOffer(2)&&Mathf.Abs(p.GatherRadius-4.8f)<.001f,"gather upgrade radius");Check(!p.BuyOffer(2),"gather cap");
            save=w.CaptureSession();p.RestoreSession(JsonUtility.FromJson<StilldrawnSave>(JsonUtility.ToJson(save)));Time.timeScale=1;
            Check(p.BindingLevel==3&&p.BottleLevel==2&&p.GatherLevel==3&&p.InkDust==save.inkDust,"wallet and upgrades serialize");Check(!p.BuyOffer(0),"cannot buy outside menu");
            save.inkDust=save.bindingLevel=save.bottleLevel=save.gatherLevel=0;p.RestoreSession(save);Check(p.InkDust==0&&p.BindingLevel==0,"legacy zero defaults");
            drops.Spawn(Vector2.zero,500);Check(drops.ActiveCount<=64&&drops.PendingValue==500,"large rewards represented by bounded visual drops");EnemyRewardDrops.Discard(w);
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/economy.json","{\"passed\":true,\"normalRespawnRewards\":true,\"eliteBossTypes\":5,\"singleRewardPerDeath\":true,\"pendingSaveRestore\":true,\"deathRetention\":true,\"pickup\":true,\"pause\":true,\"noOverspend\":true,\"abilityGate\":true,\"upgradeCaps\":true,\"permanentSave\":true,\"legacyDefaults\":true,\"boundedDrops\":true}");
            Debug.Log("ECONOMY_VALIDATION_OK");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
