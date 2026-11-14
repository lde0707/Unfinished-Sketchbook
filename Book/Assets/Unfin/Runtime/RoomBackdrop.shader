Shader "Unfin/Room Backdrop"
{
    Properties { _MainTex("Atlas",2D)="white"{} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _UVRect, _Room;
            float _Brightness, _Flip, _StilldrawnAtmosphereTime, _StilldrawnAtmosphereStrength;
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;uv.x=lerp(uv.x,1-uv.x,_Flip);
                fixed4 c=tex2D(_MainTex,_UVRect.xy+uv*_UVRect.zw);
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                // Low-frequency light variation only; masonry and painted contours stay fixed.
                float light=sin(i.world.x*.23+i.world.y*.17+_StilldrawnAtmosphereTime*.19)*sin(i.world.y*.31-_StilldrawnAtmosphereTime*.11);
                c.rgb*=_Brightness*(1+light*.035*_StilldrawnAtmosphereStrength);
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                float2 edge=min(i.world-_Room.xy,_Room.zw-i.world);
                c.a*=smoothstep(0,1.25,min(edge.x,edge.y));
                // A tiny feather avoids hard atlas crop edges without blurring the painted silhouette.
                c.a*=smoothstep(0,.012,min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y)));
                return c;
            }
            ENDCG
        }
    }
}
