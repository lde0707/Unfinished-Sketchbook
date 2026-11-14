using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Unfin.Editor
{
    public static class InteractionMenuValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Interaction/menu: "+message);}
        public static void Run(WorldPrototype w)
        {
            var p=w.Progression;p.UseAbilityLocks=true;
            void Place(WorldMarker target){typeof(WorldProgression).GetField("shopReleaseFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p,-1);var at=p.SocialPosition(target);w.Motor.x=at.x*16-6;w.Motor.y=-at.y*16-11;w.Motor.vx=w.Motor.vy=0;w.Motor.grounded=true;}
            foreach(string kind in new[]{"item_heal","lever","diary","sw_r"}){
                var target=w.Data.pickups.First(m=>m.type==kind);Place(target);
                p.Tick(.1f,0,-1);Check(!p.Collected(target),"W does not activate "+kind);
                Check(p.NearbyInteraction()==target,"nearest target "+kind);
                Check(p.TryInteract(),"E activates "+kind);
                if(kind=="lever")Check(p.ConfirmInteractionChoice(),"lever retains confirmation");
                else Check(p.PickupNoticeOpen&&!p.InteractionChoiceOpen,"item explains after acquisition "+kind);
                Check(p.Collected(target),"interaction completes "+kind);
                int count=w.CaptureSession().collected.Length;p.TryInteract();Check(w.CaptureSession().collected.Length==count,"no duplicate activation "+kind);p.ClosePickupNotice();
            }
            var friend=w.Data.pickups.First(m=>m.type=="sketch"&&!m.optionsJson.Contains("sun"));Place(friend);
            p.TryInteract();Check(p.SocialTarget==null&&!p.Collected(friend),"locked friend requires ability");
            p.Grant("cut");p.TickSocial(.4f,0,-1);Check(p.SocialTarget==null,"W does not begin dialogue");
            Check(p.TryInteract()&&!p.InteractionChoiceOpen&&p.SocialTarget==friend,"one E begins friend animation");
            p.TickSocial(.3f,0,0);Check(p.SocialProgress>0,"animation continues after releasing key");
            p.TickSocial(.01f,1,0);Check(p.SocialTarget==null&&!p.Collected(friend),"moving cancels interaction");
            Place(friend);p.TryInteract();for(int i=0;i<130;i++)p.TickSocial(.01f,0,0);Check(p.Collected(friend),"confirmed interaction completes friend");
            var portal=w.Data.pickups.First(m=>m.type=="station"&&m.optionsJson.Contains("r1s"));Place(portal);
            p.Tick(.4f,0,-1);Check(!p.PortalTravelling,"W does not travel");p.TryInteract();p.ConfirmInteractionChoice();typeof(WorldProgression).GetField("shopReleaseFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(p,-1);Check(p.PortalTravelling,"confirmed E triggers portal");
            p.Tick(.3f,0,0);Check(!p.PortalTravelling,"portal completes once");
            w.Respawn(236,131);w.Motor.grounded=true;Check(p.TryInteract()&&p.ConfirmInteractionChoice()&&p.ShopOpen,"confirmed E opens checkpoint shop");p.CloseShop();
            var panel=Resources.Load<Texture2D>("PaperMenuV2/pause-panel");var tray=Resources.Load<Texture2D>("PaperMenuV2/health-tray");
            Check(panel!=null&&Mathf.Abs((float)panel.width/panel.height-2f/3)<.001f,"portrait pause asset original ratio");
            Check(tray!=null&&Mathf.Abs((float)tray.width/tray.height-3)<.01f,"health tray original ratio");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/interaction-menu.json","{\"passed\":true,\"eChoiceThenConfirm\":true,\"wDoesNotInteract\":true,\"friendAbilityGate\":true,\"animationAfterRelease\":true,\"cancelOnMovement\":true,\"singlePortalTravel\":true,\"checkpointShop\":true,\"generatedArtAspectPreserved\":true}");
            Debug.Log("INTERACTION_MENU_OK");
        }
    }
}

