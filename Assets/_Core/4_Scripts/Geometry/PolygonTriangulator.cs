using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Geometry
{
    /// <summary>
    /// Ear clipping for simple polygons. Returns indices into the input list.
    /// Robust to dense SVG curve sampling (test_2.svg, 2026-09-24): near-duplicate and collinear points are skipped
    /// before clipping, and only points strictly inside an ear block it, so hairline ears on tiny rounded corners
    /// no longer stall the clipper.
    /// </summary>
    public static class PolygonTriangulator
    {
        /// <summary>Edge shorter than this share of the polygon extent is treated as a duplicate point.</summary>
        private const float DuplicateEdgeRatio = 1e-5f;

        /// <summary>|sin| of the turn angle below which a vertex is collinear (~0.006 degrees).</summary>
        private const float CollinearSine = 1e-4f;

        public static int[] Triangulate(IList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) throw new ArgumentException("A polygon needs at least three points.");
            List<int> remaining = new List<int>(polygon.Count);
            if (PolygonUtility.SignedArea(polygon) > 0f)
            {
                for (int i = 0; i < polygon.Count; i++) remaining.Add(i);
            }
            else
            {
                for (int i = polygon.Count - 1; i >= 0; i--) remaining.Add(i);
            }

            RemoveDegenerateVertices(polygon, remaining);
            List<int> triangles = new List<int>(Mathf.Max(1, remaining.Count - 2) * 3);
            while (remaining.Count > 3)
            {
                if (!ClipOneEar(polygon, remaining, triangles))
                    throw new InvalidOperationException("Polygon could not be triangulated; it may be self-intersecting.");
            }

            if (remaining.Count != 3) throw new InvalidOperationException("Polygon triangulation did not converge.");
            triangles.Add(remaining[0]);
            triangles.Add(remaining[1]);
            triangles.Add(remaining[2]);
            return triangles.ToArray();
        }

        private static bool ClipOneEar(IList<Vector2> polygon, List<int> remaining, List<int> triangles)
        {
            int count = remaining.Count;
            for (int i = 0; i < count; i++)
            {
                int previous = remaining[(i + count - 1) % count];
                int current = remaining[i];
                int next = remaining[(i + 1) % count];
                Vector2 a = polygon[previous];
                Vector2 b = polygon[current];
                Vector2 c = polygon[next];
                if (Cross(b - a, c - b) <= 0f) continue;
                if (AnyPointInside(polygon, remaining, previous, current, next, a, b, c)) continue;
                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                remaining.RemoveAt(i);
                return true;
            }

            return false;
        }

        private static bool AnyPointInside(IList<Vector2> polygon, List<int> remaining, int previous, int current, int next,
            Vector2 a, Vector2 b, Vector2 c)
        {
            for (int p = 0; p < remaining.Count; p++)
            {
                int candidate = remaining[p];
                if (candidate == previous || candidate == current || candidate == next) continue;
                if (StrictlyInside(polygon[candidate], a, b, c)) return true;
            }

            return false;
        }

        /// <summary>Drops near-duplicate and collinear (straight-through) vertices; spikes are kept.</summary>
        private static void RemoveDegenerateVertices(IList<Vector2> polygon, List<int> remaining)
        {
            Vector2 min = polygon[0];
            Vector2 max = polygon[0];
            for (int i = 1; i < polygon.Count; i++)
            {
                min = Vector2.Min(min, polygon[i]);
                max = Vector2.Max(max, polygon[i]);
            }

            float minEdge = Mathf.Max(max.x - min.x, max.y - min.y) * DuplicateEdgeRatio;
            bool changed = true;
            while (changed && remaining.Count > 3)
            {
                changed = false;
                for (int i = 0; i < remaining.Count && remaining.Count > 3; i++)
                {
                    int count = remaining.Count;
                    Vector2 a = polygon[remaining[(i + count - 1) % count]];
                    Vector2 b = polygon[remaining[i]];
                    Vector2 c = polygon[remaining[(i + 1) % count]];
                    Vector2 incoming = b - a;
                    Vector2 outgoing = c - b;
                    float incomingLength = incoming.magnitude;
                    bool duplicate = incomingLength < minEdge;
                    bool straight = Vector2.Dot(incoming, outgoing) > 0f &&
                        Mathf.Abs(Cross(incoming, outgoing)) <= CollinearSine * incomingLength * outgoing.magnitude;
                    if (!duplicate && !straight) continue;
                    remaining.RemoveAt(i);
                    i--;
                    changed = true;
                }
            }
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool StrictlyInside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return Cross(b - a, p - a) > 0f && Cross(c - b, p - b) > 0f && Cross(a - c, p - c) > 0f;
        }
    }
}
