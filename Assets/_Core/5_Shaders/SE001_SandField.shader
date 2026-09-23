// SE-001 SandField: renders the simulation grid as round grains (Sand Level Lab look, approved 2026-09-24).
// _BaseMap (set per renderer by SandFieldVisual, point-sampled, linear) encodes each cell:
//   R = material id, G = per-grain tone, B = flags (1 surface, 2 overhang, 4 airborne, 8 cosmetic stream, 16 stream edge, 32 trail),
//   A = coverage (255 grain, <255 cosmetic stream pixel, 0 empty).
// _Palette: material id -> sand color. Presentation only; empty/geometry cells stay transparent.
Shader "SE001/SandField"
{
    Properties
    {
        [MainTexture] _BaseMap ("Sand Cells (runtime)", 2D) = "clear" {}
        _Palette ("Palette (runtime)", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _GridSize ("Grid Size (runtime)", Vector) = (1, 1, 0, 0)
        _RoundGrains ("Round Grains", Float) = 1
        _GrainRadius ("Grain Radius (cells)", Range(0.4, 0.9)) = 0.72
        _GrainJitter ("Grain Jitter (cells)", Range(0, 0.4)) = 0.25
        _StreamSpread ("Stream Grain Spread (x jitter)", Range(1, 3)) = 1.8
        _StreamShimmer ("Stream Re-scatter Rate (per second)", Range(0, 60)) = 30
        _GapShade ("Gap Shade", Range(0.5, 1)) = 0.95
        _ToneLight ("Light Tone Mix", Range(0, 0.6)) = 0.16
        _ToneLighter ("Lighter Tone Mix", Range(0, 0.8)) = 0.37
        _ToneDeep ("Deep Tone Multiplier", Range(0.6, 1)) = 0.88
        _SurfaceLift ("Surface Tone Mix (lab: = lighter tone)", Range(0, 0.6)) = 0.37
        _OverhangShade ("Overhang Shade", Range(0.6, 1)) = 1
        _Sparkle ("Sparkle", Range(0, 1)) = 1
        _StreamTrail ("Stream Trail Alpha (lab 0.45 x keep)", Range(0, 1)) = 0.35
        _StreamGrainOpacity ("Stream Grain Opacity Boost", Range(1, 3)) = 1.8
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SE001_SandGrain.hlsl"
            TEXTURE2D(_BaseMap);
            TEXTURE2D(_Palette);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Palette_ST;
                half4 _BaseColor;
                float4 _GridSize;
                half _RoundGrains;
                half _GrainRadius;
                half _GrainJitter;
                half _StreamSpread;
                half _StreamShimmer;
                half _GapShade;
                half _ToneLight;
                half _ToneLighter;
                half _ToneDeep;
                half _SurfaceLift;
                half _OverhangShade;
                half _Sparkle;
                half _StreamTrail;
                half _StreamGrainOpacity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float4 LoadCell(int2 c)
            {
                int2 size = int2(_GridSize.xy);
                if (c.x < 0 || c.y < 0 || c.x >= size.x || c.y >= size.y) return float4(0, 0, 0, 0);
                return LOAD_TEXTURE2D(_BaseMap, c);
            }

            half3 CellColor(float4 cell, float distanceToCenter)
            {
                int id = (int)round(cell.r * 255.0);
                half3 baseColor = LOAD_TEXTURE2D(_Palette, int2(id, 0)).rgb;
                uint flags = (uint)round(cell.b * 255.0);
                half tone = (half)cell.g;
                half3 color = SandToneColor(baseColor, tone, _ToneLight, _ToneLighter, _ToneDeep);
                // Lab look: a top-surface grain is drawn in the lighter tone (not an extra lift on top of its own tone).
                if ((flags & 1u) != 0u) color = SandMixWhite(baseColor, _SurfaceLift);
                else if ((flags & 2u) != 0u) color = SandScaleGamma(color, _OverhangShade);
                half spark = SandSparkle(tone, distanceToCenter, _Time.y, _Sparkle);
                return lerp(color, half3(1.0h, 1.0h, 1.0h), spark);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 cellPos = input.uv * _GridSize.xy;
                if (_RoundGrains < 0.5h)
                {
                    float4 ownCell = LoadCell(int2(floor(cellPos)));
                    if (ownCell.a <= 0.002) return half4(0, 0, 0, 0);
                    return half4(CellColor(ownCell, 1.0), (half)ownCell.a) * _BaseColor;
                }

                // Up to 4 neighbouring grains can overlap this pixel (radius <= 1 cell, jitter <= 0.4).
                float2 origin = floor(cellPos - 0.5);
                float best = 1e4;
                float4 bestCell = float4(0, 0, 0, 0);
                int restingNeighbours = 0;
                float4 anyResting = float4(0, 0, 0, 0);
                float shimmer = floor(_Time.y * _StreamShimmer);
                [unroll] for (int j = 0; j < 2; j++)
                {
                    [unroll] for (int i = 0; i < 2; i++)
                    {
                        int2 c = int2(origin) + int2(i, j);
                        float4 cell = LoadCell(c);
                        if (cell.a <= 0.002) continue;
                        uint flags = (uint)round(cell.b * 255.0);
                        // Flag 32 = fading trail left by the stream: drawn as the faint trail layer below, never as a grain.
                        if ((flags & 32u) != 0u) continue;
                        bool moving = (flags & (4u | 8u)) != 0u;
                        if (!moving)
                        {
                            restingNeighbours++;
                            anyResting = cell;
                        }
                        // Falling / stream grains re-scatter every few frames so the stream shimmers like pouring sand.
                        float seed = moving ? shimmer : 0.0;
                        float h1 = SandHash21(float2(c) + seed * 0.37);
                        float h2 = SandHash21(float2(c.yx) + 11.3 + seed * 0.71);
                        float2 spread = moving ? float2(_StreamSpread, 1.5) : float2(1.0, 1.0);
                        float2 center = float2(c) + 0.5 + (float2(h1, h2) - 0.5) * 2.0 * _GrainJitter * spread;
                        // Moving grains are smaller (lab: 0.42..0.67 cell) so the wider x scatter still fits the 2x2 search.
                        float radius = moving ? _GrainRadius * 0.75 : _GrainRadius;
                        float d = distance(cellPos, center);
                        if (d < radius && d < best)
                        {
                            best = d;
                            bestCell = cell;
                        }
                    }
                }

                if (best > 1e3)
                {
                    // Gap between packed grains inside a resting pile (3+ of the 4 surrounding cells are grains):
                    // darker fill keeps the pile solid. Open edges stay transparent so pile silhouettes stay round.
                    if (restingNeighbours >= 3)
                    {
                        int id = (int)round(anyResting.r * 255.0);
                        half3 gap = SandScaleGamma(LOAD_TEXTURE2D(_Palette, int2(id, 0)).rgb, _GapShade);
                        return half4(gap, 1.0h) * _BaseColor;
                    }

                    // Lab "trail" layer: a faint column behind falling grains so the stream never reads as broken.
                    float4 own = LoadCell(int2(floor(cellPos)));
                    uint ownFlags = (uint)round(own.b * 255.0);
                    if (own.a <= 0.002 || (ownFlags & (4u | 8u)) == 0u || _StreamTrail <= 0.0h) return half4(0, 0, 0, 0);
                    int trailId = (int)round(own.r * 255.0);
                    half3 trail = SandMixWhite(LOAD_TEXTURE2D(_Palette, int2(trailId, 0)).rgb, _ToneLight);
                    return half4(trail, _StreamTrail * (half)own.a) * _BaseColor;
                }

                // Lab stream grains are opaque; cosmetic coverage only thins the ragged edge.
                half grainAlpha = saturate((half)bestCell.a * _StreamGrainOpacity);
                return half4(CellColor(bestCell, best), grainAlpha) * _BaseColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
