using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "SourceProfile", menuName = "SE001/Profiles/Source")]
    public sealed class SourceProfile : ScriptableObject
    {
        public Vector2 bodySize = new Vector2(0.8f, 1.2f);
        public float emissionRate = 16f;
        public int streamWidth = 1;
        public float valveOpenDelay = 0.2f;
        public float valveCloseRotateTime = 0.15f;
        public float hitPadding = 0.2f;
    }
}
