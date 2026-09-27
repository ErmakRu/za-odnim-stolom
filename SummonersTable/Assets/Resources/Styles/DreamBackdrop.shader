Shader "SummonersTable/Dream Background"
{
    Properties { _MainTex("Story background",2D)="black"{} }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_TexelSize;
            struct v2f { float4 pos:SV_POSITION;float4 screen:TEXCOORD0; };
            v2f vert(float4 vertex:POSITION){v2f o;o.pos=UnityObjectToClipPos(vertex);o.screen=ComputeScreenPos(o.pos);return o;}
            half4 frag(v2f i):SV_Target
            {
                float2 uv=i.screen.xy/i.screen.w;float screenAspect=_ScreenParams.x/_ScreenParams.y;float imageAspect=_MainTex_TexelSize.z/_MainTex_TexelSize.w;
                if(screenAspect>imageAspect)uv.y=(uv.y-.5)*(imageAspect/screenAspect)+.5;else uv.x=(uv.x-.5)*(screenAspect/imageAspect)+.5;
                return half4(tex2D(_MainTex,uv).rgb,1);
            }
            ENDHLSL
        }
    }
}
