Shader "SummonersTable/Layered Card World"
{
    Properties
    {
        _MainTex("Legacy UI texture",2D)="white" {}
        _BackgroundTex("Background",2D)="black" {}
        _RearTex("Rear",2D)="black" {}
        _ForegroundTex("Foreground",2D)="black" {}
        _WindowAspect("Window aspect",Float)=1
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "LayeredArt.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float3 view:TEXCOORD1;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.view=ObjSpaceViewDir(v.vertex);return o;}
            float4 frag(v2f i):SV_Target {return LayeredArt(i.uv,clamp(i.view.xy/max(.25,abs(i.view.z)),-1,1));}
            ENDCG
        }
    }
}
