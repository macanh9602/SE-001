using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "PrefabProfile", menuName = "SE001/Profiles/Prefab Profile")]
    public sealed class PrefabProfile : ScriptableObject
    {
        public GameObject boardWallPrefab;
        public GameObject staticObstaclePrefab;
        public GameObject rotatingObstaclePrefab;
        public GameObject sandFieldPrefab;
        public GameObject sourcePrefab;
        public GameObject cupPrefab;
        public GameObject bowlPrefab;
        public GameObject drawStrokePrefab;
    }
}
