// SE-001 SandField: unlit, alpha-blended display of the simulation texture (_BaseMap, set per-renderer
// via MaterialPropertyBlock by SandFieldVisual). Empty cells have alpha 0 so the board background shows through.
Shader "SE001/SandField"
{
    Properties
    {
        [MainTexture] _BaseMap ("Sand Cells (runtime)", 2D) = "clear" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input) { Varyings o; o.positionCS = TransformObjectToHClip(input.positionOS.xyz); o.uv = input.uv; return o; }
            half4 Frag(Varyings input) : SV_Target { return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor; }
            ENDHLSL
        }
    }
    FallBack Off
}
