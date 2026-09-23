using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "LayoutDefinition", menuName = "SE001/Layouts/Layout Definition")]
    public sealed class LayoutDefinition : ScriptableObject
    {
        public string layoutId = string.Empty;
        public GameObject layoutPrefab;
        public LayoutMaskAsset mask;
        public Vector2 boardSize;
        public Texture2D thumbnail;

        [HideInInspector]
        public string sourceSvgPath = string.Empty;
    }
}
