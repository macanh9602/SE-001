#if UNITY_EDITOR
using System.Collections.Generic;
using SE001.Data;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;
using SE001.Presentation;
using System.IO;

namespace SE001.Editor
{
    public static class PhaseCAssetSetup
    {
        [MenuItem("SE001/Phase C/Create default profiles")]
        private static void CreateDefaults()
        {
            string folder = "Assets/_Core/Resources/Profiles";
            MaterialPalette palette = GetOrCreate<MaterialPalette>(folder + "/PhaseCMaterialPalette.asset");
            palette.entries = new List<MaterialPaletteEntry>
            {
                new MaterialPaletteEntry { materialId = 1, sandColor = new Color32(248, 208, 64, 255), uiColor = new Color32(248, 208, 64, 255) },
                new MaterialPaletteEntry { materialId = 2, sandColor = new Color32(226, 62, 52, 255), uiColor = new Color32(226, 62, 52, 255) },
                new MaterialPaletteEntry { materialId = 3, sandColor = new Color32(72, 164, 232, 255), uiColor = new Color32(72, 164, 232, 255) }
            };
            SourceProfile source = GetOrCreate<SourceProfile>(folder + "/PhaseCSourceProfile.asset");
            CupProfile cup = GetOrCreate<CupProfile>(folder + "/PhaseCCupProfile.asset");
            DrawPathProfile draw = GetOrCreate<DrawPathProfile>(folder + "/PhaseCDrawPathProfile.asset");
            JuiceProfile juice = GetOrCreate<JuiceProfile>(folder + "/PhaseCJuiceProfile.asset");
            GameplayRuntimeProfile runtime = GetOrCreate<GameplayRuntimeProfile>(folder + "/PhaseCGameplayRuntimeProfile.asset");
            runtime.materialPalette = palette;
            runtime.sourceProfile = source;
            runtime.cupProfile = cup;
            runtime.drawPathProfile = draw;
            runtime.juiceProfile = juice;
            runtime.sandProfile = AssetDatabase.LoadAssetAtPath<SandSimulationProfile>(folder + "/PhaseBSandSimulationProfile.asset");
            runtime.prefabProfile = AssetDatabase.LoadAssetAtPath<PrefabProfile>(folder + "/PhaseBPrefabProfile.asset");
            runtime.visualMaterials = AssetDatabase.LoadAssetAtPath<PhaseCVisualMaterials>(folder + "/PhaseCVisualMaterials.asset");
            if (runtime.prefabProfile != null)
            {
                runtime.prefabProfile.sourcePrefab = GetOrCreateVisualPrefab(
                    "Assets/_Core/3_Prefabs/Gameplay/Source/SandSource.prefab", "SandSource", typeof(PhaseCSourceVisual), false);
                runtime.prefabProfile.cupPrefab = GetOrCreateVisualPrefab(
                    "Assets/_Core/3_Prefabs/Gameplay/Cup/Cup.prefab", "Cup", typeof(PhaseCCupVisual), false);
                runtime.prefabProfile.drawStrokePrefab = GetOrCreateVisualPrefab(
                    "Assets/_Core/3_Prefabs/Gameplay/Draw/DrawStroke.prefab", "DrawStroke", typeof(PhaseCDrawStrokeVisual), true);
                EditorUtility.SetDirty(runtime.prefabProfile);
            }
            PhaseCLevelSequence sequence = GetOrCreate<PhaseCLevelSequence>(folder + "/PhaseCLevelSequence.asset");
            runtime.levelSequence = sequence;
            sequence.levels = new List<LevelSequenceEntry>
            {
                new LevelSequenceEntry { levelId = "phase_c_level_01" },
                new LevelSequenceEntry { levelId = "phase_c_level_02" },
                new LevelSequenceEntry { levelId = "phase_c_level_03" }
            };
            EditorUtility.SetDirty(palette);
            EditorUtility.SetDirty(runtime);
            EditorUtility.SetDirty(sequence);
            EditorUtility.SetDirty(juice);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("SE001/Phase C/Create visual materials")]
        private static void CreateVisualMaterials()
        {
            EnsureFolder("Assets/_Core/0_Texture2D/Dev");
            EnsureFolder("Assets/_Core/1_Materials");
            string texturePath = "Assets/_Core/0_Texture2D/Dev/dev_rounded_rect.png";
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), texturePath)))
            {
                Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
                Color32[] pixels = new Color32[256 * 256];
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float dx = Mathf.Abs(x - 127.5f) - 103f;
                        float dy = Mathf.Abs(y - 127.5f) - 103f;
                        float distance = Mathf.Max(dx, dy);
                        pixels[y * 256 + x] = distance <= 0f ? Color.white : new Color(1f, 1f, 1f, Mathf.Clamp01(1f - distance / 24f));
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), texturePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.Refresh();
            }

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Shader spriteShader = Shader.Find("SE001/SpriteSurface");
            Shader layoutShader = Shader.Find("SE001/LayoutSurface");
            Material source = GetOrCreateMaterial("Assets/_Core/1_Materials/MAT_Source.mat", spriteShader, sourceTexture, new Color(1f, 0.82f, 0.18f, 1f));
            Material cup = GetOrCreateMaterial("Assets/_Core/1_Materials/MAT_Cup.mat", spriteShader, sourceTexture, Color.white);
            Material cupBack = GetOrCreateMaterial(
                "Assets/_Core/1_Materials/MAT_CupBack.mat", spriteShader, sourceTexture, new Color(0.82f, 0.86f, 0.92f, 0.9f));
            Material draw = GetOrCreateMaterial("Assets/_Core/1_Materials/MAT_DrawPath.mat", layoutShader, null, new Color(0.35f, 0.3f, 0.55f, 1f));
            PhaseCVisualMaterials profile = GetOrCreate<PhaseCVisualMaterials>("Assets/_Core/Resources/Profiles/PhaseCVisualMaterials.asset");
            profile.sourceMaterial = source;
            profile.cupMaterial = cup;
            profile.cupBackMaterial = cupBack;
            profile.drawPathMaterial = draw;
            GameplayRuntimeProfile runtime = GetOrCreate<GameplayRuntimeProfile>("Assets/_Core/Resources/Profiles/PhaseCGameplayRuntimeProfile.asset");
            runtime.visualMaterials = profile;
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(runtime);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Texture2D texture, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            if (texture != null) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.5f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject GetOrCreateVisualPrefab(string path, string name, global::System.Type markerType, bool withMesh)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                if (prefab.GetComponent(markerType) == null)
                {
                    AssetDatabase.DeleteAsset(path);
                    prefab = null;
                }
            }
            if (prefab != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                if (contents.GetComponent(markerType) == null) contents.AddComponent(markerType);
                if (withMesh)
                {
                    if (contents.GetComponent<MeshFilter>() == null) contents.AddComponent<MeshFilter>();
                    if (contents.GetComponent<MeshRenderer>() == null) contents.AddComponent<MeshRenderer>();
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            EnsureFolder(global::System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            GameObject root = new GameObject(name);
            root.AddComponent(markerType);
            if (withMesh)
            {
                root.AddComponent<MeshFilter>();
                root.AddComponent<MeshRenderer>();
            }
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
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
