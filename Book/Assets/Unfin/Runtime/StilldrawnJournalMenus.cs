using UnityEngine;
namespace Unfin
{
    public sealed partial class StilldrawnTitle
    {
        readonly System.Collections.Generic.List<(string name,string key)> controlRows=new System.Collections.Generic.List<(string,string)>();
        string controlNotes;
        void RefreshControlGuide()
        {
            // The title uses the saved bookmark; paused play uses the live inventory.
            // Editor movement overrides must not reveal abilities that were never earned.
            bool saved=!paused&&!preview;
            bool Owns(string ability)=>saved?record?.abilities!=null&&System.Array.IndexOf(record.abilities,ability)>=0:world.Progression.HasAcquired(ability);
            bool wall=Owns("walljump"),dash=Owns("dash"),parry=Owns("parry"),climb=Owns("climb");
            bool healing=saved?record!=null&&record.healing:world.Progression.HealingUnlocked;
            controlRows.Clear();
            controlRows.Add(("이동","A  D  /  ←  →"));
            controlRows.Add((wall?"점프 / 벽 점프":"점프","SPACE"));
            controlRows.Add(("공격","J  /  마우스 왼쪽"));
            if(dash)controlRows.Add(("대시","SHIFT"));
            if(parry)controlRows.Add(("패링","K"));
            if(healing)controlRows.Add(("회복","L"));
            if(climb)controlRows.Add(("벽 위아래 이동","W  S  /  ↑  ↓"));
            controlRows.Add(("대화 · 획득 · 작동 · 이동","E 한 번"));
            controlRows.Add(("발판 내려가기","S + SPACE"));
            controlRows.Add(("지도 / 일시 정지","M  /  ESC"));
            controlNotes="이동하거나 공중에 있을 때도 공격할 수 있습니다.";
            if(wall)controlNotes+="\n\n벽 쪽으로 이동해 벽을 짚고, 점프로 뛰어오를 수 있습니다.";
            if(dash)controlNotes+="\n\n공중에서도 대시할 수 있습니다.";
            controlNotes+="\n\n시작의 백지길과 삼색 광장의 교환소에서 E를 누르면 상점을 볼 수 있습니다.";
        }
        void Paragraph(float x,float y,float w,float h,string value,int size=22)
        {bool wrap=text.wordWrap;text.wordWrap=true;Label(x,y,w,h,value,size,muted);text.wordWrap=wrap;}
    }
}
