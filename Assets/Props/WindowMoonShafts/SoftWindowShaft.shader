Shader "Echo/SoftWindowShaft"
{
 Properties
 {
  _Tint("Moon haze",Color)=(0.52,0.66,0.86,1)
  _Density("Density per metre",Range(0,0.1))=0.035
  _Opacity("Maximum opacity",Range(0,0.2))=0.1
  _Spread("Soft spread",Float)=1.35
  _Drift("End offset",Vector)=(0,-1,0,0)
  _Exposure("Window openness",Range(0,1))=1
  _PaneLengths("Pane occlusion",Vector)=(1,1,1,1)
  [HideInInspector] _CookieMask("Window aperture",2D)="white" {}
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent"}
  Pass
  {
   Name "SoftWindowHaze"
   Tags {"LightMode"="SRPDefaultUnlit"}
   Cull Front ZWrite Off ZTest Always
   Blend One OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.5
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   TEXTURE2D(_CookieMask);
   SAMPLER(sampler_CookieMask);
   CBUFFER_START(UnityPerMaterial)
    half4 _Tint;
    float _Density,_Opacity,_Spread,_Exposure;
    float4 _Drift,_PaneLengths;
    float _UseWindowProjection,_UseCookieMask;
    float4x4 _WindowWorldToCookie;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
   Varyings Vert(Attributes v)
   {
    Varyings o;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;
   }
   half4 Frag(Varyings i):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    if(_Exposure<0.001)return 0;
    float3 cameraWS=GetCameraPositionWS();float3 directionWS=normalize(i.positionWS-cameraWS);
    float3 ro=TransformWorldToObject(cameraWS);float3 rd=TransformWorldToObjectDir(directionWS,false);
    float3 lo=min(float3(-.5,-.5,0),float3(_Drift.xy-_Spread*.5,1));
    float3 hi=max(float3(.5,.5,1),float3(_Drift.xy+_Spread*.5,1));
    float3 a=(lo-ro)/(rd+1e-6),b=(hi-ro)/(rd+1e-6);
    float3 near3=min(a,b),far3=max(a,b);
    float begin=max(0,max(near3.x,max(near3.y,near3.z)));
    float end=min(far3.x,min(far3.y,far3.z));
    float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
    float depth=SampleSceneDepth(uv);
    #if !UNITY_REVERSED_Z
     depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
    #endif
    float3 surfaceWS=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
    float surfaceDistance=dot(surfaceWS-cameraWS,directionWS);
    end=min(end,surfaceDistance-.025);
    if(end<=begin)return 0;
    int stepCount=_UseWindowProjection>.5?32:12;
    float stepLength=(end-begin)/stepCount;float total=0;
    [loop]for(int s=0;s<stepCount;s++)
    {
     float t=begin+(s+.5)*stepLength;float3 p=ro+rd*t;
     float2 q=(p.xy-_Drift.xy*p.z)/(lerp(1,_Spread,p.z)*.5);
     float edge=(1-smoothstep(.55,1,abs(q.x)))*(1-smoothstep(.55,1,abs(q.y)));
     float2 bands=q;
     if(_UseWindowProjection>.5)
     {
      float3 projected=mul(_WindowWorldToCookie,float4(cameraWS+directionWS*t,1)).xyz;
      float2 projectedBands = projected.xy / max(.001, projected.z);
      if (_UseCookieMask > .5)
      {
       // Start across the actual aperture, then rejoin the projected floor pattern.
       float floorBlend = smoothstep(.22, .88, p.z);
       bands = lerp(q, projectedBands, floorBlend);
      }
      else
      {
       bands = projectedBands;
       edge *= step(.001, projected.z) * (1-smoothstep(.85,.94,abs(bands.x))) * (1-smoothstep(.85,.94,abs(bands.y)));
      }
     }
     float mullions=smoothstep(.025,.12,abs(bands.x))*smoothstep(.025,.12,abs(bands.y));
     if (_UseCookieMask > .5)
      mullions = SAMPLE_TEXTURE2D_LOD(_CookieMask, sampler_CookieMask, saturate(bands * .5 + .5), 0).r;
     float limit=q.y<0?(q.x<0?_PaneLengths.x:_PaneLengths.y):(q.x<0?_PaneLengths.z:_PaneLengths.w);
     if (_UseCookieMask > .5)
     {
      // Avoid a hard horizontal seam between neighbouring arch occlusion samples.
      float2 blend = smoothstep(-.16, .16, q);
      limit = lerp(lerp(_PaneLengths.x, _PaneLengths.y, blend.x),
                   lerp(_PaneLengths.z, _PaneLengths.w, blend.x), blend.y);
     }
     float clipFade=1-smoothstep(max(0,limit-.065),max(.001,limit),p.z);
     float along=smoothstep(0,.055,p.z)*(1-smoothstep(.55,1,p.z));
     float drift=.94+.06*sin(dot(cameraWS+directionWS*t,float3(.65,1.7,.41))+_Time.y*.07);
     total+=edge*mullions*clipFade*along*drift*stepLength;
    }
    float alpha=min(_Opacity,1-exp(-total*_Density))*_Exposure;
    return half4(_Tint.rgb*alpha,alpha);
   }
   ENDHLSL
  }
 }
 Fallback Off
}
