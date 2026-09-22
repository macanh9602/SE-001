using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Geometry
{
    public static class PolygonTriangulator
    {
        public static int[] Triangulate(IList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3) throw new ArgumentException("A polygon needs at least three points.");
            List<int> remaining = new List<int>(polygon.Count);
            if (PolygonUtility.SignedArea(polygon) > 0f) for (int i = 0; i < polygon.Count; i++) remaining.Add(i);
            else for (int i = polygon.Count - 1; i >= 0; i--) remaining.Add(i);
            List<int> triangles = new List<int>((polygon.Count - 2) * 3);
            int guard = 0;
            while (remaining.Count > 3 && guard++ < polygon.Count * polygon.Count)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int previous = remaining[(i + remaining.Count - 1) % remaining.Count]; int current = remaining[i]; int next = remaining[(i + 1) % remaining.Count];
                    Vector2 a = polygon[previous]; Vector2 b = polygon[current]; Vector2 c = polygon[next];
                    if (Cross(b - a, c - b) <= 0f) continue;
                    bool contains = false;
                    for (int p = 0; p < remaining.Count; p++) { int candidate = remaining[p]; if (candidate == previous || candidate == current || candidate == next) continue; if (PointInTriangle(polygon[candidate], a, b, c)) { contains = true; break; } }
                    if (contains) continue;
                    triangles.Add(previous); triangles.Add(current); triangles.Add(next); remaining.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) throw new InvalidOperationException("Polygon could not be triangulated; it may be self-intersecting.");
            }
            if (remaining.Count != 3) throw new InvalidOperationException("Polygon triangulation did not converge.");
            triangles.Add(remaining[0]); triangles.Add(remaining[1]); triangles.Add(remaining[2]);
            return triangles.ToArray();
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) { float ab = Cross(b - a, p - a); float bc = Cross(c - b, p - b); float ca = Cross(a - c, p - c); return ab >= -0.00001f && bc >= -0.00001f && ca >= -0.00001f; }
    }
}
