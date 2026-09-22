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

        [Header("Surface flow & stream (defaults = legacy behaviour)")]
        [Tooltip("Horizontal momentum kept per step while airborne. Legacy 0.985 drifts diagonally after leaving an edge; ~0.75 curves briefly then falls straight.")]
        [Range(0f, 1f)] public float airMomentumRetention = 0.985f;
        [Tooltip("Air retention only applies at/above this fall speed (cells/step) so 1-cell staircase drops on ramps keep their slide momentum. 0 = always (legacy).")]
        [Range(0f, 4f)] public float airborneMinSpeed = 0f;
        [Tooltip("Edge release: if this many cells directly below a falling grain are empty it has left the surface; its horizontal momentum is scaled by airMomentumRetention BEFORE any sideways drift, so it drops straight at the edge. 0 = off (legacy).")]
        [Range(0, 8)] public int airDropProbe = 0;
        [Tooltip("Momentum added per step toward the downhill side (nearest drop within slopeProbe). 0 = off (legacy).")]
        [Range(0f, 1f)] public float slopeAccel = 0f;
        [Tooltip("Chance a landing splash picks the downhill side instead of a random side. 0.5 = random (legacy).")]
        [Range(0f, 1f)] public float splashDownhillBias = 0.5f;
        [Tooltip("Cells scanned each side to find the downhill drop.")]
        [Range(1, 8)] public int slopeProbe = 6;
        [Tooltip("Grain resting on a faster-sliding grain adopts this share of its momentum, so the layer flows as a sheet. 0 = off (legacy).")]
        [Range(0f, 1f)] public float momentumCarry = 0f;
        [Tooltip("Max cells a grain slides per step along a surface (follows ramp steps). 1 = legacy.")]
        [Range(1, 4)] public int maxSlideCells = 1;
        [Tooltip("Process each row front-first along its net momentum so sliding trains move together instead of queueing. Off = random order (legacy).")]
        public bool flowOrderedRows = false;
        [Tooltip("Minimum |sum of row momentum| before flow ordering overrides the random row order.")]
        [Range(0f, 5f)] public float flowOrderThreshold = 1f;

        [Header("Gameplay scale")]
        [Tooltip("Simulation grains per GD logical unit.")]
        [Min(1)] public int grainsPerUnit = 30;
        [Min(1)] public int stableStepsForLose = 30;
    }
}
