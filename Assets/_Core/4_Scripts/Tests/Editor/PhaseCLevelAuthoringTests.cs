using System.Collections.Generic;
using NUnit.Framework;
using SE001.Data;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class PhaseCLevelAuthoringTests
    {
        [Test]
        public void ColorProfile_MapsColorIdsToSandAndUi()
        {
            ColorProfile profile = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.GetSandColor(1), Is.EqualTo(new Color32(251, 46, 43, 255)));
            Assert.That(profile.GetSandColor(2), Is.EqualTo(new Color32(58, 108, 255, 255)));
            Assert.That(profile.GetUiColor(1), Is.EqualTo(profile.GetSandColor(1)));
            Assert.That(profile.BuildSandLookup()[2], Is.EqualTo(profile.GetSandColor(2)));
        }

        [Test]
        public void CanonicalLevels_LoadIndependently()
        {
            string[] ids = { "phase_c_level_01", "phase_c_level_02", "phase_c_level_03" };
            for (int i = 0; i < ids.Length; i++)
            {
                SE001LevelJson level = LevelDataLoader.Load(ids[i]);
                Assert.That(level.levelId, Is.EqualTo(ids[i]));
                Assert.That(level.sources.Count, Is.GreaterThan(0));
                Assert.That(level.cups.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void CanonicalSequence_Is01Then02Then03()
        {
            PhaseCLevelSequence sequence = Resources.Load<PhaseCLevelSequence>("Profiles/PhaseCLevelSequence");
            Assert.That(sequence, Is.Not.Null);
            Assert.That(sequence.levels, Has.Count.EqualTo(3));
            Assert.That(sequence.levels[0].levelId, Is.EqualTo("phase_c_level_01"));
            Assert.That(sequence.levels[1].levelId, Is.EqualTo("phase_c_level_02"));
            Assert.That(sequence.levels[2].levelId, Is.EqualTo("phase_c_level_03"));
        }

        [Test]
        public void Validator_RejectsSupplyDeficit()
        {
            SE001LevelJson level = CreateFixture();
            level.sources[0].logicalAmount = 1;
            level.cups[0].requiredAmount = 2;
            Assert.That(Validate(level), Does.Contain("supply 1 is below required 2"));
        }

        [Test]
        public void Validator_RejectsUnknownColorId()
        {
            SE001LevelJson level = CreateFixture();
            level.sources[0].materialId = 99;
            Assert.That(Validate(level), Does.Contain("unknown colorId 99"));
        }

        [Test]
        public void Validator_RejectsStaticOverlap()
        {
            SE001LevelJson level = CreateFixture();
            level.staticObstacles.Add(new StaticObstacleData
            {
                stableId = "obstacle",
                contours = new List<PolygonContourData> { Square(0.75f, 1.75f, 0.5f, 0.5f) }
            });
            Assert.That(Validate(level), Does.Contain("sources[0] overlaps board wall or static obstacle"));
        }

        [Test]
        public void Validator_RejectsUnreachableCupWithoutDrawRoute()
        {
            SE001LevelJson level = CreateFixture();
            level.board.size = new Vector2(6f, 4f);
            level.board.wallContours = BorderContours(6f, 4f, 0.2f);
            level.sources[0].position = new Vector2(1f, 2f);
            level.cups[0].position = new Vector2(5f, 2f);
            level.staticObstacles.Add(new StaticObstacleData
            {
                stableId = "barrier",
                contours = new List<PolygonContourData> { Square(2.8f, 0.2f, 0.4f, 3.6f) }
            });
            Assert.That(Validate(level), Does.Contain("no free-fall path"));
        }

        private static string Validate(SE001LevelJson level)
        {
            ColorProfile profile = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            List<string> errors = new List<string>();
            LevelDataValidator.TryValidate(level, profile, 0.1f, 10000, errors);
            return string.Join(" | ", errors);
        }

        private static SE001LevelJson CreateFixture()
        {
            return new SE001LevelJson
            {
                schemaVersion = 2,
                levelId = "authoring_fixture",
                board = new BoardData
                {
                    size = new Vector2(4f, 4f),
                    wallContours = BorderContours(4f, 4f, 0.2f)
                },
                sources = new List<SourceData>
                {
                    new SourceData
                    {
                        stableId = "source",
                        materialId = 1,
                        position = new Vector2(1f, 2f),
                        logicalAmount = 2
                    }
                },
                cups = new List<CupData>
                {
                    new CupData
                    {
                        stableId = "cup",
                        acceptedMaterialId = 1,
                        position = new Vector2(3f, 2f),
                        requiredAmount = 1
                    }
                }
            };
        }

        private static List<PolygonContourData> BorderContours(float width, float height, float thickness)
        {
            return new List<PolygonContourData>
            {
                Square(0f, 0f, width, thickness),
                Square(0f, height - thickness, width, thickness),
                Square(0f, 0f, thickness, height),
                Square(width - thickness, 0f, thickness, height)
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
