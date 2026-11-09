// MMD 卡通着色（不透明，双面，可选描边）。同时支持内置管线与 URP，
// 属性名与 MMD4Mecanim 材质一致，可直接替换 MMD4Mecanim/MMDLit-BothFaces(-Edge)。
Shader "ACT/MMDToon"
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
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertLit
            #pragma fragment fragLit
            #include "MMDToon-URP.hlsl"
            ENDHLSL
        }

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
    }

    // ---------------- Built-in ----------------
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }
            Cull Off
            ZWrite On

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertLit
            #pragma fragment fragLit
            #include "MMDToon-BuiltIn.cginc"
            ENDCG
        }

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
    }

    Fallback "Legacy Shaders/VertexLit"
}
