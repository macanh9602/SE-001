#if UNITY_EDITOR
using SE001.Data;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor.Level
{
    public static class PhaseBAssetSetup
    {
        [MenuItem("SE001/Phase B/Create default profiles")]
        private static void CreateDefaultProfiles()
        {
            EnsureFolder("Assets/_Core/Resources/Profiles");
            PrefabProfile prefabProfile = AssetDatabase.LoadAssetAtPath<PrefabProfile>("Assets/_Core/Resources/Profiles/PhaseBPrefabProfile.asset");
            if (prefabProfile == null) { prefabProfile = ScriptableObject.CreateInstance<PrefabProfile>(); AssetDatabase.CreateAsset(prefabProfile, "Assets/_Core/Resources/Profiles/PhaseBPrefabProfile.asset"); }
            prefabProfile.boardWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Layout/BoardWall.prefab");
            prefabProfile.staticObstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Obstacle/StaticObstacle.prefab");
            prefabProfile.sandFieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Sand/SandField.prefab");
            EditorUtility.SetDirty(prefabProfile);
            CreateAsset<LayoutVisualProfile>("Assets/_Core/Resources/Profiles/PhaseBLayoutVisualProfile.asset");
            CreateAsset<SandSimulationProfile>("Assets/_Core/Resources/Profiles/PhaseBSandSimulationProfile.asset");
            AssetDatabase.SaveAssets();
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Core/Resources")) AssetDatabase.CreateFolder("Assets/_Core", "Resources");
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/_Core/Resources", "Profiles");
        }
    }
}
#endif
