using UnityEngine;
namespace Unfin
{
    public sealed partial class PaperEnemyAnimation
    {
        EnemySpecialty specialty;
        float turnWait,guardFlash;
        public EnemySpecialty Specialty=>specialty;
        public EnemyArtSource Art=>source;
        public Bounds AttackBounds=>SpecialtyStrike(BodyBounds);
        public bool Guarding
        {
            get{
                if(source==null||Health<=0||stun>0||specialty.guard==EnemyGuard.None)return false;
                if(animation.State==EnemyAnimationState.Move)return true;
                if(animation.State!=EnemyAnimationState.Attack)return false;
                var t=source.clips[1].motionDurations;float start=t[0]+t[1];
                if(specialty.guard==EnemyGuard.Burrow)return clock<start*.65f;
                if(SpeciesId=="G02"||SpeciesId=="E01")return clock<start+t[2];
                return clock<start;
            }
        }
        public int BlockedHits {get;private set;}
        public bool TryBlockHit(int incomingFacing,Vector2 contact)
        {
            if(!Guarding)return false;
            if(specialty.guard==EnemyGuard.Front&&incomingFacing!=-direction)return false;
            BlockedHits++;guardFlash=.16f;
            EnemyCombatEffects.Get(world)?.Hit(new Bounds(contact,Vector3.one*.55f),new Color(.58f,.66f,.7f),incomingFacing,false,true);
            GameAudio.Emit(SoundCue.Gate,contact,.28f);
            if(SpeciesId=="H02"&&animation.State==EnemyAnimationState.Move)SetState(EnemyAnimationState.Attack);
            Render();return true;
        }
        void Face(float delta)
        {
            int next=delta>=0?1:-1;
            if(next==direction){if(specialty.guard==EnemyGuard.Front)turnWait=.45f;return;}
            // A shield cannot instantly swivel around to deny a successful flank.
            if(specialty.guard==EnemyGuard.Front&&turnWait>0)return;
            direction=next;turnWait=specialty.guard==EnemyGuard.Front?.65f:0;
        }
        Bounds SpecialtyStrike(Bounds body)
        {
            var b=specialty.radial?new Bounds(body.center,body.size+Vector3.right*source.meleeReach*2):EnemyAttackMotion.StrikeBounds(body,source.meleeReach,direction);
            if(specialty.lowSweep){b.center=new Vector3(b.center.x,body.min.y+.22f,b.center.z);b.size=new Vector3(b.size.x,.44f,b.size.z);}
            return b;
        }
        void RenderSpecialty()
        {
            if(animation.State==EnemyAnimationState.Death)return;
            Color c=enraged?new Color(1,.78f,.72f):Color.white;
            if(Guarding)c=specialty.guard==EnemyGuard.Burrow?new Color(.64f,.60f,.55f,.55f):new Color(.72f,.79f,.82f);
            if(guardFlash>0)c=Color.Lerp(c,new Color(.9f,.94f,1),guardFlash/.16f);
            visual.color=c;
        }
        public void ReportSpecialty()
        {
            Debug.Log("ENEMY_SPECIALTY "+JsonUtility.ToJson(new SpecialtySnapshot{species=SpeciesId,strength=specialty.strength,counter=specialty.counter,state=StateName,health=Health,guarding=Guarding,blocked=BlockedHits,clock=clock,x=transform.position.x,y=transform.position.y,shots=ProjectileShots}));
        }
        [System.Serializable] public class SpecialtySnapshot {public string species,strength,counter,state;public int health,blocked,shots;public bool guarding;public float clock,x,y;}
    }
}
