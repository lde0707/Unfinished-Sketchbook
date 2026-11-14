Shader "Unfin/World Air"
{
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
            float4 _Haze,_Room;float _Rays,_Quality,_StilldrawnAtmosphereTime;
            struct v2f{float4 pos:SV_POSITION;float2 world:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float t=_StilldrawnAtmosphereTime;float2 p=i.world;
                float n=noise(p*float2(.19,.37)+float2(t*.013,-t*.006));
                if(_Quality>.5)n=n*.7+noise(p*float2(.48,.24)+float2(-t*.021,t*.008))*.3;
                float floorMist=1-smoothstep(.4,4.3,abs(p.y-(_Room.y+1.3)));
                float mist=(.015+floorMist*.105)*smoothstep(.28,.82,n);
                float depth=max(0,_Room.w-p.y),width=.45+depth*.15;
                float anchor=lerp(_Room.x,_Room.z,.24),anchor2=lerp(_Room.x,_Room.z,.76);
                float lane=p.x+depth*.26;
                float beams=exp(-pow((lane-anchor)/width,2)*2.5)+.5*exp(-pow((lane-anchor2)/(width*.6),2)*2.5);
                float breath=.88+.12*sin(t*.37+p.y*.18);
                float ray=beams*_Rays*.105*breath*(.75+.25*n)*(1-smoothstep(12,25,depth));
                float2 edge=min(p-_Room.xy,_Room.zw-p);float fade=smoothstep(-2,1,min(edge.x,edge.y));
                float alpha=min(.16,(mist+ray)*fade);
                float3 color=lerp(_Haze.rgb,_Haze.rgb*1.12,ray/max(mist+ray,.001));
                #ifndef UNITY_COLORSPACE_GAMMA
                color=GammaToLinearSpace(color);
                #endif
                return float4(color,alpha);
            }
            ENDCG
        }
    }
}
