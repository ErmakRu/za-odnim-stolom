Shader "SummonersTable/BoardGray"
{
    Properties { _Color ("Color", Color) = (0.4,0.42,0.44,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        CGPROGRAM
        #pragma surface surf Lambert
        struct Input { float3 worldPos; };
        fixed4 _Color;
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
