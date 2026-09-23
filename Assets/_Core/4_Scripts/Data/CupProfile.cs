using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "CupProfile", menuName = "SE001/Profiles/Cup")]
    public sealed class CupProfile : ScriptableObject
    {
        public Vector2 bodySize = new Vector2(2f, 1.5f);
        public float wallThickness = 0.12f;
        [Range(0f, 0.45f)] public float taper = 0.2f;
        [Range(0.3f, 1f)] public float fillLine = 0.85f;
    }
}
