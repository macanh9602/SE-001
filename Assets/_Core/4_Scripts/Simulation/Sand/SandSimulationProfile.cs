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

        [Header("Gameplay scale")]
        [Tooltip("Simulation grains per GD logical unit.")]
        [Min(1)] public int grainsPerUnit = 30;
        [Min(1)] public int stableStepsForLose = 30;
    }
}
