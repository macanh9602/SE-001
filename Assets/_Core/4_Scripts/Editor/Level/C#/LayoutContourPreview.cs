#if UNITY_EDITOR
using System.Collections.Generic;
using SE001.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    /// <summary>
    /// Read-only board thumbnail for a parsed SVG layout. Geometry is set once per parse; repaint only
    /// replays the cached contour lists through Painter2D (no parsing, no allocation of new data).
    /// </summary>
    public sealed class LayoutContourPreview : VisualElement
    {
        private static readonly Color BoardColor = new Color(0.61f, 0.63f, 0.87f, 1f);
        private static readonly Color WallColor = new Color(0.18f, 0.15f, 0.39f, 1f);
        private static readonly Color ObstacleColor = new Color(0.34f, 0.29f, 0.62f, 1f);
        private static readonly Color FrameColor = new Color(1f, 1f, 1f, 0.35f);

        private readonly List<List<Vector2>> walls = new List<List<Vector2>>();
        private readonly List<List<Vector2>> obstacles = new List<List<Vector2>>();
        private Vector2 boardSize;

        public LayoutContourPreview()
        {
            AddToClassList("lb-preview");
            generateVisualContent += Draw;
        }

        public bool HasLayout => boardSize.x > 0f && boardSize.y > 0f;

        public void SetLayout(SE001LevelJson level)
        {
            walls.Clear();
            obstacles.Clear();
            boardSize = Vector2.zero;
            if (level != null && level.board != null)
            {
                boardSize = level.board.size;
                Copy(level.board.wallContours, walls);
                if (level.staticObstacles != null)
                {
                    for (int i = 0; i < level.staticObstacles.Count; i++)
                        if (level.staticObstacles[i] != null)
                            Copy(level.staticObstacles[i].contours, obstacles);
                }
            }

            MarkDirtyRepaint();
        }

        private static void Copy(List<PolygonContourData> source, List<List<Vector2>> target)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null || source[i].points == null || source[i].points.Count < 3) continue;
                target.Add(new List<Vector2>(source[i].points));
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            if (!HasLayout) return;
            Rect area = contentRect;
            if (area.width < 4f || area.height < 4f) return;
            float scale = Mathf.Min(area.width / boardSize.x, area.height / boardSize.y);
            Vector2 size = boardSize * scale;
            Vector2 origin = new Vector2(area.x + (area.width - size.x) * 0.5f, area.y + (area.height - size.y) * 0.5f);
            Painter2D painter = context.painter2D;

            painter.fillColor = BoardColor;
            painter.BeginPath();
            painter.MoveTo(origin);
            painter.LineTo(origin + new Vector2(size.x, 0f));
            painter.LineTo(origin + size);
            painter.LineTo(origin + new Vector2(0f, size.y));
            painter.ClosePath();
            painter.Fill();

            FillAll(painter, walls, WallColor, origin, scale, size.y);
            FillAll(painter, obstacles, ObstacleColor, origin, scale, size.y);

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

        private static void FillAll(Painter2D painter, List<List<Vector2>> contours, Color color, Vector2 origin, float scale, float height)
        {
            painter.fillColor = color;
            for (int c = 0; c < contours.Count; c++)
            {
                List<Vector2> points = contours[c];
                painter.BeginPath();
                for (int p = 0; p < points.Count; p++)
                {
                    // Board space is y-up; UI space is y-down.
                    Vector2 ui = new Vector2(origin.x + points[p].x * scale, origin.y + height - points[p].y * scale);
                    if (p == 0) painter.MoveTo(ui);
                    else painter.LineTo(ui);
                }

                painter.ClosePath();
                painter.Fill(FillRule.OddEven);
            }
        }
    }
}
#endif
