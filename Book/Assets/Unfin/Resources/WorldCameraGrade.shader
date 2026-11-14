Shader "Hidden/Unfin/World Camera Grade"
{
    Properties { _MainTex("World",2D)="white"{} _Brightness("Brightness",Float)=.9 }
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
            float _Brightness, _Antialiasing, _StilldrawnAtmosphereStrength;
            float4 _MainTex_TexelSize;
            float4 _PlayerLight; // Viewport center, radius in viewport height, exposure boost.
            float _ViewAspect;
            float3 SmoothEdges(float2 uv)
            {
                float2 px = _MainTex_TexelSize.xy;
                float3 center = tex2D(_MainTex,uv).rgb;
                if (_Antialiasing < .5) return center;
                float3 nw=tex2D(_MainTex,uv+float2(-1,-1)*px).rgb;
                float3 ne=tex2D(_MainTex,uv+float2(1,-1)*px).rgb;
                float3 sw=tex2D(_MainTex,uv+float2(-1,1)*px).rgb;
                float3 se=tex2D(_MainTex,uv+float2(1,1)*px).rgb;
                float3 luma=float3(.299,.587,.114);
                float m=dot(center,luma),a=dot(nw,luma),b=dot(ne,luma),c=dot(sw,luma),d=dot(se,luma);
                float lo=min(m,min(min(a,b),min(c,d))),hi=max(m,max(max(a,b),max(c,d)));
                if(hi-lo<max(.0312,hi*.125))return center;
                float2 dir=float2(-((a+b)-(c+d)),(a+c)-(b+d));
                float reduce=max((a+b+c+d)*(.25*.125),1.0/128.0);
                float scale=1.0/(min(abs(dir.x),abs(dir.y))+reduce);
                dir=clamp(dir*scale,-8.0,8.0)*px;
                float3 first=.5*(tex2D(_MainTex,uv+dir*(1.0/3.0-.5)).rgb+tex2D(_MainTex,uv+dir*(2.0/3.0-.5)).rgb);
                float3 second=first*.5+.25*(tex2D(_MainTex,uv-dir*.5).rgb+tex2D(_MainTex,uv+dir*.5).rgb);
                float value=dot(second,luma);
                return value<lo||value>hi?first:second;
            }
            fixed4 frag(v2f_img i):SV_Target
            {
                fixed4 c=fixed4(SmoothEdges(i.uv),1);
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                float2 screenUV = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) screenUV.y = 1 - screenUV.y;
                #endif
                float2 delta = (screenUV - _PlayerLight.xy) * float2(_ViewAspect, 1);
                float distance = length(delta) / max(_PlayerLight.z, .0001);
                float localLight = 1 - smoothstep(.2, 1, distance);
                c.rgb *= _Brightness * (1 + _PlayerLight.w * localLight);
                // Subtle peripheral depth, no blur/chromatic aberration; HUD renders later.
                float2 edge=abs(screenUV-.5)*2;
                float vignette=smoothstep(.38,1.3,dot(edge,edge)*.65);
                c.rgb*=1-vignette*.065*_StilldrawnAtmosphereStrength;
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
