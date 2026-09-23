#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;

namespace SE001.Editor.Level
{
    internal static class LevelEditorStableIds
    {
        public static int NormalizeInMemory(SE001LevelJson level)
        {
            if (level == null) return 0;
            level.EnsureCollections();
            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            int changed = 0;

            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData source = level.sources[i];
                if (source == null) continue;
                if (RegisterOrReplace(ref source.stableId, "source", used)) changed++;
            }

            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData cup = level.cups[i];
                if (cup == null) continue;
                if (RegisterOrReplace(ref cup.stableId, "cup", used)) changed++;
            }

            for (int i = 0; i < level.rotatingObstacles.Count; i++)
            {
                RotatingObstacleData obstacle = level.rotatingObstacles[i];
                if (obstacle == null) continue;
                if (RegisterOrReplace(ref obstacle.stableId, "rotating_obstacle", used)) changed++;
            }

            return changed;
        }

        public static string Create(string prefix)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "entity" : prefix.Trim().ToLowerInvariant();
            return safePrefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        private static bool RegisterOrReplace(ref string stableId, string prefix, HashSet<string> used)
        {
            if (!string.IsNullOrWhiteSpace(stableId) && used.Add(stableId)) return false;
            do
            {
                stableId = Create(prefix);
            }
            while (!used.Add(stableId));
            return true;
        }
    }
}
#endif
