Shader "Unfin/StructureFrameBlend" {
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _BlendNextTex("Next drawing",2D)="white"{} _BlendBaseRect("Base UV",Vector)=(0,0,1,1) _BlendNextRect("Next UV",Vector)=(0,0,1,1) _FrameBlend("Blend",Range(0,1))=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"} Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex,_BlendNextTex;float4 _BlendBaseRect,_BlendNextRect;float _FrameBlend;fixed4 _Color;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target {
   float2 uv=(i.uv-_BlendBaseRect.xy)/_BlendBaseRect.zw;
   fixed4 a=tex2D(_MainTex,i.uv),b=tex2D(_BlendNextTex,_BlendNextRect.xy+uv*_BlendNextRect.zw);
   // Premultiplied interpolation keeps paper edges from flashing dark or bright.
   a.rgb*=a.a;b.rgb*=b.a;fixed4 c=lerp(a,b,_FrameBlend);
   c.rgb*=i.color.rgb*i.color.a;c.a*=i.color.a;return c;
 }
 ENDCG }
 }
}
