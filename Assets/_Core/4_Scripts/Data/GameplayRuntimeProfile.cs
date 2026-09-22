using UnityEngine;
using SE001.Simulation.Sand;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "GameplayRuntimeProfile", menuName = "SE001/Profiles/Gameplay Runtime")]
    public sealed class GameplayRuntimeProfile : ScriptableObject
    {
        public ColorProfile colorProfile;
        public SourceProfile sourceProfile;
        public CupProfile cupProfile;
        public DrawPathProfile drawPathProfile;
        public JuiceProfile juiceProfile;
        public PhaseCLevelSequence levelSequence;
        public PhaseCVisualMaterials visualMaterials;
        public SandSimulationProfile sandProfile;
        public PrefabProfile prefabProfile;
        public int grainsPerUnit = 12;
        public int stableStepsForLose = 30;
        public float fixedStepHz = 60f;
    }
}
