// MMD 卡通着色（半透明，双面，可选描边）。同时支持内置管线与 URP，
// 可直接替换 MMD4Mecanim/MMDLit-BothFaces-Transparent(-Edge)。
Shader "ACT/MMDToonTransparent"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _ToonTex("ToonTex", 2D) = "white" {}
        _Color("Diffuse", Color) = (1,1,1,1)
        _Ambient("Ambient", Color) = (0.5,0.5,0.5,1)
        _Emissive("Emissive", Color) = (0,0,0,0)
        _EdgeColor("EdgeColor", Color) = (0,0,0,1)
        _EdgeSize("EdgeSize", Float) = 0.0005
        _EdgeWidthMul("EdgeWidthMul", Float) = 20
        [Toggle] _EdgeEnabled("EdgeEnabled", Float) = 0
    }

    // ---------------- URP ----------------
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Edge"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertEdge
            #pragma fragment fragEdge
            #include "MMDToon-URP.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vertLit
            #pragma fragment fragLit
            #include "MMDToon-URP.hlsl"
            ENDHLSL
        }
    }

    // ---------------- Built-in ----------------
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "Edge"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertEdge
            #pragma fragment fragEdge
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertLit
            #pragma fragment fragLit
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }
    }

    Fallback "Legacy Shaders/Transparent/VertexLit"
}
