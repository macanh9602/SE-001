#ifndef SE001_SAND_GRAIN_INCLUDED
#define SE001_SAND_GRAIN_INCLUDED

// Shared sand grain look (Sand Level Lab, approved 2026-09-24): round grains, 3 tones of one hue + a slightly
// deeper tone, lighter top surface, rare twinkling sparkle. Used by the sand field and by the sand inside a Source
// so both read as the same material.

float SandHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Tone mixing happens in gamma (sRGB) space, like the lab canvas. The project renders in Linear, and mixing a
// linear colour toward white by 37 % reads almost white (Movie_010: washed-out, "porous" piles).
half3 SandToGamma(half3 c)
{
#if defined(UNITY_COLORSPACE_GAMMA)
    return c;
#else
    return pow(max(c, 0.0h), 1.0h / 2.2h);
#endif
}

half3 SandToLinear(half3 c)
{
#if defined(UNITY_COLORSPACE_GAMMA)
    return c;
#else
    return pow(max(c, 0.0h), 2.2h);
#endif
}

// baseColor is the shader-space colour (linear in a Linear project). amount mixes toward white in gamma space.
half3 SandMixWhite(half3 baseColor, half amount)
{
    return SandToLinear(lerp(SandToGamma(baseColor), half3(1.0h, 1.0h, 1.0h), amount));
}

half3 SandScaleGamma(half3 baseColor, half factor)
{
    return SandToLinear(SandToGamma(baseColor) * factor);
}

// t: per-grain random 0..1. 45 % base, 30 % light, 15 % deeper, 10 % lighter.
half3 SandToneColor(half3 baseColor, half t, half lightMix, half lighterMix, half deepMul)
{
    if (t < 0.45h) return baseColor;
    if (t < 0.75h) return SandMixWhite(baseColor, lightMix);
    if (t < 0.90h) return SandScaleGamma(baseColor, deepMul);
    return SandMixWhite(baseColor, lighterMix);
}

// Sparkle on ~5 % of grains (t above threshold), small white core, slow twinkle.
half SandSparkle(half t, float distanceToCenter, float time, half amount)
{
    if (amount <= 0.0h || t < 0.95h || distanceToCenter > 0.24) return 0.0h;
    half twinkle = 0.5h + 0.5h * sin(time * 7.0 + t * 80.0);
    return twinkle > 0.35h ? twinkle * amount : 0.0h;
}

// Procedural round-grain pattern on an unbounded grid (every cell is a grain), for areas that are "all sand".
// cellPos is in grain units. Returns base-relative color; gaps between grains get gapShade.
half3 SandGrainPattern(float2 cellPos, half3 baseColor, half lightMix, half lighterMix, half deepMul,
    half gapShade, half radius, half jitter, float time, half sparkleAmount)
{
    float2 origin = floor(cellPos - 0.5);
    float best = 1e4;
    half bestTone = 0.0h;
    float bestDistance = 0.0;
    [unroll] for (int j = 0; j < 2; j++)
    {
        [unroll] for (int i = 0; i < 2; i++)
        {
            float2 c = origin + float2(i, j);
            float h1 = SandHash21(c);
            float h2 = SandHash21(c + 17.13);
            float h3 = SandHash21(c + 41.71);
            float2 center = c + 0.5 + (float2(h1, h2) - 0.5) * 2.0 * jitter;
            float d = distance(cellPos, center);
            if (d < radius + h3 * 0.08 && d < best)
            {
                best = d;
                bestTone = (half)h3;
                bestDistance = d;
            }
        }
    }

    if (best > 1e3) return SandScaleGamma(baseColor, gapShade);
    half3 color = SandToneColor(baseColor, bestTone, lightMix, lighterMix, deepMul);
    half spark = SandSparkle(bestTone, bestDistance, time, sparkleAmount);
    return lerp(color, half3(1.0h, 1.0h, 1.0h), spark);
}

#endif
