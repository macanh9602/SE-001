#if UNITY_EDITOR
using NUnit.Framework;
using SE001.Data;
using SE001.Simulation.Sand;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Tests.Editor
{
    public sealed class LogicalSandScaleTests
    {
        [Test]
        public void LogicalAmounts_ConvertThroughSingleSandProfileScale()
        {
            SandSimulationProfile sand =
                Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            SourceProfile sourceProfile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            JarVisualProfile jar = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");

            Assert.That(sand, Is.Not.Null);
            Assert.That(sourceProfile, Is.Not.Null);
            Assert.That(cupProfile, Is.Not.Null);
            Assert.That(jar, Is.Not.Null);
            Assert.That(sand.grainsPerUnit, Is.EqualTo(115));

            SourceDomain source10 = new SourceDomain(
                new SourceData { stableId = "source_10", logicalAmount = 10 },
                sourceProfile,
                sand.grainsPerUnit,
                jar);
            CupDomain cup5 = new CupDomain(
                new CupData
                {
                    stableId = "cup_5",
                    acceptedMaterialId = 1,
                    position = new Vector2(4f, 2f),
                    requiredAmount = 5
                },
                cupProfile,
                sand.grainsPerUnit,
                sand.cellSize,
                jar);

            Assert.That(source10.Initial, Is.EqualTo(10 * sand.grainsPerUnit));
            Assert.That(cup5.Required, Is.EqualTo(5 * sand.grainsPerUnit));
            Assert.That(cup5.Required, Is.LessThanOrEqualTo(cup5.Capacity));
        }

        [Test]
        public void CupRequiredAmount_RemainsAuthoredAndIsNotHardCoded()
        {
            SandSimulationProfile sand =
                Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            JarVisualProfile jar = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");

            CupDomain cup3 = new CupDomain(
                new CupData
                {
                    stableId = "cup_3",
                    acceptedMaterialId = 1,
                    position = new Vector2(4f, 2f),
                    requiredAmount = 3
                },
                cupProfile,
                sand.grainsPerUnit,
                sand.cellSize,
                jar);
            CupDomain cup5 = new CupDomain(
                new CupData
                {
                    stableId = "cup_5",
                    acceptedMaterialId = 1,
                    position = new Vector2(4f, 2f),
                    requiredAmount = 5
                },
                cupProfile,
                sand.grainsPerUnit,
                sand.cellSize,
                jar);

            Assert.That(cup3.Required, Is.EqualTo(3 * sand.grainsPerUnit));
            Assert.That(cup5.Required, Is.EqualTo(5 * sand.grainsPerUnit));
            Assert.That(cup3.Required, Is.Not.EqualTo(cup5.Required));
        }
    }
}
#endif