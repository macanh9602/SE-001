using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "JuiceProfile", menuName = "SE001/Profiles/Juice")]
    public sealed class JuiceProfile : ScriptableObject
    {
        public float valveOpenDuration = 0.2f;
        public float valveCloseDuration = 0.15f;
        public float cupPunchDuration = 0.12f;
        public float strokeExtrudeDuration = 0.15f;
    }
}
