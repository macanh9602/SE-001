using System;
using System.Collections.Generic;
using UnityEngine;
using SE001.Gameplay;

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

        private readonly List<SourceDomain> sources = new List<SourceDomain>();
        private readonly List<CupDomain> cups = new List<CupDomain>();
        private LevelContext context;
        private float stepAccumulator;
        private int stableSteps;
        private bool bound;
        private int strokeCount;
        private bool settled;

        public event Action<GameState> GameStateChanged;
        public event Action<string> SourceStateChanged;
        public event Action<string> CupChanged;
        public event Action<LoseReason> LevelLost;
        public event Action<IList<Vector2>, float> StrokeCommitted;

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
            AgentDebugAudit.Begin(DrawAudit, "Phase C draw bug — level " + value.LevelId + " gen " + value.Generation, DrawAuditEnabled, 400);
        }

        // ---- TEMP debug-audit hook (draw bug). Remove after root cause fixed. ----
        public const string DrawAudit = "phase-c-draw";
        public static bool DrawAuditEnabled = true;

        public void Configure(IEnumerable<SourceDomain> sourceValues, IEnumerable<CupDomain> cupValues, float cellSize, float wallThickness)
        {
            sources.Clear();
            cups.Clear();
            sources.AddRange(sourceValues);
            cups.AddRange(cupValues);
            InkBudget = Mathf.Max(0f, context.DrawInkBudget);
            InkRemaining = InkBudget;
            for (int i = 0; i < cups.Count; i++) cups[i].RegisterWalls(context.SandSimulation, cellSize, wallThickness);
        }

        /// <summary>Stamps the stroke into the dynamic mask. Stroke is truncated when ink runs out.</summary>
        public bool CommitStroke(IList<Vector2> points, float thickness)
        {
            AgentDebugAudit.Event(DrawAudit, "Commit.Request", $"bound={bound} state={State} points={(points == null ? -1 : points.Count)} first={(points != null && points.Count > 0 ? points[0].ToString("F2") : "-")} last={(points != null && points.Count > 0 ? points[points.Count - 1].ToString("F2") : "-")} thickness={thickness:F3} ink={InkRemaining:F2} strokes={strokeCount}/{maxStrokes}");
            if (!bound || State != GameState.Playing || points == null || points.Count < 2) { AgentDebugAudit.Event(DrawAudit, "Commit.Rejected", "guard: bound/state/points"); return false; }
            if (strokeCount >= maxStrokes || InkRemaining <= 0f)
            {
                AgentDebugAudit.Event(DrawAudit, "Commit.Rejected", "ink/strokes limit");
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
            int dyn = 0, minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            bool[] dm = sim.State.DynamicMask;
            for (int i = 0; i < dm.Length; i++)
            {
                if (!dm[i]) continue;
                dyn++;
                int x = i % sim.State.Width, y = i / sim.State.Width;
                if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            AgentDebugAudit.Event(DrawAudit, "Commit.Stamped", $"accepted={accepted.Count} radius={radius:F3} cellsR={r} inkLeft={InkRemaining:F2} dynamicCellsTotal={dyn} dynBounds=({minX},{minY})-({maxX},{maxY}) grid={sim.State.Width}x{sim.State.Height} cell={cell:F3}");
            try { StrokeCommitted?.Invoke(accepted, radius * 2f); AgentDebugAudit.Event(DrawAudit, "Commit.VisualOk", ""); }
            catch (Exception e) { AgentDebugAudit.Event(DrawAudit, "Commit.VisualException", e.GetType().Name + ": " + e.Message); throw; }
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

        private void Tick(float dt)
        {
            StepCount++;
            var sim = context.SandSimulation;

            for (int i = 0; i < sources.Count; i++)
            {
                SourceValveState before = sources[i].State;
                sources[i].Emit(sim, dt);
                if (sources[i].State != before) SourceStateChanged?.Invoke(sources[i].StableId);
            }

            int moved = sim.Step();

            int collectedTotal = 0;
            for (int i = 0; i < cups.Count; i++)
            {
                int collected = cups[i].Collect(sim);
                collectedTotal += Mathf.Abs(collected);
                if (cups[i].ForeignDetected)
                {
                    CupChanged?.Invoke(cups[i].StableId);
                    Lose(LoseReason.WrongCup);
                    return;
                }

                if (collected > 0) CupChanged?.Invoke(cups[i].StableId);
            }

            stableSteps = moved == 0 && collectedTotal == 0 ? stableSteps + 1 : 0;

            bool allFull = cups.Count > 0;
            for (int i = 0; i < cups.Count; i++) allFull &= cups[i].Full;
            if (allFull) { Win(); return; }

            bool allEmpty = true;
            for (int i = 0; i < sources.Count; i++) allEmpty &= sources[i].State == SourceValveState.Empty;
            if (allEmpty && stableSteps >= stableStepsForLose) Lose(LoseReason.NotFilled);
        }

        private void Win()
        {
            State = GameState.Won;
            GameStateChanged?.Invoke(State);
        }

        private void Lose(LoseReason reason)
        {
            State = GameState.Lost;
            LastLoseReason = reason;
            LevelLost?.Invoke(reason);
            GameStateChanged?.Invoke(State);
        }

        public void CleanupForLevelUnload()
        {
            bound = false;
            context = null;
            sources.Clear();
            cups.Clear();
            State = GameState.Playing;
            LastLoseReason = LoseReason.None;
            stableSteps = 0;
            stepAccumulator = 0f;
            StepCount = 0;
            InkBudget = 0f;
            InkRemaining = 0f;
            strokeCount = 0;
            settled = false;
        }
    }
}
