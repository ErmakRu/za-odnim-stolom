// Built-in adapter for Toon Shaders Pro lighting and PainterlyAsset's supplied HLSL.
// The original URP sources remain unmodified under ThirdParty/Shaders.
Shader "SummonersTable/Asset Styles (Built-in)"
{
    Properties
    {
        _MainTex("Albedo",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        [Toggle] _Painterly("Painterly style",Float)=0
        _DiffuseThresholds("Toon diffuse thresholds",Vector)=(.05,.08,.52,.56)
        _ShadowTint("Toon shadow",Color)=(.25,.28,.4,1)
        _MiddleTint("Toon middle",Color)=(.62,.64,.75,1)
        _LightTint("Toon light",Color)=(1.05,1,.9,1)
        _RimColor("Toon rim",Color)=(.12,.1,.07,1)
        _RimThresholds("Toon rim thresholds",Vector)=(.75,.85,0,0)
        _BrushScale("Painterly brush density",Range(1,20))=8
        _BrushStrength("Painterly normal strength",Range(0,1))=.8
        _ColorVariation("Painterly pigment variation",Range(0,.5))=.08
        [HideInInspector] _UseMasks("Armor masks",Float)=0
        _Mask01("Armor mask 1",2D)="black"{}
        _Mask02("Armor mask 2",2D)="black"{}
        _Color01("Armor color 1",Color)=(1,1,1,1)
        _Color02("Armor color 2",Color)=(1,1,1,1)
        _Color03("Armor color 3",Color)=(1,1,1,1)
        _Color04("Armor color 4",Color)=(1,1,1,1)
        _Color05("Armor color 5",Color)=(1,1,1,1)
        _Color06("Armor color 6",Color)=(1,1,1,1)
        _Color01Power("Armor power 1",Float)=1
        _Color02Power("Armor power 2",Float)=1
        _Color03Power("Armor power 3",Float)=1
        _Color04Power("Armor power 4",Float)=1
        _Color05Power("Armor power 5",Float)=1
        _Color06Power("Armor power 6",Float)=1
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "Queue"="Geometry"}
        LOD 200
        CGPROGRAM
        #pragma target 3.5
        #pragma surface surf AssetStyle vertex:vert addshadow fullforwardshadows
        #include "UnityCG.cginc"
        #include "../../ThirdParty/Shaders/PainterlyAsset/Shaders/Painterly.hlsl"
        sampler2D _MainTex,_Mask01,_Mask02;
        float4 _Mask01_ST,_Mask02_ST;
        half4 _Color,_Color01,_Color02,_Color03,_Color04,_Color05,_Color06;
        half _Color01Power,_Color02Power,_Color03Power,_Color04Power,_Color05Power,_Color06Power;
        half _UseMasks,_Painterly,_BrushScale,_BrushStrength,_ColorVariation;
        half4 _DiffuseThresholds,_RimThresholds;
        half3 _ShadowTint,_MiddleTint,_LightTint,_RimColor;
        struct Input {float2 uv_MainTex;float2 rawUV;float3 normalOS;float3 tangentOS;float3 bitangentOS;float3 localPos;};
        void vert(inout appdata_full v,out Input o)
        {UNITY_INITIALIZE_OUTPUT(Input,o);o.rawUV=v.texcoord.xy;o.normalOS=v.normal;o.tangentOS=v.tangent.xyz;o.bitangentOS=cross(v.normal,v.tangent.xyz)*v.tangent.w;o.localPos=v.vertex.xyz;}
        half4 LightingAssetStyle(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
        {
            half nDotL=dot(s.Normal,lightDir);
            // Two threshold bands and the lit-side rim from ToonFunctions.CalculateToonLighting.
            half bands=smoothstep(_DiffuseThresholds.x,_DiffuseThresholds.y,nDotL)+smoothstep(_DiffuseThresholds.z,_DiffuseThresholds.w,nDotL);
            half3 radiance=lerp(lerp(_ShadowTint,_MiddleTint,saturate(bands)),_LightTint,saturate(bands-1));
            half rim=(1-saturate(dot(s.Normal,viewDir)))*saturate(bands);
            half3 toon=s.Albedo*radiance+smoothstep(_RimThresholds.x,_RimThresholds.y,rim)*_RimColor;
            half3 paint=s.Albedo*lerp(half3(.24,.27,.38),half3(1.05,.94,.79),saturate(nDotL*.8+.2));
            return half4(lerp(toon,paint,_Painterly)*_LightColor0.rgb*atten*.58,s.Alpha);
        }
        void surf(Input i,inout SurfaceOutput o)
        {
            half3 albedo=tex2D(_MainTex,i.uv_MainTex).rgb;
            if(_UseMasks>.5)
            {
                half3 a=tex2D(_Mask01,i.rawUV*_Mask01_ST.xy+_Mask01_ST.zw).rgb,b=tex2D(_Mask02,i.rawUV*_Mask02_ST.xy+_Mask02_ST.zw).rgb;
                // Preserve the supplied PolyArtMaskTint armor palette formula.
                half3 dye=min(a.r,_Color01.rgb)*_Color01Power+min(a.g,_Color02.rgb)*_Color02Power+min(a.b,_Color03.rgb)*_Color03Power+min(b.r,_Color04.rgb)*_Color04Power+min(b.g,_Color05.rgb)*_Color05Power+min(b.b,_Color06.rgb)*_Color06Power;
                albedo=lerp(albedo,saturate(albedo*dye),dot(a+b,half3(1,1,1)));
            }
            if(_Painterly>.5)
            {
                float3 brushNormal,pigment;float distance;
                PainterlyNormalsSimple_float(normalize(i.normalOS),1,_BrushScale,brushNormal,pigment,distance);
                float3 normal=normalize(lerp(normalize(i.normalOS),normalize(brushNormal),_BrushStrength));
                o.Normal=normalize(float3(dot(normal,normalize(i.tangentOS)),dot(normal,normalize(i.bitangentOS)),dot(normal,normalize(i.normalOS))));
                // Object-space pigment cells are stable while the camera or character moves.
                VoronoiParams p;p.randomness=1;p.smoothness=0;p.metric=0;p.scale=_BrushScale;
                float3 objectScale=float3(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21),length(unity_ObjectToWorld._m02_m12_m22));
                pigment=voronoi_simple_f1(p,i.localPos*objectScale).color;
                albedo*=1+(pigment-.5)*_ColorVariation;
            }
            o.Albedo=albedo*_Color.rgb;o.Emission=o.Albedo*.14;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
