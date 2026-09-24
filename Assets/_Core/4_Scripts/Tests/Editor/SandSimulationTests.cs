using NUnit.Framework;
using SE001.Elements.Sand;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class SandSimulationTests
    {
        [Test]
        public void BowlLevelingStride_ReachesLowPocketSooner_WithoutCrossingWalls()
        {
            int slowX = RunBowlLevelingStride(1, 0);
            int fastX = RunBowlLevelingStride(3, 0);
            Assert.That(slowX, Is.InRange(11, 16));
            Assert.That(fastX, Is.GreaterThanOrEqualTo(slowX + 5));
            Assert.That(fastX, Is.LessThanOrEqualTo(30));
        }

        [Test]
        public void BowlExtraPass_AdvancesSettlingSandWithoutGlobalExtraStep()
        {
            int onePass = RunBowlLevelingStride(1, 0);
            int twoPasses = RunBowlLevelingStride(1, 1);
            Assert.That(twoPasses, Is.GreaterThan(onePass));
            Assert.That(twoPasses, Is.LessThanOrEqualTo(30));
        }

        private static int RunBowlLevelingStride(int cellsPerStep, int extraPasses)
        {
            LayoutMaskSet masks = OpenMasks(40, 10);
            for (int x = 0; x < 40; x++)
                if (x != 30) masks.StaticMask[masks.Index(x, 5)] = true;
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.maxCells = 400;
            profile.bowlCreepChance = 1f;
            profile.bowlCreepReach = 32;
            profile.bowlLevelingCellsPerStep = cellsPerStep;
            profile.bowlLevelingExtraPasses = extraPasses;
            SandSimulation simulation = new SandSimulation(profile, masks, 123u);
            try
            {
                for (int x = 8; x <= 30; x++) simulation.SetBowlFlowCell(x, 6);
                Assert.That(simulation.TryEmit(10, 6, 1), Is.True);
                for (int step = 0; step < 5; step++) simulation.Step();
                for (int x = 0; x < 40; x++)
                    if (simulation.IsOccupied(x, 6)) return x;
                Assert.Fail("Grain unexpectedly left the settling row.");
                return -1;
            }
            finally { simulation.Dispose(); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void SandFallsWithoutEnteringInvalidOrStaticCells()
        {
            LayoutMaskSet masks = new LayoutMaskSet(4, 4);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            masks.ValidMask[masks.Index(1, 0)] = false;
            masks.StaticMask[masks.Index(1, 0)] = true;
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.maxCells = 100;
            SandSimulation simulation = new SandSimulation(profile, masks);
            try
            {
                Assert.That(simulation.TryEmit(1, 3, 1), Is.True);
                simulation.Step();
                simulation.Step();
                simulation.Step();
                Assert.That(simulation.State.OccupiedCount, Is.EqualTo(1));
                Assert.That(simulation.State.Cells[masks.Index(1, 0)], Is.EqualTo(0));
            }
            finally { simulation.Dispose(); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void AirborneGrain_WithMomentum_FallsStraight()
        {
            LayoutMaskSet masks = OpenMasks(20, 60);
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.maxCells = 20 * 60;
            SandSimulation simulation = new SandSimulation(profile, masks);
            try
            {
                Assert.That(simulation.TryEmit(10, 59, 1), Is.True);
                simulation.State.Momentum[masks.Index(10, 59)] = 2f; // max sideways momentum, as after rolling off an obstacle
                for (int step = 0; step < 200 && !simulation.IsOccupied(10, 0); step++)
                {
                    simulation.Step();
                    for (int x = 0; x < 20; x++)
                        if (x != 10)
                            for (int y = 1; y < 60; y++)
                                Assert.That(simulation.IsOccupied(x, y), Is.False, $"grain drifted sideways to x={x} while airborne (step {step})");
                }

                Assert.That(simulation.IsOccupied(10, 0), Is.True, "grain should land straight below its emit point");
            }
            finally { simulation.Dispose(); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void StreamOverlay_PaintsOnlyEmptyNonGeometryCells_AndNeverTouchesState()
        {
            LayoutMaskSet masks = OpenMasks(10, 40);
            for (int y = 0; y < 40; y++) masks.StaticMask[masks.Index(6, y)] = true; // wall right next to the stream
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.maxCells = 400;
            SandSimulation simulation = new SandSimulation(profile, masks);
            GameObject host = new GameObject("SandFieldVisualTest", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                Assert.That(simulation.TryEmit(5, 20, 1), Is.True);
                simulation.State.Velocity[masks.Index(5, 20)] = 3f; // airborne, falling fast
                byte[] cellsBefore = (byte[])simulation.State.Cells.Clone();

                SandFieldVisual visual = host.AddComponent<SandFieldVisual>();
                visual.Bind(simulation);
                visual.UpdateTexture();

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                host.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                Texture2D texture = (Texture2D)block.GetTexture("_BaseMap");
                Assert.That(texture, Is.Not.Null);

                Assert.That(texture.GetPixel(5, 21).a, Is.GreaterThan(0f), "streak should paint the empty cell above an airborne grain");
                Assert.That(texture.GetPixel(6, 21).a, Is.EqualTo(0f), "stream must never paint over geometry");
                Assert.That(texture.GetPixel(5, 30).a, Is.EqualTo(0f), "streak length is bounded by fall speed");
                Assert.That(simulation.State.Cells, Is.EqualTo(cellsBefore), "visual is presentation-only");
                Assert.That(simulation.State.OccupiedCount, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(host); simulation.Dispose(); Object.DestroyImmediate(profile); }
        }

        private static LayoutMaskSet OpenMasks(int width, int height)
        {
            LayoutMaskSet masks = new LayoutMaskSet(width, height);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            return masks;
        }
    }
}
