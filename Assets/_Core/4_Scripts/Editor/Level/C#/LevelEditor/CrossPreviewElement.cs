#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    internal sealed class CrossPreviewElement : VisualElement
    {
        private readonly float length;
        private readonly float scale;
        private readonly float angle;
        private readonly float width;

        public CrossPreviewElement(float barLength, float obstacleScale, float initialAngle, float barWidth)
        {
            length = barLength;
            scale = obstacleScale;
            angle = initialAngle;
            width = barWidth;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            float diameter = Mathf.Max(0.01f, (length + width) * scale);
            float pixelsPerUnit = Mathf.Min(rect.width, rect.height) / diameter;
            float half = length * scale * pixelsPerUnit * 0.5f;
            float thickness = width * scale * pixelsPerUnit;
            Vector2 center = rect.center;
            Painter2D painter = context.painter2D;
            painter.lineCap = LineCap.Round;
            for (int bar = 0; bar < 2; bar++)
            {
                float radians = (angle + (bar == 0 ? 45f : -45f)) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
                painter.BeginPath();
                painter.MoveTo(center - direction * half);
                painter.LineTo(center + direction * half);
                painter.lineWidth = thickness + 2f;
                painter.strokeColor = new Color(0.14f, 0.16f, 0.19f);
                painter.Stroke();
                painter.BeginPath();
                painter.MoveTo(center - direction * half);
                painter.LineTo(center + direction * half);
                painter.lineWidth = thickness;
                painter.strokeColor = new Color(0.55f, 0.56f, 0.58f);
                painter.Stroke();
            }
        }
    }
}
#endif
