using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "RotatingObstacleProfile", menuName = "SE001/Profiles/Rotating Obstacle")]
    public sealed class RotatingObstacleProfile : ScriptableObject
    {
        [Min(0.05f)] public float barWidth = 0.6f;
        [Min(1)] public int pushSearchCells = 12;
    }
}
