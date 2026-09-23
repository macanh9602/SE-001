#if UNITY_EDITOR
using System.IO;
using SE001.Data;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor.Level
{
    /// <summary>Read-only helpers the Layout Bake window needs; the bake pipeline stays in LayoutBaker.cs.</summary>
    public static partial class LayoutBaker
    {
        public static bool TryGetRuntimeGrid(out float cellSize, out int maxCells, out string error)
        {
            cellSize = 0f;
            maxCells = 0;
            error = string.Empty;
            SandSimulationProfile simulationProfile = AssetDatabase.LoadAssetAtPath<SandSimulationProfile>(SimulationProfilePath);
            if (simulationProfile == null)
            {
                error = "Sand simulation profile is missing (" + SimulationProfilePath + "). Layouts cannot be baked.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<LayoutVisualProfile>(VisualProfilePath) == null)
            {
                error = "Layout visual profile is missing (" + VisualProfilePath + "). Layouts cannot be baked.";
                return false;
            }

            cellSize = simulationProfile.cellSize;
            maxCells = simulationProfile.maxCells;
            return true;
        }

        public static string ResolveProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            if (Path.IsPathRooted(path)) return path;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
