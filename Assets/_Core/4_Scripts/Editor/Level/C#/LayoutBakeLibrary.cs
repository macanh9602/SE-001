#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SE001.Data;
using UnityEditor;

namespace SE001.Editor.Level
{
    public sealed class LayoutBakeEntry
    {
        public LayoutDefinition Definition;
        public LayoutBakeFacts Facts;
        public LayoutBakeStatus Status;
        public string LayoutId => Facts.LayoutId;
        public string SourceFileName => string.IsNullOrWhiteSpace(Facts.SourceSvgPath) ? "—" : Path.GetFileName(Facts.SourceSvgPath);
    }

    /// <summary>Parsed SVG result cached by absolute path + last write time, so refreshes do not re-parse unchanged files.</summary>
    public sealed class SvgParseResult
    {
        public bool Parsed;
        public string Error = string.Empty;
        public SE001LevelJson Level;
        public string ContourHash = string.Empty;
    }

    /// <summary>
    /// Discovers baked layouts and computes their status. Only called at explicit/lifecycle boundaries
    /// (window open, focus, Refresh, after a bake) — never from repaint.
    /// </summary>
    public sealed class LayoutBakeLibrary
    {
        private readonly Dictionary<string, CachedSvg> svgCache = new Dictionary<string, CachedSvg>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> lastBakeErrors = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<LayoutBakeEntry> entries = new List<LayoutBakeEntry>();
        private float runtimeCellSize;
        private int runtimeMaxCells;
        private string profileError = string.Empty;

        public IReadOnlyList<LayoutBakeEntry> Entries => entries;
        public float RuntimeCellSize => runtimeCellSize;
        public int RuntimeMaxCells => runtimeMaxCells;
        public string ProfileError => profileError;

        public bool Contains(string layoutId)
        {
            for (int i = 0; i < entries.Count; i++)
                if (string.Equals(entries[i].LayoutId, layoutId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public LayoutBakeEntry Find(string layoutId)
        {
            for (int i = 0; i < entries.Count; i++)
                if (string.Equals(entries[i].LayoutId, layoutId, StringComparison.Ordinal))
                    return entries[i];
            return null;
        }

        public void RecordBakeResult(string layoutId, string error)
        {
            if (string.IsNullOrEmpty(layoutId)) return;
            if (string.IsNullOrEmpty(error)) lastBakeErrors.Remove(layoutId);
            else lastBakeErrors[layoutId] = error;
        }

        public void Refresh()
        {
            entries.Clear();
            if (!LayoutBaker.TryGetRuntimeGrid(out runtimeCellSize, out runtimeMaxCells, out profileError))
            {
                runtimeCellSize = 0f;
                runtimeMaxCells = 0;
            }

            string[] guids = AssetDatabase.FindAssets("t:LayoutDefinition", new[] { LayoutBaker.LayoutResourceFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                LayoutDefinition definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(path);
                if (definition == null) continue;
                LayoutBakeFacts facts = BuildFacts(definition);
                entries.Add(new LayoutBakeEntry
                {
                    Definition = definition,
                    Facts = facts,
                    Status = LayoutBakeStatusEvaluator.Evaluate(facts, RuntimeCellSize, LayoutBaker.ImporterVersion)
                });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.LayoutId, b.LayoutId));
        }

        public SvgParseResult ParseSvg(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
                return new SvgParseResult { Parsed = false, Error = "File not found." };
            DateTime stamp = File.GetLastWriteTimeUtc(absolutePath);
            CachedSvg cached;
            if (svgCache.TryGetValue(absolutePath, out cached) && cached.Stamp == stamp) return cached.Result;

            SvgParseResult result = new SvgParseResult();
            SE001LevelJson level;
            string error;
            try
            {
                if (PhaseBSvgImporter.TryParse(absolutePath, new PhaseBSvgImportSettings(), out level, out error))
                {
                    level.EnsureCollections();
                    result.Parsed = true;
                    result.Level = level;
                    result.ContourHash = LayoutBaker.ComputeContourHash(level);
                }
                else
                {
                    result.Error = string.IsNullOrEmpty(error) ? "Unknown SVG error." : error;
                }
            }
            catch (Exception exception)
            {
                result.Parsed = false;
                result.Error = exception.Message;
            }

            svgCache[absolutePath] = new CachedSvg { Stamp = stamp, Result = result };
            return result;
        }

        private LayoutBakeFacts BuildFacts(LayoutDefinition definition)
        {
            LayoutBakeFacts facts = new LayoutBakeFacts
            {
                LayoutId = definition.layoutId ?? string.Empty,
                HasMask = definition.mask != null,
                HasPrefab = definition.layoutPrefab != null,
                DefinitionHash = definition.contourHash ?? string.Empty,
                SourceSvgPath = definition.sourceSvgPath ?? string.Empty
            };
            if (definition.mask != null)
            {
                facts.MaskHash = definition.mask.contourHash ?? string.Empty;
                facts.MaskCellSize = definition.mask.cellSize;
                facts.MaskImporterVersion = definition.mask.importerVersion;
            }

            if (!string.IsNullOrWhiteSpace(facts.SourceSvgPath))
            {
                string absolute = LayoutBaker.ResolveProjectPath(facts.SourceSvgPath);
                facts.SourceExists = File.Exists(absolute);
                if (facts.SourceExists)
                {
                    SvgParseResult parse = ParseSvg(absolute);
                    facts.SourceParsed = parse.Parsed;
                    facts.SourceParseError = parse.Error;
                    facts.SourceHash = parse.ContourHash;
                }
            }

            string lastError;
            if (lastBakeErrors.TryGetValue(facts.LayoutId, out lastError)) facts.LastBakeError = lastError;
            return facts;
        }

        private struct CachedSvg
        {
            public DateTime Stamp;
            public SvgParseResult Result;
        }
    }
}
#endif
