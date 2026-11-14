using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        sealed class PickupNotice {public string kind,title,description,category;}
        readonly Queue<PickupNotice> pickupNotices=new Queue<PickupNotice>();
        PickupNotice pickupNotice;
        float pickupNoticeAge;
        Texture2D acquiredPaper;
        public const float PickupNoticeDuration=6f;
        public static Rect PickupNoticeBounds=>new Rect(1268,820,620,620f/3);
        public bool PickupNoticeOpen=>pickupNotice!=null;
        public int PendingPickupNotices=>pickupNotices.Count;
        public float PickupNoticeAge=>pickupNoticeAge;
        public string PickupNoticeTitle=>pickupNotice?.title??"";
        public string PickupNoticeDescription=>pickupNotice?.description??"";
        public string PickupNoticeCategory=>pickupNotice?.category??"";
        public static string PickupInstructions(string kind)
        {
            switch(kind){
                case "wj":return "벽 쪽으로 이동해 벽을 짚고 천천히 미끄러집니다.\nSpace로 벽을 박차고 뛰어오릅니다.";
                case "climb":return "크레파스 벽도 붙잡을 수 있습니다.\n벽에서 W / S로 이동 · Space로 벽 점프";
                case "cut":return "공격으로 엉킨 뿌리를 베어 길을 엽니다.\nJ / 마우스 왼쪽 버튼";
                case "dash":return "Shift로 돌진해 금이 간 벽을 부숩니다.\n공중에서도 사용할 수 있습니다.";
                case "parry":return "공격이 닿기 직전에 K로 받아칩니다.\n공명하는 봉인도 깨울 수 있습니다.";
                case "item_heal":return "L로 체력을 2 회복합니다.\n저장 지점에서 사용 횟수가 채워집니다.";
                case "item_shield":return "첫 피격을 한 번 막아 줍니다.\n저장 지점에서 방패가 다시 준비됩니다.";
                case "item_paint":return "찢긴 페이지 끝에서 찾은 물감입니다.\n탐험의 흔적이 기록되었습니다.";
                case "shard":return "탐험 중 발견한 흑연 조각입니다.\n수집한 조각이 기록되었습니다.";
                case "sketch":return "잊힌 그림 친구를 완성했습니다.\n만난 친구가 탐험 기록에 남습니다.";
                case "sw_r":case "sw_b":case "sw_y":return "세 가지 색의 물감통을 모으면\n잠긴 물감 문을 열 수 있습니다.";
                default:return "새로운 발견이 기록되었습니다.";
            }
        }
        void ShowPickupNotice(WorldMarker marker)
        {
            if(!Collected(marker))return;
            var notice=new PickupNotice{kind=marker.type,
                title=InteractionName(marker)+(marker.type=="diary"?" 기록":" 획득"),
                category=marker.type=="wj"?"벽타기 · 벽 점프":marker.type=="climb"?"벽 위아래 이동":marker.type=="diary"?"남겨진 이야기":"새로운 발견",
                description=marker.type=="heart"?"최대 체력이 반 칸 늘었습니다.\n현재 최대 체력  "+MaxHealth.ToString("0.#"):
                    marker.type=="healup"?"회복 물감 최대 사용 횟수 +1\n"+(HealingUnlocked?"최대 사용 횟수  "+MaxHealingCharges:"회복 물감 획득 후 적용됩니다."):
                    marker.type=="diary"?Toast:PickupInstructions(marker.type)};
            EnqueuePickupNotice(notice);
        }
        void ShowAbilityNotice(string ability)
        {
            string kind=ability=="walljump"?"wj":ability;
            EnqueuePickupNotice(new PickupNotice{kind=kind,title=InteractionName(new WorldMarker{type=kind})+" 획득",
                category="새로운 능력",description=PickupInstructions(kind)});
        }
        void EnqueuePickupNotice(PickupNotice notice)
        {
            if(pickupNotice==null){pickupNotice=notice;pickupNoticeAge=0;}else pickupNotices.Enqueue(notice);
            toastTime=0;
            // Notices never pause, consume a key or prevent picking up another item.
            if(Application.isPlaying)WorldSession.Save(world);
        }
        void TickPickupNotice(float dt)
        {
            if(pickupNotice==null)return;
            pickupNoticeAge+=Mathf.Max(0,dt);
            while(pickupNotice!=null&&pickupNoticeAge>=PickupNoticeDuration){
                pickupNoticeAge-=PickupNoticeDuration;
                pickupNotice=pickupNotices.Count>0?pickupNotices.Dequeue():null;
            }
        }
        public void ClosePickupNotice(){pickupNotice=null;pickupNotices.Clear();pickupNoticeAge=0;}
        void DrawPickupNotice()
        {
            if(pickupNotice==null)return;
            if(acquiredPaper==null)acquiredPaper=Resources.Load<Texture2D>("TradingUI/acquired-toast-v2");
            float enter=Mathf.SmoothStep(0,1,pickupNoticeAge/.28f),leave=Mathf.SmoothStep(0,1,(PickupNoticeDuration-pickupNoticeAge)/.6f);
            var old=GUI.color;GUI.color*=new Color(1,1,1,enter*leave);
            var panel=PickupNoticeBounds;panel.x+=(1-enter)*20;
            if(acquiredPaper!=null)GUI.DrawTexture(panel,acquiredPaper,ScaleMode.ScaleToFit,true);
            string kind=pickupNotice.kind;
            var icon=PaintedHudArt.Pickup(kind)??PaperPickupVisual.Load(kind.StartsWith("sw_")?"pigment":kind);
            if(icon!=null){var sr=icon.rect;float scale=Mathf.Min(74/sr.width,76/sr.height);var r=new Rect(panel.x+84-sr.width*scale*.5f,panel.y+104-sr.height*scale*.5f,sr.width*scale,sr.height*scale);GUI.DrawTextureWithTexCoords(r,icon.texture,new Rect(sr.x/icon.texture.width,sr.y/icon.texture.height,sr.width/icon.texture.width,sr.height/icon.texture.height),true);}
            TradeText(new Rect(panel.x+132,panel.y+42,452,30),PickupNoticeTitle,23,hudInk,heading:true);
            TradeText(new Rect(panel.x+132,panel.y+78,452,82),PickupNoticeDescription,kind=="diary"?16:19,hudMuted);
            GUI.color=old;
        }
    }
    public sealed partial class WorldPrototype
    {
        public void ReportPickupNotice(){var p=Progression;Debug.Log("PICKUP_NOTICE "+JsonUtility.ToJson(new PickupNoticeSnapshot{open=p.PickupNoticeOpen,title=p.PickupNoticeTitle,description=p.PickupNoticeDescription,walljump=p.HasAcquired("walljump"),healing=p.HealingUnlocked,choice=p.InteractionChoiceOpen,paused=Time.timeScale==0,age=p.PickupNoticeAge,queued=p.PendingPickupNotices,blocksInput=p.ShopBlocksInput}));}
        [System.Serializable]sealed class PickupNoticeSnapshot{public bool open,walljump,healing,choice,paused,blocksInput;public string title,description;public float age;public int queued;}
    }
}
