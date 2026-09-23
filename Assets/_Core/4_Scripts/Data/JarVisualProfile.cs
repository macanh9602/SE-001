using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "JarVisualProfile", menuName = "SE001/Profiles/Jar Visual")]
    public sealed class JarVisualProfile : ScriptableObject
    {
        [Header("Measured source art")]
        public float sourceBodyWidthPixels = 156f;
        public float sourceBodyHeightPixels = 192f;
        public float sourceMouthCenterXPixels = 78f;
        // Mouth quad center measured from the body top in authored (mouth-down) art: top 184 + half of 28 px.
        public float sourceMouthCenterYPixels = 198f;
        public float sourceCompositeHeightPixels = 212f;

        [Header("Measured cup art")]
        public float cupHeadWidthPixels = 182f;
        public float cupHeadHeightPixels = 217f;
        public float cupBodyOffsetXPixels = 13f;
        public float cupBodyOffsetYPixels = 41f;
        public float cupBodyWidthPixels = 156f;
        public float cupBodyHeightPixels = 130f;
        [Tooltip("Glass edge inset on each side of the cup body, measured in source pixels.")]
        public float cupBodyInnerInsetPixels = 7f;
        public float cupCapTopHeightPixels = 44f;
        public float cupCapBottomHeightPixels = 49f;

        [Header("Shadow and fill")]
        public Vector2 shadowOffsetPixels = new Vector2(-6f, -24f);
        public float shadowMultiplier = 0.8f;
        public int sourceFillInsetPixels = 7;
        public int sourceFillTopSkipPixels = 10;
        public int sourceFillBottomSkipPixels = 6;
        public Texture2D sourceFillMask;
        public Texture2D sourceSilhouetteMask;
        public Texture2D cupSilhouetteMask;
        public Texture2D sparkleNoise;

        // Cumulative source inner area, measured from the wide/end row toward the mouth.
        public float[] sourceFillAreaLut = new float[0];
        public Vector2[] sourceFillRowSpans = new Vector2[0];

        [Header("Source sand motion (tilt / slide / settle while the jar rotates)")]
        [Tooltip("How much of the jar rotation the sand follows instantly before sliding (0 = always level, 1 = glued).")]
        [Range(0f, 1f)] public float sandCarry = 0.85f;
        [Tooltip("Settle spring angular frequency (rad/s). Higher = sand levels out faster.")]
        [Range(1f, 40f)] public float sandSettleFrequency = 14f;
        [Tooltip("Settle spring damping ratio. <1 gives a small slosh after the jar stops.")]
        [Range(0.1f, 1.5f)] public float sandSettleDamping = 0.55f;
        [Tooltip("Max tilt of the sand surface away from level, like a pile's angle of repose (degrees).")]
        [Range(0f, 60f)] public float sandReposeAngle = 32f;
        [Tooltip("Sampling stride (pixels) for the area-correct fill solver. Lower = more precise, more CPU while rotating.")]
        [Range(1, 8)] public int sandSolverStridePixels = 3;

        public float SourceUniformScale(float authoredHeight)
        {
            return authoredHeight / Mathf.Max(0.001f, sourceBodyHeightPixels * 0.01f);
        }

        public float CupWidthScale(float authoredWidth)
        {
            return authoredWidth / Mathf.Max(0.001f, cupHeadWidthPixels * 0.01f);
        }

        public float CupBodyInnerWidthPixels =>
            Mathf.Max(1f, cupBodyWidthPixels - cupBodyInnerInsetPixels * 2f);
    }

    public static class JarVisualGeometry
    {
        public static float SourceBodyOffsetY(float authoredHeight, SourceProfile sourceProfile, JarVisualProfile visualProfile)
        {
            if (visualProfile == null)
                return authoredHeight * (sourceProfile != null ? sourceProfile.fallbackMouthAnchorFactor : 0.5f);

            float bodyHeight = Mathf.Max(0.001f, visualProfile.sourceBodyHeightPixels);
            // Position is the emit point. Sand only pours in the pour pose (authored mouth-down art, pivot 0 deg),
            // so the mouth tip (bottom of the composite) must sit on Position: the body center is above it.
            // Idle rotates 180 deg around the body center, which puts the mouth on top.
            return authoredHeight * (visualProfile.sourceCompositeHeightPixels / bodyHeight - 0.5f);
        }

        public static float SourceMouthLocalY(float authoredHeight, JarVisualProfile visualProfile)
        {
            if (visualProfile == null) return -authoredHeight * 0.5f;
            float bodyHeight = Mathf.Max(0.001f, visualProfile.sourceBodyHeightPixels);
            return authoredHeight * (0.5f - visualProfile.sourceMouthCenterYPixels / bodyHeight);
        }

        public static float AreaToHeight(float areaRatio, float[] cumulativeAreaLut)
        {
            areaRatio = Mathf.Clamp01(areaRatio);
            if (cumulativeAreaLut == null || cumulativeAreaLut.Length < 2) return areaRatio;
            if (areaRatio <= cumulativeAreaLut[0]) return 0f;
            int last = cumulativeAreaLut.Length - 1;
            if (areaRatio >= cumulativeAreaLut[last]) return 1f;

            for (int i = 1; i <= last; i++)
            {
                if (cumulativeAreaLut[i] < areaRatio) continue;
                float previous = cumulativeAreaLut[i - 1];
                float range = Mathf.Max(0.000001f, cumulativeAreaLut[i] - previous);
                float t = Mathf.Clamp01((areaRatio - previous) / range);
                return Mathf.Lerp((i - 1) / (float)last, i / (float)last, t);
            }
            return 1f;
        }
    }
}
