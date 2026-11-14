using UnityEngine;

namespace Unfin
{
    public sealed class MapMarker : MonoBehaviour
    {
        public bool enemy;
        public int sourceIndex;
        public string kind;
        [TextArea] public string originalOptions;
        public Vector2 htmlPosition;
        WorldPrototype world;
        void Start()
        {
            if(enemy)EnemyCombatVfx.Attach(this);
            if(!enemy){
                PaperPickupVisual.Attach(this);
                PaperDeviceVisual.Attach(this);
                if(kind=="save"){
                    world=FindAnyObjectByType<WorldPrototype>();
                    if(world!=null&&!CheckpointPlan.HasMarker(world.Data,htmlPosition))gameObject.SetActive(false);
                    else if(world!=null){var point=System.Array.Find(world.Data.pickups,p=>p.type=="save"&&Mathf.Abs(p.x-htmlPosition.x)<.1f&&Mathf.Abs(p.y-htmlPosition.y)<.1f);if(point!=null)gameObject.AddComponent<SaveShrineVisual>().Initialize(world,point);}
                }
                InteractionProximity.Attach(this);return;
            }
            if(PaperRegion1Elite.Attach(this))return;
            if(PaperBossRegion1.Attach(this))return;
            if(PaperRegion2Encounter.Attach(this))return;
            if(PaperEnemyAnimation.Attach(this))return;
            if(GetComponent<PaperAttackTarget>()==null)gameObject.AddComponent<PaperAttackTarget>();
            if(sourceIndex==23||sourceIndex==47||sourceIndex==69||sourceIndex==91)gameObject.AddComponent<ExplorationEncounter>().Initialize(FindAnyObjectByType<WorldPrototype>(),sourceIndex);
        }
        void Update(){
            if(enemy)return;
            if(GetComponent<PaperPickupVisual>()!=null)return;
            if(GetComponent<SaveShrineVisual>()!=null)return;
            if(GetComponent<PaperDeviceVisual>()!=null)return;
            if(world==null)world=FindAnyObjectByType<WorldPrototype>();if(world==null)return;
            if(kind!="save"){if(world.Progression.CollectedAt(kind,htmlPosition))GetComponent<SpriteRenderer>().enabled=false;return;}
            var renderer=GetComponent<SpriteRenderer>();
            bool active=Vector2.Distance(world.CheckpointPosition,transform.position)<1.3f;
            renderer.color=active?new Color(.65f,.9f,.75f,.95f):new Color(.35f,.65f,.6f,.7f);
        }
    }
}
