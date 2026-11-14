using UnityEngine;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        Texture2D tradingCounter,choicePaper;
        GUIStyle tradingText;
        void TradeText(Rect r,string value,int size,Color color,bool center=false,bool heading=false)
        {
            if(tradingText==null)tradingText=new GUIStyle(hudBody){wordWrap=true};
            tradingText.font=heading?hudHeading.font:hudBody.font;tradingText.fontSize=size;tradingText.alignment=center?TextAnchor.MiddleCenter:TextAnchor.MiddleLeft;tradingText.normal.textColor=color;
            GUI.Label(r,value,tradingText);
        }
        bool TradeButton(Rect bounds,string text,bool selected=false,bool enabled=true,bool embedded=false)
        {
            bool hover=enabled&&bounds.Contains(Event.current.mousePosition);var ink=enabled?(hover||selected?hudInk:hudMuted):new Color(.42f,.43f,.41f);
            if(!embedded){float h=bounds.width/PaintedHudArt.Aspect("key");var fitted=new Rect(bounds.x,bounds.center.y-h*.5f,bounds.width,h);PaintedHudArt.Draw("key",fitted,enabled?new Color(.80f,.76f,.66f):new Color(.42f,.42f,.39f));ink=enabled?new Color(.18f,.15f,.11f):new Color(.30f,.29f,.27f);}
            if(hover||selected)TradeText(new Rect(bounds.x+12,bounds.y,30,bounds.height),"◆",13,embedded?hudGold:new Color(.3f,.25f,.16f),true);
            TradeText(bounds,text,22,ink,true);
            if(enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&hover){Event.current.Use();return true;}return false;
        }
        string OfferEffect(int i)
        {
            if(OfferPrice(i)==0)return "끝까지 강화한 도구입니다.";
            return i==0?"최대 체력 "+MaxHealth.ToString("0.#")+" → "+(MaxHealth+1).ToString("0.#"):
                i==1?"회복 물감 사용 횟수 "+MaxHealingCharges+" → "+(MaxHealingCharges+1):
                "보상 회수 반경 "+GatherRadius.ToString("0.#")+" → "+(GatherRadius+.8f).ToString("0.#")+"칸";
        }
        void ExplainUnavailableOffer(int i){shopNotice=OfferPrice(i)==0?"이미 끝까지 강화했습니다.":!OfferUnlocked(i)?"먼저 탐험으로 회복 물감을 얻어야 합니다.":"먹빛 가루가 "+(OfferPrice(i)-InkDust)+"개 더 필요합니다.";}
        void DrawShop()
        {
            if(tradingCounter==null)tradingCounter=Resources.Load<Texture2D>("TradingUI/counter");
            if(tradingCounter!=null)GUI.DrawTexture(new Rect(192,28,1536,1024),tradingCounter,ScaleMode.ScaleToFit,true);
            TradeText(new Rect(335,114,720,66),"갈피 교환소",42,hudInk,heading:true);
            TradeText(new Rect(340,178,700,33),"남겨진 먹빛을, 오래 쓰는 도구로",19,hudMuted);
            PaintedHudArt.Draw("shard",new Rect(1280,122,32,39),new Color(.77f,.68f,.9f));
            TradeText(new Rect(1330,109,250,40),InkDust.ToString("N0"),30,hudInk);
            TradeText(new Rect(1330,150,250,30),"보유 먹빛 가루",18,hudMuted);
            for(int i=0;i<3;i++){
                float center=567+i*393;int price=OfferPrice(i),level=OfferLevel(i);bool chosen=ShopSelection==i;
                var slot=new Rect(center-166,275,332,460);bool hover=slot.Contains(Event.current.mousePosition);
                TradeText(new Rect(center-145,282,290,62),OfferNames[i],26,chosen||hover?hudInk:hudMuted,true,true);
                if(chosen)Fill(new Rect(center-84,340,168,2),hudGold);
                string status=price==0?"최대 단계 완료":!OfferUnlocked(i)?"회복 물감 획득 후 해금":"강화  "+level+" / "+OfferPrices[i].Length;
                TradeText(new Rect(center-140,615,280,44),status,18,!OfferUnlocked(i)?hudMuted:hudInk,true);
                TradeText(new Rect(center-104,677,208,42),price==0?"완료":price+" 먹빛",22,price>InkDust?new Color(.76f,.55f,.44f):hudInk,true);
                if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&hover){ShopSelection=i;Event.current.Use();}
            }
            TradeText(new Rect(353,809,1200,43),OfferNames[ShopSelection]+"  ·  "+OfferEffect(ShopSelection),25,hudInk,heading:true);
            TradeText(new Rect(353,849,1200,30),shopNotice,19,hudMuted);
            if(TradeButton(new Rect(355,881,220,66),"뒤로가기")){BackFromShop();return;}
            int cost=OfferPrice(ShopSelection);bool canBuy=cost>0&&OfferUnlocked(ShopSelection)&&InkDust>=cost;
            string buy=cost==0?"강화 완료":!OfferUnlocked(ShopSelection)?"아직 미해금":InkDust<cost?"먹빛 부족":"교환하기";
            if(TradeButton(new Rect(1345,881,220,66),buy,enabled:canBuy))RequestPurchase(ShopSelection);
            TradeText(new Rect(610,892,690,44),"← → 상품 선택 · Enter 교환 · Esc 뒤로",17,hudMuted,true);
        }
        void DrawInteractionChoice()
        {
            if(choicePaper==null)choicePaper=Resources.Load<Texture2D>("TradingUI/choice-panel");
            var panel=new Rect(510,240,900,600);if(choicePaper!=null)GUI.DrawTexture(panel,choicePaper,ScaleMode.ScaleToFit,true);
            TradeText(new Rect(625,367,670,64),ChoiceTitle,33,hudInk,true,true);
            TradeText(new Rect(625,447,670,135),ChoiceDescription,23,hudMuted,true);
            bool ready=Time.frameCount>choiceFrame;
            if(TradeButton(new Rect(675,657,270,75),ChoiceAction,ChoiceSelection==0,ready,true)){ConfirmInteractionChoice();return;}
            if(TradeButton(new Rect(975,657,270,75),"뒤로가기",ChoiceSelection==1,ready,true)){CancelInteractionChoice();return;}
            TradeText(new Rect(650,859,620,36),"← → 선택 · E / Enter 확인 · Esc 뒤로",18,hudMuted,true);
        }
    }
}
