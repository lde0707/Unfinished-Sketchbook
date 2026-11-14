using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unfin
{
    // Prototype receiver: markers retain their authored positions; no enemy AI or HP balancing.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PaperAttackTarget : MonoBehaviour
    {
        public static readonly HashSet<PaperAttackTarget> Active = new HashSet<PaperAttackTarget>();
        public UnityEvent onHit = new UnityEvent();
        public int HitCount {get;private set;}
        public Vector2 LastHitDirection {get;private set;}
        SpriteRenderer sprite;
        SpriteHitFeedback feedback;
        SpriteRenderer Visual => sprite!=null?sprite:(sprite=GetComponent<SpriteRenderer>());
        public Bounds HitBounds => TryGetComponent<PaperRegion1Elite>(out var elite)?elite.BodyBounds:TryGetComponent<PaperBossRegion1>(out var boss)?boss.BodyBounds:TryGetComponent<PaperRegion2Encounter>(out var regional)?regional.BodyBounds:TryGetComponent<PaperEnemyAnimation>(out var enemy)?enemy.BodyBounds:Visual.bounds;
        void OnEnable(){sprite=GetComponent<SpriteRenderer>();feedback=SpriteHitFeedback.Get(sprite);Active.Add(this);}
        void OnDisable(){Active.Remove(this);}
        public void ResetFeedback(){feedback?.Clear();}
        public void ReceiveHit(int facing)=>ReceiveHit(facing,HitBounds.center);
        public void ReceiveHit(int facing,Vector2 contact)
        {
            if(TryGetComponent<PaperEnemyAnimation>(out var enemy)&&enemy.TryBlockHit(facing,contact))return;
            PaperVfx.Get()?.Hit(contact,facing);GameAudio.Emit(SoundCue.Hit,contact);
            HitCount++;LastHitDirection=new Vector2(facing,0);
            if(feedback==null)feedback=SpriteHitFeedback.Get(Visual);
            feedback.Trigger(false);onHit.Invoke();
        }
    }
}
