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
            BakeProfile(profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RebuildJarPrefabs()
        {
            JarVisualProfile profile = AssetDatabase.LoadAssetAtPath<JarVisualProfile>(ProfileFolder + "/JarVisualProfile.asset");
            PhaseCVisualMaterials materials = AssetDatabase.LoadAssetAtPath<PhaseCVisualMaterials>(ProfileFolder + "/PhaseCVisualMaterials.asset");
            if (profile == null || materials == null)
                throw new InvalidOperationException("Run the Jar Materials setup from the development tooling.");
            RebuildMeshes(profile);
            RebuildPrefabs(profile, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void RebuildAll()
        {
            EnsureFolder(ProfileFolder);
            EnsureFolder(MaskFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);

            ConfigureTextureImporters();
            JarVisualProfile profile = GetOrCreate<JarVisualProfile>(ProfileFolder + "/JarVisualProfile.asset");
            BakeProfile(profile);
            RebuildMeshes(profile);

            PhaseCVisualMaterials materials = GetOrCreate<PhaseCVisualMaterials>(ProfileFolder + "/PhaseCVisualMaterials.asset");
            RebuildSharedMaterials(profile, materials);
            RebuildColorProfile(materials);
            RebuildPrefabs(profile, materials);
            BindRuntimeProfile(profile, materials);

            EditorUtility.SetDirty(profile);
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

        private static void RebuildSharedMaterials(JarVisualProfile profile, PhaseCVisualMaterials visualMaterials)
        {
            Shader tint = LoadShader("Assets/_Core/5_Shaders/SE001_JarTint.shader");
            Shader fill = LoadShader("Assets/_Core/5_Shaders/SE001_JarSandFill.shader");
            Shader shadow = LoadShader("Assets/_Core/5_Shaders/SE001_JarShadow.shader");
            Texture2D sourceBody = LoadTexture("T_Source_Body.png");
            Texture2D cupBody = LoadTexture("T_Cup_Body.png");

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

            visualMaterials.sourceBodyMaterial = sourceBodyMaterial;
            visualMaterials.cupBodyMaterial = cupBodyMaterial;
            visualMaterials.sourceFillMaterial = sourceFillMaterial;
            visualMaterials.sourceShadowMaterial = sourceShadowMaterial;
            visualMaterials.cupShadowMaterial = cupShadowMaterial;
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
                profile.entries.Add(new ColorProfileEntry
                {
                    colorId = item.colorId,
                    sandColor = sand,
                    uiColor = sand,
                    displayName = item.displayName,
                    cupCapMaterial = cap,
                    sourceMouthMaterial = mouth
                });
            }
            EditorUtility.SetDirty(profile);
        }

        private static void BindRuntimeProfile(JarVisualProfile profile, PhaseCVisualMaterials materials)
        {
            GameplayRuntimeProfile runtime = AssetDatabase.LoadAssetAtPath<GameplayRuntimeProfile>(ProfileFolder + "/PhaseCGameplayRuntimeProfile.asset");
            if (runtime == null) return;
            runtime.jarVisualProfile = profile;
            runtime.visualMaterials = materials;
            if (runtime.cupProfile != null)
            {
                runtime.cupProfile.taper = 0f;
                EditorUtility.SetDirty(runtime.cupProfile);
            }
            EditorUtility.SetDirty(runtime);
        }

        private static void RebuildMeshes(JarVisualProfile profile)
        {
            CreateQuadMesh(MeshFolder + "/SourceBody.asset", 1.56f, 1.92f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/SourceMouth.asset", 0.71f, 0.28f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/SourceSilhouette.asset", 1.56f, 2.12f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/CupBody.asset", 1.56f, 1.30f, new Rect(0f, 0f, 1f, 1f));
            CreateQuadMesh(MeshFolder + "/CupCapTop.asset", 1.60f, 0.44f, new Rect(10f / 182f, 173f / 217f, 160f / 182f, 44f / 217f));
            CreateQuadMesh(MeshFolder + "/CupCapBottom.asset", 1.56f, 0.49f, new Rect(12f / 182f, 0f, 156f / 182f, 49f / 217f));
            CreateQuadMesh(MeshFolder + "/CupSilhouette.asset", 1.82f, 2.17f, new Rect(0f, 0f, 1f, 1f));
        }

        private static void RebuildPrefabs(JarVisualProfile profile, PhaseCVisualMaterials materials)
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

            BuildSourcePrefab(
                "Assets/_Core/3_Prefabs/Gameplay/Source/SandSource.prefab",
                sourceBodyMesh, sourceMouthMesh, sourceShadowMesh,
                sourceBody, sourceFill, sourceShadow);
            BuildCupPrefab("Assets/_Core/3_Prefabs/Gameplay/Cup/Cup.prefab", cupBodyMesh, capTopMesh, capBottomMesh, cupShadowMesh, cupBody, cupShadow);
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
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
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
