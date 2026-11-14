using System;
using UnityEngine;
namespace Unfin
{
    [Serializable] public class PropPlacement {public string kind;public Vector2 html;public Vector3 position;public float height;}
    [Serializable] public class PropPlacementPlan {public PropPlacement[] entries;}
    public static class WorldPropPlacement
    {
        static PropPlacementPlan plan;
        public static Rect CollectionBox(WorldMarker p){
            var e=Find(p.type,new Vector2(p.x,p.y));
            if(e==null||p.type=="save"||p.type=="station")return new Rect(p.x/16-.15f,p.y/16-.15f,p.w/16+.3f,p.h/16+.3f);
            float width=Mathf.Max(p.w/16,e.height*.8f)+.3f,height=Mathf.Max(p.h/16,e.height)+.3f;
            return new Rect(e.position.x-width*.5f,-e.position.y-height*.5f,width,height);
        }
        public static void ClearCache(){plan=null;}
        public static PropPlacement Find(string kind,Vector2 html){
            if(plan==null){var source=Resources.Load<TextAsset>("WorldPropPlacement");plan=source==null?new PropPlacementPlan{entries=new PropPlacement[0]}:JsonUtility.FromJson<PropPlacementPlan>(source.text);}
            return Array.Find(plan.entries,p=>p.kind==kind&&Vector2.Distance(p.html,html)<.01f);
        }
    }
}
