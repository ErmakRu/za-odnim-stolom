Shader "SummonersTable/Layered Card UI"
{
    Properties
    {
        [PerRendererData] _MainTex("UI texture",2D)="white" {}
        _BackgroundTex("Background",2D)="black" {}
        _RearTex("Rear",2D)="black" {}
        _ForegroundTex("Foreground",2D)="black" {}
        _WindowAspect("Window aspect",Float)=1
        [HideInInspector] _Background("Background placement",Vector)=(0,0,1,0)
        [HideInInspector] _Rear("Rear placement",Vector)=(0,0,1,0)
        [HideInInspector] _Foreground("Foreground placement",Vector)=(0,0,1,0)
        [HideInInspector] _BackgroundInfo("Background fit",Vector)=(1,1,0,0)
        [HideInInspector] _RearInfo("Rear fit",Vector)=(1,1,0,0)
        [HideInInspector] _ForegroundInfo("Foreground fit",Vector)=(1,1,0,0)
        [HideInInspector] _Subject("Subject placement",Vector)=(0,0,1,0)
        [HideInInspector] _SubjectFoil("Subject foil",Float)=0
        [HideInInspector] _ResponsePower("Response power",Float)=1
        _ViewOffset("Look",Vector)=(0,0,0,0)
        _StencilComp("Stencil comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil operation",Float)=0
        _StencilWriteMask("Stencil write mask",Float)=255
        _StencilReadMask("Stencil read mask",Float)=255
        _ColorMask("Color mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha clip",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "LayeredArt.cginc"
            float4 _ClipRect;
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
            v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            float4 frag(v2f i):SV_Target
            {
                float4 c=LayeredArt(i.uv,_ViewOffset.xy)*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
