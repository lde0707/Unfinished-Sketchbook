Shader "Hidden/Unfin/Stilldrawn Logo Reveal"
{
    Properties { _MainTex ("Logo", 2D) = "white" {} _Reveal ("Reveal", Range(0,1)) = 0 }
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
            float _Reveal;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float front=lerp(-.12,1.12,_Reveal);
                float local=smoothstep(0,.12,front-i.uv.x);
                float2 uv=i.uv;
                uv.y-=.022*(1-local);
                fixed4 c=tex2D(_MainTex,uv);
                c.a*=local;
                float shine=exp(-pow((i.uv.x-front+.075)*34,2));
                c.rgb*=1+.24*shine;
                return c;
            }
            ENDCG
        }
    }
}
