using UnityEngine;

namespace SE001.Simulation.Sand
{
    /// <summary>
    /// Sand feel. Defaults = sand-feel-lab.html preset "Bột mịn" (powder) at 60 Hz with cellSize 0.06
    /// (lab: 1.5 px cells on a 270 px board = 10.8 units → 0.06 units/cell, 1 step per frame).
    /// </summary>
    [CreateAssetMenu(fileName = "SandSimulationProfile", menuName = "SE001/Profiles/Sand Simulation")]
    public sealed class SandSimulationProfile : ScriptableObject
    {
        public const int DefaultGrainsPerUnit = 115;

        [Min(0.001f)] public float cellSize = 0.06f;
        [Min(0.001f)] public float fixedStepSeconds = 1f / 60f;
        [Min(1)] public int maxStepsPerFrame = 4;
        [Min(1)] public int maxCells = 262144;
        public bool enableLateralSlide = true;

        [Header("Powder feel (lab 'Bột mịn')")]
        [Tooltip("Fall acceleration, cells/step² (lab g).")]
        [Range(0.05f, 0.8f)] public float gravityCellsPerStep2 = 0.3f;
        [Tooltip("Max fall speed, cells/step (lab vmax).")]
        [Range(1, 8)] public int maxFallCellsPerStep = 4;
        [Tooltip("Chance a resting grain rolls diagonally (lab repose). 1 = always.")]
        [Range(0.1f, 1f)] public float repose = 1f;
        [Tooltip("Momentum kept while sliding along a surface/line (lab slide).")]
        [Range(0f, 0.98f)] public float slide = 0.95f;
        [Tooltip("Impact speed converted to sideways momentum on landing (lab splash).")]
        [Range(0f, 1f)] public float splash = 0.5f;
        [Tooltip("Avalanche reach in cells (replaces lab random 'flow' so piles level without endless jitter). 1 = 45° piles.")]
        [Range(1, 8)] public int dispersion = 5;

        [Header("Creep (Sand Level Lab 2026-09-24)")]
        [Tooltip("Per-step chance that a resting grain with a free path walks one cell toward a drop farther away than " +
            "'dispersion'. Piles start steep, then flatten; sand drains off flat obstacles and drawn strokes. 0 = off.")]
        [Range(0f, 1f)] public float creepChance = 0.14f;
        [Tooltip("How far (cells, along a free row) a resting grain looks for a drop when creeping.")]
        [Range(1, 64)] public int creepReach = 24;

        [Header("Bowl settling")]
        [Tooltip("Chance for a resting grain over a Bowl to seek a lower cell. Applied only within the scaled Bowl opening.")]
        [Range(0f, 1f)] public float bowlCreepChance = 1f;
        [Tooltip("Search radius in cells for Bowl-local leveling. The path and target must remain over the Bowl opening.")]
        [Range(1, 64)] public int bowlCreepReach = 48;
        [Tooltip("Maximum horizontal cells a Bowl grain advances per step toward a verified lower cell. Higher values settle faster without extra simulation passes.")]
        [Range(1, 4)] public int bowlLevelingCellsPerStep = 3;
        [Tooltip("Extra Bowl-only settling passes per simulation step. More passes let newly exposed grains join the flow, at proportional CPU cost. 0 disables extra passes.")]
        [Range(0, 3)] public int bowlLevelingExtraPasses = 1;
        [Tooltip("Rows above the Bowl lip where newly landed sand still levels toward the opening.")]
        [Range(0, 8)] public int bowlSettlingHeadroomCells = 4;

        [Header("Stream")]
        [Tooltip("Momentum kept per step while airborne. Airborne grains never move sideways; 0 = drop dead-straight off edges.")]
        [Range(0f, 1f)] public float airDrag = 0.5f;

        [Header("Gameplay scale")]
        [Tooltip("Simulation grains per GD logical unit.")]
        [Min(1)] public int grainsPerUnit = DefaultGrainsPerUnit;
        [Min(1)] public int stableStepsForLose = 30;
    }
}
