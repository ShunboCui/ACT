// URP 版 MMD 卡通着色（供 ACT/MMDToon 与 ACT/MMDToonTransparent 共用）
#ifndef ACT_MMDTOON_URP_INCLUDED
#define ACT_MMDTOON_URP_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_ToonTex); SAMPLER(sampler_ToonTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half4 _Color;
    half4 _Ambient;
    half4 _Emissive;
    half4 _EdgeColor;
    float _EdgeSize;
    float _EdgeWidthMul;
    float _EdgeEnabled;
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
};

Varyings vertLit(Attributes v)
{
    Varyings o;
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    return o;
}

half4 fragLit(Varyings i, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;

    float3 n = normalize(i.normalWS);
    if (!IS_FRONT_VFACE(facing, true, false)) n = -n;

    Light mainLight = GetMainLight();
    float ndl = dot(n, mainLight.direction);
    half3 toon = SAMPLE_TEXTURE2D(_ToonTex, sampler_ToonTex, float2(0.5, saturate(ndl * 0.5 + 0.5))).rgb;

    half3 ambient = SampleSH(n) * (_Ambient.rgb * 2.0);
    half3 lighting = mainLight.color * toon + ambient;

    half4 col;
    col.rgb = baseCol.rgb * lighting + _Emissive.rgb;
    col.a = baseCol.a;
    return col;
}

Varyings vertEdge(Attributes v)
{
    Varyings o;
    float width = (_EdgeEnabled > 0.5) ? _EdgeSize * _EdgeWidthMul : 0.0;
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz + normalize(v.normalOS) * width);
    if (_EdgeEnabled <= 0.5) o.positionCS = float4(2, 2, 2, 1);
    o.uv = v.uv;
    o.normalWS = 0;
    return o;
}

half4 fragEdge(Varyings i) : SV_Target
{
    return half4(_EdgeColor.rgb, 1);
}

#endif
