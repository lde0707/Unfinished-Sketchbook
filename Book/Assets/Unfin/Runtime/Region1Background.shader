Shader "Unfin/Region1 Background"
{
    Properties
    {
        _Paper ("Paper", 2D) = "black" {}
        _Graphite ("Graphite", 2D) = "black" {}
        _Wood ("Wood", 2D) = "black" {}
        _Root ("Root", 2D) = "black" {}
        _Zones ("World anchored zone weights", 2D) = "white" {}
        _Bounds ("World width and height", Vector) = (280,140,0,0)
        _Opacity ("Background opacity", Range(0,1)) = 0.52
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            sampler2D _Paper, _Graphite, _Wood, _Root, _Zones;
            float4 _Bounds;
            float _Opacity;
            struct v2f { float4 pos:SV_POSITION; float2 world:TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.world=float2(p.x,-p.y); return o;
            }
            uint CellHash(int2 c)
            {
                uint n=(uint)(c.x+37)*374761393u ^ (uint)(c.y+71)*668265263u;
                n=(n^(n>>13))*1274126177u; return n^(n>>16);
            }
            float3 Wall(sampler2D source, float2 p)
            {
                // 8-unit panels, 6-unit stride, 2-unit overlap. Normalize the
                // four contributing panels so their overlap never darkens.
                int2 baseCell=(int2)floor(p/6.0);
                float3 sum=0; float total=0;
                [unroll] for(int y=0;y<2;y++)
                [unroll] for(int x=0;x<2;x++)
                {
                    int2 c=baseCell-int2(x,y); float2 local=p-float2(c)*6.0;
                    float2 edge=smoothstep(0.0,2.0,local)*smoothstep(0.0,2.0,8.0-local);
                    float w=edge.x*edge.y;
                    uint h=CellHash(c); float2 uv=local/8.0;
                    if((h&1u)!=0u) uv.x=1.0-uv.x;
                    if((h&2u)!=0u) uv.y=1.0-uv.y;
                    uv.y=1.0-uv.y;
                    float4 sample=tex2D(source,uv);
                    #ifndef UNITY_COLORSPACE_GAMMA
                    sample.rgb=LinearToGammaSpace(sample.rgb);
                    #endif
                    sum+=lerp(float3(22,25,29)/255.0,sample.rgb,sample.a*_Opacity)*w;
                    total+=w;
                }
                return sum/max(total,0.0001);
            }
            float4 frag(v2f i):SV_Target
            {
                float4 weights=tex2D(_Zones,(i.world+0.5)/(_Bounds.xy+1.0));
                weights/=max(dot(weights,float4(1,1,1,1)),0.0001);
                float2 p=i.world+float2(237,173)/128.0;
                float3 color=0;
                [branch] if(weights.x>0) color+=Wall(_Paper,p)*weights.x;
                [branch] if(weights.y>0) color+=Wall(_Graphite,p)*weights.y;
                [branch] if(weights.z>0) color+=Wall(_Wood,p)*weights.z;
                [branch] if(weights.w>0) color+=Wall(_Root,p)*weights.w;
                #ifndef UNITY_COLORSPACE_GAMMA
                color=GammaToLinearSpace(color);
                #endif
                return float4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
