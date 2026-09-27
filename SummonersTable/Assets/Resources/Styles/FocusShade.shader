Shader "SummonersTable/Focus Shade"
{
 Properties { _Strength("Shade",Range(0,1))=.4 _Tutorial("Tutorial",Float)=0 _Feather("Soft edge",Float)=.018 _FocusCount("Targets",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
 float _Strength,_Tutorial,_Feather,_FocusCount;float4 _Focus0,_Focus1,_Focus2,_Focus3;
 v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 float outside(float2 uv,float4 box){float2 d=max(box.xy-uv,uv-box.zw);return smoothstep(0,_Feather,max(d.x,d.y));}
 fixed4 frag(v2f i):SV_Target {
   if(_Tutorial>.5){float a=1;if(_FocusCount>0)a=min(a,outside(i.uv,_Focus0));if(_FocusCount>1)a=min(a,outside(i.uv,_Focus1));if(_FocusCount>2)a=min(a,outside(i.uv,_Focus2));if(_FocusCount>3)a=min(a,outside(i.uv,_Focus3));return fixed4(.012,.008,.006,a*_Strength);}
   float2 p=abs(i.uv-.5)*2;float edge=smoothstep(.35,1.18,length(p*float2(.82,1)));
   return fixed4(.008,.006,.004,edge*_Strength);
 }
 ENDCG }
 }
}
