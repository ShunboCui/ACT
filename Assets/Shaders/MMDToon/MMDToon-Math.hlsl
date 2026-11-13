// MMD 卡通着色的公共数学部分（内置管线 / URP 共用）。
// 公式按 MMD4Mecanim 原 shader 重新实现；不依赖运行时脚本设置的全局变量（___Eliminate），
// 因此在 build 中不会被整体 clip 掉。
//
// 包含本文件之前，调用方需要先定义：
//   half4 _Color, _Ambient, _ToonTone; half _ShadowLum, _AddLightToonCen, _AddLightToonMin, _SelfShadow;
//   宏 ACT_SAMPLE_TOON(r)：以 (r, r) 采样 toon 贴图并返回 rgb
#ifndef ACT_MMDTOON_MATH_INCLUDED
#define ACT_MMDTOON_MATH_INCLUDED

#define ACT_GLOBALLIGHTING 0.6
#define ACT_CLIP_EPS (1.0 / 255.0)

// 环境光回灌比例
inline half3 ActAmbientRate()
{
    half3 tempL = max(half3(0.5, 0.5, 0.5) - _Ambient.rgb, half3(0, 0, 0)) * 2.0;
    return half3(1, 1, 1) - tempL;
}

// ForwardBase 用的漫反射系数
inline half3 ActTempDiffuse(half3 globalAmbient)
{
    half3 t = min(_Ambient.rgb + _Color.rgb * ACT_GLOBALLIGHTING, half3(1, 1, 1));
    t = max(t - globalAmbient * ActAmbientRate(), half3(0, 0, 0));
    return t;
}

// ForwardAdd 用的漫反射系数（不含环境光）
inline half3 ActTempDiffuseNoAmbient()
{
    return saturate(_Ambient.rgb + _Color.rgb * ACT_GLOBALLIGHTING - half3(0.5, 0.5, 0.5));
}

inline half ActToonRefl(half NdotL)
{
    return NdotL * _ToonTone.y + _ToonTone.z;
}

inline half ActShadowAttenToToon(half shadowAtten)
{
    return (shadowAtten - 0.5) * _ToonTone.x + _ToonTone.z;
}

inline half ActToonShadow(half toonRefl)
{
    half s = toonRefl * 2.0;
    return saturate(s * s - 1.0);
}

inline half ActForwardAddStr(half toonRefl)
{
    half s = (toonRefl - _AddLightToonCen) * 2.0;
    return clamp(s * s - 1.0, _AddLightToonMin, 1.0);
}

// ForwardBase 的明暗渐变。_SelfShadow 对应原 SELFSHADOW_ON（用属性而非 keyword，避免变体被剔除）。
inline half3 ActRamp(half NdotL, half shadowAtten)
{
    half selfS = step(0.5, _SelfShadow);
    half refl = saturate(min(ActToonRefl(NdotL), ActShadowAttenToToon(shadowAtten)));
    half toonRefl = refl;
    refl = refl * (1.0 - selfS);

    half3 ramp = ACT_SAMPLE_TOON(refl);
    ramp = lerp(ramp, half3(1, 1, 1), ActToonShadow(toonRefl) * selfS);
    return saturate(1.0 - (1.0 - ramp) * _ShadowLum);
}

// ForwardAdd 的明暗渐变
inline half3 ActRampAdd(half toonRefl, half toonShadow)
{
    half selfS = step(0.5, _SelfShadow);
    half refl = saturate(toonRefl) * (1.0 - selfS);

    half3 ramp = ACT_SAMPLE_TOON(refl);
    half3 rampSS = (1.0 - toonShadow) * ramp + toonShadow;
    ramp = lerp(ramp, rampSS, selfS);
    return saturate(1.0 - (1.0 - ramp) * _ShadowLum);
}

#endif
