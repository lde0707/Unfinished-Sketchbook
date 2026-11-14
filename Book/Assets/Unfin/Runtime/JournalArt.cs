using UnityEngine;
namespace Unfin
{
    public static class JournalArt
    {
        static Texture2D book;
        public static Texture2D Book=>book!=null?book:(book=Resources.Load<Texture2D>("JournalUI/open-journal"));
        public static void Draw(Rect rect){if(Book!=null)GUI.DrawTexture(rect,Book,ScaleMode.StretchToFill,true);}
    }
}
