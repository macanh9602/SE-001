using System;
using System.Collections.Generic;
using SE001.Geometry;
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

        public static void Validate(SE001LevelJson level, ColorProfile colorProfile)
        {
            List<string> errors = new List<string>();
            if (!TryValidate(level, colorProfile, 0.1f, int.MaxValue, errors)) throw new FormatException(string.Join(" | ", errors));
        }

        public static void Validate(SE001LevelJson level, ColorProfile colorProfile, float cellSize, int maxCells)
        {
            List<string> errors = new List<string>();
            if (!TryValidate(level, colorProfile, cellSize, maxCells, errors)) throw new FormatException(string.Join(" | ", errors));
        }

        public static void Validate(
            SE001LevelJson level,
            ColorProfile colorProfile,
            float cellSize,
            int maxCells,
            LayoutDefinition layout)
        {
            List<string> errors = new List<string>();
            if (!TryValidate(level, colorProfile, cellSize, maxCells, layout, errors))
                throw new FormatException(string.Join(" | ", errors));
        }

        public static bool TryValidate(SE001LevelJson level, List<string> errors)
        {
            return TryValidate(level, null, 0.1f, int.MaxValue, errors);
        }

        public static bool TryValidate(SE001LevelJson level, ColorProfile colorProfile, List<string> errors)
        {
            return TryValidate(level, colorProfile, 0.1f, int.MaxValue, errors);
        }

        public static bool TryValidate(
            SE001LevelJson level,
            ColorProfile colorProfile,
            float cellSize,
            int maxCells,
            List<string> errors)
        {
            return TryValidateInternal(level, colorProfile, cellSize, maxCells, null, errors);
        }

        public static bool TryValidate(
            SE001LevelJson level,
            ColorProfile colorProfile,
            float cellSize,
            int maxCells,
            LayoutDefinition layout,
            List<string> errors)
        {
            return TryValidateInternal(level, colorProfile, cellSize, maxCells, layout, errors);
        }

        public static bool TryValidate(
            SE001LevelJson level,
            ColorProfile colorProfile,
            float cellSize,
            List<string> errors)
        {
            return TryValidateInternal(level, colorProfile, cellSize, int.MaxValue, null, errors);
        }

        private static bool TryValidateInternal(
            SE001LevelJson level,
            ColorProfile colorProfile,
            float cellSize,
            int maxCells,
            LayoutDefinition layout,
            List<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            errors.Clear();
            if (cellSize <= 0f || float.IsNaN(cellSize) || float.IsInfinity(cellSize))
                errors.Add("cellSize must be finite and positive.");
            if (maxCells <= 0) errors.Add("maxCells must be positive.");
            if (level == null)
            {
                errors.Add("Level is null.");
                return false;
            }
            level.EnsureCollections();
            if (level.schemaVersion <= 0) errors.Add("schemaVersion must be positive.");
            if (level.schemaVersion > 5) errors.Add("schemaVersion is newer than the supported runtime schema.");
            if (string.IsNullOrWhiteSpace(level.levelId)) errors.Add("levelId is required.");
            if (!Finite(level.board.size) || level.board.size.x <= 0f || level.board.size.y <= 0f)
                errors.Add("board.size must be finite and positive.");
            if (level.drawInkBudget < 0f || float.IsNaN(level.drawInkBudget) ||
                float.IsInfinity(level.drawInkBudget))
                errors.Add("drawInkBudget must be finite and non-negative.");
            if (level.schemaVersion >= 5 &&
                (!Finite(level.sourceScale) || level.sourceScale <= 0f ||
                 !Finite(level.bowlScale) || level.bowlScale <= 0f ||
                 !Finite(level.sourceEmissionRate) || level.sourceEmissionRate <= 0f ||
                 !Finite(level.sourceEmissionRate * 60f) ||
                 level.sourceStreamWidth <= 0 ||
                 (Finite(level.board.size) && cellSize > 0f &&
                  level.sourceStreamWidth > Mathf.CeilToInt(level.board.size.x / cellSize))))
                errors.Add("Level Source/Bowl tuning must have positive finite scales and emission rate, and positive stream width.");
            bool usesBakedLayout = level.schemaVersion >= 3;
            if (usesBakedLayout)
            {
                if (string.IsNullOrWhiteSpace(level.layoutId)) errors.Add("layoutId is required for schema 3.");
                if (level.board.wallContours.Count > 0 || level.staticObstacles.Count > 0)
                    errors.Add("schema 3 levels must reference layoutId instead of embedding contours.");
                if (layout == null)
                {
                    errors.Add("schema 3 levels require a loaded LayoutDefinition.");
                }
                else
                {
                    if (!Approximately(level.board.size, layout.boardSize))
                        errors.Add("board.size does not match the baked layout board size.");
                    if (layout.mask == null || layout.layoutPrefab == null)
                        errors.Add("layout definition is missing its baked mask or prefab.");
                }
            }
            else
            {
                ValidateContours(level.board.wallContours, "board.wallContours", errors);
            }
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; !usesBakedLayout && i < level.staticObstacles.Count; i++)
            {
                StaticObstacleData item = level.staticObstacles[i];
                if (item == null)
                {
                    errors.Add($"staticObstacles[{i}] is null.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.stableId) || !ids.Add(item.stableId))
                    errors.Add($"staticObstacles[{i}] has a missing or duplicate stableId.");
                ValidateContours(item.contours, $"staticObstacles[{i}].contours", errors);
            }
            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData item = level.sources[i];
                string id = item == null ? null : item.stableId;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    errors.Add($"sources[{i}] has a missing or duplicate stableId.");
                bool invalidSource = item != null &&
                    (item.materialId <= 0 || item.logicalAmount <= 0 || !Finite(item.position) ||
                     !InsideBoard(item.position, level.board.size));
                if (invalidSource)
                    errors.Add($"sources[{i}] has invalid material, amount, position, or is outside board.");
                if (item != null && colorProfile != null)
                {
                    if (!colorProfile.Contains(item.materialId))
                        errors.Add($"sources[{i}] references unknown colorId {item.materialId}.");
                    else if (!colorProfile.HasJarMaterials(item.materialId))
                        errors.Add($"sources[{i}] colorId {item.materialId} is missing sourceMouthMaterial or cupCapMaterial.");
                }
            }
            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData item = level.cups[i];
                string id = item == null ? null : item.stableId;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    errors.Add($"cups[{i}] has a missing or duplicate stableId.");
                bool invalidCup = item != null &&
                    (item.acceptedMaterialId <= 0 || item.requiredAmount <= 0 || !Finite(item.position) ||
                     !InsideBoard(item.position, level.board.size));
                if (invalidCup)
                    errors.Add($"cups[{i}] has invalid material, amount, or position.");
                if (item != null && colorProfile != null)
                {
                    if (!colorProfile.Contains(item.acceptedMaterialId))
                        errors.Add($"cups[{i}] references unknown colorId {item.acceptedMaterialId}.");
                    else if (!colorProfile.HasJarMaterials(item.acceptedMaterialId))
                        errors.Add($"cups[{i}] colorId {item.acceptedMaterialId} is missing sourceMouthMaterial or cupCapMaterial.");
                }
            }
            for (int i = 0; i < level.rotatingObstacles.Count; i++)
            {
                RotatingObstacleData item = level.rotatingObstacles[i];
                string id = item == null ? null : item.stableId;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    errors.Add($"rotatingObstacles[{i}] has a missing or duplicate stableId.");
                if (item == null || !Finite(item.position) || !InsideBoard(item.position, level.board.size) ||
                    !Finite(item.scale) || item.scale <= 0f || !Finite(item.barLength) || item.barLength <= 0f ||
                    !Finite(item.initialAngle) || !Finite(item.degreesPerSecond) || item.degreesPerSecond == 0f)
                    errors.Add($"rotatingObstacles[{i}] has invalid position, scale, length, angle, or rotation speed.");
                else if (level.schemaVersion < 4 ||
                    item.position.x - item.barLength * item.scale * 0.5f < 0f ||
                    item.position.y - item.barLength * item.scale * 0.5f < 0f ||
                    item.position.x + item.barLength * item.scale * 0.5f > level.board.size.x ||
                    item.position.y + item.barLength * item.scale * 0.5f > level.board.size.y)
                    errors.Add($"rotatingObstacles[{i}] requires schema 4 and a full sweep inside the board.");
            }
            ValidateSupply(level, errors);
            if (!usesBakedLayout) ValidateStaticOverlap(level, errors);
            if (errors.Count == 0 && !level.requiresDrawing)
                ValidateReachability(level, cellSize, maxCells, layout, errors);
            return errors.Count == 0;
        }

        private static void ValidateSupply(SE001LevelJson level, List<string> errors)
        {
            Dictionary<int, int> supply = new Dictionary<int, int>();
            Dictionary<int, int> required = new Dictionary<int, int>();
            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData source = level.sources[i];
                if (source == null) continue;
                Add(supply, source.materialId, Mathf.Max(0, source.logicalAmount));
            }
            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData cup = level.cups[i];
                if (cup == null) continue;
                Add(required, cup.acceptedMaterialId, Mathf.Max(0, cup.requiredAmount));
            }
            foreach (KeyValuePair<int, int> item in required)
            {
                int available = supply.ContainsKey(item.Key) ? supply[item.Key] : 0;
                if (available < item.Value)
                    errors.Add($"colorId {item.Key} supply {available} is below required {item.Value}.");
            }
        }

        private static void ValidateStaticOverlap(SE001LevelJson level, List<string> errors)
        {
            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData source = level.sources[i];
                if (source != null && ContainsAnyStatic(level, source.position))
                    errors.Add($"sources[{i}] overlaps board wall or static obstacle.");
            }
            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData cup = level.cups[i];
                if (cup != null && ContainsAnyStatic(level, cup.position))
                    errors.Add($"cups[{i}] overlaps board wall or static obstacle.");
            }
        }

        private static bool ContainsAnyStatic(SE001LevelJson level, Vector2 point)
        {
            if (ContainsAny(level.board.wallContours, point)) return true;
            for (int i = 0; i < level.staticObstacles.Count; i++)
                if (level.staticObstacles[i] != null && ContainsAny(level.staticObstacles[i].contours, point))
                    return true;
            return false;
        }

        private static bool ContainsAny(List<PolygonContourData> contours, Vector2 point)
        {
            if (contours == null) return false;
            for (int i = 0; i < contours.Count; i++)
                if (contours[i] != null && PolygonUtility.ContainsPoint(contours[i].points, point))
                    return true;
            return false;
        }

        private static void Add(Dictionary<int, int> values, int key, int amount)
        {
            if (values.ContainsKey(key)) values[key] += amount;
            else values.Add(key, amount);
        }

        private static void ValidateReachability(
            SE001LevelJson level,
            float cellSize,
            int maxCells,
            LayoutDefinition layout,
            List<string> errors)
        {
            LayoutMaskSet masks;
            if (layout != null)
            {
                string maskError;
                if (!layout.TryBuildMaskSet(cellSize, maxCells, out masks, out maskError))
                {
                    errors.Add("Baked layout cannot be used for reachability: " + maskError);
                    return;
                }
            }
            else
            {
                try
                {
                    masks = LayoutRasterizer.Rasterize(level, cellSize, maxCells);
                }
                catch (Exception exception)
                {
                    errors.Add("Reachability rasterization failed: " + exception.Message);
                    return;
                }
            }

            ValidateReachability(level, masks, cellSize, errors);
        }

        private static void ValidateReachability(
            SE001LevelJson level,
            LayoutMaskSet masks,
            float cellSize,
            List<string> errors)
        {
            for (int cupIndex = 0; cupIndex < level.cups.Count; cupIndex++)
            {
                CupData cup = level.cups[cupIndex];
                if (cup == null) continue;
                bool reachable = false;
                for (int sourceIndex = 0; sourceIndex < level.sources.Count; sourceIndex++)
                {
                    SourceData source = level.sources[sourceIndex];
                    if (source != null && source.materialId == cup.acceptedMaterialId && HasPath(masks, source.position, cup.position, cellSize))
                    {
                        reachable = true;
                        break;
                    }
                }

                if (!reachable)
                    errors.Add(
                        $"cups[{cupIndex}] has no free-fall path from a source with colorId " +
                        $"{cup.acceptedMaterialId}; set requiresDrawing when authoring a drawn route.");
            }
        }

        private static bool HasPath(LayoutMaskSet masks, Vector2 start, Vector2 target, float cellSize)
        {
            int startIndex = FindNearestValidCell(masks, start, cellSize);
            int targetIndex = FindNearestValidCell(masks, target, cellSize);
            if (startIndex < 0 || targetIndex < 0) return false;
            if (startIndex == targetIndex) return true;

            bool[] visited = new bool[masks.ValidMask.Length];
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startIndex);
            visited[startIndex] = true;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int x = current % masks.Width;
                int y = current / masks.Width;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0) continue;
                        int nextX = x + offsetX;
                        int nextY = y + offsetY;
                        if (nextX < 0 || nextX >= masks.Width || nextY < 0 || nextY >= masks.Height) continue;
                        int next = masks.Index(nextX, nextY);
                        if (!masks.ValidMask[next] || visited[next]) continue;
                        if (next == targetIndex) return true;
                        visited[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }
            return false;
        }

        private static int FindNearestValidCell(LayoutMaskSet masks, Vector2 point, float cellSize)
        {
            int centerX = Mathf.Clamp(Mathf.FloorToInt(point.x / cellSize), 0, masks.Width - 1);
            int centerY = Mathf.Clamp(Mathf.FloorToInt(point.y / cellSize), 0, masks.Height - 1);
            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;
            for (int y = 0; y < masks.Height; y++)
            {
                for (int x = 0; x < masks.Width; x++)
                {
                    int index = masks.Index(x, y);
                    if (!masks.ValidMask[index]) continue;
                    float distance = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    bestIndex = index;
                }
            }
            return bestIndex;
        }

        private static void ValidateContours(List<PolygonContourData> contours, string label, List<string> errors)
        {
            if (contours == null || contours.Count == 0)
            {
                errors.Add($"{label} must contain at least one contour.");
                return;
            }
            for (int i = 0; i < contours.Count; i++)
            {
                PolygonContourData contour = contours[i];
                if (contour == null || contour.points == null || contour.points.Count < 3)
                {
                    errors.Add($"{label}[{i}] must contain at least three vertices.");
                    continue;
                }
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

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        private static bool ValidOptionalSize(Vector2 value) => value == Vector2.zero || Finite(value) && value.x > 0f && value.y > 0f;
        private static bool InsideBoard(Vector2 position, Vector2 size) => position.x >= 0f && position.y >= 0f && position.x <= size.x && position.y <= size.y;
        private static bool Approximately(Vector2 left, Vector2 right) =>
            Mathf.Abs(left.x - right.x) <= 0.0001f && Mathf.Abs(left.y - right.y) <= 0.0001f;
    }
}
