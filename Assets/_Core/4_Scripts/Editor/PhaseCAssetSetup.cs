#if UNITY_EDITOR
using System.Collections.Generic;
using SE001.Data;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;
using SE001.Presentation;

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
            if (runtime.prefabProfile != null)
            {
                runtime.prefabProfile.sourcePrefab = GetOrCreateVisualPrefab("Assets/_Core/3_Prefabs/Gameplay/Source/SandSource.prefab", "SandSource", typeof(PhaseCSourceVisual), false);
                runtime.prefabProfile.cupPrefab = GetOrCreateVisualPrefab("Assets/_Core/3_Prefabs/Gameplay/Cup/Cup.prefab", "Cup", typeof(PhaseCCupVisual), false);
                runtime.prefabProfile.drawStrokePrefab = GetOrCreateVisualPrefab("Assets/_Core/3_Prefabs/Gameplay/Draw/DrawStroke.prefab", "DrawStroke", typeof(PhaseCDrawStrokeVisual), true);
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

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
