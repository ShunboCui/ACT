// URP 版 MMD 卡通着色，按 pass 由 ACT_PASS_* 宏选择要编译的部分：
//   ACT_PASS_LIT    主光 + 阴影接收 + 环境光
//   ACT_PASS_SHADOW ShadowCaster
//   ACT_PASS_EDGE   描边
// 半透明版额外定义 ACT_TRANSPARENT。URP 版暂不处理附加光。
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_ToonTex); SAMPLER(sampler_ToonTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half4 _Color;
    half4 _Ambient;
    half4 _EdgeColor;
    half4 _ToonTone;
    half _ShadowLum;
    half _AddLightToonCen;
    half _AddLightToonMin;
    half _SelfShadow;
    half _NoShadowCasting;
    float _EdgeSize;
    float _EdgeEnabled;
CBUFFER_END

#define ACT_SAMPLE_TOON(r) SAMPLE_TEXTURE2D(_ToonTex, sampler_ToonTex, float2((r), (r))).rgb
#include "MMDToon-Math.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

// ======================= Lit =======================
#if defined(ACT_PASS_LIT)

struct VaryingsLit
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
};

VaryingsLit vertLit(Attributes v)
{
    VaryingsLit o;
    o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    return o;
}

half4 fragLit(VaryingsLit i) : SV_Target
{
    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
#if defined(ACT_TRANSPARENT)
    albedo.a *= _Color.a;
#endif
    clip(albedo.a - ACT_CLIP_EPS);

    float3 N = normalize(i.normalWS);
    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
    half shadowAtten = mainLight.shadowAttenuation;

    half3 gamb = SampleSH(N);
    half3 vlight = gamb * ActAmbientRate();

    half NdotL = dot(N, mainLight.direction);
    half3 ramp = ActRamp(NdotL, shadowAtten);
    half3 lightColor = mainLight.color * shadowAtten;

    half3 c = albedo.rgb * ActTempDiffuse(gamb) * lightColor * ramp;
    c += albedo.rgb * vlight;
    return half4(c, albedo.a);
}

#endif // ACT_PASS_LIT

// ======================= ShadowCaster =======================
#if defined(ACT_PASS_SHADOW)

float3 _LightDirection;
float3 _LightPosition;

struct VaryingsShadow
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

VaryingsShadow vertShadow(Attributes v)
{
    VaryingsShadow o;
    float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    o.positionCS = positionCS;
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

half4 fragShadow(VaryingsShadow i) : SV_Target
{
    clip(-_NoShadowCasting);
#if defined(ACT_TRANSPARENT)
    half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * _Color.a;
    clip(a - ACT_CLIP_EPS);
#endif
    return 0;
}

#endif // ACT_PASS_SHADOW

// ======================= Edge =======================
#if defined(ACT_PASS_EDGE)

struct VaryingsEdge
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 vlight : TEXCOORD1;
};

VaryingsEdge vertEdge(Attributes v)
{
    VaryingsEdge o;
    float3 positionOS = v.positionOS.xyz + v.normalOS * _EdgeSize;
    o.positionWS = TransformObjectToWorld(positionOS);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    if (_EdgeEnabled < 0.5) o.positionCS = float4(2, 2, 2, 1);
    o.vlight = SampleSH(TransformObjectToWorldNormal(v.normalOS));
    return o;
}

half4 fragEdge(VaryingsEdge i) : SV_Target
{
    clip(_EdgeColor.a - ACT_CLIP_EPS);
    Light mainLight = GetMainLight();
    half3 color = mainLight.color * ACT_GLOBALLIGHTING;
    color += i.vlight;
    color *= _EdgeColor.rgb;
    return half4(color, _EdgeColor.a);
}

#endif // ACT_PASS_EDGE
