using System.Collections.Generic;
using UnityEngine;

namespace Unfin
{
    public sealed partial class PaperSpritePlayer
    {
        readonly HashSet<PaperAttackTarget> hitTargets = new HashSet<PaperAttackTarget>();
        readonly List<PaperAttackTarget> pendingHits = new List<PaperAttackTarget>();
        // Opaque main silhouette of Effects/thrust.png; detached decorative flecks do not hit.
        static readonly Vector2[] ThrustOutline = {
            new Vector2(6,8),new Vector2(170,20),new Vector2(550,118),new Vector2(994,163),
            new Vector2(550,206),new Vector2(200,294),new Vector2(35,288),
            new Vector2(372,194),new Vector2(232,167),new Vector2(372,134)
        };
        readonly Vector2[] thrustPolygon = new Vector2[ThrustOutline.Length];
        public Vector2[] ActiveHitPolygon
        {
            get
            {
                if(!Attacking)return null;
                if(SlashEffectVisible)return outerArc.HitPolygon;
                if(attackEffect==null||!attackEffect.enabled)return null;
                var sprite=attackEffect.sprite;
                for(int i=0;i<ThrustOutline.Length;i++)
                {
                    var pixel=new Vector2(ThrustOutline[i].x,sprite.rect.height-ThrustOutline[i].y);
                    thrustPolygon[i]=attackEffect.transform.TransformPoint((pixel-sprite.pivot)/sprite.pixelsPerUnit);
                }
                return thrustPolygon;
            }
        }
        void ResolveAttackHits()
        {
            var polygon=ActiveHitPolygon;if(polygon==null)return;
            EnemyProjectileSystem.Get(world)?.Cut(polygon,world.player.position);
            if(!attackVfxEmitted){attackVfxEmitted=true;GameAudio.Emit(!airAttack&&stage==2?SoundCue.Thrust:SoundCue.Swing,world.player.position);PaperVfx.Get()?.Swing((Vector2)world.player.position+Vector2.right*AttackFacing*.8f,AttackFacing);}
            pendingHits.Clear();
            foreach(var target in PaperAttackTarget.Active)
            {
                if(target==null||hitTargets.Contains(target)||!OverlapsAttack(polygon,target.HitBounds))continue;
                // Test from the torso, not the floor anchor. Closed gates and solid terrain block attacks.
                Vector2 origin=world.player.position;
                Vector2 contact=target.HitBounds.ClosestPoint(origin);
                if(AttackPathClear(world.Motor,origin,contact))pendingHits.Add(target);
            }
            // Invoke callbacks outside registry enumeration (a receiver may disable itself).
            foreach(var target in pendingHits)if(target!=null&&target.isActiveAndEnabled){hitTargets.Add(target);target.ReceiveHit(AttackFacing,target.HitBounds.ClosestPoint(world.player.position));}
        }
        public static bool AttackPathClear(GridMotor motor,Vector2 from,Vector2 to)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)*32));
            for(int i=1;i<=steps;i++)
            {
                var point=Vector2.Lerp(from,to,(float)i/steps);
                if(GridMotor.Wall(motor.Get(Mathf.FloorToInt(point.x),Mathf.FloorToInt(-point.y))))return false;
            }
            return true;
        }
        public static bool OverlapsAttack(Vector2[] polygon,Bounds bounds)
        {
            var lo=(Vector2)bounds.min;var hi=(Vector2)bounds.max;
            foreach(var p in polygon)if(p.x>=lo.x&&p.x<=hi.x&&p.y>=lo.y&&p.y<=hi.y)return true;
            var corners=new[]{lo,new Vector2(hi.x,lo.y),hi,new Vector2(lo.x,hi.y)};
            foreach(var p in corners)
            {
                bool inside=false;
                for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                {
                    var a=polygon[i];var b=polygon[j];
                    if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
                }
                if(inside)return true;
            }
            for(int i=0;i<polygon.Length;i++)for(int j=0;j<4;j++)
                if(SegmentsCross(polygon[i],polygon[(i+1)%polygon.Length],corners[j],corners[(j+1)%4]))return true;
            return false;
        }
        static bool SegmentsCross(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            float Cross(Vector2 u,Vector2 v)=>u.x*v.y-u.y*v.x;
            var r=b-a;var s=d-c;float denominator=Cross(r,s);
            if(Mathf.Abs(denominator)<.000001f)return false;
            float t=Cross(c-a,s)/denominator,u=Cross(c-a,r)/denominator;
            return t>=0&&t<=1&&u>=0&&u<=1;
        }
    }
}
