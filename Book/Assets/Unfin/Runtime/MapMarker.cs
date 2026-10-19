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
        void Start()
        {
            if(!enemy)return;
            if(PaperEnemyAnimation.Attach(this))return;
            if(GetComponent<PaperAttackTarget>()==null)gameObject.AddComponent<PaperAttackTarget>();
        }
    }
}
