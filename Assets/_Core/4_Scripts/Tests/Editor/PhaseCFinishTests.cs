using System.Collections.Generic;
using NUnit.Framework;
using SE001.Data;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.Simulation.Sand;
using SE001.System.Management;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class PhaseCFinishTests
    {
        [Test]
        public void SourceSize_UsesProfileForEverySource()
        {
            SourceProfile profile = ScriptableObject.CreateInstance<SourceProfile>();
            profile.bodySize = new Vector2(0.8f, 1.2f);
            SourceData authoredData = new SourceData
            {
                stableId = "source",
                materialId = 1,
                position = Vector2.one,
                logicalAmount = 1
            };
            SourceDomain authored = new SourceDomain(authoredData, profile, 1);
            SourceDomain fallback = new SourceDomain(
                new SourceData { stableId = "fallback", materialId = 1, logicalAmount = 1 },
                profile,
                1);

            Assert.That(authored.Size, Is.EqualTo(profile.bodySize));
            Assert.That(authored.BodyOffset, Is.EqualTo(new Vector2(0f, profile.bodySize.y * 0.5f)));
            Assert.That(fallback.Size, Is.EqualTo(profile.bodySize));
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void CupGeometry_MatchesContractExample()
        {
            CupProfile profile = ScriptableObject.CreateInstance<CupProfile>();
            CupData data = new CupData
            {
                stableId = "cup",
                acceptedMaterialId = 1,
                position = new Vector2(5.4f, 1f),
                requiredAmount = 1
            };
            CupDomain cup = new CupDomain(data, profile, 1, 0.1f);
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
            CupData data = new CupData
            {
                stableId = "cup",
                acceptedMaterialId = 1,
                position = new Vector2(5.4f, 1f),
                requiredAmount = 1
            };
            CupDomain cup = new CupDomain(data, profile, 1, 0.1f);
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
            CupData data = new CupData
            {
                stableId = "cup",
                acceptedMaterialId = 1,
                position = new Vector2(5.4f, 1f),
                requiredAmount = 1
            };
            CupDomain cup = new CupDomain(data, profile, 1, 0.1f);
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
            // A landing grain may splash sideways (powder model), but once settled it must stay put forever.
            SandSimulation sim = new SandSimulation(Profile(), OpenBoard(20, 20));
            Assert.That(sim.TryEmit(10, 10, 1), Is.True);
            for (int i = 0; i < 200; i++) sim.Step();
            byte[] settled = (byte[])sim.State.Cells.Clone();
            int moved = 0;
            for (int i = 0; i < 50; i++) moved += sim.Step();
            Assert.That(moved, Is.EqualTo(0));
            Assert.That(sim.State.Cells, Is.EqualTo(settled));
            Assert.That(sim.State.RowCount[0], Is.EqualTo(1), "Grain must rest on the floor row.");
            sim.Dispose();
        }

        [Test]
        public void Sim_ReachesStableState()
        {
            SandSimulation sim = new SandSimulation(Profile(), OpenBoard(60, 60));
            for (int i = 0; i < 40; i++) sim.EmitRegion(28, 55, 32, 55, 1);
            int lastMoved = -1;
            for (int i = 0; i < 1500; i++) { lastMoved = sim.Step(); if (i % 10 == 0) sim.EmitRegion(28, 55, 32, 55, 1); if (i > 600) break; }
            for (int i = 0; i < 2000 && (lastMoved = sim.Step()) > 0; i++) { }
            Assert.That(lastMoved, Is.EqualTo(0), "Pile never settled (endless jitter).");
            sim.Dispose();
        }

        [Test]
        public void Stroke_TruncatedAtInk()
        {
            GameObject owner = new GameObject("StrokeInkGuard");
            LevelManager manager = owner.AddComponent<LevelManager>();
            try
            {
                manager.BeginLevel("phase_c_level_01");
                GameplayManager gameplay = manager.CurrentContext.LevelRoot.GetComponent<GameplayManager>();
                List<Vector2> accepted = null;
                gameplay.StrokeCommitted += (points, thickness) => accepted = new List<Vector2>(points);
                Vector2 start = new Vector2(1f, 10f);
                Vector2 end = start + Vector2.right * gameplay.InkBudget * 2f;

                Assert.That(gameplay.CommitStroke(new[] { start, end }, 0.3f), Is.True);
                Assert.That(gameplay.InkRemaining, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(accepted, Is.Not.Null);
                Assert.That(accepted.Count, Is.EqualTo(2));
                Assert.That(Vector2.Distance(start, accepted[1]), Is.EqualTo(gameplay.InkBudget).Within(0.0001f));
            }
            finally
            {
                if (manager.CurrentContext != null)
                    manager.UnloadCurrentLevel();
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Sim_ConservesGrains_WithMomentum()
        {
            SandSimulation sim = new SandSimulation(Profile(), OpenBoard(60, 80));
            int emitted = 0;
            for (int i = 0; i < 300; i++)
            {
                emitted += sim.EmitRegion(25, 75, 35, 76, 1);
                sim.Step();
            }

            int count = 0;
            foreach (byte c in sim.State.Cells) if (c != 0) count++;
            int rows = 0;
            foreach (int r in sim.State.RowCount) rows += r;
            Assert.That(count, Is.EqualTo(emitted));
            Assert.That(rows, Is.EqualTo(emitted), "RowCount bookkeeping drifted.");
            sim.Dispose();
        }

        [Test]
        public void Sim_MomentumSlidesFurtherOnShallowRamp_ThanPureCA()
        {
            float powder = MeanRestX(Profile());
            SandSimulationProfile pure = Profile();
            pure.splash = 0f;
            pure.slide = 0f;
            float pureCa = MeanRestX(pure);
            Assert.That(powder, Is.GreaterThan(pureCa + 3f), $"powder {powder:0.0} vs pure {pureCa:0.0}");
        }

        [Test]
        public void Sim_DifferentSeeds_DifferentGrid_SameSeed_SameGrid()
        {
            LayoutMaskSet masks = OpenBoard(40, 40);
            SandSimulation a = new SandSimulation(Profile(), masks, 1u);
            SandSimulation b = new SandSimulation(Profile(), OpenBoard(40, 40), 1u);
            SandSimulation c = new SandSimulation(Profile(), OpenBoard(40, 40), 2u);
            for (int i = 0; i < 60; i++)
            {
                a.EmitRegion(18, 38, 22, 38, 1);
                b.EmitRegion(18, 38, 22, 38, 1);
                c.EmitRegion(18, 38, 22, 38, 1);
                a.Step();
                b.Step();
                c.Step();
            }

            Assert.That(b.State.Cells, Is.EqualTo(a.State.Cells));
            Assert.That(c.State.Cells, Is.Not.EqualTo(a.State.Cells));
            a.Dispose();
            b.Dispose();
            c.Dispose();
        }

        /// <summary>Drops grains onto a shallow (~1:4) ramp and returns the mean distance (cells) from the drop column where they rest.</summary>
        private static float MeanRestX(SandSimulationProfile profile)
        {
            LayoutMaskSet masks = OpenBoard(120, 60);
            SandSimulation sim = new SandSimulation(profile, masks, 7u);
            for (int x = 0; x < 120; x++)
            {
                int ramp = 40 - x / 4;
                for (int y = 0; y <= Mathf.Max(0, ramp); y++) sim.SetCupWall(x, y, true);
            }

            for (int i = 0; i < 20; i++) sim.TryEmit(10 + (i % 3), 58, 1);
            for (int i = 0; i < 1500; i++) sim.Step();
            float sum = 0f;
            int n = 0;
            for (int i = 0; i < sim.State.Cells.Length; i++)
                if (sim.State.Cells[i] != 0)
                {
                    sum += Mathf.Abs(i % sim.State.Width - 11f);
                    n++;
                }
            sim.Dispose();
            return n > 0 ? sum / n : 0f;
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
