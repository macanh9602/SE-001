Shader "SE001/JarSandFill"
{
    Properties
    {
        _MaskTex ("Inner Mask", 2D) = "white" {}
        _SandColor ("Sand Color", Color) = (1, 1, 1, 1)
        _FillLevel ("Area Correct Fill", Range(0, 1)) = 0
        _FillDirOS ("World Down Object Space", Vector) = (0, -1, 0, 0)
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
                half4 _FillDirOS;
                half _FillLevel;
                half _Sparkle;
            CBUFFER_END
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionOS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MaskTex);
                output.positionOS = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                float localHeight = saturate(input.positionOS.y + 0.5);
                float fillCoordinate = _FillDirOS.y < 0 ? localHeight : 1 - localHeight;
                clip(_FillLevel - fillCoordinate + 0.001);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 8).r;
                half sparkle = lerp(1.0h, 0.88h + noise * 0.24h, _Sparkle);
                return half4(_SandColor.rgb * sparkle, mask.a * _SandColor.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
