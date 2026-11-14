using UnityEngine;
namespace Unfin
{
    // A shared surface over the original whole drawings. No frame/UV/alpha edits.
    public static class PaperPlayerSurface
    {
        static Material material;
        public static Material Material
        {
            get
            {
                if(material!=null)return material;
                var shader=Resources.Load<Shader>("PlayerSurface/PlayerPaperSurface");
                var texture=Resources.Load<Texture2D>("PlayerSurface/paper-fibers");
                if(shader==null||texture==null)throw new System.InvalidOperationException("Missing player paper surface");
                material=new Material(shader){name="Player pressed parchment",hideFlags=HideFlags.HideAndDontSave};
                material.SetTexture("_PaperTex",texture);
                // Data texture (sRGB off); measured mean is stored alongside the art.
                var mean=Resources.Load<TextAsset>("PlayerSurface/mean");
                material.SetFloat("_PaperMean",mean!=null?float.Parse(mean.text,System.Globalization.CultureInfo.InvariantCulture):.66f);
                material.SetFloat("_PaperStrength",1);
                return material;
            }
        }
        public static void Apply(SpriteRenderer renderer)
        {
            if(renderer!=null&&renderer.sharedMaterial!=Material)renderer.sharedMaterial=Material;
        }
    }
}
