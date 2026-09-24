using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SE001.Data;
using SE001.Editor.Level;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.Simulation.Sand;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class BowlOverflowProbeTests
    {
        [Test]
        public void Level01_TwelveUnits_FullGameplay_ReachesWin()
        {
            GameObject owner = new GameObject("BowlOverflowRegression");
            LevelManager manager = owner.AddComponent<LevelManager>();
            try
            {
                manager.BeginLevel("Level_01");
                Assert.That(manager.IsReady, Is.True);
                GameplayManager game = manager.CurrentContext.LevelRoot.GetComponent<GameplayManager>();
                game.ToggleSource("source_0cf56b7f7e42");
                game.AdvanceSteps(82);
                game.ToggleSource("source_a43b13710b84");
                int remainingSteps = 1500;
                while (game.State == GameState.Playing && remainingSteps > 0)
                {
                    game.AdvanceSteps(100);
                    remainingSteps -= 100;
                }

                Assert.That(game.State, Is.EqualTo(GameState.Won),
                    "Both 12-unit Bowls must fill before sand settles or spills.");
                Assert.That(game.Cups.All(cup => cup.Full), Is.True);
            }
            finally
            {
                if (manager.CurrentContext != null) manager.UnloadCurrentLevel();
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void LevelEditor_BlocksTwelveUnits_WhenBowlIsTooSmall()
        {
            SE001LevelJson level = LevelDataLoader.Load("Level_01");
            SandSimulationProfile sand = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            ColorProfile colors = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            LayoutDefinition layout = LevelDataLoader.LoadLayout(level.layoutId);
            LayoutMaskSet masks;
            string error;
            Assert.That(layout.TryBuildMaskSet(sand.cellSize, sand.maxCells, out masks, out error), Is.True, error);
            var issues = new List<LevelEditorIssue>();

            level.bowlScale = 1.5f;
            LevelEditorValidation.Rebuild(level, layout, masks, colors, sand.cellSize, issues);
            Assert.That(issues.Count(issue => issue.What.Contains("safe fill capacity")), Is.EqualTo(2));

            level.bowlScale = 1.7f;
            LevelEditorValidation.Rebuild(level, layout, masks, colors, sand.cellSize, issues);
            Assert.That(issues.Any(issue => issue.What.Contains("safe fill capacity")), Is.False);
        }

        [Test]
        public void Level01_TwelveUnits_RejectsUnsafeScale_AndFillsAtAuthoredScale()
        {
            SE001LevelJson level = LevelDataLoader.Load("Level_01");
            SandSimulationProfile sand = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            SourceProfile sourceProfile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            CupProfile cupProfile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            BowlVisualProfile bowlProfile = Resources.Load<BowlVisualProfile>("Profiles/BowlVisualProfile");
            LayoutDefinition layout = LevelDataLoader.LoadLayout(level.layoutId);
            float authoredScale = level.bowlScale;
            Assert.That(authoredScale, Is.GreaterThan(1.5f));
            foreach (float scale in new[] { 1.5f, authoredScale })
            {
                level.bowlScale = scale;
                LayoutMaskSet masks;
                string error;
                Assert.That(layout.TryBuildMaskSet(sand.cellSize, sand.maxCells, out masks, out error), Is.True, error);
                SandSimulation simulation = new SandSimulation(sand, masks, SandSimulation.SeedFrom(level.levelId));
                try
                {
                    int grainsPerUnit = CupDomain.GrainsPerUnitFor(sand.grainsPerUnit, ReceiverStyle.Bowl,
                        bowlProfile, sand.cellSize);
                    CupDomain[] cups = new CupDomain[level.cups.Count];
                    for (int i = 0; i < cups.Length; i++)
                    {
                        cups[i] = new CupDomain(level.cups[i], cupProfile, grainsPerUnit, sand.cellSize,
                            null, ReceiverStyle.Bowl, bowlProfile, level);
                        cups[i].RegisterWalls(simulation, sand.cellSize, cupProfile.wallThickness);
                    }
                    SourceDomain[] sources = new SourceDomain[level.sources.Count];
                    for (int i = 0; i < sources.Length; i++)
                        sources[i] = new SourceDomain(level.sources[i], sourceProfile, grainsPerUnit, null, level);
                    sources[0].Toggle();
                    for (int step = 0; step < 1500; step++)
                    {
                        if (step == 82) sources[1].Toggle();
                        for (int i = 0; i < sources.Length; i++) sources[i].Emit(simulation, 1f / 60f);
                        simulation.Step();
                        for (int i = 0; i < cups.Length; i++) cups[i].Collect(simulation);
                    }
                    Assert.That(sources[0].Remaining, Is.Zero);
                    Assert.That(sources[1].Remaining, Is.Zero);
                    if (scale == 1.5f)
                    {
                        Assert.That(cups[0].Required, Is.GreaterThan(cups[0].SafeCapacity));
                        Assert.That(cups[1].Required, Is.GreaterThan(cups[1].SafeCapacity));
                        Assert.That(cups[0].Full && cups[1].Full, Is.False,
                            "Unsafe Bowl Scale must reproduce an unwinnable 12-unit level.");
                    }
                    else
                    {
                        Assert.That(cups[0].Required, Is.LessThanOrEqualTo(cups[0].SafeCapacity));
                        Assert.That(cups[1].Required, Is.LessThanOrEqualTo(cups[1].SafeCapacity));
                        Assert.That(cups[0].Full && cups[1].Full, Is.True,
                            "Both authored 12-unit receivers must fill without spilled grains blocking win.");
                    }
                }
                finally { simulation.Dispose(); }
            }
        }
    }
}
