using UnityEngine;

namespace SE001.Simulation.Sand
{
    [CreateAssetMenu(fileName = "SandSimulationProfile", menuName = "SE001/Profiles/Sand Simulation")]
    public sealed class SandSimulationProfile : ScriptableObject
    {
        [Min(0.001f)] public float cellSize = 0.1f;
        [Min(0.001f)] public float fixedStepSeconds = 0.02f;
        [Min(1)] public int maxStepsPerFrame = 4;
        [Min(1)] public int maxCells = 262144;
        public bool enableLateralSlide = true;
        [Min(1)] public int grainsPerUnit = 12;
        [Min(1)] public int stableStepsForLose = 30;
    }
}
