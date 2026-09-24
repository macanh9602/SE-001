using NUnit.Framework;
using SE001.Data;
using SE001.Editor.Level;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class LevelTuningTests
    {
        [TestCase(3)]
        [TestCase(4)]
        public void LegacyLevel_UpgradesWithProfileValues_AndRoundTrips(int schema)
        {
            SourceProfile profile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            Assert.That(profile, Is.Not.Null);
            SE001LevelJson legacy = new SE001LevelJson { schemaVersion = schema, levelId = "legacy" };
            SE001LevelJson loaded = SE001LevelJson.FromJson(legacy.ToJson());
            Assert.That(LevelTuning.SourceSize(loaded, profile), Is.EqualTo(profile.bodySize));
            Assert.That(LevelTuning.EmissionRate(loaded, profile), Is.EqualTo(profile.emissionRate));

            LevelTuning.UpgradeToSchema5(loaded, profile);
            SE001LevelJson reopened = SE001LevelJson.FromJson(loaded.ToJson());
            Assert.That(reopened.schemaVersion, Is.EqualTo(5));
            Assert.That(reopened.sourceScale, Is.EqualTo(1f));
            Assert.That(reopened.bowlScale, Is.EqualTo(1f));
            Assert.That(reopened.sourceEmissionRate, Is.EqualTo(profile.emissionRate));
            Assert.That(reopened.sourceStreamWidth, Is.EqualTo(profile.streamWidth));
        }

        [Test]
        public void SourceScale_ChangesBodyAndHitArea_ButNotNozzleOrStreamTuning()
        {
            SourceProfile profile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            JarVisualProfile art = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            SE001LevelJson level = new SE001LevelJson
            {
                sourceScale = 1.5f,
                sourceEmissionRate = profile.emissionRate * 2f,
                sourceStreamWidth = profile.streamWidth + 2
            };
            SourceData data = new SourceData { stableId = "source", materialId = 1, logicalAmount = 5,
                position = new Vector2(3f, 8f) };
            SourceDomain source = new SourceDomain(data, profile, 1, art, level);

            Assert.That(source.Position, Is.EqualTo(data.position));
            Assert.That(source.Size, Is.EqualTo(profile.bodySize * 1.5f));
            Assert.That(LevelTuning.EmissionRate(level, profile), Is.EqualTo(profile.emissionRate * 2f));
            Assert.That(LevelTuning.StreamWidth(level, profile), Is.EqualTo(profile.streamWidth + 2));
            Assert.That(LevelEditorGeometry.SourceSize(data, level), Is.EqualTo(source.Size));
            Assert.That(source.HitTest(source.Position + source.BodyOffset + new Vector2(source.Size.x * 0.49f, 0f), 0f), Is.True);
        }

        [Test]
        public void BowlScale_ChangesCapacity_WhileRequiredGrainsStayFixed()
        {
            BowlVisualProfile bowl = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            Assert.That(bowl, Is.Not.Null);
            const float cell = 0.06f;
            int grainsPerUnit = CupDomain.GrainsPerUnitFor(115, ReceiverStyle.Bowl, bowl, cell);
            CupData data = new CupData { stableId = "bowl", acceptedMaterialId = 1,
                position = new Vector2(5f, 2f), requiredAmount = 5 };
            SE001LevelJson normal = new SE001LevelJson { bowlScale = 1f };
            SE001LevelJson large = new SE001LevelJson { bowlScale = 1.5f };
            CupDomain smallBowl = new CupDomain(data, cupProfile, grainsPerUnit, cell, null, ReceiverStyle.Bowl, bowl, normal);
            CupDomain largeBowl = new CupDomain(data, cupProfile, grainsPerUnit, cell, null, ReceiverStyle.Bowl, bowl, large);

            Assert.That(largeBowl.Required, Is.EqualTo(smallBowl.Required));
            Assert.That(largeBowl.Capacity, Is.GreaterThan(smallBowl.Capacity));
            Assert.That(largeBowl.Size, Is.EqualTo(LevelEditorGeometry.CupSize(data, large)));
            Assert.That(largeBowl.Position, Is.EqualTo(data.position));
        }

        [Test]
        public void SourceStream_UsesLevelRateAndWidth_IndependentOfScale()
        {
            SourceProfile sourceProfile = ScriptableObject.CreateInstance<SourceProfile>();
            SandSimulationProfile sandProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            sandProfile.cellSize = 0.1f;
            sandProfile.maxCells = 10000;
            LayoutMaskSet masks = new LayoutMaskSet(100, 100);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            SandSimulation narrowSimulation = new SandSimulation(sandProfile, masks);
            SandSimulation wideSimulation = new SandSimulation(sandProfile, masks);
            try
            {
                SourceData data = new SourceData { stableId = "source", materialId = 1,
                    position = new Vector2(5f, 5f), logicalAmount = 100, startsOpen = true };
                SE001LevelJson narrow = new SE001LevelJson { sourceScale = 1f,
                    sourceEmissionRate = 20f, sourceStreamWidth = 1 };
                SE001LevelJson wide = new SE001LevelJson { sourceScale = 2f,
                    sourceEmissionRate = 20f, sourceStreamWidth = 5 };
                SourceDomain narrowSource = new SourceDomain(data, sourceProfile, 1, null, narrow);
                SourceDomain wideSource = new SourceDomain(data, sourceProfile, 1, null, wide);
                int narrowEmitted = narrowSource.Emit(narrowSimulation, 1f / 60f);
                int wideEmitted = wideSource.Emit(wideSimulation, 1f / 60f);

                Assert.That(narrowEmitted, Is.LessThan(wideEmitted));
                Assert.That(narrowSource.Position, Is.EqualTo(wideSource.Position));
                Assert.That(wideSource.Size, Is.EqualTo(narrowSource.Size * 2f));
            }
            finally
            {
                narrowSimulation.Dispose();
                wideSimulation.Dispose();
                Object.DestroyImmediate(sourceProfile);
                Object.DestroyImmediate(sandProfile);
            }
        }
    }
}
