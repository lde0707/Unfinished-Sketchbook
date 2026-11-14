using System;
using UnityEngine;
namespace Unfin
{
    public static class CheckpointPlan
    {
        [Serializable] public class Point {public int x,floor;public string room;public bool shop;}
        [Serializable] class Plan {public Point[] points;}
        static Point[] points;
        public static Point[] Points => points??(points=JsonUtility.FromJson<Plan>(Resources.Load<TextAsset>("CheckpointPlan").text).points);
        // Preserve the HTML source snapshot; this is the current Unity placement plan.
        public static bool Keep(WorldMarker marker)=>marker.type!="save"||Array.Exists(Points,p=>p.x==marker.tx&&p.floor==marker.floor);
        public static bool IsShop(WorldMarker marker)=>marker!=null&&marker.type=="save"&&Array.Exists(Points,p=>p.shop&&p.x==marker.tx&&p.floor==marker.floor);
        public static void Apply(WorldData data){data.pickups=Array.FindAll(data.pickups,Keep);}
        public static bool HasMarker(WorldData data,Vector2 html)=>Array.Exists(data.pickups,p=>p.type=="save"&&Mathf.Abs(p.x-html.x)<.1f&&Mathf.Abs(p.y-html.y)<.1f);
    }
}
