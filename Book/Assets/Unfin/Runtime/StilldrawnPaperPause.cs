using UnityEngine;
namespace Unfin
{
    public sealed partial class StilldrawnTitle
    {
        Texture2D paperPause;
        static readonly string[] PauseLabels={"계속하기","설정","조작 방법","저장하고 타이틀로"};
        void DrawPaperPause()
        {
            if(paperPause==null)paperPause=Resources.Load<Texture2D>("PaperMenuV2/pause-panel");
            const float size=600f/1024;
            var panel=new Rect(660,65,600,900);
            if(paperPause!=null)GUI.DrawTexture(panel,paperPause,ScaleMode.ScaleToFit,true);
            var align=button.alignment;button.alignment=TextAnchor.MiddleCenter;
            Label(720,165,480,55,"잠시 머무르기",34,ink,menu:true);
            Label(720,222,480,35,"STILLDRAWN",18,muted,menu:true);
            for(int i=0;i<PauseLabels.Length;i++){
                var r=new Rect(panel.x+150*size,panel.y+(430+200*i)*size,724*size,132*size);
                bool selected=Highlighted(i,r);
                if(selected&&!keyboardNavigation&&Event.current.type==EventType.Repaint&&selection!=i){selection=i;MenuSound(false);}
                Label(r.x,r.y,r.width,r.height,PauseLabels[i],i==3?24:28,selected?ink:new Color(.69f,.66f,.59f),menu:true);
                if(selected)Diamond(r.x+26,r.center.y,5,gold);
                var e=Event.current;if(e.type==EventType.MouseDown&&e.button==0&&r.Contains(e.mousePosition)){selection=i;Activate(i);e.Use();break;}
            }
            Label(740,810,440,40,"Esc · 계속하기",18,muted,menu:true);
            button.alignment=align;
        }
    }
}
