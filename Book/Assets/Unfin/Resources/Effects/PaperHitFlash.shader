Shader "Unfin/Paper Hit Flash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable external alpha", Float) = 0
        _HitFlash ("Hit strength", Range(0,1)) = 0
        _HitColor ("Hit color", Color) = (.82,.78,.68,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment HitFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _HitFlash;
            fixed4 _HitColor;
            fixed4 HitFrag(v2f input) : SV_Target
            {
                fixed4 c=SampleSpriteTexture(input.texcoord)*input.color;
                c.rgb=lerp(c.rgb,_HitColor.rgb,saturate(_HitFlash));
                c.rgb*=c.a;
                return c;
            }
            ENDCG
        }
    }
}
