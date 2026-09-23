#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using SE001.Data;
using SE001.Editor.Level;
using SE001.Geometry;
using UnityEngine;

namespace SE001.Tests
{

    public sealed class LevelEditorTests
    {
        [Test]
        public void StableIds_RepairMissingAndDuplicateIds()
        {
            SE001LevelJson level = new SE001LevelJson();
            level.sources.Add(new SourceData { stableId = "source_shared" });
            level.sources.Add(new SourceData { stableId = "source_shared" });
            level.cups.Add(new CupData { stableId = string.Empty });

            int changed = LevelEditorStableIds.NormalizeInMemory(level);

            Assert.That(changed, Is.EqualTo(2));
            string[] ids = level.sources.Select(value => value.stableId)
                .Concat(level.cups.Select(value => value.stableId)).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            Assert.That(ids.All(value => !string.IsNullOrWhiteSpace(value)), Is.True);
        }

        [Test]
        public void LevelNames_FollowProgressionWithoutRenamingLegacyIds()
        {
            PhaseCLevelSequence sequence = ScriptableObject.CreateInstance<PhaseCLevelSequence>();
            try
            {
                sequence.levels.Add(new LevelSequenceEntry { levelId = "phase_c_level_01" });
                sequence.levels.Add(new LevelSequenceEntry { levelId = "phase_c_level_02" });
                sequence.levels.Add(new LevelSequenceEntry { levelId = "phase_c_level_03" });

                Assert.That(LevelEditorDocumentService.DisplayName(sequence, "phase_c_level_01"), Is.EqualTo("Level_01"));
                string nextId = LevelEditorDocumentService.NextLevelId(sequence);
                Assert.That(nextId, Does.StartWith("Level_"));
                Assert.That(int.Parse(nextId.Substring("Level_".Length)), Is.GreaterThanOrEqualTo(4));
                Assert.That(LevelEditorDocumentService.DisplayName(sequence, "Level_04"), Is.EqualTo("Level_04"));
                Assert.That(LevelEditorDocumentService.AddToSequenceIfMissing(sequence, nextId), Is.True);
                Assert.That(LevelEditorDocumentService.AddToSequenceIfMissing(sequence, nextId), Is.False);
                Assert.That(sequence.levels.Count, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(sequence);
            }
        }

        [Test]
        public void Schema3_DoesNotSerializeLegacyGeometry()
        {
            SE001LevelJson level = new SE001LevelJson { schemaVersion = 3, levelId = "editor_test", layoutId = "layout_test" };
            level.board.wallContours.Add(new PolygonContourData());
            level.staticObstacles.Add(new StaticObstacleData { stableId = "legacy" });

            string json = level.ToJson();

            Assert.That(json, Does.Not.Contain("wallContours"));
            Assert.That(json, Does.Not.Contain("staticObstacles"));
            Assert.That(json, Does.Contain("layoutId"));
        }

        [Test]
        public void FootprintOpen_RejectsBakedStaticCell()
        {
            LayoutMaskSet masks = new LayoutMaskSet(10, 10);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            masks.StaticMask[masks.Index(5, 5)] = true;
            masks.ValidMask[masks.Index(5, 5)] = false;

            Assert.That(LevelEditorGeometry.FootprintOpen(masks, new Vector2(0.25f, 0.25f), new Vector2(0.2f, 0.2f), 0.1f), Is.True);
            Assert.That(LevelEditorGeometry.FootprintOpen(masks, new Vector2(0.55f, 0.55f), new Vector2(0.2f, 0.2f), 0.1f), Is.False);
        }

        [Test]
        public void Validation_ReportsMissingLayoutAndSourceCupOverlap()
        {
            SE001LevelJson level = new SE001LevelJson
            {
                levelId = "editor_test",
                layoutId = string.Empty,
                board = new BoardData { size = new Vector2(4f, 4f) }
            };
            level.sources.Add(new SourceData
            {
                stableId = "source_1",
                position = new Vector2(2f, 2f),
                materialId = 1,
                logicalAmount = 10
            });
            level.cups.Add(new CupData
            {
                stableId = "cup_1",
                position = new Vector2(2.2f, 2f),
                acceptedMaterialId = 1,
                requiredAmount = 5
            });

            List<LevelEditorIssue> issues = new List<LevelEditorIssue>();
            LevelEditorValidation.Rebuild(level, null, null, null, 0.1f, issues);

            Assert.That(issues.Any(value => value.What == "No layout is selected."), Is.True);
            Assert.That(issues.Any(value => value.What == "Source and Cup overlap."), Is.True);
        }
    }
}
#endif
