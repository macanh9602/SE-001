using System.Collections.Generic;
using NUnit.Framework;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Tests
{
    /// <summary>
    /// End-to-end scripted playthroughs through the real LevelManager → LevelSpawner → GameplayManager path.
    /// Each test is a fixed command script (taps / strokes) + deterministic headless steps.
    /// These prove the authored levels match their design intent (tap-only win, draw required, wrong cup).
    /// </summary>
    public sealed class PhaseCPlaythroughTests
    {
        private const int MaxSteps = 8000;
        private GameObject owner;
        private LevelManager levelManager;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("PhaseCPlaythroughTests");
            levelManager = owner.AddComponent<LevelManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (levelManager != null && levelManager.CurrentContext != null) levelManager.UnloadCurrentLevel();
            if (owner != null) Object.DestroyImmediate(owner);
        }

        [Test]
        public void Playthrough_Level01_TapOnly_Wins()
        {
            GameplayManager game = Load("phase_c_level_01");
            game.ToggleSource("source_yellow");
            RunToEnd(game);
            Assert.That(game.State, Is.EqualTo(GameState.Won), Describe(game));
        }

        [Test]
        public void Playthrough_Level02_NoDraw_Loses()
        {
            GameplayManager game = Load("phase_c_level_02");
            game.ToggleSource("source_yellow");
            RunToEnd(game);
            Assert.That(game.State, Is.EqualTo(GameState.Lost), Describe(game));
            Assert.That(game.LastLoseReason, Is.EqualTo(LoseReason.NotFilled));
        }

        [Test]
        public void Playthrough_Level02_ScriptedDraw_Wins()
        {
            GameplayManager game = Load("phase_c_level_02");
            // Ramp under the plank's right edge (x≈7.9) guiding sand down-right into the cup at x=9.6.
            Assert.That(game.CommitStroke(Line(new Vector2(7.5f, 6.2f), new Vector2(9.3f, 3.1f), 12), 0.3f), Is.True);
            game.ToggleSource("source_yellow");
            RunToEnd(game);
            Assert.That(game.State, Is.EqualTo(GameState.Won), Describe(game));
        }

        [Test]
        public void Playthrough_Level03_WrongRoute_LosesWrongCup()
        {
            GameplayManager game = Load("phase_c_level_03");
            // Ramp carrying yellow sand (x=3.0) over the obstacle into the red cup (x=7.8).
            Assert.That(game.CommitStroke(Line(new Vector2(2.3f, 12.2f), new Vector2(7.7f, 8.8f), 16), 0.3f), Is.True);
            game.ToggleSource("source_yellow");
            RunToEnd(game);
            Assert.That(game.State, Is.EqualTo(GameState.Lost), Describe(game));
            Assert.That(game.LastLoseReason, Is.EqualTo(LoseReason.WrongCup));
        }

        [Test]
        public void Playthrough_Level03_Correct_Wins()
        {
            GameplayManager game = Load("phase_c_level_03");
            game.ToggleSource("source_yellow");
            game.ToggleSource("source_red");
            RunToEnd(game);
            Assert.That(game.State, Is.EqualTo(GameState.Won), Describe(game));
        }

        [Test]
        public void Playthrough_IsDeterministic_SameScriptSameResult()
        {
            GameplayManager first = Load("phase_c_level_01");
            first.ToggleSource("source_yellow");
            int firstSteps = RunToEnd(first);
            byte[] firstGrid = (byte[])first.Context.SandSimulation.State.Cells.Clone();

            levelManager.ReloadCurrentLevel();
            GameplayManager second = Game();
            second.ToggleSource("source_yellow");
            int secondSteps = RunToEnd(second);

            Assert.That(secondSteps, Is.EqualTo(firstSteps));
            Assert.That(second.Context.SandSimulation.State.Cells, Is.EqualTo(firstGrid));
        }

        [Test]
        public void Playthrough_ClosedValve_NeverLoses()
        {
            GameplayManager game = Load("phase_c_level_01");
            game.AdvanceSteps(2000);
            Assert.That(game.State, Is.EqualTo(GameState.Playing), "Closed sources with sand left must never trigger NotFilled.");
        }

        [Test]
        public void Source_TapHitTest_CoversVisibleJarNotOnlyEmitPoint()
        {
            GameplayManager game = Load("phase_c_level_01");
            var source = game.Sources[0];
            Vector2 bodyCenter = source.Position + source.BodyOffset;
            Assert.That(source.HitTest(bodyCenter, 0.2f), Is.True, "Tapping the jar body must hit.");
            Assert.That(source.HitTest(bodyCenter + new Vector2(0.35f, 0.5f), 0.2f), Is.True, "Near the jar top corner must hit.");
            Assert.That(source.HitTest(bodyCenter + new Vector2(1.5f, 0f), 0.2f), Is.False, "Far away must not hit.");
        }

        private GameplayManager Load(string levelId)
        {
            levelManager.BeginLevel(levelId);
            Assert.That(levelManager.IsReady, Is.True, levelId + " failed to become ready.");
            return Game();
        }

        private GameplayManager Game()
        {
            GameplayManager game = levelManager.CurrentContext.LevelRoot.GetComponent<GameplayManager>();
            Assert.That(game, Is.Not.Null, "LevelSpawner must bind a GameplayManager on LevelRoot.");
            return game;
        }

        /// <summary>Runs until game over (then lets sand settle). Returns gameplay steps used.</summary>
        private static int RunToEnd(GameplayManager game)
        {
            int played = 0;
            while (game.State == GameState.Playing && played < MaxSteps) played += game.AdvanceSteps(100);
            game.AdvanceSteps(2000);
            return played;
        }

        private static List<Vector2> Line(Vector2 a, Vector2 b, int points)
        {
            var list = new List<Vector2>(points);
            for (int i = 0; i < points; i++) list.Add(Vector2.Lerp(a, b, i / (float)(points - 1)));
            return list;
        }

        private static string Describe(GameplayManager game)
        {
            var sb = new global::System.Text.StringBuilder();
            sb.Append($"state={game.State} reason={game.LastLoseReason} steps={game.StepCount} ink={game.InkRemaining:0.00}/{game.InkBudget:0.00}");
            foreach (var s in game.Sources) sb.Append($" | {s.StableId} {s.State} {s.Remaining}/{s.Initial}");
            foreach (var c in game.Cups) sb.Append($" | {c.StableId} {c.Collected}/{c.Required} cap {c.Capacity}{(c.ForeignDetected ? " FOREIGN" : "")}");
            return sb.ToString();
        }
    }
}
