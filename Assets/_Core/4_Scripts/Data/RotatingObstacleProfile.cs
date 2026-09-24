using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "RotatingObstacleProfile", menuName = "SE001/Profiles/Rotating Obstacle")]
    public sealed class RotatingObstacleProfile : ScriptableObject
    {
        [Min(0.05f)] public float barWidth = 0.6f;
        [Range(1, 64)] public int pushSearchCells = 12;

        [Header("Passive rotation")]
        [Tooltip("Torque multiplier for real sand contact. Runtime must still include contact direction and lever arm.")]
        [Min(0.001f)] public float sandTorqueScale = 18f;
        [Tooltip("Scales the rotor moment of inertia. Higher values make the rotor harder to accelerate.")]
        [Min(0.001f)] public float momentOfInertiaScale = 1f;
        [Tooltip("Angular velocity damping applied while the passive rotor coasts.")]
        [Min(0f)] public float angularDamping = 5f;
        [Tooltip("Absolute passive angular-speed cap in degrees/second.")]
        [Min(1f)] public float maxAngularSpeed = 240f;
        [Tooltip("Angular speed at/below which damping snaps the rotor to rest.")]
        [Min(0f)] public float restAngularSpeed = 1f;
        [Tooltip("Maximum angular increment used by swept rotor-vs-stroke collision checks.")]
        [Range(0.25f, 10f)] public float collisionSweepStepDegrees = 2f;
    }
}
