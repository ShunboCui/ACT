// 内置管线版 MMD 卡通着色（供 ACT/MMDToon 与 ACT/MMDToonTransparent 共用）
#ifndef ACT_MMDTOON_BUILTIN_INCLUDED
#define ACT_MMDTOON_BUILTIN_INCLUDED

#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"

sampler2D _MainTex;
sampler2D _ToonTex;
float4 _MainTex_ST;
fixed4 _Color;
fixed4 _Ambient;
fixed4 _Emissive;
fixed4 _EdgeColor;
float _EdgeSize;
float _EdgeWidthMul;
float _EdgeEnabled;

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 nrm : TEXCOORD1;
};

v2f vertLit(appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.nrm = UnityObjectToWorldNormal(v.normal);
    return o;
}

fixed4 fragLit(v2f i, fixed facing : VFACE) : SV_Target
{
    fixed4 baseCol = tex2D(_MainTex, i.uv) * _Color;

    float3 n = normalize(i.nrm);
    if (facing < 0) n = -n;

    float3 l = normalize(_WorldSpaceLightPos0.xyz);
    float ndl = dot(n, l);
    // 卡通渐变：MMD toon 贴图顶部为受光、底部为阴影
    float3 toon = tex2D(_ToonTex, float2(0.5, saturate(ndl * 0.5 + 0.5))).rgb;

    float3 ambient = ShadeSH9(float4(n, 1)) * (_Ambient.rgb * 2.0);
    float3 lighting = _LightColor0.rgb * toon + ambient;

    fixed4 col;
    col.rgb = baseCol.rgb * lighting + _Emissive.rgb;
    col.a = baseCol.a;
    return col;
}

// 描边：沿法线外扩的背面壳
v2f vertEdge(appdata v)
{
    v2f o;
    float width = (_EdgeEnabled > 0.5) ? _EdgeSize * _EdgeWidthMul : 0.0;
    o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + normalize(v.normal) * width, 1.0));
    if (_EdgeEnabled <= 0.5) o.pos = float4(2, 2, 2, 1); // 未启用描边：丢到屏幕外
    o.uv = v.uv;
    o.nrm = 0;
    return o;
}

fixed4 fragEdge(v2f i) : SV_Target
{
    return fixed4(_EdgeColor.rgb, 1);
}

#endif
