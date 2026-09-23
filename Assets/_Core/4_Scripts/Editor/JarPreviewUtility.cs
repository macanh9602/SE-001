#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SE001.Data;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor
{
    public enum JarPreviewKind
    {
        Source,
        Cup,
        Bowl
    }

    public static class JarPreviewUtility
    {
        private struct PreviewKey : IEquatable<PreviewKey>
        {
            public JarPreviewKind kind;
            public int colorId;
            public int width;
            public int height;
            public int fill;
            public bool pouring;
            public int profileHash;

            public bool Equals(PreviewKey other)
            {
                return kind == other.kind && colorId == other.colorId && width == other.width && height == other.height &&
                       fill == other.fill && pouring == other.pouring && profileHash == other.profileHash;
            }

            public override bool Equals(object obj) => obj is PreviewKey && Equals((PreviewKey)obj);
            public override int GetHashCode() =>
                ((((((int)kind * 397) ^ colorId) * 397 ^ width) * 397 ^ height) * 397 ^ fill) * 397 ^
                (pouring ? 1 : 0) ^ profileHash;
        }

        private static readonly Dictionary<PreviewKey, Texture2D> Cache = new Dictionary<PreviewKey, Texture2D>();
        private static int lastProfileHash;
        private static int buildCount;

        public static int BuildCount => buildCount;

        public static Texture2D GetPreview(JarPreviewKind kind, int colorId, Vector2 size,
            float fillRatio, bool pouring = false)
        {
            ColorProfile colors = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            JarVisualProfile visuals = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            BowlVisualProfile bowlVisuals = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            int profileHash = ComputeProfileHash(colors, visuals, bowlVisuals);
            if (profileHash != lastProfileHash)
            {
                ClearCache();
                lastProfileHash = profileHash;
            }

            int height = Mathf.Max(1, Mathf.RoundToInt(size.y * 100f));
            float sourceAspect = visuals != null
                ? visuals.sourceBodyWidthPixels / Mathf.Max(1f, visuals.sourceCompositeHeightPixels)
                : 156f / 212f;
            int width = kind == JarPreviewKind.Source
                ? Mathf.Max(1, Mathf.RoundToInt(height * sourceAspect))
                : Mathf.Max(1, Mathf.RoundToInt(size.x * 100f));
            if (kind == JarPreviewKind.Bowl && bowlVisuals != null)
                height = Mathf.Max(1, Mathf.RoundToInt(bowlVisuals.WorldHeightForWidth(size.x) * 100f));
            PreviewKey key = new PreviewKey
            {
                kind = kind,
                colorId = colorId,
                width = width,
                height = height,
                fill = Mathf.RoundToInt(Mathf.Clamp01(fillRatio) * 10000f),
                pouring = pouring,
                profileHash = profileHash
            };
            Texture2D cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            Texture2D preview = BuildPreview(kind, colorId, width, height, fillRatio, pouring, colors, visuals, bowlVisuals);
            Cache.Add(key, preview);
            buildCount++;
            return preview;
        }

        public static void ClearCache()
        {
            foreach (KeyValuePair<PreviewKey, Texture2D> pair in Cache)
                if (pair.Value != null) UnityEngine.Object.DestroyImmediate(pair.Value);
            Cache.Clear();
        }

        public static void GeneratePreviewGrid()
        {
            const int cellWidth = 380;
            const int cellHeight = 220;
            const int columns = 7;
            const int rows = 3;
            Texture2D grid = new Texture2D(cellWidth * columns, cellHeight * rows, TextureFormat.RGBA32, false, false);
            Color32[] clear = new Color32[grid.width * grid.height];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(38, 34, 86, 255);
            grid.SetPixels32(clear);
            grid.Apply(false, false);
            for (int colorId = 1; colorId <= columns; colorId++)
            {
                Texture2D source = GetPreview(JarPreviewKind.Source, colorId, new Vector2(0.9f, 1.8f), 0.65f);
                Texture2D cup = GetPreview(JarPreviewKind.Cup, colorId, new Vector2(1.5f, 1.8f), 0f);
                BowlVisualProfile bowlProfile = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
                Vector2 bowlSize = bowlProfile != null ? bowlProfile.WorldSize : new Vector2(1f, 1f);
                Texture2D bowl = GetPreview(JarPreviewKind.Bowl, colorId, bowlSize, 0f);
                CopyInto(grid, source, (colorId - 1) * cellWidth + (cellWidth - source.width) / 2, 20);
                CopyInto(grid, cup, (colorId - 1) * cellWidth + (cellWidth - cup.width) / 2, cellHeight + 20);
                CopyInto(grid, bowl, (colorId - 1) * cellWidth + (cellWidth - bowl.width) / 2, cellHeight * 2 + 20);
            }
            grid.Apply(false, false);
            string path = "handoff/phase-V1/evidence/JarPreviewGrid.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, grid.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(grid);
            AssetDatabase.Refresh();
            Debug.Log("[SE001] Jar preview grid written to " + path);
        }

        private static Texture2D BuildPreview(
            JarPreviewKind kind,
            int colorId,
            int width,
            int height,
            float fillRatio,
            bool pouring,
            ColorProfile colors,
            JarVisualProfile visuals,
            BowlVisualProfile bowlVisuals)
        {
            Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = "JarPreview_" + kind + "_" + colorId,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);

            ColorProfileEntry entry;
            if (colors == null || !colors.TryGetEntry(colorId, out entry))
            {
                output.SetPixels32(pixels);
                output.Apply(false, false);
                return output;
            }

            PhaseCVisualMaterials mats = AssetDatabase.LoadAssetAtPath<PhaseCVisualMaterials>("Assets/_Core/Resources/Profiles/PhaseCVisualMaterials.asset");
            if (kind == JarPreviewKind.Source)
            {
                Color32[] sourcePixels = new Color32[pixels.Length];
                for (int i = 0; i < sourcePixels.Length; i++) sourcePixels[i] = new Color32(0, 0, 0, 0);
                float compositeRatio = visuals != null
                    ? visuals.sourceCompositeHeightPixels / Mathf.Max(1f, visuals.sourceBodyHeightPixels)
                    : 212f / 192f;
                int bodyHeight = Mathf.Max(1, Mathf.RoundToInt(height / compositeRatio));
                int bodyY = height - bodyHeight;
                int mouthHeight = Mathf.Max(1, Mathf.RoundToInt(height * 28f / Mathf.Max(1f, visuals != null ? visuals.sourceCompositeHeightPixels : 212f)));
                int mouthWidth = Mathf.Max(1, Mathf.RoundToInt(width * 71f / Mathf.Max(1f, visuals != null ? visuals.sourceBodyWidthPixels : 156f)));
                int mouthX = (width - mouthWidth) / 2;
                int bodyDrawY = bodyY;

                // Shadow is deliberately subtle; the authored glass/fill layers remain the visible silhouette.
                DrawTexture(
                    sourcePixels, width, height,
                    visuals != null ? visuals.sourceSilhouetteMask : null,
                    0, 0, width, height, null,
                    new Color(0.25f, 0.25f, 0.25f, 0.22f), true);

                Material sourceBodyMaterial = mats != null ? mats.ResolveSourceBody() : null;
                Texture2D body = LoadTextureFromMaterial(sourceBodyMaterial);
                Texture2D fillMask = visuals != null ? visuals.sourceFillMask : null;
                float areaHeight = JarVisualGeometry.AreaToHeight(fillRatio, visuals != null ? visuals.sourceFillAreaLut : null);
                int fillHeight = Mathf.RoundToInt(bodyHeight * areaHeight);
                if (fillHeight > 0)
                    DrawTexture(
                        sourcePixels, width, height, fillMask, 0, bodyDrawY, width, bodyHeight, null,
                        ResolveSandColor(entry), true, int.MaxValue, bodyDrawY + bodyHeight - fillHeight);
                DrawTexture(sourcePixels, width, height, body, 0, bodyDrawY, width, bodyHeight, sourceBodyMaterial, Color.white, false);
                DrawTexture(
                    sourcePixels, width, height,
                    LoadTextureFromMaterial(entry.sourceMouthMaterial),
                    mouthX, 0, mouthWidth, mouthHeight,
                    entry.sourceMouthMaterial, Color.white, false);
                if (pouring) Array.Copy(sourcePixels, pixels, sourcePixels.Length);
                else RotatePixels180(sourcePixels, pixels, width, height);
            }
            else if (kind == JarPreviewKind.Cup)
            {
                Material cupBodyMaterial = mats != null ? mats.ResolveCupBody() : null;
                Texture2D body = LoadTextureFromMaterial(cupBodyMaterial);
                Texture2D cap = LoadTextureFromMaterial(entry.cupCapMaterial);
                float widthScale = width / Mathf.Max(1f, visuals != null ? visuals.cupHeadWidthPixels : 182f);
                int topCapHeight = Mathf.Max(1, Mathf.RoundToInt((visuals != null ? visuals.cupCapTopHeightPixels : 44f) * widthScale));
                int bottomCapHeight = Mathf.Max(1, Mathf.RoundToInt((visuals != null ? visuals.cupCapBottomHeightPixels : 49f) * widthScale));
                int bodyX = Mathf.RoundToInt((visuals != null ? visuals.cupBodyOffsetXPixels : 13f) * widthScale);
                int bodyWidth = Mathf.Max(1, Mathf.RoundToInt((visuals != null ? visuals.cupBodyWidthPixels : 156f) * widthScale));
                int bodyY = bottomCapHeight;
                int bodyHeight = Mathf.Max(1, height - topCapHeight - bottomCapHeight);
                DrawTexture(
                    pixels, width, height,
                    visuals != null ? visuals.cupSilhouetteMask : null,
                    0, 0, width, height, null,
                    new Color(0.25f, 0.25f, 0.25f, 0.22f), true);
                DrawTexture(pixels, width, height, body, bodyX, bodyY, bodyWidth, bodyHeight, cupBodyMaterial, Color.white, false);
                DrawTextureRegion(
                    pixels, width, height, cap, new RectInt(10, 173, 160, 44),
                    0, height - topCapHeight, width, topCapHeight,
                    entry.cupCapMaterial, Color.white, false);
                DrawTextureRegion(
                    pixels, width, height, cap, new RectInt(12, 0, 156, 49),
                    0, 0, width, bottomCapHeight,
                    entry.cupCapMaterial, Color.white, false);
            }
            else
            {
                Texture2D main = LoadTextureFromMaterial(entry.bowlMainMaterial);
                if (main == null && bowlVisuals != null) main = bowlVisuals.mainTexture;
                Texture2D shadow = bowlVisuals != null ? bowlVisuals.silhouetteMask : null;
                DrawTexture(pixels, width, height, shadow, 0, 0, width, height, null,
                    new Color(0.25f, 0.25f, 0.25f, 0.22f), true);
                DrawTexture(pixels, width, height, main, 0, 0, width, height,
                    entry.bowlMainMaterial, Color.white, false);
                if (bowlVisuals != null)
                {
                    Texture2D spec = bowlVisuals.specTexture;
                    int specWidth = Mathf.Max(1, Mathf.RoundToInt(width * bowlVisuals.specWidthPixels /
                        Mathf.Max(1f, bowlVisuals.mainWidthPixels)));
                    int specHeight = Mathf.Max(1, Mathf.RoundToInt(height * bowlVisuals.specHeightPixels /
                        Mathf.Max(1f, bowlVisuals.mainHeightPixels)));
                    int specX = Mathf.RoundToInt(width * bowlVisuals.specOffsetPixels.x /
                        Mathf.Max(1f, bowlVisuals.mainWidthPixels));
                    int specTop = Mathf.RoundToInt(height * bowlVisuals.specOffsetPixels.y /
                        Mathf.Max(1f, bowlVisuals.mainHeightPixels));
                    DrawTexture(pixels, width, height, spec, specX, height - specTop - specHeight,
                        specWidth, specHeight, AssetDatabase.LoadAssetAtPath<PhaseCVisualMaterials>(
                            "Assets/_Core/Resources/Profiles/PhaseCVisualMaterials.asset")?.bowlSpecMaterial,
                        Color.white, false);
                }
            }

            output.SetPixels32(pixels);
            output.Apply(false, false);
            return output;
        }

        private static Color ResolveSandColor(ColorProfileEntry entry)
        {
            return entry.sandColor;
        }

        private static void DrawTexture(
            Color32[] target,
            int targetWidth,
            int targetHeight,
            Texture2D texture,
            int x0,
            int y0,
            int drawWidth,
            int drawHeight,
            Material material,
            Color multiply,
            bool applyMultiply,
            int clipTop = int.MaxValue,
            int clipBottom = int.MinValue)
        {
            if (texture == null) return;
            Color32[] source = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            for (int y = 0; y < drawHeight; y++)
                for (int x = 0; x < drawWidth; x++)
                {
                    int tx = x0 + x;
                    int ty = y0 + y;
                    if (tx < 0 || tx >= targetWidth || ty < 0 || ty >= targetHeight) continue;
                    if (ty >= clipTop) continue;
                    if (ty < clipBottom) continue;
                    int sampleY = Mathf.Clamp(y * height / Mathf.Max(1, drawHeight), 0, height - 1);
                    int sampleX = Mathf.Clamp(x * width / Mathf.Max(1, drawWidth), 0, width - 1);
                    Color32 sample = source[sampleY * width + sampleX];
                    if (sample.a == 0) continue;
                    Color src = new Color(sample.r / 255f, sample.g / 255f, sample.b / 255f, sample.a / 255f);
                    if (material != null) src = ApplyMaterialTint(src, material);
                    if (applyMultiply) src = new Color(src.r * multiply.r, src.g * multiply.g, src.b * multiply.b, src.a * multiply.a);
                    BlendPixel(target, ty * targetWidth + tx, src);
                }
        }

        private static void RotatePixels180(Color32[] source, Color32[] target, int width, int height)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    target[(height - 1 - y) * width + (width - 1 - x)] = source[y * width + x];
        }

        private static void DrawTextureRegion(
            Color32[] target,
            int targetWidth,
            int targetHeight,
            Texture2D texture,
            RectInt sourceRect,
            int x0,
            int y0,
            int drawWidth,
            int drawHeight,
            Material material,
            Color multiply,
            bool applyMultiply)
        {
            if (texture == null) return;
            Color32[] source = texture.GetPixels32();
            int srcX0 = Mathf.Clamp(sourceRect.x, 0, texture.width - 1);
            int srcY0 = Mathf.Clamp(sourceRect.y, 0, texture.height - 1);
            int srcWidth = Mathf.Clamp(sourceRect.width, 1, texture.width - srcX0);
            int srcHeight = Mathf.Clamp(sourceRect.height, 1, texture.height - srcY0);
            for (int y = 0; y < drawHeight; y++)
                for (int x = 0; x < drawWidth; x++)
                {
                    int tx = x0 + x;
                    int ty = y0 + y;
                    if (tx < 0 || tx >= targetWidth || ty < 0 || ty >= targetHeight) continue;
                    int sx = srcX0 + Mathf.Clamp(x * srcWidth / Mathf.Max(1, drawWidth), 0, srcWidth - 1);
                    int sy = srcY0 + Mathf.Clamp(y * srcHeight / Mathf.Max(1, drawHeight), 0, srcHeight - 1);
                    Color32 sample = source[sy * texture.width + sx];
                    if (sample.a == 0) continue;
                    Color src = new Color(sample.r / 255f, sample.g / 255f, sample.b / 255f, sample.a / 255f);
                    if (material != null) src = ApplyMaterialTint(src, material);
                    if (applyMultiply) src = new Color(src.r * multiply.r, src.g * multiply.g, src.b * multiply.b, src.a * multiply.a);
                    BlendPixel(target, ty * targetWidth + tx, src);
                }
        }

        private static void BlendPixel(Color32[] target, int index, Color source)
        {
            float sourceAlpha = Mathf.Clamp01(source.a);
            if (sourceAlpha <= 0f) return;
            Color destination = target[index];
            float destinationAlpha = destination.a;
            float outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
            if (outputAlpha <= 0.0001f)
            {
                target[index] = new Color32(0, 0, 0, 0);
                return;
            }

            Color output = new Color(
                (source.r * sourceAlpha + destination.r * destinationAlpha * (1f - sourceAlpha)) / outputAlpha,
                (source.g * sourceAlpha + destination.g * destinationAlpha * (1f - sourceAlpha)) / outputAlpha,
                (source.b * sourceAlpha + destination.b * destinationAlpha * (1f - sourceAlpha)) / outputAlpha,
                outputAlpha);
            target[index] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(output.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(output.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(output.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(output.a) * 255f));
        }

        private static Color ApplyMaterialTint(Color texture, Material material)
        {
            Color tint = material.GetColor("_Color");
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            Color tuned = new Color(
                (linear ? ToTuningSpace(texture.r) : texture.r) * (linear ? ToTuningSpace(tint.r) : tint.r),
                (linear ? ToTuningSpace(texture.g) : texture.g) * (linear ? ToTuningSpace(tint.g) : tint.g),
                (linear ? ToTuningSpace(texture.b) : texture.b) * (linear ? ToTuningSpace(tint.b) : tint.b),
                texture.a * tint.a);
            Color result = HslToRgb(ApplyHsl(
                RgbToHsl(new Color(Mathf.Clamp01(tuned.r), Mathf.Clamp01(tuned.g), Mathf.Clamp01(tuned.b))),
                material.GetFloat("_Hue"),
                material.GetFloat("_Saturation"),
                material.GetFloat("_Brightness"),
                material.GetFloat("_Colorize")));
            if (linear)
                result = new Color(ToRenderSpace(result.r), ToRenderSpace(result.g), ToRenderSpace(result.b), result.a);
            result.a = texture.a * tint.a;
            return result;
        }

        public static Color EvaluateMaterialColor(Color texture, Material material)
        {
            return material == null ? texture : ApplyMaterialTint(texture, material);
        }

        private static float ToTuningSpace(float value)
        {
            value = Mathf.Clamp01(value);
            return value <= 0.0031308f ? value * 12.92f : 1.055f * Mathf.Pow(value, 1f / 2.4f) - 0.055f;
        }

        private static float ToRenderSpace(float value)
        {
            value = Mathf.Clamp01(value);
            return value <= 0.04045f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static Color RgbToHsl(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            float d = max - min;
            float h = 0f;
            float s = 0f;
            float l = (max + min) * 0.5f;
            if (d > 0.00001f)
            {
                s = d / Mathf.Max(0.00001f, 1f - Mathf.Abs(2f * l - 1f));
                if (Mathf.Approximately(max, c.r)) h = (c.g - c.b) / d + (c.g < c.b ? 6f : 0f);
                else if (Mathf.Approximately(max, c.g)) h = (c.b - c.r) / d + 2f;
                else h = (c.r - c.g) / d + 4f;
                h /= 6f;
            }
            return new Color(h, s, l, c.a);
        }

        private static Color ApplyHsl(Color hsl, float hue, float saturation, float brightness, float colorize)
        {
            float hRot = Mathf.Repeat(hsl.r + hue, 1f);
            float hAbs = Mathf.Repeat(hue, 1f);
            hsl.r = Mathf.Lerp(hRot, hAbs, colorize);
            hsl.g = Mathf.Lerp(Mathf.Clamp01(hsl.g * saturation), Mathf.Clamp01(saturation * 0.5f), colorize);
            hsl.b = Mathf.Clamp01(hsl.b * brightness);
            return hsl;
        }

        private static Color HslToRgb(Color hsl)
        {
            if (hsl.g <= 0.00001f) return new Color(hsl.b, hsl.b, hsl.b, 1f);
            float q = hsl.b < 0.5f ? hsl.b * (1f + hsl.g) : hsl.b + hsl.g - hsl.b * hsl.g;
            float p = 2f * hsl.b - q;
            return new Color(HueToRgb(p, q, hsl.r + 1f / 3f), HueToRgb(p, q, hsl.r), HueToRgb(p, q, hsl.r - 1f / 3f), 1f);
        }

        private static float HueToRgb(float p, float q, float t)
        {
            t = Mathf.Repeat(t, 1f);
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 0.5f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        private static Color32 ColorTo32(Color color)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f),
                255);
        }

        private static void ClearAbove(Color32[] pixels, int width, int height, float fillHeight)
        {
            int cutoff = Mathf.Clamp(Mathf.RoundToInt(fillHeight * height), 0, height);
            for (int y = cutoff; y < height; y++)
                for (int x = 0; x < width; x++) pixels[y * width + x].a = 0;
        }

        private static Texture2D LoadTexture(Texture2D texture) => texture;

        private static Texture2D LoadTextureFromMaterial(Material material)
        {
            return material != null ? material.GetTexture("_BaseMap") as Texture2D : null;
        }

        private static int ComputeProfileHash(
            ColorProfile colors,
            JarVisualProfile visuals,
            BowlVisualProfile bowlVisuals)
        {
            unchecked
            {
                int hash = 17;
                if (colors != null)
                    for (int i = 0; i < colors.entries.Count; i++)
                    {
                        ColorProfileEntry entry = colors.entries[i];
                        hash = hash * 31 + entry.colorId;
                        hash = hash * 31 + MaterialContentHash(entry.cupCapMaterial);
                        hash = hash * 31 + MaterialContentHash(entry.sourceMouthMaterial);
                        hash = hash * 31 + MaterialContentHash(entry.bowlMainMaterial);
                        hash = hash * 31 + entry.sandColor.GetHashCode();
                    }
                if (visuals != null)
                {
                    hash = hash * 31 + AssetContentHash(visuals.sourceFillMask);
                    hash = hash * 31 + AssetContentHash(visuals.sourceSilhouetteMask);
                    hash = hash * 31 + AssetContentHash(visuals.cupSilhouetteMask);
                    hash = hash * 31 + AssetContentHash(visuals.sparkleNoise);
                    hash = hash * 31 + visuals.sourceFillAreaLut.Length;
                    hash = hash * 31 + visuals.sourceFillInsetPixels;
                    hash = hash * 31 + visuals.sourceFillTopSkipPixels;
                    hash = hash * 31 + visuals.sourceFillBottomSkipPixels;
                    hash = hash * 31 + visuals.sourceBodyWidthPixels.GetHashCode();
                    hash = hash * 31 + visuals.sourceBodyHeightPixels.GetHashCode();
                    hash = hash * 31 + visuals.sourceCompositeHeightPixels.GetHashCode();
                }
                if (bowlVisuals != null)
                {
                    hash = hash * 31 + AssetContentHash(bowlVisuals.mainTexture);
                    hash = hash * 31 + AssetContentHash(bowlVisuals.specTexture);
                    hash = hash * 31 + AssetContentHash(bowlVisuals.silhouetteMask);
                    hash = hash * 31 + bowlVisuals.rimPixelY;
                    hash = hash * 31 + bowlVisuals.wallThicknessPixels.GetHashCode();
                    hash = hash * 31 + bowlVisuals.defaultWorldWidth.GetHashCode();
                }
                return hash;
            }
        }

        private static int MaterialContentHash(Material material)
        {
            if (material == null) return 0;
            unchecked
            {
                int hash = AssetContentHash(material);
                hash = hash * 31 + material.shader.GetInstanceID();
                hash = hash * 31 + material.GetColor("_Color").GetHashCode();
                hash = hash * 31 + material.GetFloat("_Hue").GetHashCode();
                hash = hash * 31 + material.GetFloat("_Saturation").GetHashCode();
                hash = hash * 31 + material.GetFloat("_Brightness").GetHashCode();
                hash = hash * 31 + material.GetFloat("_Colorize").GetHashCode();
                hash = hash * 31 + AssetContentHash(material.GetTexture("_BaseMap"));
                return hash;
            }
        }

        private static int AssetContentHash(UnityEngine.Object asset)
        {
            if (asset == null) return 0;
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? asset.GetInstanceID() : AssetDatabase.GetAssetDependencyHash(path).GetHashCode();
        }

        private static void CopyInto(Texture2D target, Texture2D source, int x0, int y0)
        {
            if (source == null) return;
            Color32[] src = source.GetPixels32();
            Color32[] dst = target.GetPixels32();
            for (int y = 0; y < source.height; y++)
                for (int x = 0; x < source.width; x++)
                {
                    int tx = x0 + x;
                    int ty = y0 + y;
                    if (tx < 0 || tx >= target.width || ty < 0 || ty >= target.height) continue;
                    Color32 sample = src[y * source.width + x];
                    if (sample.a == 0) continue;
                    int index = ty * target.width + tx;
                    float alpha = sample.a / 255f;
                    Color32 background = dst[index];
                    dst[index] = new Color32(
                        (byte)Mathf.RoundToInt(sample.r * alpha + background.r * (1f - alpha)),
                        (byte)Mathf.RoundToInt(sample.g * alpha + background.g * (1f - alpha)),
                        (byte)Mathf.RoundToInt(sample.b * alpha + background.b * (1f - alpha)),
                        255);
                }
            target.SetPixels32(dst);
        }
    }
}
#endif
