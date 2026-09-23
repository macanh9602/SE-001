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

        [TestCase(2.0f)]
        [TestCase(3.52f)]
        [TestCase(4.0f)]
        public void BowlUnits_FullBowlTarget_MatchesMeasuredCapacity(float width)
        {
            // Screenshot 2026-09-24: "cup 5" was full at ~60 % of the Bowl. Now N = unitsPerFullBowl units fill
            // fullFillFraction of the real collision capacity at any width.
            BowlVisualProfile source = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            BowlVisualProfile bowl = Object.Instantiate(source);
            try
            {
                const float cell = 0.06f;
                bowl.defaultWorldWidth = width;
                bowl.unitsPerFullBowl = 5f;
                bowl.fullFillFraction = 0.9f;
                Assert.That(CupDomain.GrainsPerUnitFor(115, ReceiverStyle.Cup, bowl, cell), Is.EqualTo(115), "Cup style unchanged");
                int grainsPerUnit = CupDomain.GrainsPerUnitFor(115, ReceiverStyle.Bowl, bowl, cell);
                CupData data = new CupData { stableId = "bowl", acceptedMaterialId = 1, position = new Vector2(5.03f, 3.01f), requiredAmount = 5 };
                CupDomain placed = new CupDomain(data, cupProfile, grainsPerUnit, cell, null, ReceiverStyle.Bowl, bowl);
                float fill = placed.Required / (float)placed.Capacity;
                Assert.That(fill, Is.InRange(0.84f, 0.92f), "5 units should fill ~90 % of the Bowl at width " + width);
            }
            finally
            {
                Object.DestroyImmediate(bowl);
            }
        }

        [TestCase(2.0f)]
        [TestCase(3.52f)]
        [TestCase(4.0f)]
        public void Bowl_MouthStaysOpen_SandIsCollectedAtAnyWidth(float width)
        {
            // Width 4 regression: the leak seal capped the top sink row, so sand piled on the rim instead of entering.
            BowlVisualProfile source = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            BowlVisualProfile bowlProfile = Object.Instantiate(source);
            SandSimulationProfile simProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            try
            {
                const float cell = 0.06f;
                bowlProfile.defaultWorldWidth = width;
                simProfile.cellSize = cell;
                simProfile.maxCells = 100000;
                LayoutMaskSet masks = new LayoutMaskSet(160, 160);
                for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
                SandSimulation sim = new SandSimulation(simProfile, masks);
                CupData data = new CupData { stableId = "bowl", acceptedMaterialId = 1, position = new Vector2(4.8f, 2f), requiredAmount = 100000 };
                CupDomain bowl = new CupDomain(data, cupProfile, 1, cell, null, ReceiverStyle.Bowl, bowlProfile);
                bowl.RegisterWalls(sim, cell, cupProfile.wallThickness);

                int centerX = Mathf.FloorToInt(data.position.x / cell);
                int emitY = Mathf.FloorToInt((data.position.y + bowl.Size.y + 1f) / cell);
                int budget = bowl.Capacity / 3;
                int emitted = 0;
                for (int step = 0; step < 1500; step++)
                {
                    if (emitted < budget && sim.TryEmit(centerX, emitY, 1)) emitted++;
                    sim.Step();
                }

                bowl.Collect(sim);
                Assert.That(emitted, Is.GreaterThan(0));
                Assert.That(bowl.Collected, Is.GreaterThanOrEqualTo(emitted * 9 / 10), "sand must fall into the Bowl, not rest on the rim");
            }
            finally
            {
                Object.DestroyImmediate(bowlProfile);
                Object.DestroyImmediate(simProfile);
            }
        }

        [Test]
        public void ProductionBowl_RimSitsUnderTheLip()
        {
            // Sand opening must reach the drawn lip (~88 % of the art height), not stop ~10 % lower.
            BowlVisualProfile profile = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            Assert.That(profile.rimPixelY, Is.GreaterThanOrEqualTo(Mathf.RoundToInt(profile.mainHeightPixels * 0.84f)));
            Assert.That(profile.rimPixelY, Is.LessThan(profile.mainHeightPixels - 4));
        }

        [Test]
        public void FullBowl_KeepsMouthOpen_ExtraSandStillEnters()
        {
            BowlVisualProfile bowlProfile = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            SandSimulationProfile simProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            try
            {
                const float cell = 0.06f;
                simProfile.cellSize = cell;
                simProfile.maxCells = 100000;
                LayoutMaskSet masks = new LayoutMaskSet(200, 200);
                for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
                SandSimulation sim = new SandSimulation(simProfile, masks);
                CupData data = new CupData { stableId = "bowl", acceptedMaterialId = 1, position = new Vector2(6f, 3f), requiredAmount = 40 };
                CupDomain bowl = new CupDomain(data, cupProfile, 1, cell, null, ReceiverStyle.Bowl, bowlProfile);
                bowl.RegisterWalls(sim, cell, cupProfile.wallThickness);
                int centerX = Mathf.FloorToInt(data.position.x / cell);
                int emitY = Mathf.FloorToInt((data.position.y + bowl.Size.y + 1f) / cell);
                int inside = 0;
                for (int step = 0; step < 1200; step++)
                {
                    if (step < 200) sim.TryEmit(centerX, emitY, 1);
                    sim.Step();
                    bowl.Collect(sim);
                }

                Assert.That(bowl.Full, Is.True);
                int bottomRow = Mathf.FloorToInt(data.position.y / cell);
                int topRow = Mathf.FloorToInt(bowl.FillLineY / cell);
                for (int y = bottomRow; y <= topRow; y++)
                    for (int x = 0; x < 200; x++)
                        if (sim.IsOccupied(x, y)) inside++;
                Assert.That(inside, Is.GreaterThan(bowl.Required + 100), "sand after Full must keep entering the Bowl (no lid)");
            }
            finally
            {
                Object.DestroyImmediate(simProfile);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StreamOnLeftLip_RimAssist_SendsSandInside(bool assist)
        {
            // Movie_013: a stroke tip right above the lip split the stream and half fell outside the Bowl.
            BowlVisualProfile source = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            BowlVisualProfile bowlProfile = Object.Instantiate(source);
            SandSimulationProfile simProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            try
            {
                const float cell = 0.06f;
                bowlProfile.defaultWorldWidth = 2f;
                bowlProfile.rimAssist = assist;
                simProfile.cellSize = cell;
                simProfile.maxCells = 100000;
                LayoutMaskSet masks = new LayoutMaskSet(160, 160);
                for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
                SandSimulation sim = new SandSimulation(simProfile, masks);
                CupData data = new CupData { stableId = "bowl", acceptedMaterialId = 1, position = new Vector2(4.8f, 3f), requiredAmount = 100000 };
                CupDomain bowl = new CupDomain(data, cupProfile, 1, cell, null, ReceiverStyle.Bowl, bowlProfile);
                bowl.RegisterWalls(sim, cell, cupProfile.wallThickness);

                // Topmost wall cell on the left lip = where the stream lands.
                int lipX = -1;
                int lipY = -1;
                for (int y = 159; y >= 0 && lipX < 0; y--)
                    for (int x = 0; x < 80 && lipX < 0; x++)
                        if (sim.State.CupWallMask[sim.State.Index(x, y)])
                        {
                            lipX = x;
                            lipY = y;
                        }

                Assert.That(lipX, Is.GreaterThanOrEqualTo(0));
                int emitY = lipY + 20;
                int emitted = 0;
                for (int step = 0; step < 900; step++)
                {
                    if (emitted < 120 && sim.TryEmit(lipX, emitY, 1)) emitted++;
                    sim.Step();
                }

                int bottomRow = Mathf.FloorToInt(data.position.y / cell);
                int outside = 0;
                for (int y = 0; y < 160; y++)
                    for (int x = 0; x < lipX; x++)
                        if (sim.IsOccupied(x, y)) outside++;
                for (int y = 0; y < bottomRow; y++)
                    for (int x = lipX; x < 160; x++)
                        if (sim.IsOccupied(x, y)) outside++;
                if (assist) Assert.That(outside, Is.LessThanOrEqualTo(emitted / 10), "rim assist: sand hitting the lip goes in");
                else Assert.Pass("baseline without assist: " + outside + "/" + emitted + " grains fell outside");
            }
            finally
            {
                Object.DestroyImmediate(bowlProfile);
                Object.DestroyImmediate(simProfile);
            }
        }

        [TestCase(2.0f)]
        [TestCase(1.6f)]
        public void SmallBowl_PouredAtWall_DoesNotLeak(float width)
        {
            // Movie_011: at width 2 the glass is ~1 cell thick and sand leaked through the curved wall.
            BowlVisualProfile source = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            BowlVisualProfile bowlProfile = Object.Instantiate(source);
            SandSimulationProfile simProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            try
            {
                const float cell = 0.06f;
                bowlProfile.defaultWorldWidth = width;
                simProfile.cellSize = cell;
                simProfile.maxCells = 100000;
                LayoutMaskSet masks = new LayoutMaskSet(160, 160);
                for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
                SandSimulation sim = new SandSimulation(simProfile, masks);
                CupData data = new CupData { stableId = "bowl", acceptedMaterialId = 1, position = new Vector2(4.8f, 3f), requiredAmount = 100000 };
                CupDomain bowl = new CupDomain(data, cupProfile, 1, cell, null, ReceiverStyle.Bowl, bowlProfile);
                bowl.RegisterWalls(sim, cell, cupProfile.wallThickness);

                // Pour near the left and right glass, where the staircase is steepest.
                int left = Mathf.FloorToInt((data.position.x - width * 0.3f) / cell);
                int right = Mathf.FloorToInt((data.position.x + width * 0.3f) / cell);
                int emitY = Mathf.FloorToInt((data.position.y + 2f) / cell);
                // Half the capacity: no overflow over the rim, so anything below the Bowl is a real leak.
                int budget = bowl.Capacity / 2;
                int emitted = 0;
                for (int step = 0; step < 1500; step++)
                {
                    if (emitted < budget && sim.TryEmit(step % 2 == 0 ? left : right, emitY, 1)) emitted++;

                    sim.Step();
                }

                int bottomRow = Mathf.FloorToInt(data.position.y / cell);
                int leaked = 0;
                for (int y = 0; y < bottomRow; y++)
                    for (int x = 0; x < 160; x++)
                        if (sim.IsOccupied(x, y)) leaked++;
                Assert.That(leaked, Is.EqualTo(0), "no grain may pass through the Bowl glass at width " + width);
            }
            finally
            {
                Object.DestroyImmediate(bowlProfile);
                Object.DestroyImmediate(simProfile);
            }
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
