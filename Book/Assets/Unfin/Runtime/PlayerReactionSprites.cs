using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    [Serializable] public class ReactionClipSource {public string name;public float[] durations;}
    [Serializable] public class ReactionManifest {public float pixelsPerUnit,anchorX,groundY;public ReactionClipSource[] clips;}
    public static class PlayerReactionSprites
    {
        static ReactionManifest manifest;
        static readonly Dictionary<string,PaperSpriteClip> clips=new Dictionary<string,PaperSpriteClip>();
        public static PaperSpriteClip Find(string name)
        {
            if(name!="Hurt"&&name!="Parry"&&name!="ParrySuccess")return null;
            if(clips.TryGetValue(name,out var cached)&&cached.frames[0].sprite!=null)return cached;
            if(manifest==null)manifest=JsonUtility.FromJson<ReactionManifest>(Resources.Load<TextAsset>("PlayerReactions/manifest").text);
            var source=Array.Find(manifest.clips,c=>c.name==name);
            var clip=new PaperSpriteClip{name=name,frames=new PaperSpriteFrame[source.durations.Length]};
            for(int i=0;i<clip.frames.Length;i++){
                var t=Resources.Load<Texture2D>("PlayerReactions/"+name+"/"+i.ToString("D2"));
                if(t==null)throw new InvalidOperationException("Missing player reaction "+name+" "+i);
                clip.frames[i]=new PaperSpriteFrame{sprite=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(manifest.anchorX/t.width,1-manifest.groundY/t.height),manifest.pixelsPerUnit,0,SpriteMeshType.FullRect),duration=source.durations[i]};
            }
            return clips[name]=clip;
        }
    }
}
