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
        [Tooltip("Lose also fires after this many 'quiet' steps (no cup change, at most Lose Quiet Max Moves grains moving). "
            + "Covers a rotating obstacle or creep that keeps nudging a few grains forever (audit 2026-09-24).")]
        public int loseQuietSteps = 240;
        [Tooltip("Grains allowed to move in a step that still counts as quiet.")]
        public int loseQuietMaxMoves = 3;
        public float fixedStepHz = 60f;
    }
}
