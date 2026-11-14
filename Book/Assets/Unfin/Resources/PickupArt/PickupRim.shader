Shader "Unfin/PickupRim" {
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"} Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex;float4 _MainTex_TexelSize;fixed4 _Color;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target {
 fixed4 c=tex2D(_MainTex,i.uv)*i.color;float2 d=_MainTex_TexelSize.xy*1.8;
 float a=max(max(tex2D(_MainTex,i.uv+float2(d.x,0)).a,tex2D(_MainTex,i.uv-float2(d.x,0)).a),max(tex2D(_MainTex,i.uv+float2(0,d.y)).a,tex2D(_MainTex,i.uv-float2(0,d.y)).a));
 float rim=max(0,a-tex2D(_MainTex,i.uv).a)*(.16+.04*sin(_Time.y*2));float outA=c.a+rim*i.color.a*(1-c.a);
 c.rgb=(c.rgb*c.a+fixed3(1,.91,.75)*rim*i.color.a*(1-c.a))/max(outA,.0001);c.a=outA;return c;
 }
 ENDCG }
 }
}
