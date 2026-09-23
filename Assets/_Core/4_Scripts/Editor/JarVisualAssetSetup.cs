#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SE001.Data;
using SE001.Presentation;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor
{
    public static class JarVisualAssetSetup
    {
        private const string ProfileFolder = "Assets/_Core/Resources/Profiles";
        private const string MaskFolder = ProfileFolder + "/JarMasks";
        private const string MaterialFolder = "Assets/_Core/1_Materials/Jar";
        private const string MeshFolder = "Assets/_Core/3_Prefabs/Gameplay/JarVisualMeshes";
        private const string BowlProfilePath = ProfileFolder + "/BowlVisualProfile.asset";
        private const string BowlPrefabPath = "Assets/_Core/3_Prefabs/Gameplay/Bowl/Bowl.prefab";
        private const string TuningPath = "handoff/phase-V1/jar-tint-tuning.json";
        private const string EnvPath = "Assets/_Core/0_Texture2D/Env/";

        [Serializable]
        private sealed class TuningList
        {
            public List<TuningEntry> items = new List<TuningEntry>();
        }

        [Serializable]
        private sealed class TuningEntry
        {
            public int colorId;
            public string displayName;
            public string sandColor;
            public string capTint;
            public float colorize;
            public float hue;
            public float saturation;
            public float lightness;
        }

        public static void RebuildJarMaterials()
        {
            RebuildAll();
        }

        public static void BakeJarMasks()
        {
            JarVisualProfile profile = GetOrCreate<JarVisualProfile>(ProfileFolder + "/JarVisualProfile.asset");
            BowlVisualProfile bowlProfile = GetOrCreate<BowlVisualProfile>(BowlProfilePath);
            BakeProfile(profile);
            BakeBowlProfile(bowlProfile);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(bowlProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RebuildJarPrefabs()
        {
            JarVisualProfile profile = AssetDatabase.LoadAssetAtPath<JarVisualProfile>(ProfileFolder + "/JarVisualProfile.asset");
            BowlVisualProfile bowlProfile = AssetDatabase.LoadAssetAtPath<BowlVisualProfile>(BowlProfilePath);
            PhaseCVisualMaterials materials = AssetDatabase.LoadAssetAtPath<PhaseCVisualMaterials>(ProfileFolder + "/PhaseCVisualMaterials.asset");
            if (profile == null || bowlProfile == null || materials == null)
                throw new InvalidOperationException("Run the Jar Materials setup from the development tooling.");
            RebuildMeshes(profile, bowlProfile);
            RebuildPrefabs(profile, bowlProfile, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RebuildAll()
        {
            EnsureFolder(ProfileFolder);
            EnsureFolder(MaskFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder("Assets/_Core/3_Prefabs/Gameplay/Bowl");

            ConfigureTextureImporters();
            JarVisualProfile profile = GetOrCreate<JarVisualProfile>(ProfileFolder + "/JarVisualProfile.asset");
            BowlVisualProfile bowlProfile = GetOrCreate<BowlVisualProfile>(BowlProfilePath);
            BakeProfile(profile);
            BakeBowlProfile(bowlProfile);
            RebuildMeshes(profile, bowlProfile);

            PhaseCVisualMaterials materials = GetOrCreate<PhaseCVisualMaterials>(ProfileFolder + "/PhaseCVisualMaterials.asset");
            RebuildSharedMaterials(profile, bowlProfile, materials);
            RebuildColorProfile(materials);
            RebuildPrefabs(profile, bowlProfile, materials);
            BindRuntimeProfile(profile, bowlProfile, materials);

            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(bowlProfile);
            EditorUtility.SetDirty(materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SE001] Jar visual assets rebuilt: profile, masks, 14 color materials, meshes and prefabs.");
        }

        private static void ConfigureTextureImporters()
        {
            ConfigureTexture("T_Cup_Body.png", false);
            ConfigureTexture("T_Source_Body.png", false);
            ConfigureTexture("T_Source_Mounth.png", false);
            ConfigureTexture("T_Bowl_Main.png", false);
            ConfigureTexture("T_Bowl_Spec.png", false);

            string path = EnvPath + "T_Cup_Head.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritesheet = new[]
            {
                new SpriteMetaData { name = "CupCap_Top", rect = new Rect(10f, 173f, 160f, 44f), pivot = new Vector2(0.5f, 0.5f) },
                new SpriteMetaData { name = "CupCap_Bottom", rect = new Rect(12f, 0f, 156f, 49f), pivot = new Vector2(0.5f, 0.5f) }
            };
            importer.SaveAndReimport();
        }

        private static void ConfigureTexture(string fileName, bool multiple)
        {
            string path = EnvPath + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void BakeProfile(JarVisualProfile profile)
        {
            Texture2D sourceBody = LoadTexture("T_Source_Body.png");
            Texture2D sourceMouth = LoadTexture("T_Source_Mounth.png");
            Texture2D cupBody = LoadTexture("T_Cup_Body.png");
            Texture2D cupHead = LoadTexture("T_Cup_Head.png");

            Texture2D sourceFill = CreateOrUpdateMask(MaskFolder + "/SourceFillMask.asset", sourceBody.width, sourceBody.height);
            Texture2D sourceSilhouette = CreateOrUpdateMask(MaskFolder + "/SourceSilhouetteMask.asset", 156, 212);
            Texture2D cupSilhouette = CreateOrUpdateMask(MaskFolder + "/CupSilhouetteMask.asset", 182, 217);
            Texture2D noise = CreateOrUpdateNoise(MaskFolder + "/JarSparkleNoise.asset");

            Color32[] bodyPixels = sourceBody.GetPixels32();
            Color32[] fillPixels = new Color32[sourceBody.width * sourceBody.height];
            Vector2[] spans = new Vector2[sourceBody.height];
            float totalArea = 0f;
            for (int y = 0; y < sourceBody.height; y++)
            {
                int min = sourceBody.width;
                int max = -1;
                for (int x = 0; x < sourceBody.width; x++)
                {
                    if (bodyPixels[y * sourceBody.width + x].a < 20) continue;
                    min = Mathf.Min(min, x);
                    max = Mathf.Max(max, x);
                }
                if (max >= min && y >= profile.sourceFillBottomSkipPixels && y < sourceBody.height - profile.sourceFillTopSkipPixels)
                {
                    min += profile.sourceFillInsetPixels;
                    max -= profile.sourceFillInsetPixels;
                    if (max >= min)
                    {
                        spans[y] = new Vector2(min, max);
                        for (int x = min; x <= max; x++) fillPixels[y * sourceBody.width + x] = new Color32(255, 255, 255, 255);
                        totalArea += max - min + 1;
                    }
                }
                else spans[y] = new Vector2(1f, 0f);
            }
            sourceFill.SetPixels32(fillPixels);
            sourceFill.Apply(false, false);

            float[] lut = new float[sourceBody.height + 1];
            float running = 0f;
            lut[0] = 0f;
            for (int y = 0; y < sourceBody.height; y++)
            {
                Vector2 span = spans[y];
                if (span.y >= span.x) running += span.y - span.x + 1f;
                lut[y + 1] = totalArea > 0f ? running / totalArea : (y + 1f) / sourceBody.height;
            }
            for (int i = 1; i < lut.Length; i++) lut[i] = Mathf.Max(lut[i], lut[i - 1]);

            Color32[] sourceSilhouettePixels = new Color32[156 * 212];
            CopyAlpha(sourceBody.GetPixels32(), sourceBody.width, sourceBody.height, sourceSilhouettePixels, 156, 212, 0, 20);
            CopyAlpha(sourceMouth.GetPixels32(), sourceMouth.width, sourceMouth.height, sourceSilhouettePixels, 156, 212, 42, 0);
            sourceSilhouette.SetPixels32(sourceSilhouettePixels);
            sourceSilhouette.Apply(false, false);

            Color32[] cupSilhouettePixels = new Color32[182 * 217];
            CopyAlpha(cupHead.GetPixels32(), cupHead.width, cupHead.height, cupSilhouettePixels, 182, 217, 0, 0);
            CopyAlpha(cupBody.GetPixels32(), cupBody.width, cupBody.height, cupSilhouettePixels, 182, 217, 13, 46);
            cupSilhouette.SetPixels32(cupSilhouettePixels);
            cupSilhouette.Apply(false, false);

            profile.sourceFillMask = sourceFill;
            profile.sourceSilhouetteMask = sourceSilhouette;
            profile.cupSilhouetteMask = cupSilhouette;
            profile.sparkleNoise = noise;
            profile.sourceFillAreaLut = lut;
            profile.sourceFillRowSpans = spans;
        }

        private static void BakeBowlProfile(BowlVisualProfile profile)
        {
            Texture2D main = LoadTexture("T_Bowl_Main.png");
            Texture2D spec = LoadTexture("T_Bowl_Spec.png");
            const int alphaThreshold = 64;
            profile.mainTexture = main;
            profile.specTexture = spec;
            profile.mainWidthPixels = main.width;
            profile.mainHeightPixels = main.height;
            profile.specWidthPixels = spec.width;
            profile.specHeightPixels = spec.height;
            profile.alphaThreshold = alphaThreshold;
            profile.wallThicknessPixels = Mathf.Max(1f, main.width * 0.03f);
            profile.rimPixelY = DetectBowlRim(main, alphaThreshold, profile.wallThicknessPixels);
            // Keep the GD-tuned width on re-bake; only seed it the first time.
            if (profile.defaultWorldWidth <= 0f) profile.defaultWorldWidth = main.width * 0.01f;
            profile.specOffsetPixels = new Vector2(
                (main.width - spec.width) * 0.5f,
                (main.height - spec.height) * 0.5f);

            Color32[] source = main.GetPixels32();
            BowlRowSpan[] outer = new BowlRowSpan[main.height];
            BowlRowSpan[] inner = new BowlRowSpan[main.height];
            Color32[] maskPixels = new Color32[source.Length];
            for (int y = 0; y < main.height; y++)
            {
                int min = main.width;
                int max = -1;
                for (int x = 0; x < main.width; x++)
                {
                    if (source[y * main.width + x].a < alphaThreshold) continue;
                    min = Mathf.Min(min, x);
                    max = Mathf.Max(max, x);
                }

                if (max < min)
                {
                    outer[y] = new BowlRowSpan(1f, 0f);
                    inner[y] = new BowlRowSpan(1f, 0f);
                    continue;
                }

                outer[y] = new BowlRowSpan(min, max);
                for (int x = min; x <= max; x++)
                    maskPixels[y * main.width + x] = new Color32(255, 255, 255, source[y * main.width + x].a);
            }

            ErodeBowlInterior(outer, inner, main.width, profile.wallThicknessPixels);

            Texture2D mask = CreateOrUpdateMask(MaskFolder + "/BowlSilhouetteMask.asset", main.width, main.height);
            mask.SetPixels32(maskPixels);
            mask.Apply(false, false);
            profile.outerRowSpans = outer;
            profile.innerRowSpans = inner;
            profile.outerContour = BuildContour(outer, main.width, main.height);
            profile.innerContour = BuildContour(inner, main.width, main.height);
            profile.silhouetteMask = mask;
        }

        /// <summary>
        /// Inner (sand-free) spans = the filled outer silhouette eroded by a disk of the glass wall thickness.
        /// Erosion works in every direction, so the curved bottom gets a real floor (a horizontal-only inset left
        /// the bottom rows open and sand fell straight through the Bowl). Rows above the image top count as open
        /// sky so the mouth stays open.
        /// </summary>
        public static void ErodeBowlInterior(BowlRowSpan[] outer, BowlRowSpan[] inner, int width, float radius)
        {
            int height = outer.Length;
            int r = Mathf.Max(1, Mathf.CeilToInt(radius));
            float radiusSquared = radius * radius;
            for (int y = 0; y < height; y++)
            {
                int min = width;
                int max = -1;
                BowlRowSpan row = outer[y];
                if (row.IsValid)
                {
                    for (int x = Mathf.CeilToInt(row.minX); x <= Mathf.FloorToInt(row.maxX); x++)
                    {
                        if (!DiskInside(outer, x, y, r, radiusSquared)) continue;
                        min = Mathf.Min(min, x);
                        max = Mathf.Max(max, x);
                    }
                }

                inner[y] = max >= min ? new BowlRowSpan(min, max) : new BowlRowSpan(1f, 0f);
            }
        }

        private static bool DiskInside(BowlRowSpan[] outer, int x, int y, int r, float radiusSquared)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                float remaining = radiusSquared - dy * dy;
                if (remaining < 0f) continue;
                int row = y + dy;
                if (row >= outer.Length) continue;
                if (row < 0) return false;
                BowlRowSpan span = outer[row];
                if (!span.IsValid) return false;
                float halfWidth = Mathf.Sqrt(remaining);
                if (x - halfWidth < span.minX || x + halfWidth > span.maxX) return false;
            }

            return true;
        }

        /// <summary>
        /// Sand opening = just under the lip: the widest row (lip) minus one glass-wall thickness, bottom-up pixels.
        /// The old "width drops 4 %" rule landed ~10 % below the visual rim, so sand rested inside the drawn glass.
        /// </summary>
        public static int DetectBowlRim(Texture2D texture, int alphaThreshold, float wallThicknessPixels)
        {
            Color32[] pixels = texture.GetPixels32();
            int widest = 0;
            int widestTopRow = 0;
            for (int topY = 0; topY < texture.height; topY++)
            {
                int width = RowWidth(pixels, texture.width, texture.height - 1 - topY, alphaThreshold);
                if (width <= widest) continue;
                widest = width;
                widestTopRow = topY;
            }

            int lipBottomUp = texture.height - 1 - widestTopRow;
            return Mathf.Clamp(lipBottomUp - Mathf.RoundToInt(wallThicknessPixels), 0, texture.height - 1);
        }

        private static int RowWidth(Color32[] pixels, int width, int y, int alphaThreshold)
        {
            int min = width;
            int max = -1;
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < alphaThreshold) continue;
                min = Mathf.Min(min, x);
                max = Mathf.Max(max, x);
            }
            return max >= min ? max - min + 1 : 0;
        }

        private static Vector2[] BuildContour(BowlRowSpan[] rows, int width, int height)
        {
            List<Vector2> left = new List<Vector2>();
            List<Vector2> right = new List<Vector2>();
            int stride = Mathf.Max(1, Mathf.CeilToInt(height / 19f));
            for (int y = 0; y < height; y += stride)
            {
                BowlRowSpan span = rows[y];
                if (!span.IsValid) continue;
                left.Add(new Vector2(span.minX / Mathf.Max(1f, width), y / Mathf.Max(1f, height - 1f)));
                right.Add(new Vector2(span.maxX / Mathf.Max(1f, width), y / Mathf.Max(1f, height - 1f)));
            }
            BowlRowSpan last = rows[height - 1];
            if (last.IsValid)
            {
                left.Add(new Vector2(last.minX / (float)width, (height - 1f) / Mathf.Max(1f, height - 1f)));
                right.Add(new Vector2(last.maxX / (float)width, (height - 1f) / Mathf.Max(1f, height - 1f)));
            }
            right.Reverse();
            left.AddRange(right);
            return left.ToArray();
        }

        private static void CopyAlpha(
            Color32[] source,
            int sourceWidth,
            int sourceHeight,
            Color32[] target,
            int targetWidth,
            int targetHeight,
            int offsetX,
            int offsetY)
        {
            int width = Mathf.Min(sourceWidth, targetWidth - offsetX);
            int height = Mathf.Min(sourceHeight, targetHeight - offsetY);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Color32 value = source[y * sourceWidth + x];
                    if (value.a == 0) continue;
                    int index = (y + offsetY) * targetWidth + x + offsetX;
                    target[index] = new Color32(255, 255, 255, value.a);
                }
        }

        private static void RebuildSharedMaterials(
            JarVisualProfile profile,
            BowlVisualProfile bowlProfile,
            PhaseCVisualMaterials visualMaterials)
        {
            Shader tint = LoadShader("Assets/_Core/5_Shaders/SE001_JarTint.shader");
            Shader fill = LoadShader("Assets/_Core/5_Shaders/SE001_JarSandFill.shader");
            Shader shadow = LoadShader("Assets/_Core/5_Shaders/SE001_JarShadow.shader");
            Texture2D sourceBody = LoadTexture("T_Source_Body.png");
            Texture2D cupBody = LoadTexture("T_Cup_Body.png");
            Texture2D bowlSpec = LoadTexture("T_Bowl_Spec.png");

            Material sourceBodyMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_SourceBody.mat", tint);
            SetTintMaterial(sourceBodyMaterial, tint, sourceBody, Color.white, 0f, 1f, 1f, 0f, 3002);
            Material cupBodyMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_CupBody.mat", tint);
            SetTintMaterial(cupBodyMaterial, tint, cupBody, Color.white, 0f, 1f, 1f, 0f, 3002);
            Material sourceFillMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_SourceFill.mat", fill);
            sourceFillMaterial.shader = fill;
            sourceFillMaterial.SetTexture("_MaskTex", profile.sourceFillMask);
            sourceFillMaterial.SetTexture("_NoiseTex", profile.sparkleNoise);
            sourceFillMaterial.renderQueue = 3001;
            Material sourceShadowMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_SourceShadow.mat", shadow);
            SetShadowMaterial(sourceShadowMaterial, shadow, profile.sourceSilhouetteMask, profile.shadowMultiplier);
            Material cupShadowMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_CupShadow.mat", shadow);
            SetShadowMaterial(cupShadowMaterial, shadow, profile.cupSilhouetteMask, profile.shadowMultiplier);
            Material bowlSpecMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_BowlSpec.mat", tint);
            SetTintMaterial(bowlSpecMaterial, tint, bowlSpec, Color.white, 0f, 1f, 1f, 0f, 3004);
            Material bowlShadowMaterial = GetOrCreateMaterial(MaterialFolder + "/MAT_Jar_BowlShadow.mat", shadow);
            SetShadowMaterial(bowlShadowMaterial, shadow, bowlProfile.silhouetteMask, profile.shadowMultiplier);

            visualMaterials.sourceBodyMaterial = sourceBodyMaterial;
            visualMaterials.cupBodyMaterial = cupBodyMaterial;
            visualMaterials.sourceFillMaterial = sourceFillMaterial;
            visualMaterials.sourceShadowMaterial = sourceShadowMaterial;
            visualMaterials.cupShadowMaterial = cupShadowMaterial;
            visualMaterials.bowlSpecMaterial = bowlSpecMaterial;
            visualMaterials.bowlShadowMaterial = bowlShadowMaterial;
            visualMaterials.sourceMaterial = sourceBodyMaterial;
            visualMaterials.cupMaterial = cupBodyMaterial;
            visualMaterials.cupBackMaterial = cupBodyMaterial;
            visualMaterials.drawPathMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Core/1_Materials/MAT_DrawPath.mat");
        }

        private static void RebuildColorProfile(PhaseCVisualMaterials materials)
        {
            string json = File.ReadAllText(TuningPath);
            TuningList tuning = JsonUtility.FromJson<TuningList>("{\"items\":" + json + "}");
            ColorProfile profile = GetOrCreate<ColorProfile>(ProfileFolder + "/PhaseCColorProfile.asset");
            profile.entries = new List<ColorProfileEntry>(tuning.items.Count);
            for (int i = 0; i < tuning.items.Count; i++)
            {
                TuningEntry item = tuning.items[i];
                Color32 sand = ParseHex(item.sandColor);
                Material cap = GetOrCreateMaterial(
                    MaterialFolder + "/MAT_CupCap_" + item.displayName + ".mat",
                    LoadShader("Assets/_Core/5_Shaders/SE001_JarTint.shader"));
                SetTintMaterial(
                    cap, cap.shader, LoadTexture("T_Cup_Head.png"), ParseHex(item.capTint),
                    item.hue, item.saturation, item.lightness, item.colorize, 3003);
                Material mouth = GetOrCreateMaterial(MaterialFolder + "/MAT_SourceMouth_" + item.displayName + ".mat", cap.shader);
                SetTintMaterial(
                    mouth, cap.shader, LoadTexture("T_Source_Mounth.png"), ParseHex(item.capTint),
                    item.hue, item.saturation, item.lightness, item.colorize, 3003);
                Material bowl = GetOrCreateMaterial(MaterialFolder + "/MAT_BowlMain_" + item.displayName + ".mat", cap.shader);
                SetTintMaterial(
                    bowl, cap.shader, LoadTexture("T_Bowl_Main.png"), ParseHex(item.capTint),
                    item.hue, item.saturation, item.lightness, item.colorize, 3002);
                profile.entries.Add(new ColorProfileEntry
                {
                    colorId = item.colorId,
                    sandColor = sand,
                    uiColor = sand,
                    displayName = item.displayName,
                    cupCapMaterial = cap,
                    sourceMouthMaterial = mouth,
                    bowlMainMaterial = bowl
                });
            }
            EditorUtility.SetDirty(profile);
        }

        private static void BindRuntimeProfile(
            JarVisualProfile profile,
            BowlVisualProfile bowlProfile,
            PhaseCVisualMaterials materials)
        {
            GameplayRuntimeProfile runtime = AssetDatabase.LoadAssetAtPath<GameplayRuntimeProfile>(ProfileFolder + "/PhaseCGameplayRuntimeProfile.asset");
            if (runtime == null) return;
            runtime.jarVisualProfile = profile;
            runtime.bowlVisualProfile = bowlProfile;
            runtime.receiverStyle = ReceiverStyle.Bowl;
            runtime.visualMaterials = materials;
            if (runtime.prefabProfile != null)
            {
                runtime.prefabProfile.bowlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BowlPrefabPath);
                EditorUtility.SetDirty(runtime.prefabProfile);
            }
            if (runtime.cupProfile != null)
            {
                runtime.cupProfile.taper = 0f;
                EditorUtility.SetDirty(runtime.cupProfile);
            }
            EditorUtility.SetDirty(runtime);
        }

        private static void RebuildMeshes(JarVisualProfile profile, BowlVisualProfile bowlProfile)
        {
            CreateQuadMesh(MeshFolder + "/SourceBody.asset", 1.56f, 1.92f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/SourceMouth.asset", 0.71f, 0.28f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/SourceSilhouette.asset", 1.56f, 2.12f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/CupBody.asset", 1.56f, 1.30f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/CupCapTop.asset", 1.60f, 0.44f, new Rect(10f / 182f, 173f / 217f, 160f / 182f, 44f / 217f));
            CreateQuadMesh(MeshFolder + "/CupCapBottom.asset", 1.56f, 0.49f, new Rect(12f / 182f, 0f, 156f / 182f, 49f / 217f));
            CreateQuadMesh(MeshFolder + "/CupSilhouette.asset", 1.82f, 2.17f, new Rect(0f, 0f, 1f, 1f));
            float bowlWidth = bowlProfile != null ? bowlProfile.defaultWorldWidth : 1f;
            float bowlHeight = bowlProfile != null ? bowlProfile.WorldHeightForWidth(bowlWidth) : 1f;
            float specWidth = bowlProfile != null
                ? bowlWidth * bowlProfile.specWidthPixels / Mathf.Max(1f, bowlProfile.mainWidthPixels)
                : bowlWidth;
            float specHeight = bowlProfile != null
                ? bowlHeight * bowlProfile.specHeightPixels / Mathf.Max(1f, bowlProfile.mainHeightPixels)
                : bowlHeight;
            CreateQuadMesh(MeshFolder + "/BowlMain.asset", bowlWidth, bowlHeight, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/BowlSpec.asset", specWidth, specHeight, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/BowlSilhouette.asset", bowlWidth, bowlHeight, new Rect(0f, 0f, 1f, 1f));
        }

        private static void RebuildPrefabs(
            JarVisualProfile profile,
            BowlVisualProfile bowlProfile,
            PhaseCVisualMaterials materials)
        {
            Material sourceBody = materials.ResolveSourceBody();
            Material cupBody = materials.ResolveCupBody();
            Material sourceFill = materials.sourceFillMaterial;
            Material sourceShadow = materials.sourceShadowMaterial;
            Material cupShadow = materials.cupShadowMaterial;
            Mesh sourceBodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SourceBody.asset");
            Mesh sourceMouthMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SourceMouth.asset");
            Mesh sourceShadowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/SourceSilhouette.asset");
            Mesh cupBodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/CupBody.asset");
            Mesh capTopMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/CupCapTop.asset");
            Mesh capBottomMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/CupCapBottom.asset");
            Mesh cupShadowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/CupSilhouette.asset");
            Mesh bowlMainMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/BowlMain.asset");
            Mesh bowlSpecMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/BowlSpec.asset");
            Mesh bowlShadowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshFolder + "/BowlSilhouette.asset");

            BuildSourcePrefab(
                "Assets/_Core/3_Prefabs/Gameplay/Source/SandSource.prefab",
                sourceBodyMesh, sourceMouthMesh, sourceShadowMesh,
                sourceBody, sourceFill, sourceShadow);
            BuildCupPrefab("Assets/_Core/3_Prefabs/Gameplay/Cup/Cup.prefab", cupBodyMesh, capTopMesh, capBottomMesh, cupShadowMesh, cupBody, cupShadow);
            BuildBowlPrefab(BowlPrefabPath, bowlMainMesh, bowlSpecMesh, bowlShadowMesh,
                materials.bowlSpecMaterial, materials.bowlShadowMaterial);
        }

        private static void BuildSourcePrefab(
            string path,
            Mesh bodyMesh,
            Mesh mouthMesh,
            Mesh shadowMesh,
            Material bodyMaterial,
            Material fillMaterial,
            Material shadowMaterial)
        {
            GameObject root = LoadOrCreatePrefabRoot(path, "SandSource");
            RemoveAllChildren(root.transform);
            if (root.GetComponent<PhaseCSourceVisual>() == null) root.AddComponent<PhaseCSourceVisual>();
            Transform pivot = CreateChild(root.transform, "Pivot");
            Transform view = CreateChild(pivot, "View");
            AddRenderer(CreateChild(view, "Shadow"), shadowMesh, shadowMaterial);
            AddRenderer(CreateChild(view, "SandFill"), bodyMesh, fillMaterial);
            AddRenderer(CreateChild(view, "Body"), bodyMesh, bodyMaterial);
            AddRenderer(CreateChild(view, "Mouth"), mouthMesh, null);
            Transform anchors = CreateChild(root.transform, "Anchors");
            CreateChild(anchors, "EmitPoint");
            SavePrefabRoot(root, path);
        }

        private static void BuildCupPrefab(
            string path,
            Mesh bodyMesh,
            Mesh capTopMesh,
            Mesh capBottomMesh,
            Mesh shadowMesh,
            Material bodyMaterial,
            Material shadowMaterial)
        {
            GameObject root = LoadOrCreatePrefabRoot(path, "Cup");
            RemoveAllChildren(root.transform);
            if (root.GetComponent<PhaseCCupVisual>() == null) root.AddComponent<PhaseCCupVisual>();
            Transform view = CreateChild(root.transform, "View");
            AddRenderer(CreateChild(view, "Shadow"), shadowMesh, shadowMaterial);
            AddRenderer(CreateChild(view, "Body"), bodyMesh, bodyMaterial);
            AddRenderer(CreateChild(view, "CapTop"), capTopMesh, null);
            AddRenderer(CreateChild(view, "CapBottom"), capBottomMesh, null);
            Transform anchors = CreateChild(root.transform, "Anchors");
            CreateChild(anchors, "Entry");
            CreateChild(anchors, "Feedback");
            SavePrefabRoot(root, path);
        }

        private static void BuildBowlPrefab(
            string path,
            Mesh mainMesh,
            Mesh specMesh,
            Mesh shadowMesh,
            Material specMaterial,
            Material shadowMaterial)
        {
            GameObject root = LoadOrCreatePrefabRoot(path, "Bowl");
            RemoveAllChildren(root.transform);
            if (root.GetComponent<BowlVisual>() == null) root.AddComponent<BowlVisual>();
            Transform view = CreateChild(root.transform, "View");
            AddRenderer(CreateChild(view, "Shadow"), shadowMesh, shadowMaterial);
            AddRenderer(CreateChild(view, "Main"), mainMesh, null);
            AddRenderer(CreateChild(view, "Spec"), specMesh, specMaterial);
            Transform anchors = CreateChild(root.transform, "Anchors");
            CreateChild(anchors, "Entry");
            CreateChild(anchors, "Feedback");
            SavePrefabRoot(root, path);
        }

        private static GameObject LoadOrCreatePrefabRoot(string path, string name)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return PrefabUtility.LoadPrefabContents(path);
            GameObject root = new GameObject(name);
            return root;
        }

        private static void SavePrefabRoot(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            bool isPrefabContents = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            if (isPrefabContents) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }

        private static void RemoveAllChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void AddRenderer(Transform target, Mesh mesh, Material material)
        {
            MeshFilter filter = target.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = target.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private static Texture2D CreateOrUpdateMask(string path, int width, int height)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null && !texture.isReadable)
            {
                AssetDatabase.DeleteAsset(path);
                texture = null;
            }
            if (texture == null)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true) { name = Path.GetFileNameWithoutExtension(path) };
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.CreateAsset(texture, path);
            }
            return texture;
        }

        private static Texture2D CreateOrUpdateNoise(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(32, 32, TextureFormat.R8, false, true) { name = "JarSparkleNoise" };
                Color32[] pixels = new Color32[32 * 32];
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte value = (byte)((i * 37 + 17) % 256);
                    pixels[i] = new Color32(value, value, value, 255);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
                AssetDatabase.CreateAsset(texture, path);
            }
            return texture;
        }

        private static Mesh CreateQuadMesh(string path, float width, float height, Rect uv)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-width * 0.5f, -height * 0.5f, 0f), new Vector3(width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(-width * 0.5f, height * 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin),
                new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetTintMaterial(
            Material material,
            Shader shader,
            Texture2D texture,
            Color color,
            float hue,
            float saturation,
            float brightness,
            float colorize,
            int renderQueue)
        {
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_Color", color);
            material.SetFloat("_Hue", hue);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Brightness", brightness);
            material.SetFloat("_Colorize", colorize);
            material.renderQueue = renderQueue;
            EditorUtility.SetDirty(material);
        }

        private static void SetShadowMaterial(Material material, Shader shader, Texture2D mask, float multiplier)
        {
            material.shader = shader;
            material.SetTexture("_MaskTex", mask);
            material.SetFloat("_ShadowMul", multiplier);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
        }

        private static Shader LoadShader(string path)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) throw new InvalidOperationException("Shader asset is missing: " + path);
            return shader;
        }

        private static Texture2D LoadTexture(string name)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(EnvPath + name);
            if (texture == null) throw new InvalidOperationException("Texture asset is missing: " + name);
            return texture;
        }

        private static Color32 ParseHex(string value)
        {
            if (string.IsNullOrEmpty(value)) return Color.white;
            string hex = value.Trim().TrimStart('#');
            if (hex.Length == 6) hex += "FF";
            return new Color32(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
