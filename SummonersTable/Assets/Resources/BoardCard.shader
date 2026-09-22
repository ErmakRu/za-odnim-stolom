Shader "SummonersTable/BoardCard"
{
    Properties { _MainTex ("Card art", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) _Dissolve ("Dissolve", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" } Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex; float4 _Color; float _Dissolve;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            fixed4 frag(v2f i) : SV_Target { float noise=frac(sin(dot(floor(i.uv*90),float2(12.9898,78.233)))*43758.5453); clip(noise-_Dissolve);return tex2D(_MainTex,i.uv)*_Color; }
            ENDCG
        }
    }
}
