Shader "Unfin/Regional Tiles"
{
    Properties { [PerRendererData] _MainTex ("Tile", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 tex=tex2D(_MainTex,i.uv);
                // Preserve painted relief, edge alpha and luminance. White is a bypass
                // for original gameplay tiles (gates, wax, hazards), not a grey palette.
                float3 luma=float3(.299,.587,.114);
                float strength=step(.005,dot(abs(i.color.rgb-1),float3(1,1,1)));
                float3 recolored=dot(tex.rgb,luma)*i.color.rgb/max(.05,dot(i.color.rgb,luma));
                return fixed4(lerp(tex.rgb,recolored,strength),tex.a*i.color.a);
            }
            ENDCG
        }
    }
}
