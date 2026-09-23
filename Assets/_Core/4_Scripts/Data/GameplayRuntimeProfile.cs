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
        public ReceiverStyle receiverStyle = ReceiverStyle.Bowl;
        public BowlVisualProfile bowlVisualProfile;
        public RotatingObstacleProfile rotatingObstacleProfile;
        public DrawPathProfile drawPathProfile;
        public JuiceProfile juiceProfile;
        public PhaseCLevelSequence levelSequence;
        public PhaseCVisualMaterials visualMaterials;
        public JarVisualProfile jarVisualProfile;
        public SandSimulationProfile sandProfile;
        public PrefabProfile prefabProfile;
        public int stableStepsForLose = 30;
        [Tooltip("After every Source is Empty, lose if no receiver gains accepted sand for this many simulation steps. "
            + "This intentionally ignores microscopic creep/dispersion/rotating motion elsewhere on the board.")]
        public int noProgressStepsForLose = 480;
        public float fixedStepHz = 60f;
    }
}
