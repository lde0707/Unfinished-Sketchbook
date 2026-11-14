using UnityEngine;
using UnityEngine.InputSystem;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        WorldMarker pendingInteraction,shopSource;
        int pendingPurchase=-1,choiceFrame=-1;
        public int ChoiceSelection {get;private set;}
        public bool InteractionChoiceOpen=>pendingInteraction!=null||pendingPurchase>=0;
        public string ChoiceKind=>pendingPurchase>=0?"purchase":pendingInteraction?.type??"";
        public static bool NeedsInteractionChoice(string kind)=>kind=="save"||kind=="station"||kind=="lever";
        public string ChoiceTitle=>pendingPurchase>=0?OfferNames[pendingPurchase]+" 교환":InteractionName(pendingInteraction);
        public string ChoiceAction=>pendingPurchase>=0?"교환하기":pendingInteraction==null?"확인":pendingInteraction.type=="save"?"상점 보기":pendingInteraction.type=="station"?"이동하기":pendingInteraction.type=="lever"?"작동하기":pendingInteraction.type=="sketch"?"기억하기":pendingInteraction.type=="diary"?"읽기":"획득하기";
        static string InteractionName(WorldMarker p)
        {
            if(p==null)return "";
            switch(p.type){case "save":return "갈피 교환소";case "station":return "리본 포탈";case "lever":return "지름길 레버";case "sketch":return p.optionsJson.Contains("sun")?"그림 친구 · 해님":"그림 친구 · 강아지";case "diary":return "남겨진 일지";case "wj":return "또렷해진 손";case "shard":return "흑연 조각";case "heart":return "체력 조각";case "healup":return "회복 물감 병";case "item_heal":return "회복 물감";case "item_shield":return "종이 방패";case "item_paint":return "미완성 물감";case "sw_r":return "빨강 물감통";case "sw_b":return "파랑 물감통";case "sw_y":return "노랑 물감통";default:return AbilityName(p.type);}
        }
        public string ChoiceDescription
        {
            get{
                if(pendingPurchase>=0)return OfferEffect(pendingPurchase)+"\n먹빛 가루 "+OfferPrice(pendingPurchase)+"개를 사용합니다.  보유 "+InkDust+"개";
                var p=pendingInteraction;if(p==null)return "";
                switch(p.type){case "save":return "모아 둔 먹빛 가루로 책등과 도구를 강화합니다.\n상점의 물건을 살펴보시겠어요?";case "station":return PortalDestinationLabel(p)+"\n연결된 리본을 따라 이동하시겠어요?";case "lever":return "잠긴 뿌리 지름길을 엽니다.\n레버를 작동하시겠어요?";case "sketch":return "미완성 그림을 완성하고 친구를 기억합니다.\n이 친구를 기록에 남기시겠어요?";case "diary":return "이곳에 남겨진 이야기를 읽고 기록합니다.";case "wj":return PickupInstructions("wj");case "heart":return "최대 체력이 반 칸 늘어납니다.";case "healup":return "회복 물감의 최대 사용 횟수가 한 번 늘어납니다.";case "item_heal":return "회복 물감을 사용할 수 있게 됩니다.";case "item_shield":return "저장 후 첫 피격을 막아 주는 종이 방패를 얻습니다.";default:return PickupInstructions(p.type);}
            }
        }
        string PortalDestinationLabel(WorldMarker p)
        {
            var source=JsonUtility.FromJson<Station>(p.optionsJson);
            if(!Pairs.TryGetValue(source.sid,out string destination))return "아직 이어지지 않은 리본";
            var target=System.Array.Find(world.Data.pickups,m=>m.type=="station"&&JsonUtility.FromJson<Station>(m.optionsJson).sid==destination);
            if(target==null)return "연결된 리본";
            var room=System.Array.Find(world.Data.rooms,r=>target.tx>=r.x&&target.tx<r.x+r.w&&target.floor>r.y&&target.floor<=r.y+r.h);
            return "목적지 · "+(room==null?(target.tx<world.Data.regionX?"연필 숲":"크레파스 언덕"):room.name);
        }
        void QueueInteraction(WorldMarker p)
        {
            // Acquisition never opens a modal, even if a future caller uses this entry point.
            if(!NeedsInteractionChoice(p.type)){ExecuteInteraction(p);return;}
            pendingInteraction=p;pendingPurchase=-1;ChoiceSelection=0;choiceFrame=Time.frameCount;
            Time.timeScale=0;GameAudio.Emit(SoundCue.MapOpen,world.player.position,.35f);
        }
        public bool RequestPurchase(int index)
        {
            if(!ShopOpen||InteractionChoiceOpen||index<0||index>=3)return false;
            ShopSelection=index;
            if(OfferPrice(index)==0||!OfferUnlocked(index)||InkDust<OfferPrice(index)){ExplainUnavailableOffer(index);return false;}
            pendingPurchase=index;pendingInteraction=null;ChoiceSelection=0;choiceFrame=Time.frameCount;return true;
        }
        public void CancelInteractionChoice()
        {
            if(!InteractionChoiceOpen)return;pendingInteraction=null;pendingPurchase=-1;
            shopReleaseFrame=Time.frameCount;Time.timeScale=ShopOpen?0:1;
        }
        public bool ConfirmInteractionChoice()
        {
            if(!InteractionChoiceOpen)return false;
            int purchase=pendingPurchase;var target=pendingInteraction;pendingInteraction=null;pendingPurchase=-1;
            Time.timeScale=ShopOpen?0:1;shopReleaseFrame=-1;bool completed=false;
            if(purchase>=0)completed=BuyOffer(purchase);
            else if(target!=null&&!world.HazardInputBlocked&&!world.ActionLocked&&!world.IsDashing&&!world.flying&&
                (target.type=="save"||target.type=="station"||!Collected(target))&&
                Vector2.Distance(world.Motor.WorldBounds.center,SocialPosition(target))<=(target.type=="save"?2.2f:1.8f)&&
                InteractionProximity.Visible(world,world.Motor.WorldBounds.center,SocialPosition(target)))completed=ExecuteInteraction(target);
            else Say("물체에 다시 가까이 다가가 주세요.",2);
            shopReleaseFrame=Time.frameCount;return completed;
        }
        public void BackFromShop()
        {
            if(!ShopOpen)return;var source=shopSource??NearbyShop();CloseShop();
            if(source!=null)QueueInteraction(source);
        }
        bool HandleInteractionChoiceInput(Keyboard k)
        {
            if(Time.frameCount<=choiceFrame)return true;
            if(k.escapeKey.wasPressedThisFrame){CancelInteractionChoice();return true;}
            if(k.leftArrowKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame||k.upArrowKey.wasPressedThisFrame||k.downArrowKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)ChoiceSelection=1-ChoiceSelection;
            if(k.enterKey.wasPressedThisFrame||k.eKey.wasPressedThisFrame){if(ChoiceSelection==0)ConfirmInteractionChoice();else CancelInteractionChoice();}
            return true;
        }
        void ResetInteractionChoice(){pendingInteraction=shopSource=null;pendingPurchase=-1;choiceFrame=-1;ClosePickupNotice();}
    }
}

