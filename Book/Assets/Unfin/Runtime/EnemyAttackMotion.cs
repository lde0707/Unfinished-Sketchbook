using System;
using UnityEngine;

namespace Unfin
{
    public static class EnemyAttackMotion
    {
        public struct Profile
        {
            public float distance,arc,drop;
            public Profile(float distance,float arc=0,float drop=0){this.distance=distance;this.arc=arc;this.drop=drop;}
        }
        public static Profile For(string id)
        {
            switch(id)
            {
                case "COMMON01":return new Profile(3.3f);
                case "COMMON02":return new Profile(2.6f,0,1.4f);
                case "R101":return new Profile(1.1f);
                case "R103":return new Profile(1.3f);
                case "A01":return new Profile(.65f);
                case "A02":return new Profile(2.8f,.9f);
                case "B01":return new Profile(.9f,.35f);
                case "B02":return new Profile(2.1f);
                case "B03":return new Profile(0,0,3.5f);
                case "C01":return new Profile(0,0,2.2f);
                case "C02":return new Profile(2.2f,.75f);
                case "C03":return new Profile(3.4f);
                case "D01":return new Profile(.9f);
                default:return new Profile(0);
            }
        }
        // Telegraph stays still. Frame 3 carries the strike; frame 4 recovers.
        public static Vector2 Offset(Profile p,float time,float[] durations,int facing)
        {
            float start=durations[0]+durations[1];
            float t=Mathf.Clamp01((time-start)/durations[2]);
            float recovery=Mathf.Clamp01((time-start-durations[2])/durations[3]);
            return new Vector2(facing*p.distance*t,p.arc*4*t*(1-t)-p.drop*Mathf.SmoothStep(0,1,t)*(1-Mathf.SmoothStep(0,1,recovery)));
        }
        // Lock the player's position during the telegraph; the charge stays
        // dodgeable rather than steering after the player throughout the strike.
        public static Vector2 Aim(Profile p,Vector2 delta)
        {
            return Vector2.ClampMagnitude(delta,Mathf.Max(p.distance,p.drop));
        }
        public static Vector2 AimedOffset(Vector2 aim,float time,float[] durations,bool returnHome)
        {
            float start=durations[0]+durations[1];
            float strike=Mathf.Clamp01((time-start)/durations[2]);
            float recovery=returnHome?Mathf.SmoothStep(0,1,Mathf.Clamp01((time-start-durations[2])/durations[3])):0;
            return aim*strike*(1-recovery);
        }
        public static Vector2 Sweep(Vector2 from,Vector2 to,float halfWidth,float height,Func<int,int,int> tile,bool guardEdge,float groundY)
        {
            if(tile==null)return to;
            Vector2 delta=to-from;int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.06f));
            Vector2 step=delta/steps,pos=from;
            for(int i=0;i<steps;i++)
            {
                Vector2 next=pos+Vector2.right*step.x;
                bool supported=!guardEdge||Support(next.x,groundY,halfWidth,tile);
                if(supported&&!Solid(next,halfWidth,height,tile))pos=next;
                next=pos+Vector2.up*step.y;
                if(!Solid(next,halfWidth,height,tile))pos=next;
            }
            return pos;
        }
        static bool Support(float x,float y,float halfWidth,Func<int,int,int> tile)
        {
            int row=Mathf.FloorToInt(-y+.08f);
            foreach(float px in new[]{x-halfWidth*.75f,x+halfWidth*.75f})
            {int t=tile(Mathf.FloorToInt(px),row);if(!GridMotor.Wall(t)&&!GridMotor.Platform(t))return false;}
            return true;
        }
        static bool Solid(Vector2 foot,float halfWidth,float height,Func<int,int,int> tile)
        {
            for(int x=Mathf.FloorToInt(foot.x-halfWidth+.02f);x<=Mathf.FloorToInt(foot.x+halfWidth-.02f);x++)
            for(int y=Mathf.FloorToInt(-foot.y-height+.02f);y<=Mathf.FloorToInt(-foot.y-.02f);y++)
                if(GridMotor.Wall(tile(x,y)))return true;
            return false;
        }
    }
}
