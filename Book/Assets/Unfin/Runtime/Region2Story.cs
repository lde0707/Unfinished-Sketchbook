using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    [Serializable] public class Region2Beat {public int room;public string text;}
    [Serializable] public class Region2Narrative {public Region2Beat[] beats;public string[] before,after;public string diary;}
    public sealed class Region2Story : MonoBehaviour
    {
        static Region2Narrative story;static readonly Dictionary<WorldPrototype,int> conversations=new Dictionary<WorldPrototype,int>();
        WorldPrototype world;readonly HashSet<int> seen=new HashSet<int>();
        public static Region2Narrative Text=>story??(story=JsonUtility.FromJson<Region2Narrative>(Resources.Load<TextAsset>("Region2/story").text));
        public static void Talk(WorldPrototype w){conversations.TryGetValue(w,out int n);var lines=w.Progression.DefeatedEncounters.Contains(91)?Text.after:Text.before;w.Progression.Say(lines[n%lines.Length],5);conversations[w]=n+1;Region2Presentation.Burst(w,w.player.position,"item");}
        public static void ReadDiary(WorldPrototype w){w.Progression.Say(Text.diary,7);Region2Presentation.Burst(w,w.player.position,"item");}
        public static void Create(WorldPrototype w){if(!Application.isPlaying)return;var s=w.gameObject.AddComponent<Region2Story>();s.world=w;}
        void Update(){if(world==null||world.MapOpen||world.HazardInputBlocked||world.player.position.x<world.Data.regionX)return;var p=world.player.position;var room=Array.Find(world.Data.rooms,r=>r.r==2&&p.x>=r.x&&p.x<r.x+r.w&&-p.y>=r.y&&-p.y<r.y+r.h);if(room==null||seen.Contains(room.n))return;seen.Add(room.n);var beat=Array.Find(Text.beats,b=>b.room==room.n);if(beat!=null)world.Progression.Say(beat.text,4.5f);}
    }
}
