Shader "SE001/JarSandFill"
{
    // Sand inside a Source jar. The CPU solver (SourceSandFillSolver) gives a world-down direction in object
    // space and an area-correct plane offset; sand = inner mask AND dot(positionOS.xy, _FillDirOS.xy) >= _FillThreshold.
    // The surface stays level in the world while the jar rotates (tilt / slide / pour / settle).
    // Grain look = SE001_SandGrain.hlsl, sized in world units = sand sim cell, so jar sand matches field sand.
    Properties
    {
        _MaskTex ("Inner Mask", 2D) = "white" {}
        _SandColor ("Sand Color", Color) = (1, 1, 1, 1)
        _FillLevel ("Fill Ratio (info only)", Range(0, 1)) = 0
        _FillDirOS ("Felt Down (object space)", Vector) = (0, -1, 0, 0)
        _FillThreshold ("Sand Plane Offset (object units)", Float) = 1000
        _SurfaceBand ("Surface Highlight Width (object units)", Range(0, 0.2)) = 0.05
        _SurfaceLift ("Surface Highlight Lighten", Range(0, 0.6)) = 0.22
        _Sparkle ("Sparkle", Range(0, 1)) = 1
        [Header(Grain)]
        _GrainCellWorld ("Grain Size (world, = sand cell)", Float) = 0.06
        _GrainRadius ("Grain Radius (cells)", Range(0.4, 0.9)) = 0.72
        _GrainJitter ("Grain Jitter (cells)", Range(0, 0.4)) = 0.25
        _GapShade ("Gap Shade", Range(0.5, 1)) = 0.95
        _ToneLight ("Light Tone Mix", Range(0, 0.6)) = 0.16
        _ToneLighter ("Lighter Tone Mix", Range(0, 0.8)) = 0.37
        _ToneDeep ("Deep Tone Multiplier", Range(0.6, 1)) = 0.88
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+1" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SE001_SandGrain.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MaskTex_ST;
                half4 _SandColor;
                float4 _FillDirOS;
                float _FillThreshold;
                half _FillLevel;
                half _SurfaceBand;
                half _SurfaceLift;
                half _Sparkle;
                float _GrainCellWorld;
                half _GrainRadius;
                half _GrainJitter;
                half _GapShade;
                half _ToneLight;
                half _ToneLighter;
                half _ToneDeep;
            CBUFFER_END
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 positionOS : TEXCOORD1; float2 grainPos : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MaskTex);
                output.positionOS = input.positionOS.xy;
                // Uniform object scale → grain size in world units matches the sand field cell.
                float scaleWS = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
                output.grainPos = input.positionOS.xy * scaleWS / max(_GrainCellWorld, 0.001);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv).a;
                float depth = dot(input.positionOS, _FillDirOS.xy) - _FillThreshold;
                clip(min(depth, mask - 0.5h));
                half3 color = SandGrainPattern(input.grainPos, _SandColor.rgb, _ToneLight, _ToneLighter, _ToneDeep,
                    _GapShade, _GrainRadius, _GrainJitter, _Time.y, _Sparkle);
                half surface = 1.0h - saturate((half)depth / max(_SurfaceBand, 0.001h));
                color = SandMixWhite(color, surface * _SurfaceLift);
                return half4(color, _SandColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
