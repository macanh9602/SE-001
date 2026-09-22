using System.Collections.Generic;
using UnityEngine;

namespace SE001.Geometry
{
    public static class PolygonUtility
    {
        public static bool ContainsPoint(IList<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i]; Vector2 b = polygon[j];
                if (PointOnSegment(a, b, point)) return true;
                bool crosses = (a.y > point.y) != (b.y > point.y);
                if (crosses && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public static bool PointOnSegment(Vector2 a, Vector2 b, Vector2 point)
        {
            Vector2 ab = b - a; Vector2 ap = point - a;
            float cross = ab.x * ap.y - ab.y * ap.x;
            if (Mathf.Abs(cross) > 0.00001f) return false;
            return point.x >= Mathf.Min(a.x, b.x) - 0.00001f && point.x <= Mathf.Max(a.x, b.x) + 0.00001f && point.y >= Mathf.Min(a.y, b.y) - 0.00001f && point.y <= Mathf.Max(a.y, b.y) + 0.00001f;
        }

        public static float SignedArea(IList<Vector2> polygon)
        {
            float area = 0f; for (int i = 0; i < polygon.Count; i++) { Vector2 a = polygon[i]; Vector2 b = polygon[(i + 1) % polygon.Count]; area += a.x * b.y - b.x * a.y; } return area * 0.5f;
        }
    }
}
