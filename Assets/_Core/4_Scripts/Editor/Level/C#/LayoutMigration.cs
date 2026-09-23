#if UNITY_EDITOR
using System;
using System.IO;
using SE001.Data;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor.Level
{
    public static class LayoutMigration
    {
        public static void MigrateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/_Core/Resources/Levels" });
            int migrated = 0;
            int skipped = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!assetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                string error;
                if (!TryMigrateFile(assetPath, out error))
                {
                    if (error == "already migrated") skipped++;
                    else Debug.LogError("[LayoutMigration] " + assetPath + ": " + error);
                    continue;
                }
                migrated++;
            }
            AssetDatabase.Refresh();
            Debug.Log("[LayoutMigration] Migrated " + migrated + " level(s); skipped " + skipped + ".");
        }

        public static string MigrateInMemory(string legacyJson, string layoutId)
        {
            SE001LevelJson level = SE001LevelJson.FromJson(legacyJson);
            if (level.schemaVersion >= 3) throw new FormatException("Input level is already schema 3.");
            ConvertToLayoutReference(level, layoutId);
            return level.ToJson(true);
        }

        private static bool TryMigrateFile(string assetPath, out string error)
        {
            error = string.Empty;
            try
            {
                string json = File.ReadAllText(assetPath);
                SE001LevelJson level = SE001LevelJson.FromJson(json);
                if (level.schemaVersion >= 3)
                {
                    error = "already migrated";
                    return false;
                }

                string layoutId = level.levelId + "_layout";
                LayoutDefinition definition;
                if (!LayoutBaker.TryBakeLevel(level, layoutId, string.Empty, out definition, out error)) return false;
                ConvertToLayoutReference(level, layoutId);
                File.WriteAllText(assetPath, level.ToJson(true) + "\n");
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void ConvertToLayoutReference(SE001LevelJson level, string layoutId)
        {
            if (string.IsNullOrWhiteSpace(layoutId)) throw new ArgumentException("A layout id is required.", nameof(layoutId));
            level.schemaVersion = 3;
            level.layoutId = layoutId;
            level.board.wallContours.Clear();
            level.staticObstacles.Clear();
        }
    }
}
#endif
