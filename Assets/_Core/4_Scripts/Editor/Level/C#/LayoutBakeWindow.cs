#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SE001.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed class LayoutBakeWindow : EditorWindow
    {
        private TextField svgPathField;
        private TextField layoutIdField;
        private Label previewLabel;
        private Label statusLabel;
        private ScrollView layoutList;

        [MenuItem("SE001/Phase D/Layout Bake")]
        public static void Open()
        {
            LayoutBakeWindow window = GetWindow<LayoutBakeWindow>();
            window.titleContent = new GUIContent("Layout Bake");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 8f;
            rootVisualElement.style.paddingRight = 8f;
            rootVisualElement.style.paddingTop = 8f;
            rootVisualElement.style.paddingBottom = 8f;

            rootVisualElement.Add(new Label("Layout Bake"));
            svgPathField = new TextField("SVG file") { isDelayed = true };
            layoutIdField = new TextField("Layout ID") { isDelayed = true };
            rootVisualElement.Add(svgPathField);
            rootVisualElement.Add(layoutIdField);

            Button browseButton = new Button(BrowseSvg) { text = "Browse SVG" };
            Button previewButton = new Button(PreviewSvg) { text = "Preview" };
            Button bakeButton = new Button(BakeSvg) { text = "Bake" };
            Button rebakeAllButton = new Button(RebakeAll) { text = "Rebake All" };
            rootVisualElement.Add(browseButton);
            rootVisualElement.Add(previewButton);
            rootVisualElement.Add(bakeButton);
            rootVisualElement.Add(rebakeAllButton);

            previewLabel = new Label("Choose an SVG to preview contour count.");
            statusLabel = new Label("Ready.");
            rootVisualElement.Add(previewLabel);
            rootVisualElement.Add(statusLabel);
            rootVisualElement.Add(new Label("Existing layouts"));
            layoutList = new ScrollView(ScrollViewMode.Vertical);
            layoutList.style.flexGrow = 1f;
            rootVisualElement.Add(layoutList);
            RefreshLayoutList();
        }

        private void BrowseSvg()
        {
            string path = EditorUtility.OpenFilePanel("Choose SVG", Application.dataPath, "svg");
            if (string.IsNullOrWhiteSpace(path)) return;
            svgPathField.value = path;
            if (string.IsNullOrWhiteSpace(layoutIdField.value))
                layoutIdField.value = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            PreviewSvg();
        }

        private void PreviewSvg()
        {
            string sourcePath = ResolvePath(svgPathField.value);
            if (!File.Exists(sourcePath))
            {
                previewLabel.text = "SVG file was not found.";
                return;
            }

            SE001LevelJson level;
            string error;
            if (!PhaseBSvgImporter.TryParse(sourcePath, new PhaseBSvgImportSettings(), out level, out error))
            {
                previewLabel.text = "SVG error: " + error;
                return;
            }
            int pointCount = CountPoints(level);
            previewLabel.text = "Contours: " + CountContours(level) + " | Points: " + pointCount +
                " | Board: " + level.board.size.x.ToString("0.##") + " x " + level.board.size.y.ToString("0.##");
        }

        private void BakeSvg()
        {
            string sourcePath = ResolvePath(svgPathField.value);
            LayoutDefinition definition;
            string error;
            bool success = LayoutBaker.TryBakeSvg(
                sourcePath,
                layoutIdField.value,
                new PhaseBSvgImportSettings(),
                out definition,
                out error);
            statusLabel.text = success ? "Baked: " + definition.layoutId : "Bake failed: " + error;
            if (success) RefreshLayoutList();
        }

        private void RebakeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:LayoutDefinition", new[] { "Assets/_Core/Resources/Layouts" });
            int baked = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                LayoutDefinition definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(path);
                if (definition == null || string.IsNullOrWhiteSpace(definition.sourceSvgPath)) continue;
                LayoutDefinition output;
                string error;
                if (LayoutBaker.TryBakeSvg(
                    ResolvePath(definition.sourceSvgPath),
                    definition.layoutId,
                    new PhaseBSvgImportSettings(),
                    out output,
                    out error)) baked++;
                else Debug.LogError("[LayoutBake] " + definition.layoutId + ": " + error);
            }
            statusLabel.text = "Rebaked: " + baked;
            RefreshLayoutList();
        }

        private void RefreshLayoutList()
        {
            if (layoutList == null) return;
            layoutList.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LayoutDefinition", new[] { "Assets/_Core/Resources/Layouts" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                LayoutDefinition definition = AssetDatabase.LoadAssetAtPath<LayoutDefinition>(path);
                if (definition == null) continue;
                string source = string.IsNullOrWhiteSpace(definition.sourceSvgPath) ? "no source" : "source recorded";
                layoutList.Add(new Label(definition.layoutId + " - " + source));
            }
        }

        private static int CountContours(SE001LevelJson level)
        {
            int count = level.board.wallContours.Count;
            for (int i = 0; i < level.staticObstacles.Count; i++)
                if (level.staticObstacles[i] != null) count += level.staticObstacles[i].contours.Count;
            return count;
        }

        private static int CountPoints(SE001LevelJson level)
        {
            int count = 0;
            for (int i = 0; i < level.board.wallContours.Count; i++) count += level.board.wallContours[i].points.Count;
            for (int i = 0; i < level.staticObstacles.Count; i++)
                if (level.staticObstacles[i] != null)
                    for (int c = 0; c < level.staticObstacles[i].contours.Count; c++)
                        count += level.staticObstacles[i].contours[c].points.Count;
            return count;
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            if (Path.IsPathRooted(path)) return path;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
