Shader "Unfin/InteractionGlow" {
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _SpriteRect("Sprite UV",Vector)=(0,0,1,1) _BlendNextTex("Next drawing",2D)="white"{} _BlendBaseRect("Base UV",Vector)=(0,0,1,1) _BlendNextRect("Next UV",Vector)=(0,0,1,1) _FrameBlend("Blend",Range(0,1))=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"} Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex,_BlendNextTex;float4 _MainTex_TexelSize,_SpriteRect,_BlendBaseRect,_BlendNextRect;float _FrameBlend;fixed4 _Color;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 float alphaAt(float2 uv){float2 hi=_SpriteRect.xy+_SpriteRect.zw;if(any(uv<_SpriteRect.xy)||any(uv>hi))return 0;float a=tex2D(_MainTex,uv).a;if(_FrameBlend<=0)return a;float2 nextUV=_BlendNextRect.xy+(uv-_BlendBaseRect.xy)/_BlendBaseRect.zw*_BlendNextRect.zw;return lerp(a,tex2D(_BlendNextTex,nextUV).a,_FrameBlend);}
 float edgeAt(float2 uv,float a,float width){float2 d=_MainTex_TexelSize.xy*width;return max(0,a-min(min(alphaAt(uv+float2(d.x,0)),alphaAt(uv-float2(d.x,0))),min(alphaAt(uv+float2(0,d.y)),alphaAt(uv-float2(0,d.y)))));}
 fixed4 frag(v2f i):SV_Target{
   float a=alphaAt(i.uv);float edge=edgeAt(i.uv,a,1.5),soft=edgeAt(i.uv,a,4);
   float breath=.90+.10*sin(_Time.y*2.1);
   // Paint only a quiet inner rim and a very slight warm lift. Never replace
   // the sprite, emit white bloom, or light through an opaque foreground tile.
   return fixed4(.96,.85,.64,(edge*.22+soft*.10+a*.018)*i.color.a*breath);
 }
 ENDCG }
 }
}
