Shader "Unfin/Boss Paper"
{
    Properties {
        [PerRendererData] _MainTex ("Sprite",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer color",Color)=(1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector)=(1,1,1,1)
        [PerRendererData] _AlphaTex ("External alpha",2D)="white" {}
        [PerRendererData] _EnableExternalAlpha ("External alpha enable",Float)=0
        _HitFlash ("Hit strength",Range(0,1))=0
        _HitColor ("Hit color",Color)=(.82,.78,.68,1)
        _AttackReadability ("Attacking outline",Range(0,1))=0
        _BossSurface ("Keep boss surface",Float)=1
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _MainTex_TexelSize;float _HitFlash,_AttackReadability;fixed4 _HitColor;
            fixed4 frag(v2f i):SV_Target{
                fixed4 c=SampleSpriteTexture(i.texcoord)*i.color;
                float2 d=_MainTex_TexelSize.xy*2;
                float nearAlpha=min(min(SampleSpriteTexture(i.texcoord+float2(d.x,0)).a,SampleSpriteTexture(i.texcoord-float2(d.x,0)).a),min(SampleSpriteTexture(i.texcoord+float2(0,d.y)).a,SampleSpriteTexture(i.texcoord-float2(0,d.y)).a));
                float rim=saturate(c.a-nearAlpha)*(.14+_AttackReadability*.36);
                c.rgb=lerp(c.rgb,fixed3(.78,.74,.64),rim);
                c.rgb=lerp(c.rgb,_HitColor.rgb,saturate(_HitFlash));c.rgb*=c.a;return c;
            }
            ENDCG
        }
    }
}
