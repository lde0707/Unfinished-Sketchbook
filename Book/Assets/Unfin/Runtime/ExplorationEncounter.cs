using UnityEngine;
namespace Unfin
{
    // Reuses the existing prototype receivers, without inventing new boss AI.
    public sealed class ExplorationEncounter : MonoBehaviour
    {
        WorldPrototype world;PaperAttackTarget target;SpriteRenderer visual;int hits,required,index,serial;string ability;
        public void Initialize(WorldPrototype w,int source)
        {
            world=w;index=source;serial=w.ResetSerial;
            switch(index){case 23:ability="cut";required=12;break;case 47:ability="dash";required=16;break;case 69:ability="parry";required=8;break;case 91:ability="climb";required=16;break;default:Destroy(this);return;}
            target=GetComponent<PaperAttackTarget>();if(target==null)target=gameObject.AddComponent<PaperAttackTarget>();
            visual=GetComponent<SpriteRenderer>();target.onHit.AddListener(Hit);
        }
        void Hit(){if(world.Progression.DefeatedEncounters.Contains(index))return;if(++hits<required)return;EnemyRewardDrops.Emit(world,target.HitBounds.center,index==23?25:60);world.Progression.DefeatedEncounters.Add(index);world.Progression.Grant(ability);PaperVfx.Get()?.Death(target.HitBounds);target.enabled=false;visual.enabled=false;}
        void Update(){if(world!=null&&serial!=world.ResetSerial){serial=world.ResetSerial;if(!world.Progression.DefeatedEncounters.Contains(index))hits=0;}}
        void OnDestroy(){if(target!=null)target.onHit.RemoveListener(Hit);}
    }
}
