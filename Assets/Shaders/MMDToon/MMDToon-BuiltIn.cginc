// 内置管线版 MMD 卡通着色，按 pass 由 ACT_PASS_* 宏选择要编译的部分：
//   ACT_PASS_BASE   ForwardBase（主光 + 阴影接收 + 环境光）
//   ACT_PASS_ADD    ForwardAdd（附加光）
//   ACT_PASS_SHADOW ShadowCaster
//   ACT_PASS_EDGE   描边（ForwardBase 着色）
// 半透明版额外定义 ACT_TRANSPARENT。
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

sampler2D _MainTex;
sampler2D _ToonTex;
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

#define ACT_SAMPLE_TOON(r) tex2D(_ToonTex, half2((r), (r))).rgb
#include "MMDToon-Math.hlsl"

// 取贴图 alpha；半透明版再乘 _Color.a，并按原版裁掉近乎全透明的像素
inline half ActGetAlpha(float2 uv)
{
    half a = tex2D(_MainTex, uv).a;
#if defined(ACT_TRANSPARENT)
    a *= _Color.a;
#endif
    return a;
}

// ======================= ForwardBase =======================
#if defined(ACT_PASS_BASE)

struct v2fBase
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    half3 normal : TEXCOORD1;
    half3 vlight : TEXCOORD2;
    half3 gamb : TEXCOORD3;
    float3 worldPos : TEXCOORD4;
    UNITY_SHADOW_COORDS(5)
};

v2fBase vertBase(appdata_full v)
{
    v2fBase o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

    half3 sh = ShadeSH9(half4(o.normal, 1.0));
    o.gamb = sh;
#ifdef VERTEXLIGHT_ON
    sh += Shade4PointLights(
        unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
        unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
        unity_4LightAtten0, o.worldPos, o.normal);
#endif
    o.vlight = sh * ActAmbientRate();

    UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);
    return o;
}

half4 fragBase(v2fBase i) : SV_Target
{
    half4 albedo = tex2D(_MainTex, i.uv);
#if defined(ACT_TRANSPARENT)
    albedo.a *= _Color.a;
#endif
    clip(albedo.a - ACT_CLIP_EPS);

    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
    half shadowAtten = UNITY_SHADOW_ATTENUATION(i, i.worldPos);

    half3 N = normalize(i.normal);
    half3 L = normalize(_WorldSpaceLightPos0.xyz);
    half NdotL = dot(N, L);

    half3 ramp = ActRamp(NdotL, shadowAtten);
    half3 lightColor = _LightColor0.rgb * atten;

    half3 baseD = albedo.rgb * ActTempDiffuse(i.gamb);
    half3 c = baseD * lightColor * ramp;
    c += albedo.rgb * i.vlight;

    return half4(c, albedo.a);
}

#endif // ACT_PASS_BASE

// ======================= ForwardAdd =======================
#if defined(ACT_PASS_ADD)

struct v2fAdd
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    half3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    UNITY_SHADOW_COORDS(3)
};

v2fAdd vertAdd(appdata_full v)
{
    v2fAdd o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);
    return o;
}

half4 fragAdd(v2fAdd i) : SV_Target
{
    half4 albedo = tex2D(_MainTex, i.uv);
#if defined(ACT_TRANSPARENT)
    albedo.a *= _Color.a;
#endif
    clip(albedo.a - ACT_CLIP_EPS);

    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

    half3 N = normalize(i.normal);
    half3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
    half toonRefl = ActToonRefl(dot(N, L));
    half toonShadow = ActToonShadow(toonRefl);
    half addStr = ActForwardAddStr(toonRefl);

    half3 ramp = ActRampAdd(toonRefl, toonShadow);
    half3 lightColor = _LightColor0.rgb * atten;

    half3 baseD = albedo.rgb * ActTempDiffuseNoAmbient();
    half3 c = baseD * lightColor * ramp * addStr;

#if defined(ACT_TRANSPARENT)
    c = min(c, half3(1, 1, 1)) * albedo.a;
#endif
    return half4(c, 0.0);
}

#endif // ACT_PASS_ADD

// ======================= ShadowCaster =======================
#if defined(ACT_PASS_SHADOW)

struct v2fShadow
{
    V2F_SHADOW_CASTER;
    float2 uv : TEXCOORD1;
};

v2fShadow vertShadow(appdata_base v)
{
    v2fShadow o;
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
    return o;
}

half4 fragShadow(v2fShadow i) : SV_Target
{
    clip(-_NoShadowCasting); // _NoShadowCasting = 1 时不投影
#if defined(ACT_TRANSPARENT)
    clip(ActGetAlpha(i.uv) - ACT_CLIP_EPS);
#endif
    SHADOW_CASTER_FRAGMENT(i)
}

#endif // ACT_PASS_SHADOW

// ======================= Edge =======================
#if defined(ACT_PASS_EDGE)

struct v2fEdge
{
    float4 pos : SV_POSITION;
    half3 vlight : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    UNITY_SHADOW_COORDS(2)
};

v2fEdge vertEdge(appdata_full v)
{
    v2fEdge o;
    // 沿法线外扩 _EdgeSize（与原版一致）；未启用描边时把顶点丢到屏幕外
    v.vertex.xyz += v.normal * _EdgeSize;
    o.pos = UnityObjectToClipPos(v.vertex);
    if (_EdgeEnabled < 0.5) o.pos = float4(2, 2, 2, 1);
    half3 worldN = UnityObjectToWorldNormal(v.normal);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.vlight = ShadeSH9(half4(worldN, 1.0));
    UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);
    return o;
}

half4 fragEdge(v2fEdge i) : SV_Target
{
    clip(_EdgeColor.a - ACT_CLIP_EPS);
    UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
    half3 color = _LightColor0.rgb * atten * ACT_GLOBALLIGHTING;
    color += i.vlight;
    color *= _EdgeColor.rgb;
    return half4(color, _EdgeColor.a);
}

#endif // ACT_PASS_EDGE
