Shader "SE001/JarSandFill"
{
    // Sand inside a Source jar. The CPU solver (SourceSandFillSolver) gives a world-down direction in object
    // space and an area-correct plane offset; sand = inner mask AND dot(positionOS.xy, _FillDirOS.xy) >= _FillThreshold.
    // The surface stays level in the world while the jar rotates (tilt / slide / pour / settle).
    Properties
    {
        _MaskTex ("Inner Mask", 2D) = "white" {}
        _SandColor ("Sand Color", Color) = (1, 1, 1, 1)
        _FillLevel ("Fill Ratio (info only)", Range(0, 1)) = 0
        _FillDirOS ("Felt Down (object space)", Vector) = (0, -1, 0, 0)
        _FillThreshold ("Sand Plane Offset (object units)", Float) = 1000
        _SurfaceBand ("Surface Highlight Width (object units)", Range(0, 0.2)) = 0.05
        _SurfaceLift ("Surface Highlight Lighten", Range(0, 0.6)) = 0.22
        _NoiseTex ("Sparkle Noise", 2D) = "white" {}
        _Sparkle ("Sparkle", Range(0, 1)) = 1
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
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _MaskTex_ST;
                float4 _NoiseTex_ST;
                half4 _SandColor;
                float4 _FillDirOS;
                float _FillThreshold;
                half _FillLevel;
                half _SurfaceBand;
                half _SurfaceLift;
                half _Sparkle;
            CBUFFER_END
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 positionOS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MaskTex);
                output.positionOS = input.positionOS.xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv).a;
                float depth = dot(input.positionOS, _FillDirOS.xy) - _FillThreshold;
                clip(min(depth, mask - 0.5h));
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 8).r;
                half sparkle = lerp(1.0h, 0.88h + noise * 0.24h, _Sparkle);
                half surface = 1.0h - saturate((half)depth / max(_SurfaceBand, 0.001h));
                half3 color = _SandColor.rgb * sparkle;
                color = lerp(color, half3(1.0h, 1.0h, 1.0h), surface * _SurfaceLift);
                return half4(color, _SandColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
