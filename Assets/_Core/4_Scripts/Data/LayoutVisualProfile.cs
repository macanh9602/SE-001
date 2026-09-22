using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "LayoutVisualProfile", menuName = "SE001/Profiles/Layout Visual")]
    public sealed class LayoutVisualProfile : ScriptableObject
    {
        [Min(0.001f)] public float wallHeight = 0.4f;
        [Min(0.001f)] public float obstacleHeight = 0.25f;
        [Min(0f)] public float bevelRadius = 0.04f;
        [Min(1)] public int bevelSegments = 1;
        public Material wallMaterial;
        public Material obstacleMaterial;
        [Min(0.001f)] public float uvScale = 1f;
        public bool castShadows = true;
        public bool receiveShadows = true;
    }
}
