Shader "Unfin/Ink Depth" {
 Properties { _MainTex("Painted ink",2D)="black"{} _PaintRect("Paint bounds",Vector)=(0,0,1,1) _Depth("Depth",Float)=10 _Offset("Offset",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float _Depth,_Offset;sampler2D _MainTex;float4 _PaintRect;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v vert(a i){v o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
 fixed4 frag(v i):SV_Target {
  float x=i.uv.x+_Offset,t=_Time.y;
  float wave=.035+.016*sin(x*4.1+t*1.5)+.011*sin(x*9.7-t*.8);
  float depth=i.uv.y-wave;clip(depth);
  float foam=pow(.5+.5*sin(x*13+sin(x*5-t)),3);
  float rim=exp(-depth*24)*(.35+.65*foam);
  float churn=(.5+.5*sin(x*3.7+t+depth*12))*exp(-depth*5);
  float2 paint=float2(frac(x*.53+.025*sin(depth*4-t*.25)),1-saturate(depth/1.7));
  float3 color=tex2D(_MainTex,_PaintRect.xy+paint*_PaintRect.zw).rgb*float3(.75,.38,.58);
  color+=rim*float3(.06,.008,.025)+churn*float3(.008,.001,.004);
  color=lerp(color,float3(.0006,.0004,.0008),saturate(depth/2.1));
  // The far end dissolves into the existing dark ground, with no visible floor rim.
  float alpha=1-smoothstep(max(.4,_Depth-1.7),_Depth,depth);
  return fixed4(color,alpha);
 }
 ENDCG }
 }
}
