using System;
using System.Collections.Generic;
using UnityEngine;
using SE001.Gameplay;
using SE001.Data;
using SE001.Simulation.Sand;

namespace SE001.System.Management
{
    public enum GameState { Playing, Won, Lost }
    public enum LoseReason { None, WrongCup, NotFilled }

    /// <summary>Per-level gameplay owner: valve commands, emission, simulation step, cups, ink, win/lose.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayManager : MonoBehaviour, ILevelLifecycleParticipant
    {
        [SerializeField] private float fixedStepHz = 60f;
        [SerializeField, Min(1)] private int maxStepsPerFrame = 4;
        [SerializeField, Min(1)] private int stableStepsForLose = 30;
        [SerializeField, Min(1)] private int maxStrokes = 16;
        [SerializeField, Min(1)] private int noProgressStepsForLose = 480;
        [Tooltip("Debug-audit (2026-09-24): writes AgentAudit/sand-lose-stability.md — why NotFilled lose does not fire.")]
        [SerializeField] private bool auditLoseStability;

        private const string LoseAuditChannel = "sand-lose-stability";
        private bool auditAllEmptySeen;
        private int auditNextSampleStep;

        private readonly List<SourceDomain> sources = new List<SourceDomain>();
        private readonly List<CupDomain> cups = new List<CupDomain>();
        private RotatingObstacleSystem rotatingObstacles;
        private LevelContext context;
        private float stepAccumulator;
        private int stableSteps;
        private int noProgressSteps;
        private bool bound;
        private int strokeCount;
        private bool settled;

        public event Action<GameState> GameStateChanged;
        public event Action<string> SourceStateChanged;
        public event Action<string> CupChanged;
        public event Action<LoseReason> LevelLost;
        public event Action<IList<Vector2>, float> StrokeCommitted;
        public event Action<float, float> InkChanged;

        public GameState State { get; private set; } = GameState.Playing;
        public LoseReason LastLoseReason { get; private set; }
        public IReadOnlyList<SourceDomain> Sources => sources;
        public IReadOnlyList<CupDomain> Cups => cups;
        public float InkBudget { get; private set; }
        public float InkRemaining { get; private set; }
        public int StepCount { get; private set; }
        public LevelContext Context => context;

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            State = GameState.Playing;
            fixedStepHz = Mathf.Max(1f, fixedStepHz);
            bound = true;
        }

        public void Configure(IEnumerable<SourceDomain> sourceValues, IEnumerable<CupDomain> cupValues, float cellSize, float wallThickness)
        {
            sources.Clear();
            cups.Clear();
            sources.AddRange(sourceValues);
            cups.AddRange(cupValues);
            InkBudget = Mathf.Max(0f, context.DrawInkBudget);
            InkRemaining = InkBudget;
            for (int i = 0; i < cups.Count; i++) cups[i].RegisterWalls(context.SandSimulation, cellSize, wallThickness);
            auditAllEmptySeen = false;
            auditNextSampleStep = 0;
            AgentDebugAudit.Begin(LoseAuditChannel,
                "Level start -> Win/Lose. Expect: all sources Empty + (stable "
                + stableStepsForLose + " steps OR no receiver progress "
                + noProgressStepsForLose + " steps) -> Lose(NotFilled).",
                auditLoseStability, 300);
            AgentDebugAudit.Event(LoseAuditChannel, "LevelStart", SourceSummary() + " | " + CupSummary());
        }

        public void ConfigureRuntime(GameplayRuntimeProfile profile)
        {
            if (profile == null) return;
            fixedStepHz = Mathf.Max(1f, profile.fixedStepHz);
            stableStepsForLose = Mathf.Max(1, profile.stableStepsForLose);
            noProgressStepsForLose = Mathf.Max(1, profile.noProgressStepsForLose);
            if (profile.drawPathProfile != null) maxStrokes = Mathf.Max(1, profile.drawPathProfile.maxStrokes);
        }

        public void ConfigureRotatingObstacles(RotatingObstacleSystem system)
        {
            rotatingObstacles = system;
        }

        /// <summary>Stamps the stroke into the dynamic mask. Stroke is truncated when ink runs out.</summary>
        public bool CommitStroke(IList<Vector2> points, float thickness)
        {
            if (!bound || State != GameState.Playing || points == null || points.Count < 2) return false;
            if (strokeCount >= maxStrokes || InkRemaining <= 0f)
            {
                Debug.LogWarning($"[GameplayManager] Stroke rejected: strokes {strokeCount}/{maxStrokes}, ink {InkRemaining:0.00}.", this);
                return false;
            }

            var sim = context.SandSimulation;
            float cell = sim.CellSize;
            // >= 1.5 cells so the barrier is 3+ cells wide: diagonal CA moves cannot leak through.
            float radius = Mathf.Max(thickness * 0.5f, cell * 1.5f);
            int r = Mathf.CeilToInt(radius / cell);
            var accepted = new List<Vector2>(points.Count) { points[0] };

            for (int i = 1; i < points.Count && InkRemaining > 0f; i++)
            {
                Vector2 a = points[i - 1];
                Vector2 b = points[i];
                float length = Vector2.Distance(a, b);
                if (length > InkRemaining) { b = a + (b - a) * (InkRemaining / length); length = InkRemaining; }
                InkRemaining -= length;
                accepted.Add(b);

                int steps = Mathf.Max(1, Mathf.CeilToInt(length / (cell * 0.5f)));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                    int cx = Mathf.FloorToInt(p.x / cell);
                    int cy = Mathf.FloorToInt(p.y / cell);
                    for (int y = cy - r; y <= cy + r; y++)
                        for (int x = cx - r; x <= cx + r; x++)
                        {
                            Vector2 center = new Vector2((x + 0.5f) * cell, (y + 0.5f) * cell);
                            if ((center - p).sqrMagnitude <= radius * radius) sim.SetDynamic(x, y, true);
                        }
                }
            }

            strokeCount++;
            StrokeCommitted?.Invoke(accepted, radius * 2f);
            InkChanged?.Invoke(InkRemaining, InkBudget);
            return true;
        }

        public void ToggleSource(string id)
        {
            if (!bound || State != GameState.Playing) return;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].StableId != id) continue;
                sources[i].Toggle();
                SourceStateChanged?.Invoke(id);
                return;
            }
        }

        private void Update()
        {
            if (!bound || context == null) return;
            if (State != GameState.Playing)
            {
                // Result is final; keep stepping sand only so in-flight grains settle visually (no emission, no rules).
                rotatingObstacles?.Advance(Time.deltaTime);
                if (!settled) settled = context.SandSimulation.Step() == 0;
                return;
            }

            float dt = 1f / fixedStepHz;
            stepAccumulator += Time.deltaTime;
            int steps = 0;
            while (stepAccumulator >= dt && steps < maxStepsPerFrame && State == GameState.Playing)
            {
                stepAccumulator -= dt;
                steps++;
                Tick(dt);
            }

            if (steps == maxStepsPerFrame) stepAccumulator = 0f; // drop backlog after a hitch
        }

        /// <summary>
        /// Deterministic headless advance: runs gameplay steps (or post-game settle steps) without frame time.
        /// Used by playthrough tests and replays; identical to what Update does per step.
        /// Returns the number of gameplay steps executed while Playing.
        /// </summary>
        public int AdvanceSteps(int count)
        {
            if (!bound || context == null || count <= 0) return 0;
            float dt = 1f / fixedStepHz;
            int played = 0;
            for (int i = 0; i < count; i++)
            {
                if (State == GameState.Playing) { Tick(dt); played++; continue; }
                if (settled) break;
                rotatingObstacles?.Advance(dt);
                settled = context.SandSimulation.Step() == 0;
            }

            return played;
        }

        private void Tick(float dt)
        {
            StepCount++;
            var sim = context.SandSimulation;
            int pushed = rotatingObstacles != null ? rotatingObstacles.Advance(dt) : 0;

            for (int i = 0; i < sources.Count; i++)
            {
                SourceValveState before = sources[i].State;
                sources[i].Emit(sim, dt);
                if (sources[i].State != before)
                {
                    SourceStateChanged?.Invoke(sources[i].StableId);
                    AgentDebugAudit.Event(LoseAuditChannel, "SourceState", "step " + StepCount + " " + sources[i].StableId
                        + " " + before + "->" + sources[i].State + " remaining " + sources[i].Remaining);
                }
            }

            int moved = sim.Step();

            int collectionActivity = 0;
            bool receiverProgressed = false;
            for (int i = 0; i < cups.Count; i++)
            {
                int collected = cups[i].Collect(sim);
                collectionActivity += Mathf.Abs(collected);
                if (cups[i].ForeignDetected)
                {
                    CupChanged?.Invoke(cups[i].StableId);
                    Lose(LoseReason.WrongCup);
                    return;
                }

                if (collected > 0)
                {
                    receiverProgressed = true;
                    CupChanged?.Invoke(cups[i].StableId);
                }
            }

            // Fast path: truly settled sand can fail quickly.
            stableSteps = moved == 0 && pushed == 0 && collectionActivity == 0 ? stableSteps + 1 : 0;

            bool allFull = cups.Count > 0;
            for (int i = 0; i < cups.Count; i++) allFull &= cups[i].Full;
            if (allFull) { Win(); return; }

            bool allEmpty = true;
            for (int i = 0; i < sources.Count; i++) allEmpty &= sources[i].State == SourceValveState.Empty;

            // Fallback is based on GAMEPLAY progress, not microscopic sand motion.
            // Creep/dispersion/rotating pushes may keep 1..N grains moving for a long time even when the level
            // can no longer make receiver progress. Count only after every Source is Empty, and reset only when
            // at least one receiver actually gains accepted grains.
            noProgressSteps = allEmpty && !receiverProgressed ? noProgressSteps + 1 : 0;

            AuditStability(allEmpty, moved, pushed, collectionActivity);
            if (allEmpty &&
                (stableSteps >= stableStepsForLose || noProgressSteps >= noProgressStepsForLose))
                Lose(LoseReason.NotFilled);
        }

        private void AuditStability(bool allEmpty, int moved, int pushed, int collectedTotal)
        {
            if (!auditLoseStability || !allEmpty) return;
            if (!auditAllEmptySeen)
            {
                auditAllEmptySeen = true;
                auditNextSampleStep = StepCount;
                AgentDebugAudit.Event(LoseAuditChannel, "AllSourcesEmpty", "step " + StepCount + " | " + CupSummary());
            }

            if (StepCount < auditNextSampleStep) return;
            auditNextSampleStep = StepCount + 60;
            SandSimulation.SandStepStats stats = context.SandSimulation.LastStepStats;
            AgentDebugAudit.Event(LoseAuditChannel, "StabilitySample",
                "step " + StepCount + " stable " + stableSteps + "/" + stableStepsForLose
                + " noProgress " + noProgressSteps + "/" + noProgressStepsForLose
                + " moved " + moved + " pushed " + pushed + " collectionActivity " + collectedTotal
                + " | fall " + stats.Fall + " roll " + stats.Roll + " slide " + stats.Slide
                + " disperse " + stats.Disperse + " creep " + stats.Creep
                + " | last rule " + stats.LastRule + " (" + stats.LastX + "," + stats.LastY + ")->("
                + stats.LastToX + "," + stats.LastToY + ") | " + CupSummary());
        }

        private string SourceSummary()
        {
            string text = "sources:";
            for (int i = 0; i < sources.Count; i++)
                text += " " + sources[i].StableId + " " + sources[i].State + " " + sources[i].Remaining + "/" + sources[i].Initial;
            return text;
        }

        private string CupSummary()
        {
            string text = "cups:";
            for (int i = 0; i < cups.Count; i++)
                text += " " + cups[i].StableId + " " + cups[i].Collected + "/" + cups[i].Required + " cap " + cups[i].Capacity
                    + (cups[i].Full ? " FULL" : "");
            return text;
        }

        private void Win()
        {
            AgentDebugAudit.Event(LoseAuditChannel, "Win", "step " + StepCount + " | " + CupSummary());
            State = GameState.Won;
            GameStateChanged?.Invoke(State);
        }

        private void Lose(LoseReason reason)
        {
            AgentDebugAudit.Event(LoseAuditChannel, "Lose", "step " + StepCount + " " + reason + " | " + CupSummary());
            State = GameState.Lost;
            LastLoseReason = reason;
            LevelLost?.Invoke(reason);
            GameStateChanged?.Invoke(State);
        }

        public void CleanupForLevelUnload()
        {
            bound = false;
            rotatingObstacles = null;
            context = null;
            sources.Clear();
            cups.Clear();
            State = GameState.Playing;
            LastLoseReason = LoseReason.None;
            stableSteps = 0;
            noProgressSteps = 0;
            stepAccumulator = 0f;
            StepCount = 0;
            InkBudget = 0f;
            InkRemaining = 0f;
            strokeCount = 0;
            settled = false;
        }
    }
}
