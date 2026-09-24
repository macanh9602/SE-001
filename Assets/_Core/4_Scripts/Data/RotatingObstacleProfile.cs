using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "RotatingObstacleProfile", menuName = "SE001/Profiles/Rotating Obstacle")]
    public sealed class RotatingObstacleProfile : ScriptableObject
    {
        [Min(0.05f)] public float barWidth = 0.6f;
        [Range(1, 64)] public int pushSearchCells = 12;

        [Header("Passive rotation")]
        [Tooltip("Torque multiplier for real incoming sand motion at rotor contact.")]
        [Min(0.001f)] public float sandTorqueScale = 36f;
        [Tooltip("Scales the rotor moment of inertia. Lower values make the rotor easier to spin.")]
        [Min(0.001f)] public float momentOfInertiaScale = 0.25f;
        [Tooltip("Exponential angular damping per second. Lower values preserve more free-spin inertia.")]
        [Min(0f)] public float angularDamping = 0.8f;
        [Tooltip("Absolute passive angular-speed cap in degrees/second.")]
        [Min(1f)] public float maxAngularSpeed = 360f;
        [Tooltip("Max change of angular speed in degrees/second^2. Smooths 0 -> full-speed jumps when a stream hits. 0 = unlimited.")]
        [Min(0f)] public float maxAngularAcceleration = 0f;
        [Tooltip("Angular speed at/below which damping snaps the rotor to rest.")]
        [Min(0f)] public float restAngularSpeed = 0.2f;
        [Tooltip("Velocity retained when sand is too densely packed to yield this simulation step. DrawStroke still hard-stops.")]
        [Range(0f, 1f)] public float sandBlockVelocityRetention = 0.85f;
        [Tooltip("Maximum angular increment used by swept rotor-vs-stroke collision checks.")]
        [Range(0.25f, 10f)] public float collisionSweepStepDegrees = 2f;
    }
}