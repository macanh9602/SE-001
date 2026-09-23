Shader "IN006/CakeBasic"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _Color ("Color", Color) = (1,1,1,1)
        _Hue ("Hue", Range(-0.5,0.5)) = 0
        _Saturation ("Saturation", Range(0,2)) = 1
        _Brightness ("Lightness", Range(0,2)) = 1
        [Toggle] _Colorize ("Colorize (nhu Photoshop)", Float) = 0
        _SpiralColor ("Spiral Detail Color", Color) = (1,1,1,1)
        _SpiralStrength ("Spiral Detail Strength", Range(0,1)) = 0
        _SpiralMaskMin ("Spiral Mask Min", Range(0,1)) = 0.68
        _SpiralMaskMax ("Spiral Mask Max", Range(0,1)) = 0.78
        [Header(Piece inner highlight)]
        _PieceInnerHighlightColor ("Piece Inner Highlight Color Override", Color) = (0,0,0,0)
        _PieceInnerHighlightStrength ("Piece Inner Highlight Strength", Range(0,1)) = 0.55
        _PieceInnerHighlightWidth ("Piece Inner Highlight Width", Range(0.005,0.05)) = 0.015
        _PieceInnerHighlightInset ("Piece Inner Highlight Inset", Range(0,0.1)) = 0.012
        _PieceInnerHighlightSoftness ("Piece Inner Highlight Softness", Range(0.002,0.04)) = 0.006
        _PieceInnerHighlightLift ("Piece Inner Highlight Lighten", Range(0,0.3)) = 0.14
        _PieceInnerHighlightUpperDirection ("Piece Inner Highlight Upper Direction XZ", Vector) = (0,1,0,0)
        _PieceInnerHighlightUpperStart ("Piece Inner Highlight Upper Start", Range(-1,1)) = 0
        _PieceInnerHighlightUpperEnd ("Piece Inner Highlight Upper End", Range(-1,1)) = 0.75
        [HideInInspector] _PieceInnerHighlightCenterOS ("Piece Inner Highlight Center OS", Vector) = (0,0,0,0)
        [HideInInspector] _PieceInnerHighlightExtentOS ("Piece Inner Highlight Extent OS", Vector) = (1,1,1,0)
        [HideInInspector] _PieceInnerHighlightEnabled ("Piece Inner Highlight Enabled", Float) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.35
        [Header(Color preserving fake lighting)]
        _LightDirWS ("Light Direction (world)", Vector) = (-0.35, 0.55, -0.75, 0)
        _Ambient ("Ambient Floor", Range(0,1)) = 0.78
        _SpecIntensity ("Spec Intensity", Range(0,1)) = 0.16
        _SpecPower ("Spec Power", Range(2,128)) = 42
        _SpecTint ("Spec Tint", Color) = (1, 0.97, 0.92, 1)
        [HideInInspector] _Cull ("__cull", Float) = 2
        [HideInInspector] _CakeCutProgress ("Whole Roll Remaining", Range(0,1)) = 1
        [HideInInspector] _CakeCutMinY ("Whole Roll Min Y", Float) = -1
        [HideInInspector] _CakeCutMaxY ("Whole Roll Max Y", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                half _Hue;
                half _Saturation;
                half _Brightness;
                half _Colorize;
                half4 _SpiralColor;
                half _SpiralStrength;
                half _SpiralMaskMin;
                half _SpiralMaskMax;
                half4 _PieceInnerHighlightColor;
                half _PieceInnerHighlightStrength;
                half _PieceInnerHighlightWidth;
                half _PieceInnerHighlightInset;
                half _PieceInnerHighlightSoftness;
                half _PieceInnerHighlightLift;
                float4 _PieceInnerHighlightUpperDirection;
                half _PieceInnerHighlightUpperStart;
                half _PieceInnerHighlightUpperEnd;
                float4 _PieceInnerHighlightCenterOS;
                float4 _PieceInnerHighlightExtentOS;
                half _PieceInnerHighlightEnabled;
                half _Smoothness;
                half4 _LightDirWS;
                half _Ambient;
                half _SpecIntensity;
                half _SpecPower;
                half4 _SpecTint;
                half _CakeCutProgress;
                float _CakeCutMinY;
                float _CakeCutMaxY;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half fogFactor : TEXCOORD3; float positionOSY : TEXCOORD4; float2 positionOSXZ : TEXCOORD5; };

            float3 RgbToHsl(float3 c)
            {
                float maxC = max(c.r, max(c.g, c.b));
                float minC = min(c.r, min(c.g, c.b));
                float d = maxC - minC;
                float h = 0;
                float s = 0;
                float l = (maxC + minC) * 0.5;
                if (d > 0.00001)
                {
                    s = d / max(0.00001, 1 - abs(2 * l - 1));
                    if (maxC == c.r) h = (c.g - c.b) / d + (c.g < c.b ? 6 : 0);
                    else if (maxC == c.g) h = (c.b - c.r) / d + 2;
                    else h = (c.r - c.g) / d + 4;
                    h /= 6;
                }
                return float3(h, s, l);
            }

            float HueToRgb(float p, float q, float t)
            {
                t = frac(t);
                if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
                return p;
            }

            float3 HslToRgb(float3 hsl)
            {
                if (hsl.y <= 0.00001) return hsl.zzz;
                float q = hsl.z < 0.5 ? hsl.z * (1 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
                float p = 2 * hsl.z - q;
                return float3(HueToRgb(p, q, hsl.x + 1.0 / 3.0), HueToRgb(p, q, hsl.x), HueToRgb(p, q, hsl.x - 1.0 / 3.0));
            }

            float3 ApplyHsl(float3 rgb)
            {
                float3 hsl = RgbToHsl(saturate(rgb));

                // _Colorize = 0  -> hanh vi cu: Hue XOAY tuong doi, Saturation NHAN.
                // _Colorize = 1  -> giong checkbox Colorize cua Photoshop:
                //                   Hue la mau dich TUYET DOI, Saturation la gia tri
                //                   tuyet doi. Chi tiet van giu nguyen o kenh lightness.
                float hRot = frac(hsl.x + _Hue + 1);
                float hAbs = frac(_Hue + 1);
                float sMul = saturate(hsl.y * _Saturation);
                float sAbs = saturate(_Saturation * 0.5);

                hsl.x = lerp(hRot, hAbs, _Colorize);
                hsl.y = lerp(sMul, sAbs, _Colorize);
                hsl.z = saturate(hsl.z * _Brightness);
                return HslToRgb(hsl);
            }

            // Cake Material Studio tune mau trong gamma/sRGB space (giong Photoshop), con
            // project chay Linear color space. Phai doi ca texel lan _Color sang sRGB, nhan
            // va chay HSL o do, roi moi doi nguoc ve linear. Neu lam thang tren gia tri
            // linear thi hue lech han: Orange #FF9400 ra vang chanh ~#E8B600.
            // Chi phi: 2 x LinearToSRGB + 1 x SRGBToLinear moi pixel. Can re hon thi doi
            // sang FastLinearToSRGB/FastSRGBToLinear, nhung phai sua mirror ben
            // CakeMaterialPreviewUtility cho khop.
#ifdef UNITY_COLORSPACE_GAMMA
            #define CAKE_TO_TUNING_SPACE(c) (c)
            #define CAKE_TO_RENDER_SPACE(c) (c)
#else
            #define CAKE_TO_TUNING_SPACE(c) LinearToSRGB(c)
            #define CAKE_TO_RENDER_SPACE(c) SRGBToLinear(c)
#endif

            float3 ApplyCakeTint(float3 texRgb, float3 colorRgb)
            {
                float3 tuned = CAKE_TO_TUNING_SPACE(saturate(texRgb)) * CAKE_TO_TUNING_SPACE(saturate(colorRgb));
                return CAKE_TO_RENDER_SPACE(saturate(ApplyHsl(tuned)));
            }

            float GetSpiralMask(float3 texRgb)
            {
                float3 source = CAKE_TO_TUNING_SPACE(saturate(texRgb));
                float sourceMax = max(source.r, max(source.g, source.b));
                float sourceMin = min(source.r, min(source.g, source.b));
                float sourceLightness = (sourceMax + sourceMin) * 0.5;
                float maskMax = max(_SpiralMaskMax, _SpiralMaskMin + 0.001);
                return smoothstep(_SpiralMaskMin, maskMax, sourceLightness);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.positionOSY = input.positionOS.y;
                output.positionOSXZ = input.positionOS.xz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // The roll prefab's cylinder axis is object-space Y. Opaque alpha clip removes the
                // already-cut section without transparency overdraw or rebuilding the mesh.
                float cutCoordinate = (input.positionOSY - _CakeCutMinY) / max(0.0001, _CakeCutMaxY - _CakeCutMinY);
                // Remaining progress shrinks the visible roll from its top end, matching the queue's
                // authored top-to-bottom knife and piece order.
                // At full progress the far cap lies exactly on the cut boundary. Small interpolation
                // precision errors can otherwise discard that entire cap. Only enable geometric
                // clipping after cutting has actually started.
                UNITY_BRANCH
                if (_CakeCutProgress < 0.9999h)
                {
                    clip(_CakeCutProgress - cutCoordinate);
                }
                float4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 sampled = texel * _Color;
                float3 albedo = ApplyCakeTint(texel.rgb, _Color.rgb);
                UNITY_BRANCH
                if (_SpiralStrength > 0.0001)
                {
                    float spiralBlend = GetSpiralMask(texel.rgb) * saturate(_SpiralStrength);
                    albedo = lerp(albedo, saturate(_SpiralColor.rgb), spiralBlend);
                }
                half3 normal = normalize(input.normalWS);
                half3 lightDirection = normalize(_LightDirWS.xyz);
                half wrappedNdl = dot(normal, lightDirection) * 0.5h + 0.5h;
                half diffuse = lerp(_Ambient, 1.0h, wrappedNdl * wrappedNdl);
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize(lightDirection + viewDirection);
                half specular = pow(saturate(dot(normal, halfDirection)), max(2.0h, _SpecPower))
                    * _SpecIntensity * (0.35h + _Smoothness);
                half3 lit = (albedo * diffuse) + (_SpecTint.rgb * specular);
                UNITY_BRANCH
                if (_PieceInnerHighlightEnabled > 0.5h)
                {
                    float2 extent = max(abs(_PieceInnerHighlightExtentOS.xz), float2(0.0001, 0.0001));
                    float2 normalizedPosition = (input.positionOSXZ - _PieceInnerHighlightCenterOS.xz) / extent;
                    half contourDistance = length(normalizedPosition);
                    half outerEdge = saturate(1.0h - _PieceInnerHighlightInset);
                    half innerEdge = saturate(outerEdge - _PieceInnerHighlightWidth);
                    half highlightBand = smoothstep(
                        innerEdge - _PieceInnerHighlightSoftness,
                        innerEdge + _PieceInnerHighlightSoftness,
                        contourDistance) * (1.0h - smoothstep(
                        outerEdge - _PieceInnerHighlightSoftness,
                        outerEdge + _PieceInnerHighlightSoftness,
                        contourDistance));
                    float2 upperDirection = normalize(_PieceInnerHighlightUpperDirection.xy + float2(0.0001, 0.0001));
                    half upperFacing = dot(normalize(normalizedPosition + float2(0.0001, 0.0001)), upperDirection);
                    half upperMask = smoothstep(
                        _PieceInnerHighlightUpperStart,
                        max(_PieceInnerHighlightUpperStart + 0.0001h, _PieceInnerHighlightUpperEnd),
                        upperFacing);
                    highlightBand *= saturate(_PieceInnerHighlightStrength) * upperMask;
                    half hasOverride = step(0.0001h, _PieceInnerHighlightColor.a);
                    half3 derivedHighlight = lerp(albedo, half3(1.0h, 1.0h, 1.0h), saturate(_PieceInnerHighlightLift));
                    half3 highlightColor = lerp(derivedHighlight, _PieceInnerHighlightColor.rgb, hasOverride) * diffuse;
                    lit = lerp(lit, highlightColor, highlightBand);
                }
                return half4(MixFog(lit, input.fogFactor), sampled.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };
            Varyings ShadowVert(Attributes input) { Varyings output; output.positionHCS = TransformObjectToHClip(input.positionOS.xyz); return output; }
            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            ZWrite On
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };
            Varyings DepthVert(Attributes input) { Varyings output; output.positionHCS = TransformObjectToHClip(input.positionOS.xyz); return output; }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings NormalsVert(Attributes input) { Varyings output; output.positionHCS = TransformObjectToHClip(input.positionOS.xyz); output.normalWS = TransformObjectToWorldNormal(input.normalOS); return output; }
            half4 NormalsFrag(Varyings input) : SV_Target { return half4(NormalizeNormalPerPixel(input.normalWS), 0); }
            ENDHLSL
        }
    }
}
