using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    public static class SpecialBlockFeedback
    {
        public const int MaxCells=10;
        public static int Events {get;private set;}
        public static int LastCells {get;private set;}
        public static SoundCue LastCue {get;private set;}
        public static SoundCue Cue(int type)=>type==2?SoundCue.BreakGraphite:type==5?SoundCue.BreakVine:type==13?SoundCue.BreakSeal:type==15||type==18?SoundCue.BreakWax:SoundCue.BreakWood;
        public static void Emit(WorldPrototype world,IList<Vector2Int> cells,int type)
        {
            if(cells.Count==0)return;Events++;LastCells=cells.Count;LastCue=Cue(type);
            Vector2 at=new Vector2(cells[0].x+.5f,-cells[0].y-.5f);float nearest=float.MaxValue;
            foreach(var c in cells){var p=new Vector2(c.x+.5f,-c.y-.5f);float d=Vector2.SqrMagnitude(p-(Vector2)world.player.position);if(d<nearest){nearest=d;at=p;}}
            GameAudio.Emit(LastCue,at,.95f);
            var fx=PaperVfx.Get();if(fx==null)return;
            int n=Mathf.Min(MaxCells,cells.Count);
            for(int i=0;i<n;i++){var c=cells[i*cells.Count/n];var p=new Vector2(c.x+.5f,-c.y-.5f);if(Vector2.Distance(p,world.player.position)<14)fx.BreakTile(p,type,cells.Count>4?4:7);}
        }
    }
}
