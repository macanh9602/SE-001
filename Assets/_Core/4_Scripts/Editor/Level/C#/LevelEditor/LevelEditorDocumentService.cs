#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

        internal sealed class SavedLevelDescriptor
        {
            public string ProjectPath;
            public string LevelId;
            public string DisplayName;
            public string Error;
            public bool IsValid;
        }

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

        public static List<SavedLevelDescriptor> EnumerateSavedLevels(PhaseCLevelSequence sequence)
        {
            List<SavedLevelDescriptor> result = new List<SavedLevelDescriptor>();
            string absoluteFolder = ToAbsolutePath(LevelsFolder);
            if (!Directory.Exists(absoluteFolder)) return result;

            string[] files = Directory.GetFiles(absoluteFolder, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                string projectPath = ToProjectRelativePath(files[i]);
                SE001LevelJson level;
                string openedPath;
                string error;
                bool upgraded;
                if (TryOpen(files[i], out level, out openedPath, out upgraded, out error))
                {
                    result.Add(new SavedLevelDescriptor
                    {
                        ProjectPath = projectPath,
                        LevelId = level.levelId,
                        DisplayName = DisplayName(sequence, level.levelId),
                        Error = string.Empty,
                        IsValid = true
                    });
                }
                else
                {
                    result.Add(new SavedLevelDescriptor
                    {
                        ProjectPath = projectPath,
                        LevelId = Path.GetFileNameWithoutExtension(files[i]),
                        DisplayName = Path.GetFileNameWithoutExtension(files[i]),
                        Error = error,
                        IsValid = false
                    });
                }
            }

            return result;
        }

        public static bool TryDeleteSavedLevel(
            string projectPath,
            string fallbackLevelId,
            PhaseCLevelSequence sequence,
            out string deletedLevelId,
            out string error)
        {
            deletedLevelId = fallbackLevelId ?? string.Empty;
            error = string.Empty;
            string normalized = (projectPath ?? string.Empty).Replace('\\', '/');
            if (!normalized.StartsWith(LevelsFolder + "/", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(normalized), ".json", StringComparison.OrdinalIgnoreCase))
            {
                error = "Only saved level JSON files can be deleted from the Levels folder.";
                return false;
            }

            string absolutePath = ToAbsolutePath(normalized);
            if (!File.Exists(absolutePath) && string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(normalized)))
            {
                error = "The saved level was already deleted or could not be found.";
                return false;
            }

            try
            {
                if (File.Exists(absolutePath))
                {
                    string json = File.ReadAllText(absolutePath);
                    SE001LevelJson level = SE001LevelJson.FromJson(json);
                    if (level != null && !string.IsNullOrWhiteSpace(level.levelId)) deletedLevelId = level.levelId;
                }
            }
            catch
            {
                // The asset can still be removed safely; a malformed file has no trustworthy ID to remove from the sequence.
            }

            if (!AssetDatabase.DeleteAsset(normalized))
            {
                error = "Unity could not delete the saved level asset. Refresh the Project window and try again.";
                return false;
            }

            bool sequenceChanged = RemoveFromSequence(sequence, deletedLevelId);
            if (sequenceChanged)
            {
                EditorUtility.SetDirty(sequence);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
            return true;
        }

        private static bool RemoveFromSequence(PhaseCLevelSequence sequence, string levelId)
        {
            if (sequence == null || sequence.levels == null || string.IsNullOrWhiteSpace(levelId)) return false;
            bool removed = false;
            for (int i = sequence.levels.Count - 1; i >= 0; i--)
            {
                if (sequence.levels[i].levelId != levelId) continue;
                sequence.levels.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        private static string FormatLevelName(int number)
        {
            return "Level_" + number.ToString("00", CultureInfo.InvariantCulture);
        }

        public static bool TryOpen(string absolutePath, out SE001LevelJson level, out string projectPath,
            out bool upgraded, out string error)
        {
            level = null;
            projectPath = string.Empty;
            upgraded = false;
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
                if (parsed.schemaVersion != 3 && parsed.schemaVersion != 4 && parsed.schemaVersion != 5)
                {
                    error = "Level uses unsupported schema " + parsed.schemaVersion + ".";
                    return false;
                }

                SourceProfile sourceProfile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
                if (sourceProfile == null)
                {
                    error = "SourceProfile is required to upgrade level tuning.";
                    return false;
                }
                upgraded = parsed.schemaVersion < 5;
                LevelTuning.UpgradeToSchema5(parsed, sourceProfile);

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

        public static bool TryOpenProjectPath(string projectPath, out SE001LevelJson level,
            out bool upgraded, out string error)
        {
            string openedProjectPath;
            return TryOpen(ToAbsolutePath(projectPath), out level, out openedProjectPath, out upgraded, out error);
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

                if (level.schemaVersion != 5)
                {
                    error = "Only schema-5 levels can be saved by the production Level Editor.";
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
