using System;
using System.Collections.Generic;
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
        [NonSerialized] AnimationTimeline timeline;
        [NonSerialized] PaperSpriteFrame[] cachedFrames;
        AnimationTimeline Timeline
        {
            get
            {
                if(timeline==null||!ReferenceEquals(cachedFrames,frames))
                {
                    cachedFrames=frames;
                    timeline=new AnimationTimeline(frames.Length,i=>frames[i].duration);
                }
                return timeline;
            }
        }
        public float Duration=>Timeline.Duration;
        public int FrameAt(float time)=>Timeline.FrameAt(time,loop);
        public void InvalidateTiming(){timeline=null;cachedFrames=null;}
    }
    public sealed class PaperSpriteLibrary : ScriptableObject
    {
        public float pixelsPerUnit=256;
        public Sprite thrustEffect;
        public PaperSpriteClip[] clips;
        readonly Dictionary<string,PaperSpriteClip> lookup=new Dictionary<string,PaperSpriteClip>(StringComparer.Ordinal);
        PaperSpriteClip[] cachedClips;
        public PaperSpriteClip Find(string name)
        {
            if(!ReferenceEquals(cachedClips,clips))RebuildLookup();
            return name!=null&&lookup.TryGetValue(name,out var clip)?clip:null;
        }
        void RebuildLookup()
        {
            lookup.Clear();cachedClips=clips;
            if(clips==null)return;
            foreach(var clip in clips)if(clip!=null&&clip.name!=null){clip.InvalidateTiming();lookup[clip.name]=clip;}
        }
        void OnEnable(){RebuildLookup();}
        void OnValidate(){RebuildLookup();}
    }
}
