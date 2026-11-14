Shader "Hidden/Unfin/Stilldrawn Living Book"
{
    Properties { _MainTex ("Painting", 2D) = "white" {} _Phase ("Loop phase", Float) = 0 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Phase;
            float region(float2 p, float2 center, float2 radius)
            {
                float2 q=(p-center)/radius;
                return 1-smoothstep(.45,1,dot(q,q));
            }
            fixed4 frag(v2f_img i) : SV_Target
            {
                // Coordinates follow the painting from top to bottom. All waves are
                // integer harmonics of one loop; the spine and stone shelf stay pinned.
                float2 p=float2(i.uv.x,1-i.uv.y);
                float a=_Phase;
                float2 d=0;
                float loose=region(p,float2(.865,.080),float2(.145,.145));
                d+=loose*float2(.0045*sin(3*a+p.y*18),.010*sin(2*a+p.x*17));
                float left=region(p,float2(.615,.40),float2(.145,.26));
                float right=region(p,float2(.830,.405),float2(.125,.29));
                float anchor=smoothstep(.012,.13,abs(p.x-(.758-p.y*.085)));
                float page=anchor*(left+right);
                d+=page*float2(.0028*sin(2*a+p.y*8),.0045*sin(2*a+p.x*13));
                // Tiny ripples inside the painted waterfall, never across the ledge.
                float water=region(p,float2(.674,.812),float2(.026,.15));
                d.x+=water*.0017*sin(8*a+p.y*100);
                d.y+=water*.0025*sin(4*a+p.y*64);
                float2 uv=float2(p.x+d.x,1-p.y-d.y);
                return tex2D(_MainTex,uv);
            }
            ENDCG
        }
    }
}
