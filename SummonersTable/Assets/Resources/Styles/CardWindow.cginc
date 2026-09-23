// Recessed illustration with a fixed rim. Independent UVs keep each card's
// contents inside its own window even when many UI/world cards overlap.
sampler2D _MainTex;
float _Depth, _Foil;
float4 _ViewOffset;
float CardHeight(float2 uv)
{
    float2 p=(uv-float2(.5,.48))*float2(1.8,1.5);
    float subject=1-smoothstep(.1,.68,length(p));
    return .18+.65*subject;
}
float4 CardWindow(float2 uv,float2 look)
{
    look=clamp(look,-1,1);
    float2 inner=(uv-.5)*.88+.5;
    float2 ray=look*_Depth;
    float2 sampleUV=inner-ray;
    // Relief parallax keeps the central subject closer than the background.
    for(int k=0;k<6;k++)sampleUV=inner-ray*(1-CardHeight(sampleUV));
    float4 art=tex2D(_MainTex,saturate(sampleUV));
    float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
    float window=smoothstep(.021,.033,edge);
    float rim=1-smoothstep(.035,.075,edge);
    float3 rainbow=.5+.5*cos(6.28318*(uv.x*.72+uv.y*.31+look.x*.55-look.y*.4+float3(0,.333,.667)));
    float etched=.5+.5*sin((uv.x+uv.y)*280+look.x*8);
    float sheen=pow(saturate(1-abs((uv.x+uv.y-1)*.7+look.x*.55+look.y*.4)),12);
    art.rgb*=1-.28*rim;
    art.rgb+=rainbow*_Foil*(.12+.45*sheen+.18*etched*rim);
    float bevel=saturate(.6+dot(normalize(float2(uv.x-.5,uv.y-.5)+.0001),look)*.3);
    float3 frame=lerp(float3(.12,.08,.035),float3(.91,.66,.3),bevel);
    return float4(lerp(frame,art.rgb,window),art.a);
}
