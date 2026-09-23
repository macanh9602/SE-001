#if UNITY_EDITOR
using System;
using System.IO;
using System.Globalization;
using SE001.Data;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor.Level
{
    internal static class LevelEditorDocumentService
    {
        public const string LevelsFolder = "Assets/_Core/Resources/Levels";

        public static string DisplayName(PhaseCLevelSequence sequence, string levelId)
        {
            if (!string.IsNullOrEmpty(levelId) && levelId.StartsWith("Level_", StringComparison.Ordinal))
                return levelId;
            if (sequence != null && sequence.levels != null)
                for (int i = 0; i < sequence.levels.Count; i++)
                    if (sequence.levels[i].levelId == levelId) return FormatLevelName(i + 1);
            return levelId ?? string.Empty;
        }

        public static string NextLevelId(PhaseCLevelSequence sequence)
        {
            int number = 1;
            string candidate;
            do
            {
                candidate = FormatLevelName(number++);
            }
            while (File.Exists(ToAbsolutePath(LevelsFolder + "/" + candidate + ".json")));
            return candidate;
        }

        public static bool IsInSequence(PhaseCLevelSequence sequence, string levelId)
        {
            if (sequence == null || sequence.levels == null) return false;
            for (int i = 0; i < sequence.levels.Count; i++)
                if (sequence.levels[i].levelId == levelId) return true;
            return false;
        }

        public static bool AddToSequenceIfMissing(PhaseCLevelSequence sequence, string levelId)
        {
            if (sequence == null || string.IsNullOrWhiteSpace(levelId) || IsInSequence(sequence, levelId))
                return false;
            if (sequence.levels == null) sequence.levels = new global::System.Collections.Generic.List<LevelSequenceEntry>();
            sequence.levels.Add(new LevelSequenceEntry { levelId = levelId });
            return true;
        }

        private static string FormatLevelName(int number)
        {
            return "Level_" + number.ToString("00", CultureInfo.InvariantCulture);
        }

        public static bool TryOpen(string absolutePath, out SE001LevelJson level, out string projectPath, out string error)
        {
            level = null;
            projectPath = string.Empty;
            error = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
                {
                    error = "Level file was not found.";
                    return false;
                }

                string json = File.ReadAllText(absolutePath);
                if (json.IndexOf("wallContours", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    json.IndexOf("staticObstacles", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    error = "Schema-3 levels cannot contain wallContours or staticObstacles. Edit layout geometry in Layout Bake.";
                    return false;
                }
                SE001LevelJson parsed = SE001LevelJson.FromJson(json);
                if (parsed.schemaVersion != 3 && parsed.schemaVersion != 4)
                {
                    error = "Level uses unsupported schema " + parsed.schemaVersion + ".";
                    return false;
                }

                parsed.schemaVersion = 4;

                parsed.EnsureCollections();
                level = parsed;
                projectPath = ToProjectRelativePath(absolutePath);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TrySave(SE001LevelJson level, string projectPath, out string error)
        {
            error = string.Empty;
            try
            {
                if (level == null)
                {
                    error = "No level is open.";
                    return false;
                }

                if (level.schemaVersion != 4)
                {
                    error = "Only schema-4 levels can be saved by the production Level Editor.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(projectPath))
                {
                    error = "Choose a level file first.";
                    return false;
                }

                string normalized = projectPath.Replace('\\', '/');
                if (!normalized.StartsWith(LevelsFolder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    error = "Save levels under " + LevelsFolder + ".";
                    return false;
                }

                string absolutePath = ToAbsolutePath(normalized);
                string directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                string temporaryPath = absolutePath + ".tmp";
                File.WriteAllText(temporaryPath, level.ToJson(true) + Environment.NewLine);
                if (File.Exists(absolutePath)) File.Replace(temporaryPath, absolutePath, null);
                else File.Move(temporaryPath, absolutePath);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static string ToProjectRelativePath(string absolutePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
            string normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');
            if (!normalized.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase)) return normalized;
            return normalized.Substring(projectRoot.Length + 1);
        }

        public static string ToAbsolutePath(string projectPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
#endif
