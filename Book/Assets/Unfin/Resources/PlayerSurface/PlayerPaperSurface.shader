Shader "Unfin/Player Paper Surface"
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
        _PaperTex ("Pressed paper fibers", 2D) = "gray" {}
        _PaperMean ("Paper mean", Float) = .66
        _PaperStrength ("Paper surface", Range(0,1)) = 1
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
            #pragma vertex PaperVert
            #pragma fragment PaperFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            sampler2D _PaperTex;
            float _PaperMean, _PaperStrength, _HitFlash;
            fixed4 _HitColor;
            struct paper_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 paper : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            paper_v2f PaperVert(appdata_t v)
            {
                v2f baseVertex=SpriteVert(v);
                paper_v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=baseVertex.vertex; o.color=baseVertex.color; o.texcoord=baseVertex.texcoord;
                // Drawing-local units: no time/world/camera term and no atlas UV seams.
                o.paper=v.vertex.xy*.85+float2(.37,.19);
                return o;
            }
            fixed4 PaperFrag(paper_v2f input) : SV_Target
            {
                fixed4 source=SampleSpriteTexture(input.texcoord);
                float3 color=source.rgb;
                float pulp=tex2D(_PaperTex,input.paper).r-_PaperMean;
                float fine=tex2D(_PaperTex,input.paper*2.3+float2(.23,.61)).r-_PaperMean;
                float fiber=clamp(pulp*1.8+fine*.3,-.2,.2);
                // Strengthen existing painted folds; never generate new shape/alpha.
                float3 pressed=pow(max(color,0),1.24)*float3(.94,.937,.924)*(1+fiber);
                float paperMask=smoothstep(.08,.38,dot(color,float3(.299,.587,.114)));
                color=lerp(color,pressed,_PaperStrength*paperMask);
                fixed4 c=fixed4(color,source.a)*input.color;
                c.rgb=lerp(c.rgb,_HitColor.rgb,saturate(_HitFlash));
                c.rgb*=c.a;
                return c;
            }
            ENDCG
        }
    }
}
