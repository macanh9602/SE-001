using NUnit.Framework;
using SE001.Data;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.Simulation.Sand;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class PhaseCFinishTests
    {
        [Test]
        public void CupGeometry_MatchesContractExample()
        {
            CupProfile profile = ScriptableObject.CreateInstance<CupProfile>();
            CupDomain cup = new CupDomain(new CupData { stableId = "cup", acceptedMaterialId = 1, position = new Vector2(5.4f, 1f), size = new Vector2(2f, 1.5f), requiredAmount = 1 }, profile, 1, 0.1f);
            Assert.That(cup.EffectiveWall, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(cup.MinY, Is.EqualTo(12));
            Assert.That(cup.FillLineY, Is.GreaterThan(1.25f));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Cup_SandStaysAndCountsTowardFillLine()
        {
            CupProfile profile = ScriptableObject.CreateInstance<CupProfile>();
            LayoutMaskSet masks = OpenBoard(120, 200);
            SandSimulation sim = new SandSimulation(Profile(), masks);
            CupDomain cup = new CupDomain(new CupData { stableId = "cup", acceptedMaterialId = 1, position = new Vector2(5.4f, 1f), size = new Vector2(2f, 1.5f), requiredAmount = 1 }, profile, 1, 0.1f);
            cup.RegisterWalls(sim, 0.1f, profile.wallThickness);
            Assert.That(sim.TryEmit(54, 12, 1), Is.True);
            cup.Collect(sim);
            Assert.That(cup.Collected, Is.EqualTo(1));
            Assert.That(sim.IsOccupied(54, 12), Is.True);
            sim.Dispose();
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Cup_FullClosesMouth()
        {
            CupProfile profile = ScriptableObject.CreateInstance<CupProfile>();
            SandSimulation sim = new SandSimulation(Profile(), OpenBoard(120, 200));
            CupDomain cup = new CupDomain(new CupData { stableId = "cup", acceptedMaterialId = 1, position = new Vector2(5.4f, 1f), size = new Vector2(2f, 1.5f), requiredAmount = 1 }, profile, 1, 0.1f);
            cup.RegisterWalls(sim, 0.1f, profile.wallThickness);
            for (int y = cup.MinY; y <= cup.MaxY; y++) for (int x = 40; x < 70; x++) sim.TryEmit(x, y, 1);
            cup.Collect(sim);
            Assert.That(cup.Full, Is.True);
            sim.Dispose();
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Sim_NoLateralJitterOnFlatSurface()
        {
            SandSimulation sim = new SandSimulation(Profile(), OpenBoard(20, 20));
            Assert.That(sim.TryEmit(10, 10, 1), Is.True);
            for (int i = 0; i < 30; i++) sim.Step();
            Assert.That(sim.IsOccupied(10, 0), Is.True);
            sim.Dispose();
        }

        [Test]
        public void Sim_Deterministic_SameSeedSameGrid()
        {
            SandSimulation a = new SandSimulation(Profile(), OpenBoard(20, 20));
            SandSimulation b = new SandSimulation(Profile(), OpenBoard(20, 20));
            a.EmitRegion(8, 12, 12, 12, 1);
            b.EmitRegion(8, 12, 12, 12, 1);
            for (int i = 0; i < 20; i++) { a.Step(); b.Step(); }
            Assert.That(a.State.Cells, Is.EqualTo(b.State.Cells));
            a.Dispose();
            b.Dispose();
        }

        [Test]
        public void PrefabProfile_BindsPhaseCVisualPrefabs()
        {
            PrefabProfile profile = Resources.Load<PrefabProfile>("Profiles/PhaseBPrefabProfile");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.sourcePrefab, Is.Not.Null);
            Assert.That(profile.cupPrefab, Is.Not.Null);
            Assert.That(profile.drawStrokePrefab, Is.Not.Null);
            Assert.That(profile.sourcePrefab.GetComponent<PhaseCSourceVisual>(), Is.Not.Null);
            Assert.That(profile.cupPrefab.GetComponent<PhaseCCupVisual>(), Is.Not.Null);
            Assert.That(profile.drawStrokePrefab.GetComponent<PhaseCDrawStrokeVisual>(), Is.Not.Null);
        }

        private static SandSimulationProfile Profile()
        {
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.cellSize = 0.1f;
            profile.maxCells = 100000;
            profile.dispersion = 1;
            return profile;
        }

        private static LayoutMaskSet OpenBoard(int width, int height)
        {
            LayoutMaskSet masks = new LayoutMaskSet(width, height);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            return masks;
        }
    }
}
