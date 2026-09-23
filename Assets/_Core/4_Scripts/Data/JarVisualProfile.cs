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
        public float sourceMouthCenterYPixels = 184f;
        public float sourceCompositeHeightPixels = 212f;

        [Header("Measured cup art")]
        public float cupHeadWidthPixels = 182f;
        public float cupHeadHeightPixels = 217f;
        public float cupBodyOffsetXPixels = 13f;
        public float cupBodyOffsetYPixels = 41f;
        public float cupBodyWidthPixels = 156f;
        public float cupBodyHeightPixels = 130f;
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

        public float SourceUniformScale(float authoredHeight)
        {
            return authoredHeight / Mathf.Max(0.001f, sourceBodyHeightPixels * 0.01f);
        }

        public float CupWidthScale(float authoredWidth)
        {
            return authoredWidth / Mathf.Max(0.001f, cupHeadWidthPixels * 0.01f);
        }
    }

    public static class JarVisualGeometry
    {
        public static float SourceBodyOffsetY(float authoredHeight, SourceProfile sourceProfile, JarVisualProfile visualProfile)
        {
            if (visualProfile == null)
                return authoredHeight * (sourceProfile != null ? sourceProfile.fallbackMouthAnchorFactor : 0.5f);

            float bodyHeight = Mathf.Max(0.001f, visualProfile.sourceBodyHeightPixels);
            // The authored Source art is mouth-down. Idle presentation rotates it 180 degrees,
            // so the visual body center sits below the gameplay Position/EmitPoint.
            return authoredHeight * (0.5f - visualProfile.sourceMouthCenterYPixels / bodyHeight);
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
