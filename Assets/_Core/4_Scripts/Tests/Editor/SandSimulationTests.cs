using NUnit.Framework;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class SandSimulationTests
    {
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
    }
}
