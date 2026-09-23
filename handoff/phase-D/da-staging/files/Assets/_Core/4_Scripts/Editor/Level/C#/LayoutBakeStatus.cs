#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SE001.Editor.Level
{
    public enum LayoutBakeState
    {
        Ready,
        NeedsRebake,
        SourceMissing,
        InvalidSvg,
        BakeFailed
    }

    /// <summary>User-facing status of one baked layout: WHAT (headline + reason) and HOW (recovery).</summary>
    public readonly struct LayoutBakeStatus
    {
        public LayoutBakeStatus(LayoutBakeState state, string reason, string recovery, bool canRebake, bool levelsCanLoad)
        {
            State = state;
            Reason = reason ?? string.Empty;
            Recovery = recovery ?? string.Empty;
            CanRebake = canRebake;
            LevelsCanLoad = levelsCanLoad;
        }

        public LayoutBakeState State { get; }
        public string Reason { get; }
        public string Recovery { get; }
        public bool CanRebake { get; }
        public bool LevelsCanLoad { get; }
        public string Headline => LayoutBakeStatusEvaluator.Headline(State);
    }

    /// <summary>Plain facts about one layout, gathered at refresh time. No Unity objects so status logic is unit-testable.</summary>
    public sealed class LayoutBakeFacts
    {
        public string LayoutId = string.Empty;
        public bool HasMask;
        public bool HasPrefab;
        public string DefinitionHash = string.Empty;
        public string MaskHash = string.Empty;
        public float MaskCellSize;
        public int MaskImporterVersion;
        public string SourceSvgPath = string.Empty;
        public bool SourceExists;
        public bool SourceParsed;
        public string SourceParseError = string.Empty;
        public string SourceHash = string.Empty;
        public string LastBakeError = string.Empty;
    }

    public static class LayoutBakeStatusEvaluator
    {
        public static string Headline(LayoutBakeState state)
        {
            switch (state)
            {
                case LayoutBakeState.Ready: return "Ready";
                case LayoutBakeState.NeedsRebake: return "Needs Rebake";
                case LayoutBakeState.SourceMissing: return "Source Missing";
                case LayoutBakeState.InvalidSvg: return "Invalid SVG";
                default: return "Bake Failed";
            }
        }

        public static LayoutBakeStatus Evaluate(LayoutBakeFacts facts, float runtimeCellSize, int importerVersion)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            bool blocksRuntime;
            string staleReason = FindStaleBakeReason(facts, runtimeCellSize, importerVersion, out blocksRuntime);
            bool levelsCanLoad = !blocksRuntime;
            string fileName = SafeFileName(facts.SourceSvgPath);
            bool hasSourcePath = !string.IsNullOrWhiteSpace(facts.SourceSvgPath);

            if (!string.IsNullOrEmpty(facts.LastBakeError))
            {
                return new LayoutBakeStatus(
                    LayoutBakeState.BakeFailed,
                    "Last bake failed: " + facts.LastBakeError,
                    "Fix the SVG " + (hasSourcePath ? "(" + fileName + ") " : string.Empty) + "and press Rebake.",
                    hasSourcePath && facts.SourceExists,
                    levelsCanLoad);
            }

            if (!hasSourcePath)
            {
                return new LayoutBakeStatus(
                    LayoutBakeState.SourceMissing,
                    "No SVG is linked to this layout. " + LoadNote(levelsCanLoad),
                    "To update it: choose its SVG under SVG Source, type Layout ID '" + facts.LayoutId + "', then Bake Layout.",
                    false,
                    levelsCanLoad);
            }

            if (!facts.SourceExists)
            {
                return new LayoutBakeStatus(
                    LayoutBakeState.SourceMissing,
                    "SVG file not found: " + facts.SourceSvgPath + ". " + LoadNote(levelsCanLoad),
                    "Restore the file, or choose the SVG again under SVG Source with Layout ID '" + facts.LayoutId + "'.",
                    false,
                    levelsCanLoad);
            }

            if (!facts.SourceParsed)
            {
                return new LayoutBakeStatus(
                    LayoutBakeState.InvalidSvg,
                    "SVG could not be read (" + fileName + "): " + facts.SourceParseError,
                    "Fix the SVG path/geometry, save it, then press Refresh.",
                    false,
                    levelsCanLoad);
            }

            if (staleReason != null)
            {
                return new LayoutBakeStatus(
                    LayoutBakeState.NeedsRebake,
                    staleReason + " " + LoadNote(levelsCanLoad),
                    "Press Rebake.",
                    true,
                    levelsCanLoad);
            }

            return new LayoutBakeStatus(LayoutBakeState.Ready, "Baked from " + fileName + ".", string.Empty, true, true);
        }

        // Returns null when the baked data is consistent with the current source and runtime settings.
        // blocksRuntime mirrors LayoutDefinition.TryBuildMaskSet: true when levels using the layout fail to load.
        private static string FindStaleBakeReason(
            LayoutBakeFacts facts,
            float runtimeCellSize,
            int importerVersion,
            out bool blocksRuntime)
        {
            blocksRuntime = true;
            if (!facts.HasMask || !facts.HasPrefab) return "The bake is incomplete (mask or prefab missing).";
            if (string.IsNullOrEmpty(facts.DefinitionHash) || !string.Equals(facts.DefinitionHash, facts.MaskHash, StringComparison.Ordinal))
                return "The baked mask does not match the layout (partial or old bake).";
            if (Math.Abs(facts.MaskCellSize - runtimeCellSize) > 0.000001f)
            {
                return "The sand grid size changed (baked " + Format(facts.MaskCellSize) + ", game now uses " +
                    Format(runtimeCellSize) + ").";
            }

            blocksRuntime = false;
            if (facts.MaskImporterVersion < importerVersion) return "The SVG importer was updated since this bake.";
            bool sourceKnown = facts.SourceExists && facts.SourceParsed && !string.IsNullOrEmpty(facts.SourceHash);
            if (sourceKnown && !string.Equals(facts.SourceHash, facts.MaskHash, StringComparison.Ordinal))
                return "The SVG changed since the last bake.";
            return null;
        }

        private static string LoadNote(bool levelsCanLoad)
        {
            return levelsCanLoad ? "Levels using it still load." : "Levels using it will NOT load until it is rebaked.";
        }

        private static string Format(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static string SafeFileName(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return Path.GetFileName(path.Replace('\\', '/'));
        }
    }

    public enum LayoutIdCheck
    {
        Empty,
        Invalid,
        NewLayout,
        UpdatesExisting
    }

    public static class LayoutIdRules
    {
        private static readonly Regex Pattern = new Regex("^[a-z0-9_]{3,64}$", RegexOptions.CultureInvariant);

        public static LayoutIdCheck Check(string layoutId, Func<string, bool> exists, out string message)
        {
            string value = layoutId != null ? layoutId.Trim() : string.Empty;
            if (value.Length == 0)
            {
                message = "Type a Layout ID. Levels refer to the layout by this name.";
                return LayoutIdCheck.Empty;
            }

            if (!Pattern.IsMatch(value))
            {
                message = "Use 3–64 lowercase letters, digits or _ (example: stage_07).";
                return LayoutIdCheck.Invalid;
            }

            if (exists != null && exists(value))
            {
                message = "Updates the existing layout '" + value + "'. Levels using it get the new geometry.";
                return LayoutIdCheck.UpdatesExisting;
            }

            message = "Creates a new layout '" + value + "'.";
            return LayoutIdCheck.NewLayout;
        }

        public static string Suggest(string svgPath)
        {
            if (string.IsNullOrWhiteSpace(svgPath)) return string.Empty;
            string name = Path.GetFileNameWithoutExtension(svgPath.Replace('\\', '/')).ToLowerInvariant();
            name = Regex.Replace(name, "[^a-z0-9_]+", "_").Trim('_');
            return name.Length > 64 ? name.Substring(0, 64) : name;
        }
    }
}
#endif
