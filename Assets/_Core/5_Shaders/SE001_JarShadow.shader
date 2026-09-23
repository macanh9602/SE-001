Shader "SE001/JarShadow"
{
    Properties
    {
        _MaskTex ("Silhouette Mask", 2D) = "white" {}
        _ShadowMul ("Shadow Multiplier", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Blend DstColor Zero
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
                half _ShadowMul;
            CBUFFER_END
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MaskTex);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv).a;
                clip(alpha - 0.01h);
                return half4(_ShadowMul, _ShadowMul, _ShadowMul, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
