#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SE001.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    internal sealed class LevelEditorLayoutPreviewData
    {
        public readonly Vector2 BoardSize;
        public readonly List<List<Vector2>> Walls;
        public readonly List<List<Vector2>> Obstacles;
        public readonly string LayoutId;
        public readonly string ContourHash;
        public readonly string SourceSvgPath;

        public LevelEditorLayoutPreviewData(
            string layoutId,
            string contourHash,
            string sourceSvgPath,
            SE001LevelJson level)
        {
            LayoutId = layoutId ?? string.Empty;
            ContourHash = contourHash ?? string.Empty;
            SourceSvgPath = sourceSvgPath ?? string.Empty;
            BoardSize = level != null && level.board != null ? level.board.size : Vector2.zero;
            Walls = new List<List<Vector2>>();
            Obstacles = new List<List<Vector2>>();
            if (level == null || level.board == null) return;

            Copy(level.board.wallContours, Walls);
            if (level.staticObstacles == null) return;
            for (int i = 0; i < level.staticObstacles.Count; i++)
            {
                StaticObstacleData obstacle = level.staticObstacles[i];
                if (obstacle != null) Copy(obstacle.contours, Obstacles);
            }
        }

        public bool IsRenderable => BoardSize.x > 0f && BoardSize.y > 0f &&
            (Walls.Count > 0 || Obstacles.Count > 0);

        private static void Copy(List<PolygonContourData> source, List<List<Vector2>> target)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                PolygonContourData contour = source[i];
                if (contour == null || contour.points == null || contour.points.Count < 3) continue;
                target.Add(new List<Vector2>(contour.points));
            }
        }
    }

    internal sealed class LevelEditorLayoutPreviewCache
    {
        private readonly LayoutBakeLibrary svgLibrary;
        private string cachedLayoutId = string.Empty;
        private string cachedContourHash = string.Empty;
        private string cachedSourcePath = string.Empty;
        private DateTime cachedSourceStamp;
        private long cachedSourceLength = -1L;
        private bool hasCacheKey;
        private LevelEditorLayoutPreviewData cachedData;

        public LevelEditorLayoutPreviewCache(LayoutBakeLibrary svgLibrary)
        {
            this.svgLibrary = svgLibrary ?? throw new ArgumentNullException(nameof(svgLibrary));
        }

        public LevelEditorLayoutPreviewData Resolve(LayoutDefinition definition)
        {
            if (definition == null) return null;

            string sourcePath = LayoutBaker.ResolveProjectPath(definition.sourceSvgPath);
            bool sourceExists = !string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath);
            DateTime sourceStamp = sourceExists ? File.GetLastWriteTimeUtc(sourcePath) : DateTime.MinValue;
            long sourceLength = sourceExists ? new FileInfo(sourcePath).Length : -1L;
            string layoutId = definition.layoutId ?? string.Empty;
            string contourHash = definition.contourHash ?? string.Empty;

            if (hasCacheKey && string.Equals(cachedLayoutId, layoutId, StringComparison.Ordinal) &&
                string.Equals(cachedContourHash, contourHash, StringComparison.Ordinal) &&
                string.Equals(cachedSourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) &&
                cachedSourceStamp == sourceStamp && cachedSourceLength == sourceLength)
                return cachedData;

            cachedLayoutId = layoutId;
            cachedContourHash = contourHash;
            cachedSourcePath = sourcePath;
            cachedSourceStamp = sourceStamp;
            cachedSourceLength = sourceLength;
            hasCacheKey = true;
            cachedData = null;

            if (!sourceExists || string.IsNullOrEmpty(contourHash)) return null;

            SvgParseResult parsed = svgLibrary.ParseSvg(sourcePath);
            if (parsed == null || !parsed.Parsed || parsed.Level == null ||
                !string.Equals(parsed.ContourHash, contourHash, StringComparison.Ordinal))
                return null;

            cachedData = new LevelEditorLayoutPreviewData(layoutId, contourHash, sourcePath, parsed.Level);
            return cachedData.IsRenderable ? cachedData : null;
        }

        public void Clear()
        {
            cachedLayoutId = string.Empty;
            cachedContourHash = string.Empty;
            cachedSourcePath = string.Empty;
            cachedSourceStamp = default(DateTime);
            cachedSourceLength = -1L;
            hasCacheKey = false;
            cachedData = null;
        }
    }

    internal sealed class LevelEditorLayoutPreviewElement : VisualElement
    {
        private static readonly Color BoardColor = new Color32(218, 219, 221, 255);
        private static readonly Color WallColor = new Color32(148, 150, 153, 255);
        private static readonly Color ObstacleColor = new Color32(133, 135, 140, 255);
        private static readonly Color FrameColor = new Color32(90, 92, 96, 255);
        private LevelEditorLayoutPreviewData data;

        public LevelEditorLayoutPreviewElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void SetData(LevelEditorLayoutPreviewData value)
        {
            data = value;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (data == null || !data.IsRenderable) return;
            Rect area = contentRect;
            if (area.width < 4f || area.height < 4f) return;
            float scale = Mathf.Min(area.width / data.BoardSize.x, area.height / data.BoardSize.y);
            Vector2 size = data.BoardSize * scale;
            Vector2 origin = new Vector2(area.x + (area.width - size.x) * 0.5f,
                area.y + (area.height - size.y) * 0.5f);
            Painter2D painter = context.painter2D;

            painter.fillColor = BoardColor;
            painter.BeginPath();
            painter.MoveTo(origin);
            painter.LineTo(origin + new Vector2(size.x, 0f));
            painter.LineTo(origin + size);
            painter.LineTo(origin + new Vector2(0f, size.y));
            painter.ClosePath();
            painter.Fill();

            FillAll(painter, data.Walls, WallColor, origin, scale, size.y);
            FillAll(painter, data.Obstacles, ObstacleColor, origin, scale, size.y);

            painter.strokeColor = FrameColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(origin);
            painter.LineTo(origin + new Vector2(size.x, 0f));
            painter.LineTo(origin + size);
            painter.LineTo(origin + new Vector2(0f, size.y));
            painter.ClosePath();
            painter.Stroke();
        }

        private static void FillAll(Painter2D painter, List<List<Vector2>> contours, Color color,
            Vector2 origin, float scale, float height)
        {
            painter.fillColor = color;
            for (int contourIndex = 0; contourIndex < contours.Count; contourIndex++)
            {
                List<Vector2> points = contours[contourIndex];
                painter.BeginPath();
                for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
                {
                    Vector2 point = points[pointIndex];
                    Vector2 ui = new Vector2(origin.x + point.x * scale,
                        origin.y + height - point.y * scale);
                    if (pointIndex == 0) painter.MoveTo(ui);
                    else painter.LineTo(ui);
                }

                painter.ClosePath();
                painter.Fill(FillRule.OddEven);
            }
        }
    }
}
#endif
