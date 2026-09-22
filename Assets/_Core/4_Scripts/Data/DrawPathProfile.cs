using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "DrawPathProfile", menuName = "SE001/Profiles/Draw Path")]
    public sealed class DrawPathProfile : ScriptableObject
    {
        public float drawThickness = 0.12f;
        public float minPointDistance = 0.08f;
        public int maxPointsPerStroke = 128;
        public int maxStrokes = 16;
        public float defaultInkBudget = 8f;
        public float drawStartDeadZone = 18f;
        public float extrudeHeight = 0.18f;
    }
}
