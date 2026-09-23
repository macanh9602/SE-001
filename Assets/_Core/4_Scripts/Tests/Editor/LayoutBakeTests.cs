#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SE001.Data;
using SE001.Editor.Level;
using SE001.Geometry;
using UnityEngine;

namespace SE001.Editor.Level.Tests
{
    public sealed class LayoutBakeTests
    {
        [Test]
        public void LayoutBake_MaskMatchesRuntimeRasterizer()
        {
            SE001LevelJson level = CreateLegacyFixture();
            LayoutMaskAsset baked = LayoutBaker.BuildMaskAsset(level, 0.5f, 1000);
            try
            {
                LayoutMaskSet expected = LayoutRasterizer.Rasterize(level, 0.5f, 1000);
                LayoutMaskSet actual;
                string error;
                Assert.That(baked.TryBuildMaskSet(0.5f, 1000, out actual, out error), Is.True, error);
                Assert.That(actual.ValidMask, Is.EqualTo(expected.ValidMask));
                Assert.That(actual.StaticMask, Is.EqualTo(expected.StaticMask));
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void LayoutBake_StaleCellSize_BlocksLoad()
        {
            LayoutMaskAsset baked = LayoutBaker.BuildMaskAsset(CreateLegacyFixture(), 0.5f, 1000);
            try
            {
                LayoutMaskSet masks;
                string error;
                Assert.That(baked.TryBuildMaskSet(0.25f, 1000, out masks, out error), Is.False);
                StringAssert.Contains("0.5", error);
                StringAssert.Contains("0.25", error);
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void LayoutBake_ReimportSameSvg_IsDeterministic()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(projectRoot, "TrashStuff", "test_tool.svg");
            Assert.That(File.Exists(source), Is.True, source);
            PhaseBSvgImportSettings settings = new PhaseBSvgImportSettings { legacyTestFixture = true };
            SE001LevelJson firstLevel;
            SE001LevelJson secondLevel;
            string error;
            Assert.That(PhaseBSvgImporter.TryParse(source, settings, out firstLevel, out error), Is.True, error);
            Assert.That(PhaseBSvgImporter.TryParse(source, settings, out secondLevel, out error), Is.True, error);
            LayoutBakeSnapshot first = LayoutBaker.BuildSnapshot(firstLevel, 0.5f, 100000);
            LayoutBakeSnapshot second = LayoutBaker.BuildSnapshot(secondLevel, 0.5f, 100000);
            Assert.That(second.ContourHash, Is.EqualTo(first.ContourHash));
            Assert.That(second.StaticBits, Is.EqualTo(first.StaticBits));
            Assert.That(second.ValidBits, Is.EqualTo(first.ValidBits));
        }

        [Test]
        public void LevelLoad_UsesBakedMask_NoRasterizeCall()
        {
            LayoutRasterizer.ResetCallCount();
            SE001LevelJson level = LevelDataLoader.Load("phase_c_level_02");
            Assert.That(level.layoutId, Is.EqualTo("phase_c_level_02_layout"));
            Assert.That(LayoutRasterizer.RasterizeCallCount, Is.EqualTo(0));
        }

        [Test]
        public void LevelJson_MigratesContoursToLayoutId()
        {
            string legacyJson = CreateLegacyFixture().ToJson();
            string migratedJson = LayoutMigration.MigrateInMemory(legacyJson, "fixture_layout");
            SE001LevelJson migrated = SE001LevelJson.FromJson(migratedJson);
            Assert.That(migrated.schemaVersion, Is.EqualTo(3));
            Assert.That(migrated.layoutId, Is.EqualTo("fixture_layout"));
            Assert.That(migrated.board.wallContours, Is.Empty);
            Assert.That(migrated.staticObstacles, Is.Empty);
            Assert.That(migratedJson, Does.Not.Contain("wallContours"));
            Assert.That(migratedJson, Does.Not.Contain("staticObstacles"));
        }

        private static SE001LevelJson CreateLegacyFixture()
        {
            return new SE001LevelJson
            {
                schemaVersion = 2,
                levelId = "layout_bake_fixture",
                board = new BoardData
                {
                    size = new Vector2(4f, 4f),
                    wallContours = new List<PolygonContourData>
                    {
                        Square(0f, 0f, 4f, 0.5f),
                        Square(0f, 3.5f, 4f, 0.5f)
                    }
                },
                staticObstacles = new List<StaticObstacleData>
                {
                    new StaticObstacleData
                    {
                        stableId = "fixture_obstacle",
                        contours = new List<PolygonContourData> { Square(1.5f, 1.5f, 1f, 0.5f) }
                    }
                }
            };
        }

        private static PolygonContourData Square(float x, float y, float width, float height)
        {
            return new PolygonContourData
            {
                points = new List<Vector2>
                {
                    new Vector2(x, y),
                    new Vector2(x + width, y),
                    new Vector2(x + width, y + height),
                    new Vector2(x, y + height)
                }
            };
        }
    }
}
#endif
