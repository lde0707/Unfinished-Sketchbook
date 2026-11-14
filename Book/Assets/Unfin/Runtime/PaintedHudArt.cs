using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    public static class PaintedHudArt
    {
        [Serializable]public class Entry{public string name,sheet;public int x,y,w,h;}
        [Serializable]public class Catalog{public Entry[] entries;}
        static Catalog catalog;static readonly Dictionary<string,Texture2D> sheets=new Dictionary<string,Texture2D>();static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static Entry Find(string name){if(catalog==null){var json=Resources.Load<TextAsset>("PaintedUI/atlas");if(json==null)return null;catalog=JsonUtility.FromJson<Catalog>(json.text);}return Array.Find(catalog.entries,e=>e.name==name);}
        static Texture2D Sheet(string name){if(!sheets.TryGetValue(name,out var tex)){tex=Resources.Load<Texture2D>("PaintedUI/"+name);sheets[name]=tex;}return tex;}
        public static bool Ready=>Find("leaf")!=null;
        public static Texture2D MapPaper=>Sheet("cartographer-panel");
        public static void DrawMapPaper(Rect rect){var tex=MapPaper;if(tex!=null)GUI.DrawTextureWithTexCoords(rect,tex,new Rect(.13f,.18f,.78f,.58f),true);}
        public static float Aspect(string name){var e=Find(name);return e==null?4:(float)e.w/e.h;}
        public static void Draw(string name,Rect rect,Color? tint=null,float fraction=1){
            var e=Find(name);if(e==null)return;var tex=Sheet(e.sheet);if(tex==null)return;
            var old=GUI.color;if(tint.HasValue)GUI.color*=tint.Value;
            fraction=Mathf.Clamp01(fraction);GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,rect.width*fraction,rect.height),tex,new Rect((float)e.x/tex.width,(float)e.y/tex.height,(float)e.w/tex.width*fraction,(float)e.h/tex.height),true);GUI.color=old;
        }
        public static Sprite Pickup(string kind){string name=kind=="item_heal"?"heal":kind=="item_shield"?"shield":kind=="item_paint"?"paint":kind=="heart"||kind=="shard"||kind=="healup"?kind:null;return name==null?null:SpriteFor(name);}
        static Sprite SpriteFor(string name){if(sprites.TryGetValue(name,out var sprite)&&sprite!=null)return sprite;var e=Find(name);if(e==null)return null;var tex=Sheet(e.sheet);sprite=Sprite.Create(tex,new Rect(e.x,e.y,e.w,e.h),new Vector2(.5f,.5f),256,0,SpriteMeshType.FullRect);sprites[name]=sprite;return sprite;}
    }
}
