#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SE001.Data;
using SE001.Editor.Level;
using SE001.Geometry;
using SE001.System.Management;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SE001.Editor.Level.Tests
{
    /// <summary>Phase D-A: contour-hash stale contract, production spawn without rasterization, Layout Bake status + rebake identity.</summary>
    public sealed class LayoutBakeProductionTests
    {
        private const string ProductionLevelId = "phase_c_level_02";
        private const string ProductionLayoutId = "phase_c_level_02_layout";
        private const string FixtureLayoutId = "zz_layout_bake_test_fixture";

        // ---------- §2.1 LayoutDefinition.TryBuildMaskSet ----------

        [Test]
        public void LayoutDefinition_ValidBake_BuildsMasks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                Assert.That(definition.TryBuildMaskSet(0.5f, 1000, out LayoutMaskSet masks, out string error), Is.True, error);
                Assert.That(masks.Width, Is.EqualTo(mask.width));
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        [Test]
        public void LayoutDefinition_NullMask_Blocks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                definition.mask = null;
                AssertBlocked(definition, "no baked mask");
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        [Test]
        public void LayoutDefinition_MissingExpectedHash_Blocks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                definition.contourHash = string.Empty;
                AssertBlocked(definition, "identity");
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        [Test]
        public void LayoutDefinition_HashMismatch_Blocks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                definition.contourHash = "stale";
                AssertBlocked(definition, "does not match");
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        [Test]
        public void LayoutDefinition_StaleCellSize_Blocks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                Assert.That(definition.TryBuildMaskSet(0.25f, 1000, out _, out string error), Is.False);
                StringAssert.Contains("Rebake", error);
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        [Test]
        public void LayoutDefinition_BitLengthOrOverflow_Blocks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                Assert.That(definition.TryBuildMaskSet(0.5f, 4, out _, out string overflow), Is.False);
                StringAssert.Contains("rebake", overflow.ToLowerInvariant());
                mask.validBits = new byte[1];
                Assert.That(definition.TryBuildMaskSet(0.5f, 1000, out _, out string length), Is.False);
                StringAssert.Contains("rebake", length.ToLowerInvariant());
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        [Test]
        public void LayoutDefinition_BoardSizeMismatch_Blocks()
        {
            LayoutDefinition definition = CreateFixtureDefinition(out LayoutMaskAsset mask);
            try
            {
                definition.boardSize = new Vector2(8f, 4f);
                AssertBlocked(definition, "board size");
            }
            finally
            {
                DestroyFixture(definition, mask);
            }
        }

        // ---------- §2.2 production composition ----------

        [Test]
        public void ProductionSpawn_UsesBakedMask_ZeroRasterizeCalls()
        {
            GameObject owner = new GameObject("LayoutBakeProductionTests");
            try
            {
                LevelManager manager = owner.AddComponent<LevelManager>();
                LayoutRasterizer.ResetCallCount();
                manager.BeginLevel(ProductionLevelId);
                Assert.That(manager.IsReady, Is.True);
                Assert.That(manager.CurrentContext.SandSimulation, Is.Not.Null);
                Assert.That(LayoutRasterizer.RasterizeCallCount, Is.EqualTo(0));
                manager.UnloadCurrentLevel();
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ProductionSpawn_StaleHash_BlocksWithRebakeMessage_NoFallbackRasterize()
        {
            LayoutDefinition layout = Resources.Load<LayoutDefinition>("Layouts/" + ProductionLayoutId);
            Assert.That(layout, Is.Not.Null);
            string original = layout.contourHash;
            GameObject owner = new GameObject("LayoutBakeProductionTests_Stale");
            try
            {
                layout.contourHash = "stale-" + original;
                LevelManager manager = owner.AddComponent<LevelManager>();
                LayoutRasterizer.ResetCallCount();
                Exception thrown = Assert.Catch<Exception>(() => manager.BeginLevel(ProductionLevelId));
                StringAssert.Contains("rebake", thrown.Message.ToLowerInvariant());
                Assert.That(manager.IsReady, Is.False);
                Assert.That(LayoutRasterizer.RasterizeCallCount, Is.EqualTo(0));
            }
            finally
            {
                // In-memory only: never saved to disk.
                layout.contourHash = original;
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ProductionLayouts_AllBuildThroughAuthoritativePath()
        {
            string[] ids = { "phase_c_level_01_layout", "phase_c_level_02_layout", "phase_c_level_03_layout" };
            LayoutBakeLibrary library = new LayoutBakeLibrary();
            library.Refresh();
            Assert.That(library.ProfileError, Is.Empty);
            for (int i = 0; i < ids.Length; i++)
            {
                LayoutDefinition layout = Resources.Load<LayoutDefinition>("Layouts/" + ids[i]);
                Assert.That(layout, Is.Not.Null, ids[i]);
                bool ok = layout.TryBuildMaskSet(library.RuntimeCellSize, library.RuntimeMaxCells, out _, out string error);
                Assert.That(ok, Is.True, ids[i] + ": " + error);
            }
        }

        // ---------- §3.9 status + rebake identity ----------

        [Test]
        public void Status_NoSvgLinked_IsSourceMissing()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.SourceSvgPath = string.Empty;
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.SourceMissing));
            Assert.That(status.CanRebake, Is.False);
            Assert.That(status.LevelsCanLoad, Is.True);
            Assert.That(status.Recovery, Is.Not.Empty);
        }

        [Test]
        public void Status_SvgFileMissing_IsSourceMissing()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.SourceExists = false;
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.SourceMissing));
            StringAssert.Contains("not found", status.Reason);
        }

        [Test]
        public void Status_StaleCellSize_IsNeedsRebake_AndBlocksLevels()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.MaskCellSize = 0.05f;
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.NeedsRebake));
            Assert.That(status.LevelsCanLoad, Is.False);
            Assert.That(status.CanRebake, Is.True);
        }

        [Test]
        public void Status_HashMismatch_IsNeedsRebake()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.DefinitionHash = "other";
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.NeedsRebake));
            Assert.That(status.LevelsCanLoad, Is.False);
        }

        [Test]
        public void Status_SvgEditedAfterBake_IsNeedsRebake_LevelsStillLoad()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.SourceHash = "edited";
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.NeedsRebake));
            Assert.That(status.LevelsCanLoad, Is.True);
        }

        [Test]
        public void Status_UnreadableSvg_IsInvalidSvg()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.SourceParsed = false;
            facts.SourceParseError = "path data";
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.InvalidSvg));
        }

        [Test]
        public void Status_LastBakeError_IsBakeFailed()
        {
            LayoutBakeFacts facts = ReadyFacts();
            facts.LastBakeError = "boom";
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(facts, 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.BakeFailed));
            Assert.That(status.CanRebake, Is.True);
        }

        [Test]
        public void Status_Consistent_IsReady()
        {
            LayoutBakeStatus status = LayoutBakeStatusEvaluator.Evaluate(ReadyFacts(), 0.06f, LayoutBaker.ImporterVersion);
            Assert.That(status.State, Is.EqualTo(LayoutBakeState.Ready));
        }

        [TestCase("", LayoutIdCheck.Empty)]
        [TestCase("Stage 7", LayoutIdCheck.Invalid)]
        [TestCase("ab", LayoutIdCheck.Invalid)]
        [TestCase("stage_07", LayoutIdCheck.NewLayout)]
        [TestCase("existing_layout", LayoutIdCheck.UpdatesExisting)]
        public void LayoutId_Rules(string id, LayoutIdCheck expected)
        {
            LayoutIdCheck result = LayoutIdRules.Check(id, value => value == "existing_layout", out string message);
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(message, Is.Not.Empty);
        }

        [Test]
        public void Rebake_PreservesAssetGuids_AndWritesMatchingHashes()
        {
            string svg = TestSvgPath();
            string definitionPath = LayoutBaker.LayoutResourceFolder + "/" + FixtureLayoutId + ".asset";
            string maskPath = LayoutBaker.LayoutResourceFolder + "/" + FixtureLayoutId + "_Mask.asset";
            string prefabPath = LayoutBaker.LayoutPrefabFolder + "/" + FixtureLayoutId + ".prefab";
            try
            {
                PhaseBSvgImportSettings settings = new PhaseBSvgImportSettings();
                bool firstOk = LayoutBaker.TryBakeSvg(svg, FixtureLayoutId, settings, out LayoutDefinition first, out string error);
                Assert.That(firstOk, Is.True, error);
                string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
                string maskGuid = AssetDatabase.AssetPathToGUID(maskPath);
                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                string firstHash = first.contourHash;
                Assert.That(firstHash, Is.Not.Empty);
                Assert.That(first.mask.contourHash, Is.EqualTo(firstHash));

                bool secondOk = LayoutBaker.TryBakeSvg(svg, FixtureLayoutId, settings, out LayoutDefinition second, out error);
                Assert.That(secondOk, Is.True, error);
                Assert.That(AssetDatabase.AssetPathToGUID(definitionPath), Is.EqualTo(definitionGuid));
                Assert.That(AssetDatabase.AssetPathToGUID(maskPath), Is.EqualTo(maskGuid));
                Assert.That(AssetDatabase.AssetPathToGUID(prefabPath), Is.EqualTo(prefabGuid));
                Assert.That(second.contourHash, Is.EqualTo(firstHash), "Same SVG must bake to the same identity.");
                Assert.That(second.mask.contourHash, Is.EqualTo(firstHash));
            }
            finally
            {
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.DeleteAsset(maskPath);
                AssetDatabase.DeleteAsset(definitionPath);
            }
        }

        [Test]
        public void Library_ParseCache_ReusesResultForUnchangedFile()
        {
            LayoutBakeLibrary library = new LayoutBakeLibrary();
            string svg = TestSvgPath();
            SvgParseResult first = library.ParseSvg(svg);
            SvgParseResult second = library.ParseSvg(svg);
            Assert.That(first.Parsed, Is.True, first.Error);
            Assert.That(second, Is.SameAs(first));
        }

        // ---------- helpers ----------

        private static void AssertBlocked(LayoutDefinition definition, string expectedFragment)
        {
            LayoutRasterizer.ResetCallCount();
            bool ok = definition.TryBuildMaskSet(0.5f, 1000, out LayoutMaskSet masks, out string error);
            Assert.That(ok, Is.False);
            Assert.That(masks, Is.Null);
            StringAssert.Contains(expectedFragment, error);
            StringAssert.Contains("rebake", error.ToLowerInvariant());
            Assert.That(LayoutRasterizer.RasterizeCallCount, Is.EqualTo(0));
        }

        private static LayoutDefinition CreateFixtureDefinition(out LayoutMaskAsset mask)
        {
            SE001LevelJson level = new SE001LevelJson
            {
                schemaVersion = 2,
                levelId = "layout_hash_fixture",
                board = new BoardData
                {
                    size = new Vector2(4f, 4f),
                    wallContours = new List<PolygonContourData>
                    {
                        new PolygonContourData
                        {
                            points = new List<Vector2>
                            {
                                new Vector2(0f, 0f),
                                new Vector2(4f, 0f),
                                new Vector2(4f, 0.5f),
                                new Vector2(0f, 0.5f)
                            }
                        }
                    }
                }
            };
            mask = LayoutBaker.BuildMaskAsset(level, 0.5f, 1000);
            LayoutDefinition definition = ScriptableObject.CreateInstance<LayoutDefinition>();
            definition.layoutId = "layout_hash_fixture";
            definition.mask = mask;
            definition.boardSize = level.board.size;
            definition.contourHash = mask.contourHash;
            return definition;
        }

        private static void DestroyFixture(LayoutDefinition definition, LayoutMaskAsset mask)
        {
            if (definition != null) Object.DestroyImmediate(definition);
            if (mask != null) Object.DestroyImmediate(mask);
        }

        private static LayoutBakeFacts ReadyFacts()
        {
            return new LayoutBakeFacts
            {
                LayoutId = "fixture",
                HasMask = true,
                HasPrefab = true,
                DefinitionHash = "h1",
                MaskHash = "h1",
                MaskCellSize = 0.06f,
                MaskImporterVersion = LayoutBaker.ImporterVersion,
                SourceSvgPath = "TrashStuff/fixture.svg",
                SourceExists = true,
                SourceParsed = true,
                SourceHash = "h1"
            };
        }

        private static string TestSvgPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string svg = Path.Combine(projectRoot, "TrashStuff", "test_tool.svg");
            Assert.That(File.Exists(svg), Is.True, svg);
            return svg;
        }
    }
}
#endif
