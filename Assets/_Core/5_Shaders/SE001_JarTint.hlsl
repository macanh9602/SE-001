#ifndef SE001_JAR_TINT_INCLUDED
#define SE001_JAR_TINT_INCLUDED

float3 SE001_RgbToHsl(float3 c)
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

float SE001_HueToRgb(float p, float q, float t)
{
    t = frac(t);
    if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
    if (t < 1.0 / 2.0) return q;
    if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
    return p;
}

float3 SE001_HslToRgb(float3 hsl)
{
    if (hsl.y <= 0.00001) return hsl.zzz;
    float q = hsl.z < 0.5 ? hsl.z * (1 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
    float p = 2 * hsl.z - q;
    return float3(
        SE001_HueToRgb(p, q, hsl.x + 1.0 / 3.0),
        SE001_HueToRgb(p, q, hsl.x),
        SE001_HueToRgb(p, q, hsl.x - 1.0 / 3.0));
}

float3 SE001_ApplyHsl(float3 rgb, float hue, float saturation, float brightness, float colorize)
{
    float3 hsl = SE001_RgbToHsl(saturate(rgb));
    float hRot = frac(hsl.x + hue + 1);
    float hAbs = frac(hue + 1);
    float sMul = saturate(hsl.y * saturation);
    float sAbs = saturate(saturation * 0.5);
    hsl.x = lerp(hRot, hAbs, colorize);
    hsl.y = lerp(sMul, sAbs, colorize);
    hsl.z = saturate(hsl.z * brightness);
    return SE001_HslToRgb(hsl);
}

#ifdef UNITY_COLORSPACE_GAMMA
    #define SE001_TO_TUNING_SPACE(c) (c)
    #define SE001_TO_RENDER_SPACE(c) (c)
#else
    #define SE001_TO_TUNING_SPACE(c) LinearToSRGB(c)
    #define SE001_TO_RENDER_SPACE(c) SRGBToLinear(c)
#endif

float3 SE001_ApplyJarTint(float3 texRgb, float3 colorRgb, float hue, float saturation, float brightness, float colorize)
{
    float3 tuned = SE001_TO_TUNING_SPACE(saturate(texRgb)) * SE001_TO_TUNING_SPACE(saturate(colorRgb));
    return SE001_TO_RENDER_SPACE(saturate(SE001_ApplyHsl(tuned, hue, saturation, brightness, colorize)));
}

#endif
