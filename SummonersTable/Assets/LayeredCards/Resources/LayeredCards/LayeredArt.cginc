sampler2D _BackgroundTex,_RearTex,_ForegroundTex;
float4 _Background,_Rear,_Foreground;
float4 _BackgroundInfo,_RearInfo,_ForegroundInfo;
float4 _ViewOffset;
float _WindowAspect;
float4 _Subject;
float _SubjectFoil,_ResponsePower;

float4 ArtLayer(sampler2D tex,float2 uv,float2 look,float4 settings,float4 info)
{
    float ratio=_WindowAspect/max(.001,info.x);
    float2 contain=ratio>1?float2(ratio,1):float2(1,1/ratio);
    float2 cover=ratio>1?float2(1,1/ratio):float2(ratio,1);
    float2 fit=lerp(contain,cover,info.y);
    // A uniform scale and aspect-correct crop: never stretch the source or repeat edge pixels.
    float2 sampleUv=((uv-.5)-settings.xy-look*settings.w)*fit/settings.z+.5;
    float inside=step(0,sampleUv.x)*step(sampleUv.x,1)*step(0,sampleUv.y)*step(sampleUv.y,1);
    float4 result=tex2D(tex,sampleUv);result.a*=inside;
    return result;
}
float4 LayeredArt(float2 uv,float2 look)
{
    // Gentle response near the neutral pose. Clamp equally in UI and world renderers.
    look=clamp(look,-1,1);
    look=sign(look)*pow(abs(look),max(1,_ResponsePower));
    // The environment supplies most of the depth, moving behind an almost stationary subject.
    float4 b=ArtLayer(_BackgroundTex,uv,-look,_Background,_BackgroundInfo);
    float2 subjectUv=(uv-.5-_Subject.xy-look*_Subject.w)/_Subject.z+.5;
    float4 r=ArtLayer(_RearTex,subjectUv,0,_Rear,_RearInfo);
    float4 f=ArtLayer(_ForegroundTex,subjectUv,0,_Foreground,_ForegroundInfo);
    // Composite both sources before applying one shared foil highlight.
    float alpha=f.a+r.a*(1-f.a);
    float3 premultiplied=f.rgb*f.a+r.rgb*r.a*(1-f.a);
    float sweep=pow(saturate(1-abs((uv.x+uv.y*.28)-(.5+look.x*.6+look.y*.3))*3),5);
    float3 foil=.5+.5*cos(float3(0,2.1,4.2)+uv.x*4+uv.y*2+look.x*3-look.y*2);
    premultiplied+=foil*_SubjectFoil*sweep*.5*alpha;
    float3 color=lerp(float3(.035,.045,.065),b.rgb,b.a);
    color=premultiplied+color*(1-alpha);
    return float4(color,1);
}
