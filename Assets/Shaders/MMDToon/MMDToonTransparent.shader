// MMD 卡通着色（半透明，双面，可选描边）。同时支持内置管线与 URP。
// 按 MMD4Mecanim/MMDLit-BothFaces-Transparent(-Edge) 的光照公式重新实现：
// ZWrite On、队列 Geometry+2、Blend SrcAlpha OneMinusSrcAlpha，alpha = 贴图 alpha * _Color.a，
// 近乎全透明的像素被裁掉。不依赖运行时脚本设置的全局变量。
Shader "ACT/MMDToonTransparent"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _ToonTex("ToonTex", 2D) = "white" {}
        _Color("Diffuse", Color) = (1,1,1,1)
        _Ambient("Ambient", Color) = (0.5,0.5,0.5,1)
        _ShadowLum("ShadowLum", Range(0,10)) = 1.5
        _ToonTone("ToonTone", Vector) = (1.0, 0.5, 0.5, 0.0)
        _AddLightToonCen("AddLightToonCen", Float) = -0.1
        _AddLightToonMin("AddLightToonMin", Float) = 0.5
        _EdgeColor("EdgeColor", Color) = (0,0,0,1)
        _EdgeSize("EdgeSize", Float) = 0.0005
        [Toggle] _EdgeEnabled("EdgeEnabled", Float) = 0
        [Toggle] _SelfShadow("SelfShadow", Float) = 1
        [Toggle] _NoShadowCasting("NoShadowCasting", Float) = 0
    }

    // ---------------- URP ----------------
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+2" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma vertex vertLit
            #pragma fragment fragLit
            #define ACT_TRANSPARENT
            #define ACT_PASS_LIT
            #include "MMDToon-URP.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #define ACT_TRANSPARENT
            #define ACT_PASS_SHADOW
            #include "MMDToon-URP.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "FORWARD_EDGE"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha
            Offset 0.1, 1

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vertEdge
            #pragma fragment fragEdge
            #define ACT_TRANSPARENT
            #define ACT_PASS_EDGE
            #include "MMDToon-URP.hlsl"
            ENDHLSL
        }
    }

    // ---------------- Built-in ----------------
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Geometry+2" }
        Cull Off

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap
            #pragma vertex vertBase
            #pragma fragment fragBase
            #define ACT_TRANSPARENT
            #define ACT_PASS_BASE
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }

        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_fwdadd
            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #define ACT_TRANSPARENT
            #define ACT_PASS_ADD
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Offset 1, 1

            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #define ACT_TRANSPARENT
            #define ACT_PASS_SHADOW
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }

        Pass
        {
            Name "FORWARD_EDGE"
            Tags { "LightMode" = "ForwardBase" }
            Cull Front
            ZWrite On
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha
            Offset 0.1, 1

            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap novertexlight
            #pragma vertex vertEdge
            #pragma fragment fragEdge
            #define ACT_TRANSPARENT
            #define ACT_PASS_EDGE
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }
    }
}
