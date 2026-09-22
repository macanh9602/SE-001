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
            ColorProfile colorProfile = GetOrCreate<ColorProfile>(folder + "/PhaseCColorProfile.asset");
            colorProfile.entries = new List<ColorProfileEntry>
            {
                new ColorProfileEntry
                {
                    colorId = 1,
                    sandColor = new Color32(232, 65, 79, 255),
                    uiColor = new Color32(232, 65, 79, 255),
                    displayName = "Coral"
                },
                new ColorProfileEntry
                {
                    colorId = 2,
                    sandColor = new Color32(47, 107, 255, 255),
                    uiColor = new Color32(47, 107, 255, 255),
                    displayName = "Blue"
                }
            };
            SourceProfile source = GetOrCreate<SourceProfile>(folder + "/PhaseCSourceProfile.asset");
            source.bodySize = new Vector2(0.8f, 1.2f);
            CupProfile cup = GetOrCreate<CupProfile>(folder + "/PhaseCCupProfile.asset");
            DrawPathProfile draw = GetOrCreate<DrawPathProfile>(folder + "/PhaseCDrawPathProfile.asset");
            JuiceProfile juice = GetOrCreate<JuiceProfile>(folder + "/PhaseCJuiceProfile.asset");
            GameplayRuntimeProfile runtime = GetOrCreate<GameplayRuntimeProfile>(folder + "/PhaseCGameplayRuntimeProfile.asset");
            runtime.colorProfile = colorProfile;
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
            EditorUtility.SetDirty(colorProfile);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(runtime);
            EditorUtility.SetDirty(sequence);
            EditorUtility.SetDirty(juice);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("SE001/Phase C/Rebuild Gameplay Visual Prefabs")]
        public static void RebuildGameplayVisualPrefabs()
        {
            GetOrCreateVisualPrefab("Assets/_Core/3_Prefabs/Gameplay/Source/SandSource.prefab", "SandSource", typeof(PhaseCSourceVisual), false);
            GetOrCreateVisualPrefab("Assets/_Core/3_Prefabs/Gameplay/Cup/Cup.prefab", "Cup", typeof(PhaseCCupVisual), false);
            GetOrCreateVisualPrefab("Assets/_Core/3_Prefabs/Gameplay/Draw/DrawStroke.prefab", "DrawStroke", typeof(PhaseCDrawStrokeVisual), true);
            PrefabProfile profile = AssetDatabase.LoadAssetAtPath<PrefabProfile>("Assets/_Core/Resources/Profiles/PhaseBPrefabProfile.asset");
            if (profile != null)
            {
                profile.sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Source/SandSource.prefab");
                profile.cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Cup/Cup.prefab");
                profile.drawStrokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Draw/DrawStroke.prefab");
                EditorUtility.SetDirty(profile);
            }
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
            Material source = GetOrCreateMaterial("Assets/_Core/1_Materials/MAT_Source.mat", spriteShader, sourceTexture, new Color32(232, 65, 79, 255));
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
                BuildVisualHierarchy(contents, markerType, withMesh);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            EnsureFolder(global::System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            GameObject root = new GameObject(name);
            root.AddComponent(markerType);
            BuildVisualHierarchy(root, markerType, withMesh);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildVisualHierarchy(GameObject root, global::System.Type markerType, bool withMesh)
        {
            Mesh quad = GetOrCreateVisualQuad();
            if (markerType == typeof(PhaseCSourceVisual))
            {
                Transform pivot = EnsureChild(root.transform, "Pivot");
                Transform view = EnsureChild(pivot, "View");
                EnsureMeshRenderer(EnsureChild(view, "Body"), quad);
                EnsureMeshRenderer(EnsureChild(view, "Nozzle"), quad);
                Transform anchors = EnsureChild(root.transform, "Anchors");
                EnsureChild(anchors, "EmitPoint");
                return;
            }
            if (markerType == typeof(PhaseCCupVisual))
            {
                Transform view = EnsureChild(root.transform, "View");
                EnsureMeshRenderer(EnsureChild(view, "WallL"), quad);
                EnsureMeshRenderer(EnsureChild(view, "WallR"), quad);
                EnsureMeshRenderer(EnsureChild(view, "WallB"), quad);
                EnsureMeshRenderer(EnsureChild(view, "Back"), quad);
                EnsureLineRenderer(EnsureChild(view, "Rim"));
                EnsureMeshRenderer(EnsureChild(view, "FillLine"), quad);
                EnsureMeshRenderer(EnsureChild(root.transform, "FillView"), quad);
                Transform anchors = EnsureChild(root.transform, "Anchors");
                EnsureChild(anchors, "Entry");
                EnsureChild(anchors, "Feedback");
                return;
            }
            if (markerType == typeof(PhaseCDrawStrokeVisual))
            {
                MeshFilter rootFilter = root.GetComponent<MeshFilter>();
                MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
                if (rootFilter != null) Object.DestroyImmediate(rootFilter);
                if (rootRenderer != null) Object.DestroyImmediate(rootRenderer);
                Transform view = EnsureChild(root.transform, "View");
                EnsureMeshRenderer(view, quad);
                EnsureLineRenderer(EnsureChild(root.transform, "Preview"));
                return;
            }
            if (withMesh) EnsureMeshRenderer(root.transform, quad);
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Mesh GetOrCreateVisualQuad()
        {
            const string path = "Assets/_Core/3_Prefabs/Gameplay/VisualQuad.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "VisualQuad" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void EnsureMeshRenderer(Transform target, Mesh quad)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null) filter = target.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = quad;
            if (target.GetComponent<MeshRenderer>() == null) target.gameObject.AddComponent<MeshRenderer>();
        }

        private static void EnsureLineRenderer(Transform target)
        {
            LineRenderer line = target.GetComponent<LineRenderer>();
            if (line == null) line = target.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 0;
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
