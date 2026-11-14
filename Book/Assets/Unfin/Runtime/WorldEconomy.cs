using UnityEngine;
using UnityEngine.InputSystem;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        public int InkDust { get; private set; }
        public int BindingLevel { get; private set; }
        public int BottleLevel { get; private set; }
        public int GatherLevel { get; private set; }
        public bool ShopOpen { get; private set; }
        public int ShopSelection { get; private set; }
        int shopReleaseFrame=-1;
        public bool ShopBlocksInput=>ShopOpen||InteractionChoiceOpen||Time.frameCount<=shopReleaseFrame;
        public float GatherRadius=>2.4f+GatherLevel*.8f;
        static readonly string[] OfferNames={"책등 보강","물감 주머니","먹빛 끌림"};
        static readonly string[] OfferIcons={"leaf","heal","shard"};
        static readonly int[][] OfferPrices={new[]{40,90,160},new[]{60,120},new[]{20,50,90}};
        public int OfferLevel(int i)=>i==0?BindingLevel:i==1?BottleLevel:GatherLevel;
        public int OfferPrice(int i)=>i<0||i>=3||OfferLevel(i)>=OfferPrices[i].Length?0:OfferPrices[i][OfferLevel(i)];
        public bool OfferUnlocked(int i)=>i!=1||HealingUnlocked;
        public void AddInkDust(int amount){if(amount<=0)return;InkDust=(int)System.Math.Min(999999L,(long)InkDust+amount);}
        public WorldMarker NearbyShop()
        {
            if(world.Motor==null)return null;
            var player=world.Motor.WorldBounds.center;
            foreach(var p in world.Data.pickups)if(CheckpointPlan.IsShop(p)){
                var place=WorldPropPlacement.Find("save",new Vector2(p.x,p.y));
                Vector2 at=place!=null?(Vector2)place.position:new Vector2(p.tx+.5f,-p.floor+.8f);
                if(Vector2.Distance(player,at)<2.2f&&InteractionProximity.Visible(world,player,at))return p;
            }
            return null;
        }
        public bool OpenShop()
        {
            if(ShopOpen||world.MapOpen||world.flying||world.HazardInputBlocked||world.ActionLocked||world.IsDashing||NearbyShop()==null)return false;
            ShopOpen=true;shopSource=NearbyShop();ShopSelection=0;shopNotice="상품을 선택하면 교환 효과와 필요한 먹빛을 확인할 수 있습니다.";Time.timeScale=0;GameAudio.Emit(SoundCue.MapOpen,world.player.position,.45f);return true;
        }
        public void CloseShop()
        {
            if(!ShopOpen)return;CancelInteractionChoice();ShopOpen=false;shopReleaseFrame=Time.frameCount;Time.timeScale=1;GameAudio.Emit(SoundCue.MapClose,world.player.position,.4f);
        }
        public bool HandleShopInput()
        {
            if(Time.frameCount<=shopReleaseFrame)return true;
            var k=Keyboard.current;if(k==null)return ShopOpen||InteractionChoiceOpen;
            if(InteractionChoiceOpen)return HandleInteractionChoiceInput(k);
            if(!ShopOpen){if(k.eKey.wasPressedThisFrame)TryInteract();return ShopOpen||InteractionChoiceOpen;}
            if(k.eKey.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame){BackFromShop();return true;}
            if(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)ShopSelection=(ShopSelection+1)%3;
            if(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame||k.leftArrowKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame)ShopSelection=(ShopSelection+2)%3;
            if(k.enterKey.wasPressedThisFrame)RequestPurchase(ShopSelection);
            return true;
        }
        string shopNotice="처치 보상으로 작은 성장을 쌓아 보세요.";
        public bool BuyOffer(int i)
        {
            if(!ShopOpen||i<0||i>=3||NearbyShop()==null)return false;
            int price=OfferPrice(i);
            if(price==0){shopNotice="이미 끝까지 강화했습니다.";return false;}
            if(!OfferUnlocked(i)){shopNotice="먼저 탐험으로 회복 물감을 얻어야 합니다.";return false;}
            if(InkDust<price){shopNotice="먹빛 가루가 "+(price-InkDust)+"개 더 필요합니다.";return false;}
            InkDust-=price;
            if(i==0){BindingLevel++;Health=Mathf.Min(MaxHealth,Health+1);}
            else if(i==1){BottleLevel++;HealingCharges=Mathf.Min(MaxHealingCharges,HealingCharges+1);}
            else GatherLevel++;
            shopNotice=OfferNames[i]+" 강화 완료 · "+OfferLevel(i)+"단계";
            GameAudio.Emit(SoundCue.Restore,world.player.position,.55f);
            if(Application.isPlaying)WorldSession.Save(world);
            return true;
        }
        void CaptureEconomy(StilldrawnSave s)
        {
            // Uncollected drops remain earned across saving/reloading, including a boss
            // dying immediately before an autosave. They are not credited twice in RAM.
            s.inkDust=Mathf.Min(999999,InkDust+EnemyRewardDrops.Pending(world));
            s.bindingLevel=BindingLevel;s.bottleLevel=BottleLevel;s.gatherLevel=GatherLevel;
        }
        void RestoreEconomy(StilldrawnSave s)
        {
            EnemyRewardDrops.Discard(world);
            InkDust=Mathf.Clamp(s.inkDust,0,999999);BindingLevel=Mathf.Clamp(s.bindingLevel,0,3);
            BottleLevel=Mathf.Clamp(s.bottleLevel,0,2);GatherLevel=Mathf.Clamp(s.gatherLevel,0,3);
            ShopOpen=false;shopReleaseFrame=-1;ResetInteractionChoice();
        }
        void DrawEconomyHud()
        {
            PaintedHudArt.Draw("shard",new Rect(114,190,23,28),new Color(.77f,.68f,.9f));
            HudText(new Rect(157,192,120,30),"먹빛 가루",hudMuted,18);
            HudText(new Rect(280,187,150,38),InkDust.ToString("N0"),hudInk,25,number:true);
            if(!world.flying&&!world.HazardInputBlocked&&NearbyShop()!=null){
                HudPanel(new Rect(720,922,480,59),1);HudText(new Rect(780,937,390,32),"E  갈피 교환소",hudInk,21);
            }
        }

    }
    public sealed partial class WorldPrototype
    {
        [System.Serializable] sealed class EconomySnapshot {public int dust,pending,dropEvents,binding,bottle,gather;public float maxHealth,radius;public bool choice;public string choiceKind,choiceTitle;public bool shop,nearShop,paused,grounded,hazard,locked,dashing,flying;}
        public void ReportEconomy()
        {
            var p=Progression;Debug.Log("ECONOMY_STATE "+JsonUtility.ToJson(new EconomySnapshot{dust=p.InkDust,pending=EnemyRewardDrops.Pending(this),dropEvents=GetComponent<EnemyRewardDrops>()?.SpawnEvents??0,binding=p.BindingLevel,bottle=p.BottleLevel,gather=p.GatherLevel,maxHealth=p.MaxHealth,radius=p.GatherRadius,shop=p.ShopOpen,choice=p.InteractionChoiceOpen,choiceKind=p.ChoiceKind,choiceTitle=p.ChoiceTitle,nearShop=p.NearbyShop()!=null,paused=Time.timeScale==0,grounded=Motor.grounded,hazard=HazardInputBlocked,locked=ActionLocked,dashing=IsDashing,flying=flying}));
        }
    }
}
