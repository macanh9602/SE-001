using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Data
{
    public static class LevelDataValidator
    {
        public static void Validate(SE001LevelJson level)
        {
            List<string> errors = new List<string>();
            if (!TryValidate(level, errors)) throw new FormatException(string.Join(" | ", errors));
        }

        public static bool TryValidate(SE001LevelJson level, List<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            errors.Clear();
            if (level == null) { errors.Add("Level is null."); return false; }
            level.EnsureCollections();
            if (level.schemaVersion <= 0) errors.Add("schemaVersion must be positive.");
            if (string.IsNullOrWhiteSpace(level.levelId)) errors.Add("levelId is required.");
            if (!Finite(level.board.size) || level.board.size.x <= 0f || level.board.size.y <= 0f) errors.Add("board.size must be finite and positive.");
            ValidateContours(level.board.wallContours, "board.wallContours", errors);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < level.staticObstacles.Count; i++)
            {
                StaticObstacleData item = level.staticObstacles[i];
                if (item == null) { errors.Add($"staticObstacles[{i}] is null."); continue; }
                if (string.IsNullOrWhiteSpace(item.stableId) || !ids.Add(item.stableId)) errors.Add($"staticObstacles[{i}] has a missing or duplicate stableId.");
                ValidateContours(item.contours, $"staticObstacles[{i}].contours", errors);
            }
            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData item = level.sources[i];
                string id = item == null ? null : item.stableId;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) errors.Add($"sources[{i}] has a missing or duplicate stableId.");
            }
            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData item = level.cups[i];
                string id = item == null ? null : item.stableId;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) errors.Add($"cups[{i}] has a missing or duplicate stableId.");
            }
            return errors.Count == 0;
        }

        private static void ValidateContours(List<PolygonContourData> contours, string label, List<string> errors)
        {
            if (contours == null || contours.Count == 0) { errors.Add($"{label} must contain at least one contour."); return; }
            for (int i = 0; i < contours.Count; i++)
            {
                PolygonContourData contour = contours[i];
                if (contour == null || contour.points == null || contour.points.Count < 3) { errors.Add($"{label}[{i}] must contain at least three vertices."); continue; }
                HashSet<Vector2Int> unique = new HashSet<Vector2Int>();
                for (int p = 0; p < contour.points.Count; p++)
                {
                    Vector2 point = contour.points[p];
                    if (!Finite(point)) errors.Add($"{label}[{i}] contains a non-finite point.");
                    unique.Add(new Vector2Int(Mathf.RoundToInt(point.x * 100000f), Mathf.RoundToInt(point.y * 100000f)));
                }
                if (unique.Count < 3) errors.Add($"{label}[{i}] has fewer than three unique vertices.");
                if (contour.points[0] == contour.points[contour.points.Count - 1]) errors.Add($"{label}[{i}] contains a duplicate terminal vertex.");
                if (Mathf.Abs(SignedArea(contour.points)) <= 0.000001f) errors.Add($"{label}[{i}] is degenerate.");
            }
        }

        private static float SignedArea(List<Vector2> points)
        {
            float area = 0f;
            for (int i = 0; i < points.Count; i++) { Vector2 a = points[i]; Vector2 b = points[(i + 1) % points.Count]; area += a.x * b.y - b.x * a.y; }
            return area * 0.5f;
        }

        private static bool Finite(Vector2 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
