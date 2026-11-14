using UnityEngine;
namespace Unfin
{
    public sealed class ZoneEntryBanner
    {
        public static readonly string[] Names={"구겨진 종이굴","흑연 광산","스케치 나무 속","엉킨 뿌리 심부","노을 크레파스 굴","밀랍 공방","색칠 지하 협곡","기사의 탑"};
        public const float FadeIn=.7f,Hold=2.3f,FadeOut=1f,Duration=FadeIn+Hold+FadeOut;
        public const float LetterBrightness=.85f;
        public int Zone {get;private set;}=-1;
        public int Serial {get;private set;}
        public float Age {get;private set;}
        public float Alpha=>Mathf.SmoothStep(0,1,Mathf.Clamp01(Age/FadeIn))*Mathf.SmoothStep(0,1,Mathf.Clamp01((Duration-Age)/FadeOut));
        public float OffsetY=>Mathf.Lerp(-19,0,Mathf.SmoothStep(0,1,Mathf.Clamp01(Age/FadeIn)));
        int candidate=-1;float stable;
        public void Tick(int next,float dt)
        {
            Age=Mathf.Min(Duration,Age+dt);
            if(next<0||next>=Names.Length)return;
            if(Zone<0){Enter(next);return;}
            if(next==Zone){candidate=-1;stable=0;return;}
            if(candidate!=next){candidate=next;stable=0;}
            stable+=dt;if(stable>=.3f)Enter(next);
        }
        void Enter(int next){Zone=next;Age=0;candidate=-1;stable=0;Serial++;}
        public void Draw()
        {
            if(Zone<0||Alpha<=0)return;
            // Shared height preserves lettering size; short names stay narrower.
            float height=112,width=PaintedHudArt.Aspect("zone-"+Zone)*height;
            var old=GUI.color;GUI.color=new Color(old.r*LetterBrightness,old.g*LetterBrightness,old.b*LetterBrightness,old.a*Alpha);
            PaintedHudArt.Draw("zone-"+Zone,new Rect(960-width*.5f,72+OffsetY,width,height));GUI.color=old;
        }
    }
}
