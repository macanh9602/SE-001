using NUnit.Framework;
using SE001.Data;
using SE001.Editor;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Tests
{
    /// <summary>Regression: sand fell straight through the Bowl because its bottom rows had no floor.</summary>
    public sealed class BowlReceiverTests
    {
        [Test]
        public void ErodeBowlInterior_ClosesFloor_KeepsMouthOpen()
        {
            // Synthetic bowl: 40 px wide at the top, 20 px at the bottom, 20 rows (bottom-up).
            BowlRowSpan[] outer = new BowlRowSpan[20];
            for (int y = 0; y < outer.Length; y++)
            {
                float half = Mathf.Lerp(10f, 20f, y / 19f);
                outer[y] = new BowlRowSpan(20f - half, 20f + half - 1f);
            }

            BowlRowSpan[] inner = new BowlRowSpan[outer.Length];
            JarVisualAssetSetup.ErodeBowlInterior(outer, inner, 40, 3f);
            for (int y = 0; y < 3; y++) Assert.That(inner[y].IsValid, Is.False, "floor row " + y + " must be solid");
            Assert.That(inner[outer.Length - 1].IsValid, Is.True, "top row must stay open (mouth)");
            for (int y = 0; y < outer.Length; y++)
            {
                if (!inner[y].IsValid) continue;
                Assert.That(inner[y].minX, Is.GreaterThan(outer[y].minX));
                Assert.That(inner[y].maxX, Is.LessThan(outer[y].maxX));
            }
        }

        [Test]
        public void ProductionBowlProfile_HasSolidFloorRows()
        {
            BowlVisualProfile profile = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.IsBaked, Is.True);
            int firstRow = -1;
            for (int y = 0; y < profile.mainHeightPixels && firstRow < 0; y++)
                if (profile.outerRowSpans[y].IsValid) firstRow = y;
            Assert.That(firstRow, Is.GreaterThanOrEqualTo(0));
            Assert.That(profile.innerRowSpans[firstRow].IsValid, Is.False, "bottom row of the glass must be wall, not interior");
        }

        [Test]
        public void Bowl_SandPouredIntoCenter_StaysInsideAndIsCollected()
        {
            BowlVisualProfile bowlProfile = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            SandSimulationProfile simProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            try
            {
                const float cell = 0.06f;
                simProfile.cellSize = cell;
                simProfile.maxCells = 100000;
                simProfile.dispersion = 1;
                LayoutMaskSet masks = new LayoutMaskSet(200, 200);
                for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
                SandSimulation sim = new SandSimulation(simProfile, masks);

                CupData data = new CupData
                {
                    stableId = "bowl",
                    acceptedMaterialId = 1,
                    position = new Vector2(6f, 3f),
                    requiredAmount = 100000
                };
                CupDomain bowl = new CupDomain(data, cupProfile, 1, cell, null, ReceiverStyle.Bowl, bowlProfile);
                bowl.RegisterWalls(sim, cell, cupProfile.wallThickness);

                int centerX = Mathf.FloorToInt(data.position.x / cell);
                int emitY = Mathf.FloorToInt((data.position.y + 3f) / cell);
                for (int step = 0; step < 900; step++)
                {
                    if (step < 300) sim.TryEmit(centerX, emitY, 1);
                    sim.Step();
                }

                int bottomRow = Mathf.FloorToInt(data.position.y / cell);
                int leaked = 0;
                for (int y = 0; y < bottomRow; y++)
                    for (int x = 0; x < 200; x++)
                        if (sim.IsOccupied(x, y)) leaked++;
                Assert.That(leaked, Is.EqualTo(0), "no grain may fall below the Bowl");
                bowl.Collect(sim);
                Assert.That(bowl.Collected, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(simProfile);
            }
        }
    }
}
