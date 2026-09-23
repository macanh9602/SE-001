using System.Collections.Generic;
using SE001.Data;
using UnityEngine;

namespace SE001.Presentation
{
    /// <summary>
    /// Area-correct sand level for a Source jar at any rotation.
    /// The jar interior (baked row spans of the fill mask) is sampled once into object-space points.
    /// For a "down" direction in object space and a fill ratio, the sand occupies the lowest fill% of those
    /// points, so the threshold is a k-th order statistic of their projections (quickselect, O(n), no GC).
    /// Object space = SourceBody quad: 1 unit = 100 art pixels, origin at the body center.
    /// </summary>
    public sealed class SourceSandFillSolver
    {
        public const float NoSand = 1000f;
        public const float FullSand = -1000f;

        private static readonly Dictionary<int, Vector2[]> PointCache = new Dictionary<int, Vector2[]>();

        private readonly Vector2[] points;
        private readonly float[] scratch;

        private SourceSandFillSolver(Vector2[] samplePoints)
        {
            points = samplePoints;
            scratch = new float[samplePoints.Length];
        }

        public int SampleCount => points.Length;

        /// <summary>Returns null when the profile has no baked spans (caller falls back to "no sand motion").</summary>
        public static SourceSandFillSolver Create(JarVisualProfile profile)
        {
            if (profile == null || profile.sourceFillRowSpans == null || profile.sourceFillRowSpans.Length == 0) return null;
            int stride = Mathf.Clamp(profile.sandSolverStridePixels, 1, 8);
            int key = profile.GetInstanceID() * 16 + stride;
            Vector2[] cached;
            if (!PointCache.TryGetValue(key, out cached))
            {
                cached = BuildPoints(profile, stride);
                PointCache[key] = cached;
            }

            return cached.Length > 0 ? new SourceSandFillSolver(cached) : null;
        }

        /// <summary>Sand covers object-space positions p where dot(p, downOS) &gt;= returned threshold.</summary>
        public float SolveThreshold(Vector2 downOS, float fill)
        {
            int count = points.Length;
            int sandCount = Mathf.RoundToInt(Mathf.Clamp01(fill) * count);
            if (sandCount <= 0) return NoSand;
            if (sandCount >= count) return FullSand;
            for (int i = 0; i < count; i++)
                scratch[i] = points[i].x * downOS.x + points[i].y * downOS.y;
            // Ascending order: the sandCount largest projections are the lowest points of the jar.
            return Select(scratch, count - sandCount);
        }

        private static Vector2[] BuildPoints(JarVisualProfile profile, int stride)
        {
            Vector2[] spans = profile.sourceFillRowSpans;
            float halfWidth = profile.sourceBodyWidthPixels * 0.5f;
            float halfHeight = profile.sourceBodyHeightPixels * 0.5f;
            List<Vector2> result = new List<Vector2>();
            for (int y = 0; y < spans.Length; y += stride)
            {
                Vector2 span = spans[y];
                if (span.y < span.x) continue;
                for (float x = span.x; x <= span.y; x += stride)
                {
                    // Row index y is bottom-up (Texture2D.GetPixels32 order), matching the quad UVs.
                    result.Add(new Vector2((x + 0.5f - halfWidth) * 0.01f, (y + 0.5f - halfHeight) * 0.01f));
                }
            }

            return result.ToArray();
        }

        // Hoare quickselect: value that would be at index k after an ascending sort. Mutates values.
        private static float Select(float[] values, int k)
        {
            int left = 0;
            int right = values.Length - 1;
            while (left < right)
            {
                float pivot = values[(left + right) >> 1];
                int i = left;
                int j = right;
                while (i <= j)
                {
                    while (values[i] < pivot) i++;
                    while (values[j] > pivot) j--;
                    if (i > j) continue;
                    float swap = values[i];
                    values[i] = values[j];
                    values[j] = swap;
                    i++;
                    j--;
                }

                if (k <= j) right = j;
                else if (k >= i) left = i;
                else break;
            }

            return values[k];
        }
    }
}
