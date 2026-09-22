#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using SE001.Data;
using SE001.Geometry;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor.Level
{
    public sealed class PhaseBSvgImportSettings
    {
        public float boardUnitsPerSvgUnit = 0.01f;
        public float curveTolerance = 0.01f;
        public int roundedShapeSegments = 8;
        public int circleSegments = 24;
        public bool legacyTestFixture;
    }

    public static class PhaseBSvgImporter
    {
        private static readonly Regex NumberPattern = new Regex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);

        [MenuItem("SE001/Phase B/Import test_tool.svg")]
        private static void ImportTestFixture()
        {
            string source = Path.Combine(Application.dataPath, "../TrashStuff/test_tool.svg");
            string output = Path.Combine(Application.dataPath, "_Core/Resources/Levels/phase_b_svg_test.json");
            PhaseBSvgImportSettings settings = new PhaseBSvgImportSettings { legacyTestFixture = true };
            string error;
            if (!TryImport(source, output, settings, out error)) throw new InvalidOperationException(error);
            AssetDatabase.Refresh();
        }

        public static bool TryImport(string sourcePath, string outputPath, PhaseBSvgImportSettings settings, out string error)
        {
            error = string.Empty;
            try
            {
                if (settings == null) throw new ArgumentNullException(nameof(settings));
                if (settings.boardUnitsPerSvgUnit <= 0f) throw new ArgumentOutOfRangeException(nameof(settings.boardUnitsPerSvgUnit));
                if (settings.curveTolerance <= 0f) throw new ArgumentOutOfRangeException(nameof(settings.curveTolerance));
                XmlDocument document = new XmlDocument { PreserveWhitespace = false };
                document.Load(sourcePath);
                XmlElement root = document.DocumentElement;
                if (root == null || root.LocalName != "svg") throw new FormatException("SVG root element is missing.");
                Rect viewBox = ParseViewBox(root.GetAttribute("viewBox"));
                SE001LevelJson level = ImportDocument(root, viewBox, settings);
                LevelDataValidator.Validate(level);
                string json = level.ToJson(true) + "\n";
                string previous = File.Exists(outputPath) ? File.ReadAllText(outputPath) : null;
                if (!string.Equals(previous, json, StringComparison.Ordinal))
                {
                    string directory = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    File.WriteAllText(outputPath, json, new UTF8Encoding(false));
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static SE001LevelJson ImportDocument(XmlElement root, Rect viewBox, PhaseBSvgImportSettings settings)
        {
            List<ImportedContour> contours = new List<ImportedContour>();
            CollectContours(root, viewBox, settings, contours);
            if (contours.Count == 0) throw new FormatException("SVG contains no supported closed geometry.");
            SE001LevelJson level = new SE001LevelJson
            {
                levelId = "phase_b_svg_test",
                board = new BoardData { size = new Vector2(viewBox.width * settings.boardUnitsPerSvgUnit, viewBox.height * settings.boardUnitsPerSvgUnit) }
            };

            int wallIndex = -1;
            float largestArea = -1f;
            for (int i = 0; i < contours.Count; i++)
            {
                if (contours[i].Ignored) continue;
                float area = Mathf.Abs(SignedArea(contours[i].Points));
                if (area > largestArea) { largestArea = area; wallIndex = i; }
            }
            if (settings.legacyTestFixture && wallIndex < 0) throw new FormatException("Legacy fixture has no wall geometry.");
            for (int i = 0; i < contours.Count; i++)
            {
                ImportedContour imported = contours[i];
                if (imported.Ignored) continue;
                NormalizePoints(imported.Points);
                PolygonContourData polygon = new PolygonContourData { points = imported.Points };
                if (settings.legacyTestFixture && i == wallIndex)
                {
                    level.board.wallContours.Add(polygon);
                }
                else
                {
                    string id = imported.StableId;
                    if (string.IsNullOrEmpty(id)) id = "fixture_obstacle_" + i.ToString("D3", CultureInfo.InvariantCulture);
                    level.staticObstacles.Add(new StaticObstacleData { stableId = id, contours = new List<PolygonContourData> { polygon } });
                }
            }
            return level;
        }

        private static void CollectContours(XmlElement element, Rect viewBox, PhaseBSvgImportSettings settings, List<ImportedContour> output)
        {
            string localName = element.LocalName;
            if (localName == "path")
            {
                string data = element.GetAttribute("d");
                if (string.IsNullOrWhiteSpace(data)) throw new FormatException("path has no d attribute.");
                ImportedContour contour = new ImportedContour { StableId = SemanticId(element), Ignored = IsIgnored(element) };
                ParsePath(data, viewBox, settings, contour.Points);
                if (contour.Points.Count < 3 || !contour.Closed) throw new FormatException("Only closed path contours are supported.");
                output.Add(contour);
            }
            else if (localName == "rect")
            {
                output.Add(new ImportedContour { StableId = SemanticId(element), Ignored = IsIgnored(element), Points = RectPoints(element, viewBox, settings) });
            }
            else if (localName == "circle")
            {
                output.Add(new ImportedContour { StableId = SemanticId(element), Ignored = IsIgnored(element), Points = CirclePoints(element, viewBox, settings) });
            }
            else if (localName != "svg" && localName != "g" && localName != "defs" && localName != "style")
            {
                throw new FormatException("Unsupported SVG element: " + localName);
            }
            for (int i = 0; i < element.ChildNodes.Count; i++)
            {
                XmlElement child = element.ChildNodes[i] as XmlElement;
                if (child != null) CollectContours(child, viewBox, settings, output);
            }
        }

        private static List<Vector2> RectPoints(XmlElement element, Rect viewBox, PhaseBSvgImportSettings settings)
        {
            float x = Number(element, "x"); float y = Number(element, "y"); float w = Number(element, "width"); float h = Number(element, "height");
            float rx = Mathf.Clamp(Number(element, "rx"), 0f, w * 0.5f); float ry = Mathf.Clamp(Number(element, "ry"), 0f, h * 0.5f);
            if (rx <= 0f || ry <= 0f) return MapPoints(new List<Vector2> { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) }, viewBox, settings);
            List<Vector2> points = new List<Vector2>();
            AddArc(points, new Vector2(x + w - rx, y + ry), rx, ry, -90f, 0f, settings.roundedShapeSegments);
            AddArc(points, new Vector2(x + w - rx, y + h - ry), rx, ry, 0f, 90f, settings.roundedShapeSegments);
            AddArc(points, new Vector2(x + rx, y + h - ry), rx, ry, 90f, 180f, settings.roundedShapeSegments);
            AddArc(points, new Vector2(x + rx, y + ry), rx, ry, 180f, 270f, settings.roundedShapeSegments);
            return MapPoints(points, viewBox, settings);
        }

        private static List<Vector2> CirclePoints(XmlElement element, Rect viewBox, PhaseBSvgImportSettings settings)
        {
            float cx = Number(element, "cx"); float cy = Number(element, "cy"); float radius = Number(element, "r");
            if (radius <= 0f) throw new FormatException("circle radius must be positive.");
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < Mathf.Max(3, settings.circleSegments); i++) { float a = i * Mathf.PI * 2f / settings.circleSegments; points.Add(new Vector2(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius)); }
            return MapPoints(points, viewBox, settings);
        }

        private static void ParsePath(string data, Rect viewBox, PhaseBSvgImportSettings settings, List<Vector2> result)
        {
            MatchCollection matches = Regex.Matches(data, @"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");
            int index = 0; char command = '\0'; Vector2 current = Vector2.zero; Vector2 start = Vector2.zero; Vector2 control = Vector2.zero; bool hasControl = false; bool closed = false;
            while (index < matches.Count)
            {
                string token = matches[index++].Value; if (token.Length == 1 && char.IsLetter(token[0])) command = token[0]; else { index--; }
                if (command == '\0') throw new FormatException("Path command is missing.");
                char absolute = char.ToUpperInvariant(command); bool relative = char.IsLower(command);
                if (absolute == 'Z') { current = start; closed = true; command = '\0'; continue; }
                int count = absolute == 'M' || absolute == 'L' ? 2 : absolute == 'H' || absolute == 'V' ? 1 : absolute == 'C' ? 6 : 0;
                if (count == 0) throw new FormatException("Unsupported path command: " + command);
                if (index + count > matches.Count) throw new FormatException("Incomplete path command: " + command);
                float[] values = new float[count]; for (int i = 0; i < count; i++) values[i] = ParseFloat(matches[index++].Value);
                if (absolute == 'M' || absolute == 'L') { Vector2 next = new Vector2(values[0], values[1]); if (relative) next += current; current = next; if (absolute == 'M') { start = current; command = relative ? 'l' : 'L'; } result.Add(current); hasControl = false; }
                else if (absolute == 'H') { current.x = relative ? current.x + values[0] : values[0]; result.Add(current); hasControl = false; }
                else if (absolute == 'V') { current.y = relative ? current.y + values[0] : values[0]; result.Add(current); hasControl = false; }
                else { Vector2 c1 = new Vector2(values[0], values[1]); Vector2 c2 = new Vector2(values[2], values[3]); Vector2 end = new Vector2(values[4], values[5]); if (relative) { c1 += current; c2 += current; end += current; } int segments = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(current, end) / settings.curveTolerance), 2, 128); for (int s = 1; s <= segments; s++) { float t = s / (float)segments; float u = 1f - t; result.Add(u * u * u * current + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * end); } current = end; control = c2; hasControl = true; }
            }
            for (int i = 0; i < result.Count; i++) result[i] = MapPoint(result[i], viewBox, settings);
            if (closed && result.Count > 1 && result[0] == result[result.Count - 1]) result.RemoveAt(result.Count - 1);
        }

        private static List<Vector2> MapPoints(List<Vector2> points, Rect viewBox, PhaseBSvgImportSettings settings) { for (int i = 0; i < points.Count; i++) points[i] = MapPoint(points[i], viewBox, settings); return points; }
        private static Vector2 MapPoint(Vector2 point, Rect viewBox, PhaseBSvgImportSettings settings) => new BoardSpaceMapper(viewBox, settings.boardUnitsPerSvgUnit).SvgToBoard(point);
        private static float Number(XmlElement element, string name) => string.IsNullOrEmpty(element.GetAttribute(name)) ? 0f : ParseFloat(element.GetAttribute(name));
        private static float ParseFloat(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        private static Rect ParseViewBox(string value) { MatchCollection matches = NumberPattern.Matches(value); if (matches.Count != 4) throw new FormatException("SVG viewBox must contain four numbers."); return new Rect(ParseFloat(matches[0].Value), ParseFloat(matches[1].Value), ParseFloat(matches[2].Value), ParseFloat(matches[3].Value)); }
        private static string SemanticId(XmlElement element) { string id = element.GetAttribute("id"); if (id.StartsWith("Obstacle_", StringComparison.OrdinalIgnoreCase)) return id.Substring("Obstacle_".Length); if (id.Equals("Board", StringComparison.OrdinalIgnoreCase) || id.Equals("Wall", StringComparison.OrdinalIgnoreCase)) return string.Empty; return string.Empty; }
        private static bool IsIgnored(XmlElement element) { string id = element.GetAttribute("id"); return id.StartsWith("Ignore_", StringComparison.OrdinalIgnoreCase) || element.GetAttribute("class").IndexOf("st1", StringComparison.OrdinalIgnoreCase) >= 0; }
        private static float SignedArea(List<Vector2> points) { float area = 0f; for (int i = 0; i < points.Count; i++) { Vector2 a = points[i]; Vector2 b = points[(i + 1) % points.Count]; area += a.x * b.y - b.x * a.y; } return area * 0.5f; }
        private static void NormalizePoints(List<Vector2> points) { for (int i = points.Count - 1; i > 0; i--) if (points[i] == points[i - 1]) points.RemoveAt(i); if (points.Count > 1 && points[0] == points[points.Count - 1]) points.RemoveAt(points.Count - 1); }
        private static void AddArc(List<Vector2> points, Vector2 center, float rx, float ry, float from, float to, int segments) { for (int i = 0; i < segments; i++) { float t = Mathf.Lerp(from, to, i / (float)segments) * Mathf.Deg2Rad; points.Add(center + new Vector2(Mathf.Cos(t) * rx, Mathf.Sin(t) * ry)); } }
        private sealed class ImportedContour { public List<Vector2> Points = new List<Vector2>(); public string StableId; public bool Ignored; public bool Closed = true; }
    }
}
#endif
