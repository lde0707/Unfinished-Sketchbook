Shader "Unfin/Hanging Paper Wind"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        [MaterialToggle] PixelSnap("Pixel snap",Float)=0
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
        [PerRendererData] _AlphaTex("Alpha",2D)="white"{}
        [PerRendererData] _EnableExternalAlpha("External Alpha",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SpriteFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _StilldrawnAtmosphereTime,_StilldrawnAtmosphereStrength;
            v2f vert(appdata_t v)
            {
                float weight=saturate(-v.vertex.y);weight*=weight;
                float phase=unity_ObjectToWorld._m03*.73+unity_ObjectToWorld._m13*.31;
                v.vertex.x+=weight*(sin(_StilldrawnAtmosphereTime*.75+phase)*.018+sin(_StilldrawnAtmosphereTime*1.23+phase)*.006)*_StilldrawnAtmosphereStrength;
                return SpriteVert(v);
            }
            ENDCG
        }
    }
}
