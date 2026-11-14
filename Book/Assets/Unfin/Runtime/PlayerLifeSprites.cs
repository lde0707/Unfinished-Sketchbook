using System;
using UnityEngine;
namespace Unfin
{
    public static class PlayerLifeSprites
    {
        public const float ReviveHoldDuration=1f;
        public const float DeathDuration=.83f,ReviveDuration=ReviveHoldDuration+.71f;
        [Serializable] class Cell {public int[] sourceRect;public float bottom,anchorX;}
        [Serializable] class Scale {public float death,revive;}
        [Serializable] class Registration {public Scale scale;public Cell[] death,revive;}
        static PaperSpriteClip death,revive;
        static readonly float[] DeathTimes={.09f,.09f,.11f,.12f,.14f,.28f},ReviveTimes={ReviveHoldDuration+.14f,.11f,.1f,.09f,.09f,.18f};
        public static PaperSpriteClip Find(string name)
        {
            if(name!="Death"&&name!="Revive")return null;
            if(death==null||death.frames[0].sprite==null){
                var r=JsonUtility.FromJson<Registration>(Resources.Load<TextAsset>("PlayerLife/registration").text);
                death=Create("Death","death",r.death,r.scale.death,DeathTimes);
                revive=Create("Revive","revive",r.revive,r.scale.revive,ReviveTimes);
            }
            return name=="Death"?death:revive;
        }
        static PaperSpriteClip Create(string name,string file,Cell[] cells,float scale,float[] times)
        {
            var texture=Resources.Load<Texture2D>("PlayerLife/"+file);
            var clip=new PaperSpriteClip{name=name,frames=new PaperSpriteFrame[cells.Length]};
            // Preview Idle uses 0.75x source size; match its approved head/body scale.
            float ppu=256*.75f/scale;
            for(int i=0;i<cells.Length;i++){
                var c=cells[i];var rect=c.sourceRect;
                var sprite=Sprite.Create(texture,new Rect(rect[0],texture.height-rect[1]-rect[3],rect[2],rect[3]),new Vector2(c.anchorX/rect[2],1-c.bottom/rect[3]),ppu,0,SpriteMeshType.FullRect);
                clip.frames[i]=new PaperSpriteFrame{sprite=sprite,duration=times[i]};
            }
            return clip;
        }
    }
}
