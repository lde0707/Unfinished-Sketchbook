using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class TradingUIIntegration
    {
        static void Check(bool value,string message){if(!value)throw new Exception("Trading UI: "+message);}
        static void Release(WorldProgression p)=>typeof(WorldProgression).GetField("shopReleaseFrame",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,-1);
        public static void RunAndBuild()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/Unfin/Resources/TradingUI","*.png")){
                var art=(TextureImporter)AssetImporter.GetAtPath(path);art.textureType=TextureImporterType.Default;art.alphaIsTransparency=true;art.npotScale=TextureImporterNPOTScale.None;art.mipmapEnabled=false;art.maxTextureSize=2048;art.filterMode=FilterMode.Bilinear;art.wrapMode=TextureWrapMode.Clamp;art.textureCompression=TextureImporterCompression.Uncompressed;
                var web=art.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=2048;web.format=TextureImporterFormat.RGBA32;art.SetPlatformTextureSettings(web);art.SaveAndReimport();
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            foreach(var title in UnityEngine.Object.FindObjectsByType<StilldrawnTitle>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(title);
            var p=w.Progression;p.UseAbilityLocks=true;var original=w.CaptureSession();
            void Place(WorldMarker target,bool reset=true){if(reset)w.Respawn(target.tx,target.floor);Release(p);var at=p.SocialPosition(target);w.Motor.x=at.x*16-6;w.Motor.y=-at.y*16-11;w.Motor.grounded=true;}
            var checkpoints=w.Data.pickups.Where(m=>m.type=="save").ToArray();
            Check(checkpoints.Length==10&&checkpoints.Count(CheckpointPlan.IsShop)==2,"ten checkpoints with only two shops");
            Check(checkpoints.Count(m=>CheckpointPlan.IsShop(m)&&m.tx<w.Data.regionX)==1&&checkpoints.Count(m=>CheckpointPlan.IsShop(m)&&m.tx>=w.Data.regionX)==1,"one shop per region");
            foreach(var checkpoint in checkpoints){
                Place(checkpoint);bool isShop=CheckpointPlan.IsShop(checkpoint);
                Check((p.NearbyShop()!=null)==isShop,"shop availability at "+checkpoint.tx+","+checkpoint.floor);
                if(isShop){Check(p.TryInteract()&&p.ConfirmInteractionChoice()&&p.ShopOpen,"designated hub opens shop");p.CloseShop();}
                else{Check(p.NearbyInteraction()!=checkpoint&&!p.OpenShop()&&!p.ShopOpen&&!p.InteractionChoiceOpen,"save-only checkpoint cannot open shop");}
            }
            Debug.Log("SHOP_PLACEMENT_OK checkpoints=10 shops=2 regions=1+1");
            var save=w.Data.pickups.First(m=>m.type=="save"&&m.tx==236);Place(save);
            Check(p.TryInteract()&&p.InteractionChoiceOpen&&!p.ShopOpen&&p.ChoiceAction=="상점 보기","E first shows shop choice");
            Check(w.MapOpen&&Time.timeScale==0,"world paused while choosing");Check(!p.TryInteract(),"one press cannot execute through dialog");
            p.CancelInteractionChoice();Check(!p.ShopOpen&&!p.InteractionChoiceOpen&&Time.timeScale==1,"back has no side effect");Release(p);
            p.TryInteract();Check(p.ConfirmInteractionChoice()&&p.ShopOpen&&Time.timeScale==0,"explicit shop entry");
            Check(!p.RequestPurchase(0)&&!p.InteractionChoiceOpen&&p.InkDust==0,"insufficient funds never opens charge confirmation");p.AddInkDust(150);
            Check(!p.RequestPurchase(1),"healing remains gated");Check(p.RequestPurchase(0)&&p.InkDust==150&&p.BindingLevel==0,"purchase only previews cost");
            p.CancelInteractionChoice();Check(p.ShopOpen&&p.InkDust==150&&Time.timeScale==0,"cancel purchase returns to paused shop");
            p.RequestPurchase(0);Check(p.ConfirmInteractionChoice()&&p.InkDust==110&&p.BindingLevel==1,"confirm charges exact price once");
            Check(!p.ConfirmInteractionChoice()&&p.InkDust==110,"double confirm cannot double spend");
            var bought=w.CaptureSession();p.BackFromShop();Check(!p.ShopOpen&&p.InteractionChoiceOpen&&p.ChoiceKind=="save"&&Time.timeScale==0,"shop back returns to entry choice");
            p.CancelInteractionChoice();p.RestoreSession(bought);Time.timeScale=1;Check(p.BindingLevel==1&&p.InkDust==110&&!p.InteractionChoiceOpen,"wallet persists but modal does not");
            foreach(string type in new[]{"item_heal","item_shield","item_paint","heart","healup","shard","diary","sw_r","sw_b","sw_y"}){
                var target=w.Data.pickups.First(m=>m.type==type);Place(target); p.Tick(.01f,0,0); Check(!p.Collected(target),"approach alone never collects "+type);
                Check(p.TryInteract()&&!p.InteractionChoiceOpen&&p.Collected(target)&&p.PickupNoticeOpen,"one E commits then explains "+type);
                Check(!w.MapOpen&&!p.ShopBlocksInput&&Time.timeScale==1&&p.PickupNoticeDescription.Length>10,"notice leaves gameplay running "+type);
                int count=w.CaptureSession().collected.Length;p.Collect(target);Check(count==w.CaptureSession().collected.Length&&p.PendingPickupNotices==0,"same item cannot duplicate acquisition "+type);
                var acquired=w.CaptureSession();Check(acquired.collected.Contains(type+":"+target.tx+":"+target.floor),"acquired before dismissal "+type);
                p.ClosePickupNotice();Check(p.Collected(target)&&!p.PickupNoticeOpen&&Time.timeScale==1,"closing keeps acquisition "+type);Release(p);
            }
            var beforeQueue=w.CaptureSession();p.RestoreSession(original);Release(p);
            var heal=w.Data.pickups.First(m=>m.type=="item_heal");var shield=w.Data.pickups.First(m=>m.type=="item_shield");
            Place(heal);p.TryInteract();Place(shield,false);Check(p.TryInteract()&&p.Collected(shield)&&p.PendingPickupNotices==1,"second pickup remains usable and queues notice");
            Check(p.PickupNoticeTitle=="회복 물감 획득","new notice does not overwrite first");p.Tick(6.05f,0,0);
            Check(p.PickupNoticeTitle=="종이 방패 획득"&&p.PendingPickupNotices==0,"queued notice advances after six seconds");
            float age=p.PickupNoticeAge;w.ToggleExplorationMap();p.Tick(2,0,0);Check(p.PickupNoticeAge==age,"map pauses notice timer");w.ToggleExplorationMap();
            p.Tick(6.1f,0,0);Check(!p.PickupNoticeOpen&&Time.timeScale==1&&!p.ShopBlocksInput,"notice expires without blocking or changing time scale");
            p.RestoreSession(beforeQueue);Release(p);
            var bounds=WorldProgression.PickupNoticeBounds;Check(bounds.xMin>1200&&bounds.yMin>750&&bounds.xMax<=1920&&bounds.yMax<=1080,"compact safe bottom-right bounds");
            var toastArt=Resources.Load<Texture2D>("TradingUI/acquired-toast-v2");Check(toastArt!=null&&Mathf.Abs((float)toastArt.width/toastArt.height-3)<.01f,"toast art keeps original 3:1 aspect");
            var lever=w.Data.pickups.First(m=>m.type=="lever");Place(lever);p.TryInteract();Check(p.InteractionChoiceOpen&&!p.Collected(lever),"lever retains confirmation");p.CancelInteractionChoice();Release(p);p.TryInteract();Check(p.ConfirmInteractionChoice()&&p.Collected(lever),"confirm lever");
            var hand=w.Data.pickups.First(m=>m.type=="wj");Place(hand);p.TryInteract();Check(!p.InteractionChoiceOpen&&!p.Collected(hand),"guardian seal cannot be bypassed by confirmation");
            p.DefeatedEncounters.Add(AbilityTrials.PaperGuardian);Release(p);p.TryInteract();Check(!p.InteractionChoiceOpen&&p.HasAcquired("walljump")&&p.PickupNoticeOpen,"earned ability acquired immediately");
            Check(p.PickupNoticeTitle=="또렷해진 손 획득"&&p.PickupNoticeCategory.Contains("벽타기")&&p.PickupNoticeDescription.Contains("미끄러")&&p.PickupNoticeDescription.Contains("Space"),"wall ability explains actual controls");
            var earnedHand=w.CaptureSession();
            foreach(string ability in new[]{"cut","dash","parry","climb"}){p.RestoreSession(original);p.Grant(ability);Check(p.HasAcquired(ability)&&p.PickupNoticeOpen&&!p.InteractionChoiceOpen&&Time.timeScale==1&&p.PickupNoticeDescription.Length>10,"boss ability uses corner notice "+ability);p.Grant(ability);Check(p.PendingPickupNotices==0,"ability notice cannot duplicate "+ability);}
            p.RestoreSession(earnedHand);
            var noticeSave=w.CaptureSession();p.RestoreSession(noticeSave);Check(p.HasAcquired("walljump")&&!p.PickupNoticeOpen,"reload retains acquisition without stale popup");Release(p);
            Check(WorldProgression.PickupInstructions("climb").Contains("W / S"),"up-down climbing only in later ability description");
            Debug.Log("PICKUP_NOTICE_OK direct-acquisition nonblocking queue six-second-expiry pause-timer safe-corner gated-wall persistence");
            var portal=w.Data.pickups.First(m=>m.type=="station"&&m.optionsJson.Contains("r1s"));Place(portal);p.TryInteract();Check(p.InteractionChoiceOpen&&!p.PortalTravelling&&p.ChoiceDescription.Contains("목적지"),"portal destination preview");p.CancelInteractionChoice();Check(!p.PortalTravelling,"portal cancellation");Release(p);p.TryInteract();p.ConfirmInteractionChoice();Check(p.PortalTravelling,"confirmed travel only");Release(p);p.Tick(.3f,0,0);Check(!p.PortalTravelling,"travel finishes");
            var friend=w.Data.pickups.First(m=>m.type=="sketch"&&!m.optionsJson.Contains("sun"));Place(friend);p.Grant("cut");p.ClosePickupNotice();p.TryInteract();Check(!p.InteractionChoiceOpen&&p.SocialTarget==friend,"one E begins friend animation without confirmation");Release(p);for(int i=0;i<130;i++)p.TickSocial(.01f,0,0);Check(p.Collected(friend)&&p.PickupNoticeTitle.Contains("강아지")&&!p.InteractionChoiceOpen,"friend acquisition ends with corner notice");
            var npc=w.Data.pickups.First(m=>m.type=="npc");Place(npc);p.TryInteract();Check(!p.InteractionChoiceOpen&&p.SocialTarget==npc,"ordinary conversation has no extra confirmation");p.TickSocial(.01f,1,0);
            Check(!WorldProgression.NeedsInteractionChoice("shard")&&!WorldProgression.NeedsInteractionChoice("brazier"),"small loot and combat mechanisms remain direct");
            Place(save);p.TryInteract();w.Motor.x+=100*16;Check(!p.ConfirmInteractionChoice()&&!p.ShopOpen,"stale target cannot act remotely");
            Place(save);p.TryInteract();w.Respawn(236,131);Check(!p.InteractionChoiceOpen&&Time.timeScale==1,"respawn clears choice");
            foreach(var name in new[]{"counter","choice-panel","acquired-panel-v1"}){var image=Resources.Load<Texture2D>("TradingUI/"+name);Check(image!=null&&Mathf.Abs((float)image.width/image.height-1.5f)<.001f,"original generated aspect "+name);}
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/trading-ui.json","{\"passed\":true,\"checks\":[\"two-step shop entry\",\"paused choice\",\"cancel without effects\",\"purchase preview and exact once charge\",\"insufficient funds and locked upgrades\",\"shop back hierarchy\",\"persistent upgrades\",\"ten immediate pickups with nonblocking corner notice and queue\",\"guardian ability gate\",\"portal destination and travel\",\"friend animation without confirm\",\"NPC and small loot direct\",\"stale target revalidation\",\"respawn clears modal\",\"generated 3:2 assets and acquisition panel\"]}");
            Debug.Log("TRADING_UI_OK");Time.timeScale=1;EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}

