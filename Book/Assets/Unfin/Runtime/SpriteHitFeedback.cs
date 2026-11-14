using UnityEngine;

namespace Unfin
{
    // A visual reaction only: no sprite swapping, transform changes or hit stop.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteHitFeedback : MonoBehaviour
    {
        static Material shared;
        static readonly int Amount=Shader.PropertyToID("_HitFlash"),Tint=Shader.PropertyToID("_HitColor");
        MaterialPropertyBlock block;
        SpriteRenderer visual;
        WorldPrototype world;
        int resetSerial;
        float remaining;
        bool player;
        public bool Active => remaining>0;
        public static SpriteHitFeedback Get(SpriteRenderer renderer)
        {
            var fx=renderer.GetComponent<SpriteHitFeedback>();
            if(fx==null)fx=renderer.gameObject.AddComponent<SpriteHitFeedback>();
            fx.visual=renderer;
            fx.world=FindAnyObjectByType<WorldPrototype>();fx.resetSerial=fx.world!=null?fx.world.ResetSerial:0;
            return fx;
        }
        public void Trigger(bool isPlayer)
        {
            if(shared==null){
                var shader=Resources.Load<Shader>("Effects/PaperHitFlash");
                if(shader==null)throw new System.InvalidOperationException("Missing paper hit shader");
                shared=new Material(shader){name="Paper hit feedback",hideFlags=HideFlags.HideAndDontSave};
            }
            // Keep the player's paper surface under the existing flash properties.
            if(isPlayer)PaperPlayerSurface.Apply(visual);
            else if(visual.sharedMaterial==null||!visual.sharedMaterial.HasProperty("_BossSurface"))visual.sharedMaterial=shared;
            player=isPlayer;remaining=player?.32f:.18f;Apply();
        }
        void Update()
        {
            if(world!=null&&resetSerial!=world.ResetSerial){resetSerial=world.ResetSerial;Clear();}
            if(world!=null&&world.MapOpen)return;
            Tick(Mathf.Min(Time.deltaTime,.1f));
        }
        public void Tick(float dt){if(remaining<=0)return;remaining=Mathf.Max(0,remaining-dt);Apply();}
        void Apply()
        {
            if(block==null)block=new MaterialPropertyBlock();
            float age=(player?.32f:.18f)-remaining;
            bool impact=age<(player?.055f:.04f);
            float strength=remaining<=0?0:impact?.78f:(player?.58f:.4f)*Mathf.Clamp01(remaining/(player?.265f:.14f));
            Color color=player?(impact?new Color(.88f,.76f,.63f):new Color(.7f,.27f,.24f)):new Color(.82f,.78f,.68f);
            visual.GetPropertyBlock(block);block.SetFloat(Amount,strength);block.SetColor(Tint,color);visual.SetPropertyBlock(block);
        }
        public void Clear(){remaining=0;if(visual!=null)Apply();}
        void OnDisable(){Clear();}
    }
}
