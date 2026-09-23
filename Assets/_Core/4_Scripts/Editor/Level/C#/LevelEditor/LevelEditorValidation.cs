#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;
using SE001.Geometry;
using UnityEngine;

namespace SE001.Editor.Level
{
    internal static class LevelEditorValidation
    {
        public static void Rebuild(
            SE001LevelJson level,
            LayoutDefinition layout,
            LayoutMaskSet masks,
            ColorProfile colors,
            float cellSize,
            List<LevelEditorIssue> issues)
        {
            issues.Clear();
            if (level == null)
            {
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, string.Empty,
                    "No level is open.", "Level document", "Create or open a level.");
                return;
            }

            level.EnsureCollections();
            if (string.IsNullOrWhiteSpace(level.levelId))
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, "levelId",
                    "The level has no name.", "Level settings", "Set a level name before saving.");
            if (level.board == null || !FinitePositive(level.board.size))
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, "board",
                    "The board size is invalid.", "Board settings", "Enter a positive width and height.");

            if (string.IsNullOrWhiteSpace(level.layoutId))
            {
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, "layoutId",
                    "No layout is selected.", "Layout", "Choose a Ready layout before authoring entities.");
            }
            else if (layout == null)
            {
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, "layoutId",
                    "The selected layout is missing.", "Layout " + level.layoutId, "Open Layout Bake and choose or rebake a layout.");
            }
            else if (masks == null)
            {
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, "layoutId",
                    "The selected layout is not ready.", "Layout " + level.layoutId, "Open Layout Bake and rebake this layout.");
            }

            Dictionary<string, string> ids = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<int, int> supply = new Dictionary<int, int>();
            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData source = level.sources[i];
                if (source == null) continue;
                ValidateId(source.stableId, "Source", i, ids, issues);
                ValidateColor(source.materialId, colors, source.stableId, "Color", issues);
                Vector2 size = LevelEditorGeometry.SourceVisualSize();
                Vector2 center = source.position + LevelEditorGeometry.SourceVisualOffset(source);
                ValidateBounds(source.stableId, LevelEditorSelectionKind.Source, center, size, level.board.size, masks, cellSize, issues);
                if (source.logicalAmount <= 0)
                    Add(issues, LevelEditorIssueSeverity.Warning, source.stableId, "amount",
                        "This Source has no sand.", "Source", "Set Amount above zero if this Source should emit sand.");
                int current;
                supply.TryGetValue(source.materialId, out current);
                supply[source.materialId] = current + Mathf.Max(0, source.logicalAmount);
            }

            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData cup = level.cups[i];
                if (cup == null) continue;
                ValidateId(cup.stableId, "Cup", i, ids, issues);
                ValidateColor(cup.acceptedMaterialId, colors, cup.stableId, "Accepted color", issues);
                Vector2 size = LevelEditorGeometry.CupSize(cup);
                ValidateBounds(cup.stableId, LevelEditorSelectionKind.Cup,
                    cup.position + LevelEditorGeometry.CupVisualOffset(), size, level.board.size, masks, cellSize, issues);
                if (cup.requiredAmount <= 0)
                    Add(issues, LevelEditorIssueSeverity.Warning, cup.stableId, "requiredAmount",
                        "This Cup does not require sand.", "Cup", "Set Required Amount above zero for a collection goal.");
                int amount;
                supply.TryGetValue(cup.acceptedMaterialId, out amount);
                if (amount < cup.requiredAmount)
                    Add(issues, LevelEditorIssueSeverity.Warning, cup.stableId, "requiredAmount",
                        "Available sand may be lower than this Cup requires.", "Cup", "Add matching Source sand or lower Required Amount.");
            }

            for (int i = 0; i < level.rotatingObstacles.Count; i++)
            {
                RotatingObstacleData obstacle = level.rotatingObstacles[i];
                if (obstacle == null) continue;
                ValidateId(obstacle.stableId, "Rotating Obstacle", i, ids, issues);
                if (float.IsNaN(obstacle.scale) || float.IsInfinity(obstacle.scale) || obstacle.scale <= 0f ||
                    float.IsNaN(obstacle.barLength) || float.IsInfinity(obstacle.barLength) || obstacle.barLength <= 0f ||
                    float.IsNaN(obstacle.degreesPerSecond) || float.IsInfinity(obstacle.degreesPerSecond) ||
                    obstacle.degreesPerSecond == 0f || float.IsNaN(obstacle.initialAngle) || float.IsInfinity(obstacle.initialAngle))
                    Add(issues, LevelEditorIssueSeverity.Blocking, obstacle.stableId, "scale",
                        "Rotating Obstacle parameters are invalid.", "Rotating Obstacle",
                        "Use a positive scale/length and a nonzero finite rotation speed.");
                else ValidateBounds(obstacle.stableId, LevelEditorSelectionKind.RotatingObstacle,
                    obstacle.position, LevelEditorGeometry.RotatingSize(obstacle), level.board.size,
                    masks, cellSize, issues);
            }

            for (int sourceIndex = 0; sourceIndex < level.sources.Count; sourceIndex++)
            {
                SourceData source = level.sources[sourceIndex];
                if (source == null) continue;
                for (int cupIndex = 0; cupIndex < level.cups.Count; cupIndex++)
                {
                    CupData cup = level.cups[cupIndex];
                    if (cup == null || !LevelEditorGeometry.Overlaps(
                            source.position + LevelEditorGeometry.SourceVisualOffset(source), LevelEditorGeometry.SourceVisualSize(),
                            cup.position + LevelEditorGeometry.CupVisualOffset(), LevelEditorGeometry.CupSize(cup))) continue;
                    Add(issues, LevelEditorIssueSeverity.Blocking, source.stableId, "position",
                        "Source and Cup overlap.", "Source and Cup placement", "Move one entity so their footprints do not overlap.");
                }
            }
        }

        public static bool IsPositionValid(
            SE001LevelJson level,
            LayoutMaskSet masks,
            float cellSize,
            string stableId,
            LevelEditorSelectionKind kind,
            Vector2 position)
        {
            if (level == null || level.board == null || masks == null) return false;
            Vector2 size = LevelEditorGeometry.VisualSizeFor(level, stableId, kind);
            Vector2 center = position + LevelEditorGeometry.VisualOffsetFor(level, stableId, kind);
            if (!LevelEditorGeometry.InBoard(center, size, level.board.size)) return false;
            return kind == LevelEditorSelectionKind.RotatingObstacle
                ? LevelEditorGeometry.RotatingFootprintOpen(masks, center, size.x * 0.5f, cellSize)
                : LevelEditorGeometry.FootprintOpen(masks, center, size, cellSize);
        }

        private static void ValidateId(string id, string label, int index, Dictionary<string, string> ids, List<LevelEditorIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Add(issues, LevelEditorIssueSeverity.Blocking, string.Empty, "stableId",
                    label + " is missing its stable identity.", label + " " + index, "Use the repair action or reopen the document.");
                return;
            }
            string previous;
            if (ids.TryGetValue(id, out previous))
                Add(issues, LevelEditorIssueSeverity.Blocking, id, "stableId",
                    "Two entities use the same identity.", label + " " + id, "Reopen the document so duplicate identities are repaired in memory.");
            else ids.Add(id, label);
        }

        private static void ValidateColor(int colorId, ColorProfile colors, string stableId, string field, List<LevelEditorIssue> issues)
        {
            if (colors == null || !colors.Contains(colorId))
                Add(issues, LevelEditorIssueSeverity.Blocking, stableId, field,
                    "The selected color is not available.", field, "Choose one of the available color swatches.");
        }

        private static void ValidateBounds(
            string stableId, LevelEditorSelectionKind kind, Vector2 position, Vector2 size,
            Vector2 boardSize, LayoutMaskSet masks, float cellSize, List<LevelEditorIssue> issues)
        {
            if (!Finite(position) || !FinitePositive(size))
            {
                Add(issues, LevelEditorIssueSeverity.Blocking, stableId, "position",
                    "The placement is not finite.", kind.ToString(), "Enter finite position and size values.");
                return;
            }
            if (!LevelEditorGeometry.InBoard(position, size, boardSize))
                Add(issues, LevelEditorIssueSeverity.Blocking, stableId, "position",
                    "The entity is outside the board.", kind.ToString(), "Move it inside the visible board area.");
            else if (masks != null && !(kind == LevelEditorSelectionKind.RotatingObstacle
                ? LevelEditorGeometry.RotatingFootprintOpen(masks, position, size.x * 0.5f, cellSize)
                : LevelEditorGeometry.FootprintOpen(masks, position, size, cellSize)))
                Add(issues, LevelEditorIssueSeverity.Blocking, stableId, "position",
                    "The entity overlaps baked layout geometry.", kind.ToString(), "Move it away from walls and static obstacles.");
        }

        private static bool FinitePositive(Vector2 value)
        {
            return Finite(value) && value.x > 0f && value.y > 0f;
        }

        private static bool Finite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }

        private static void Add(
            List<LevelEditorIssue> issues, LevelEditorIssueSeverity severity, string stableId,
            string field, string what, string where, string how)
        {
            issues.Add(new LevelEditorIssue
            {
                Severity = severity,
                StableId = stableId ?? string.Empty,
                FieldKey = field ?? string.Empty,
                What = what,
                Where = where,
                How = how
            });
        }
    }

    internal static class LevelEditorGeometry
    {
        public static Vector2 SourceSize(SourceData source)
        {
            SourceProfile profile = UnityEngine.Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            Vector2 fallback = profile != null ? profile.bodySize : new Vector2(0.8f, 1.2f);
            return fallback;
        }

        public static Vector2 SourceVisualSize()
        {
            Vector2 body = SourceSize(null);
            JarVisualProfile visual = UnityEngine.Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            if (visual == null) return body;
            float ratio = body.y / Mathf.Max(0.001f, visual.sourceBodyHeightPixels);
            return new Vector2(visual.sourceBodyWidthPixels * ratio, visual.sourceCompositeHeightPixels * ratio);
        }

        public static Vector2 SourceVisualOffset(SourceData authored = null)
        {
            return SourceVisualOffset(authored != null && authored.startsOpen);
        }

        public static Vector2 SourceVisualOffset(bool pouring)
        {
            Vector2 body = SourceSize(null);
            Vector2 composite = SourceVisualSize();
            if (pouring)
                return new Vector2(0f, composite.y * 0.5f);
            SourceProfile source = UnityEngine.Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            JarVisualProfile visual = UnityEngine.Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            float bodyCenter = JarVisualGeometry.SourceBodyOffsetY(body.y, source, visual);
            float extra = composite.y - body.y;
            return new Vector2(0f, bodyCenter + extra * 0.5f);
        }

        public static Vector2 CupVisualOffset()
        {
            return new Vector2(0f, CupSize(null).y * 0.5f);
        }

        public static Vector2 VisualOffsetFor(SE001LevelJson level, string stableId,
            LevelEditorSelectionKind kind)
        {
            if (kind == LevelEditorSelectionKind.Source)
            {
                for (int i = 0; i < level.sources.Count; i++)
                    if (level.sources[i] != null && level.sources[i].stableId == stableId)
                        return SourceVisualOffset(level.sources[i]);
                return SourceVisualOffset();
            }
            return kind == LevelEditorSelectionKind.Cup ? CupVisualOffset() : Vector2.zero;
        }

        public static Vector2 VisualSizeFor(SE001LevelJson level, string stableId, LevelEditorSelectionKind kind)
        {
            return kind == LevelEditorSelectionKind.Source ? SourceVisualSize() : SizeFor(level, stableId, kind);
        }

        public static Vector2 CupSize(CupData cup)
        {
            CupProfile profile = UnityEngine.Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            return profile != null ? profile.bodySize : new Vector2(2f, 2f);
        }

        public static float RotatingBarWidth()
        {
            RotatingObstacleProfile profile = UnityEngine.Resources.Load<RotatingObstacleProfile>(
                "Profiles/RotatingObstacleProfile");
            return profile != null ? profile.barWidth : 0.6f;
        }

        public static Vector2 RotatingSize(RotatingObstacleData obstacle)
        {
            float halfLength = obstacle.barLength * obstacle.scale * 0.5f;
            float halfWidth = RotatingBarWidth() * obstacle.scale * 0.5f;
            float diameter = 2f * Mathf.Sqrt(halfLength * halfLength + halfWidth * halfWidth);
            return new Vector2(diameter, diameter);
        }

        public static bool RotatingFootprintOpen(LayoutMaskSet masks, Vector2 position, float radius,
            float cellSize)
        {
            if (masks == null || cellSize <= 0f || radius <= 0f) return false;
            int minX = Mathf.FloorToInt((position.x - radius) / cellSize);
            int maxX = Mathf.CeilToInt((position.x + radius) / cellSize);
            int minY = Mathf.FloorToInt((position.y - radius) / cellSize);
            int maxY = Mathf.CeilToInt((position.y + radius) / cellSize);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 sample = new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                if ((sample - position).sqrMagnitude > radius * radius) continue;
                if (x < 0 || y < 0 || x >= masks.Width || y >= masks.Height) return false;
                int index = masks.Index(x, y);
                if (!masks.ValidMask[index] || masks.StaticMask[index]) return false;
            }
            return true;
        }

        public static Vector2 SizeFor(SE001LevelJson level, string stableId, LevelEditorSelectionKind kind)
        {
            if (level == null) return Vector2.one;
            if (kind == LevelEditorSelectionKind.Source)
            {
                for (int i = 0; i < level.sources.Count; i++)
                    if (level.sources[i] != null && level.sources[i].stableId == stableId) return SourceSize(level.sources[i]);
            }
            if (kind == LevelEditorSelectionKind.Cup)
            {
                for (int i = 0; i < level.cups.Count; i++)
                    if (level.cups[i] != null && level.cups[i].stableId == stableId) return CupSize(level.cups[i]);
            }
            if (kind == LevelEditorSelectionKind.RotatingObstacle)
            {
                for (int i = 0; i < level.rotatingObstacles.Count; i++)
                    if (level.rotatingObstacles[i] != null && level.rotatingObstacles[i].stableId == stableId)
                        return RotatingSize(level.rotatingObstacles[i]);
            }
            return Vector2.one;
        }

        public static Vector2 PositionFor(SE001LevelJson level, string stableId, LevelEditorSelectionKind kind)
        {
            if (level == null) return Vector2.zero;
            if (kind == LevelEditorSelectionKind.Source)
                for (int i = 0; i < level.sources.Count; i++)
                    if (level.sources[i] != null && level.sources[i].stableId == stableId) return level.sources[i].position;
            if (kind == LevelEditorSelectionKind.Cup)
                for (int i = 0; i < level.cups.Count; i++)
                    if (level.cups[i] != null && level.cups[i].stableId == stableId) return level.cups[i].position;
            if (kind == LevelEditorSelectionKind.RotatingObstacle)
                for (int i = 0; i < level.rotatingObstacles.Count; i++)
                    if (level.rotatingObstacles[i] != null && level.rotatingObstacles[i].stableId == stableId)
                        return level.rotatingObstacles[i].position;
            return Vector2.zero;
        }

        public static bool Overlaps(Vector2 firstPosition, Vector2 firstSize, Vector2 secondPosition, Vector2 secondSize)
        {
            return Mathf.Abs(firstPosition.x - secondPosition.x) < (firstSize.x + secondSize.x) * 0.5f &&
                Mathf.Abs(firstPosition.y - secondPosition.y) < (firstSize.y + secondSize.y) * 0.5f;
        }

        public static bool InBoard(Vector2 position, Vector2 size, Vector2 boardSize)
        {
            return position.x - size.x * 0.5f >= 0f && position.y - size.y * 0.5f >= 0f &&
                position.x + size.x * 0.5f <= boardSize.x && position.y + size.y * 0.5f <= boardSize.y;
        }

        public static bool FootprintOpen(LayoutMaskSet masks, Vector2 position, Vector2 size, float cellSize)
        {
            if (masks == null || cellSize <= 0f) return false;
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            Vector2[] samples =
            {
                position,
                position + new Vector2(-halfX, -halfY),
                position + new Vector2(-halfX, halfY),
                position + new Vector2(halfX, -halfY),
                position + new Vector2(halfX, halfY)
            };
            for (int i = 0; i < samples.Length; i++)
            {
                int x = Mathf.FloorToInt(samples[i].x / cellSize);
                int y = Mathf.FloorToInt(samples[i].y / cellSize);
                if (x < 0 || x >= masks.Width || y < 0 || y >= masks.Height) return false;
                int index = masks.Index(x, y);
                if (!masks.ValidMask[index] || masks.StaticMask[index]) return false;
            }
            return true;
        }
    }
}
#endif
