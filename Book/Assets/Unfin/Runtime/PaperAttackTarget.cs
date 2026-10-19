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
        Color original;
        float flash;
        public Bounds HitBounds => TryGetComponent<PaperEnemyAnimation>(out var enemy)?enemy.BodyBounds:sprite.bounds;
        void OnEnable(){sprite=GetComponent<SpriteRenderer>();original=sprite.color;Active.Add(this);}
        void OnDisable(){Active.Remove(this);if(sprite!=null)sprite.color=original;}
        public void ReceiveHit(int facing)=>ReceiveHit(facing,HitBounds.center);
        public void ReceiveHit(int facing,Vector2 contact)
        {
            PaperVfx.Get()?.Hit(contact,facing);GameAudio.Emit(SoundCue.Hit,contact);
            HitCount++;LastHitDirection=new Vector2(facing,0);flash=.18f;
            sprite.color=new Color(original.r*.82f,original.g*.82f,original.b*.82f,original.a);onHit.Invoke();
        }
        void Update(){if(flash<=0)return;flash=Mathf.Max(0,flash-Time.deltaTime);sprite.color=Color.Lerp(original,new Color(original.r*.82f,original.g*.82f,original.b*.82f,original.a),flash/.18f);}
    }
}
