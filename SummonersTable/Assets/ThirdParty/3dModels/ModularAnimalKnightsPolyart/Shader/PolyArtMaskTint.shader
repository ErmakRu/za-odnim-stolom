Shader "PolyArtMaskTint"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
        _Color01("Color01", Color) = (0.7205882,0.08477508,0.08477508,0)
        _Color02("Color02", Color) = (0.02649222,0.3602941,0.09785674,0)
        _Color03("Color03", Color) = (0.07628676,0.2567445,0.6102941,0)
        _Color04("Color04", Color) = (0.6207737,0.1119702,0.8014706,0)
        _Color05("Color05", Color) = (0.9056604,0.5051349,0.09825563,0)
        _Color06("Color06", Color) = (1,0.7848822,0,0)
        _PolyArtAlbedo("PolyArtAlbedo", 2D) = "white" {}
        _Mask01("Mask01", 2D) = "white" {}
        _Mask02("Mask02", 2D) = "white" {}
        _Color01Power("Color01Power", Range(0, 6)) = 1
        _Color02Power("Color02Power", Range(0, 6)) = 1
        _Color03Power("Color03Power", Range(0, 6)) = 1
        _Color04Power("Color04Power", Range(0, 6)) = 1
        _Color05Power("Color05Power", Range(0, 6)) = 1
        _Color06Power("Color06Power", Range(0, 6)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };

            TEXTURE2D(_PolyArtAlbedo);
            SAMPLER(sampler_PolyArtAlbedo);
            TEXTURE2D(_Mask01);
            SAMPLER(sampler_Mask01);
            TEXTURE2D(_Mask02);
            SAMPLER(sampler_Mask02);

            CBUFFER_START(UnityPerMaterial)
                float4 _PolyArtAlbedo_ST;
                float4 _Mask01_ST;
                float4 _Mask02_ST;
                float4 _Color01;
                float4 _Color02;
                float4 _Color03;
                float4 _Color04;
                float4 _Color05;
                float4 _Color06;
                float _Color01Power;
                float _Color02Power;
                float _Color03Power;
                float _Color04Power;
                float _Color05Power;
                float _Color06Power;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvAlbedo = TRANSFORM_TEX(input.uv, _PolyArtAlbedo);
                float2 uvMask01 = TRANSFORM_TEX(input.uv, _Mask01);
                float2 uvMask02 = TRANSFORM_TEX(input.uv, _Mask02);

                half4 albedo = SAMPLE_TEXTURE2D(_PolyArtAlbedo, sampler_PolyArtAlbedo, uvAlbedo);
                half4 m1 = SAMPLE_TEXTURE2D(_Mask01, sampler_Mask01, uvMask01);
                half4 m2 = SAMPLE_TEXTURE2D(_Mask02, sampler_Mask02, uvMask02);

                half4 blendOpDest = (min(m1.r, _Color01) * _Color01Power) +
                                    (min(m1.g, _Color02) * _Color02Power) +
                                    (min(m1.b, _Color03) * _Color03Power) +
                                    (min(m2.r, _Color04) * _Color04Power) +
                                    (min(m2.g, _Color05) * _Color05Power) +
                                    (min(m2.b, _Color06) * _Color06Power);

                half maskSum = saturate(m1.r + m1.g + m1.b + m2.r + m2.g + m2.b);
                half4 tinted = saturate(albedo * blendOpDest);
                half3 finalAlbedo = lerp(albedo.rgb, tinted.rgb, maskSum);

                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 ambient = half3(0.35, 0.35, 0.35);
                half3 lighting = ambient + mainLight.color * (NdotL * 0.65);

                return half4(finalAlbedo * lighting, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}