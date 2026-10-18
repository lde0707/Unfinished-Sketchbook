using System;
using UnityEngine;

namespace Unfin
{
    [Serializable] public class PaperSpriteFrame
    {
        public Sprite sprite;
        public float duration;
        public Vector2 head;
    }
    [Serializable] public class PaperSpriteClip
    {
        public string name;
        public bool loop;
        public float anchorX;
        public float groundY=448;
        public PaperSpriteFrame[] frames;
        public float Duration { get { float t=0; foreach(var f in frames)t+=f.duration; return t; } }
        public int FrameAt(float time)
        {
            time=loop?Mathf.Repeat(time,Duration):Mathf.Max(0,time);
            for(int i=0;i<frames.Length;i++){if(time<frames[i].duration)return i;time-=frames[i].duration;}
            return frames.Length-1;
        }
    }
    public sealed class PaperSpriteLibrary : ScriptableObject
    {
        public float pixelsPerUnit=256;
        public Sprite thrustEffect;
        public PaperSpriteClip[] clips;
        public PaperSpriteClip Find(string name) => Array.Find(clips,c=>c.name==name);
    }
}
