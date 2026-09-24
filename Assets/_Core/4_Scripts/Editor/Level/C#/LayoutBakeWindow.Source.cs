#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using SE001.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LayoutBakeWindow
    {
        private VisualElement sourceSection;
        private VisualElement sourceEmpty;
        private VisualElement sourceBody;
        private TextField svgField;
        private Label svgPathLabel;
        private TextField layoutIdField;
        private Label layoutIdMessage;
        private LayoutContourPreview preview;
        private Label boardValue;
        private Label contoursValue;
        private Label pointsValue;
        private Label gridValue;
        private Label parseValue;
        private Button bakeButton;
        private Label bakeDisabledReason;
        private VisualElement bakeMessage;
        private bool parseErrorShown;

        private void BuildSourceSection(VisualElement parent)
        {
            sourceSection = Section(parent, "SVG Source");

            sourceEmpty = new VisualElement();
            Label emptyText = new Label("Choose an SVG layout to preview it and bake it for use by levels.");
            emptyText.AddToClassList("empty-state");
            Button emptyBrowse = new Button(BrowseSvg) { text = "Choose SVG…", tooltip = "Pick the SVG file exported from the layout drawing." };
            emptyBrowse.AddToClassList("btn-primary");
            emptyBrowse.style.alignSelf = Align.Center;
            sourceEmpty.Add(emptyText);
            sourceEmpty.Add(emptyBrowse);
            sourceSection.Add(sourceEmpty);

            sourceBody = new VisualElement();
            sourceSection.Add(sourceBody);

            VisualElement svgRow = new VisualElement();
            svgRow.AddToClassList("lb-row");
            svgField = new TextField("SVG file") { isDelayed = true, tooltip = "SVG file to bake. Paste a path or use Browse." };
            svgField.AddToClassList("lb-svg-field");
            svgField.RegisterValueChangedCallback(OnSvgFieldChanged);
            Button browse = new Button(BrowseSvg) { text = "Browse…", tooltip = "Choose a different SVG file." };
            browse.AddToClassList("btn-secondary");
            Button clear = new Button(ClearSvg) { text = "Clear SVG", tooltip = "Clear the current SVG selection and return to the empty state." };
            clear.AddToClassList("btn-secondary");
            svgRow.Add(svgField);
            svgRow.Add(browse);
            svgRow.Add(clear);
            sourceBody.Add(svgRow);
            svgPathLabel = new Label();
            svgPathLabel.AddToClassList("lb-path");
            sourceBody.Add(svgPathLabel);

            layoutIdField = new TextField("Layout ID") { tooltip = "Name levels use to reference this layout. Lowercase, digits and _." };
            layoutIdField.RegisterValueChangedCallback(OnLayoutIdChanged);
            sourceBody.Add(layoutIdField);
            layoutIdMessage = new Label();
            layoutIdMessage.AddToClassList("lb-hint");
            sourceBody.Add(layoutIdMessage);

            VisualElement previewRow = new VisualElement();
            previewRow.AddToClassList("lb-preview-row");
            preview = new LayoutContourPreview { tooltip = "Parsed walls and obstacles (read-only preview)." };
            previewRow.Add(preview);
            VisualElement metrics = new VisualElement();
            metrics.AddToClassList("lb-metrics");
            boardValue = Metric(metrics, "Board");
            contoursValue = Metric(metrics, "Contours");
            pointsValue = Metric(metrics, "Points");
            gridValue = Metric(metrics, "Sand grid");
            parseValue = Metric(metrics, "Status");
            previewRow.Add(metrics);
            sourceBody.Add(previewRow);

            VisualElement actionRow = new VisualElement();
            actionRow.AddToClassList("lb-action-row");
            bakeDisabledReason = new Label();
            bakeDisabledReason.AddToClassList("lb-disabled-reason");
            bakeButton = new Button(BakeCurrent) { text = "Bake Layout" };
            bakeButton.AddToClassList("btn-primary");
            actionRow.Add(bakeDisabledReason);
            actionRow.Add(bakeButton);
            sourceBody.Add(actionRow);

            bakeMessage = new VisualElement();
            sourceBody.Add(bakeMessage);

            svgField.SetValueWithoutNotify(DisplayName(svgPath));
            layoutIdField.SetValueWithoutNotify(layoutId);
        }

        private void BrowseSvg()
        {
            string startFolder = string.IsNullOrWhiteSpace(svgPath)
                ? Application.dataPath
                : Path.GetDirectoryName(ToAbsolute(svgPath));
            string picked = EditorUtility.OpenFilePanel("Choose SVG layout", startFolder, "svg");
            if (string.IsNullOrWhiteSpace(picked)) return;
            SetSvgPath(picked, true);
        }

        private void ClearSvg()
        {
            svgPath = string.Empty;
            layoutId = string.Empty;
            selectedLayoutId = string.Empty;
            layoutIdEditedByUser = false;
            currentParse = null;
            parseErrorShown = false;
            if (svgField != null) svgField.SetValueWithoutNotify(string.Empty);
            if (layoutIdField != null) layoutIdField.SetValueWithoutNotify(string.Empty);
            ClearMessage(bakeMessage);
            RefreshSourceView();
            RefreshAdvancedView();
            SetStatus("info", "Choose an SVG layout to preview and bake it for use by levels.");
        }

        private void OnSvgFieldChanged(ChangeEvent<string> change)
        {
            string typed = change.newValue != null ? change.newValue.Trim().Trim('"') : string.Empty;
            // The field shows the file name; only treat the edit as a new path when it is not just the displayed name.
            if (string.Equals(typed, DisplayName(svgPath), global::System.StringComparison.Ordinal)) return;
            SetSvgPath(typed, true);
        }

        private void SetSvgPath(string path, bool suggestId)
        {
            svgPath = LayoutBaker.NormalizeProjectPath(path);
            svgField.SetValueWithoutNotify(DisplayName(svgPath));
            ClearMessage(bakeMessage);
            if (suggestId && !layoutIdEditedByUser)
            {
                LayoutBakeEntry linked = FindEntryBySource(svgPath);
                layoutId = linked != null ? linked.LayoutId : LayoutIdRules.Suggest(svgPath);
                layoutIdField.SetValueWithoutNotify(layoutId);
            }

            RefreshSourceParse();
            RefreshSourceView();
        }

        private void OnLayoutIdChanged(ChangeEvent<string> change)
        {
            layoutId = change.newValue != null ? change.newValue.Trim() : string.Empty;
            layoutIdEditedByUser = layoutId.Length > 0;
            ClearMessage(bakeMessage);
            RefreshSourceView();
        }

        private void RefreshSourceParse()
        {
            currentParse = string.IsNullOrWhiteSpace(svgPath) ? null : library.ParseSvg(ToAbsolute(svgPath));
        }

        private void RefreshSourceView()
        {
            if (sourceSection == null) return;
            bool hasSvg = !string.IsNullOrWhiteSpace(svgPath);
            sourceEmpty.style.display = hasSvg ? DisplayStyle.None : DisplayStyle.Flex;
            sourceBody.style.display = hasSvg ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasSvg)
            {
                preview.SetLayout(null);
                boardValue.text = "—";
                contoursValue.text = "—";
                pointsValue.text = "—";
                gridValue.text = "—";
                parseValue.text = "—";
                layoutIdMessage.text = string.Empty;
                return;
            }

            svgPathLabel.text = svgPath;
            string idMessage;
            LayoutIdCheck idCheck = LayoutIdRules.Check(layoutId, library.Contains, out idMessage);
            layoutIdMessage.text = idMessage;
            layoutIdMessage.EnableInClassList("field-error", idCheck == LayoutIdCheck.Invalid || idCheck == LayoutIdCheck.Empty);
            layoutIdMessage.EnableInClassList("field-ok", idCheck == LayoutIdCheck.NewLayout || idCheck == LayoutIdCheck.UpdatesExisting);

            bool parsed = currentParse != null && currentParse.Parsed;
            preview.SetLayout(parsed ? currentParse.Level : null);
            if (parsed)
            {
                SE001LevelJson level = currentParse.Level;
                boardValue.text = level.board.size.x.ToString("0.##", CultureInfo.InvariantCulture) + " × " +
                    level.board.size.y.ToString("0.##", CultureInfo.InvariantCulture);
                contoursValue.text = CountContours(level).ToString("N0", CultureInfo.InvariantCulture);
                pointsValue.text = CountPoints(level).ToString("N0", CultureInfo.InvariantCulture);
                gridValue.text = GridText(level.board.size);
                parseValue.text = ParseStatusText();
            }
            else
            {
                boardValue.text = "—";
                contoursValue.text = "—";
                pointsValue.text = "—";
                gridValue.text = "—";
                parseValue.text = currentParse == null ? "—" : "Cannot read this SVG";
            }

            string blocker = FindBakeBlocker(idCheck, idMessage);
            bakeButton.SetEnabled(blocker == null);
            bakeButton.text = idCheck == LayoutIdCheck.UpdatesExisting ? "Bake Layout (update)" : "Bake Layout";
            bakeButton.tooltip = blocker ?? "Bake this SVG into layout '" + layoutId + "'.";
            bakeDisabledReason.text = blocker ?? string.Empty;

            if (currentParse != null && !currentParse.Parsed)
            {
                parseErrorShown = true;
                ShowMessage(
                    bakeMessage,
                    "error",
                    "SVG could not be read.",
                    "Source: " + DisplayName(svgPath) + " — " + currentParse.Error,
                    "Fix the SVG path/geometry, save it, then choose the file again.");
            }
            else if (parseErrorShown)
            {
                parseErrorShown = false;
                ClearMessage(bakeMessage);
            }
        }

        private string ParseStatusText()
        {
            LayoutBakeEntry existing = library.Find(layoutId);
            if (existing == null) return "Parsed OK — not baked yet";
            if (string.Equals(existing.Facts.MaskHash, currentParse.ContourHash, global::System.StringComparison.Ordinal))
                return "Parsed OK — same geometry as baked '" + existing.LayoutId + "'";
            return "Parsed OK — differs from baked '" + existing.LayoutId + "' (Bake will update it)";
        }

        private string GridText(Vector2 board)
        {
            if (library.RuntimeCellSize <= 0f) return "—";
            int width = Mathf.Max(1, Mathf.CeilToInt(board.x / library.RuntimeCellSize));
            int height = Mathf.Max(1, Mathf.CeilToInt(board.y / library.RuntimeCellSize));
            string text = width + " × " + height;
            if ((long)width * height > library.RuntimeMaxCells) text += " — too large for the sand grid";
            return text;
        }

        private string FindBakeBlocker(LayoutIdCheck idCheck, string idMessage)
        {
            string editorBlocker = BlockingEditorState();
            if (editorBlocker != null) return editorBlocker;
            if (currentParse == null || !currentParse.Parsed) return "Choose an SVG that can be read.";
            if (idCheck == LayoutIdCheck.Empty || idCheck == LayoutIdCheck.Invalid) return "Layout ID: " + idMessage;
            Vector2 board = currentParse.Level.board.size;
            int width = Mathf.Max(1, Mathf.CeilToInt(board.x / library.RuntimeCellSize));
            int height = Mathf.Max(1, Mathf.CeilToInt(board.y / library.RuntimeCellSize));
            if ((long)width * height > library.RuntimeMaxCells) return "Board is too large for the sand grid. Make the SVG board smaller.";
            if (CountContours(currentParse.Level) == 0) return "The SVG has no walls or obstacles to bake.";
            return null;
        }

        private void BakeCurrent()
        {
            string id = layoutId;
            string sourcePath = svgPath;
            ClearMessage(bakeMessage);
            string error;
            bool success = RunBake(sourcePath, id, out error);
            if (success)
            {
                selectedLayoutId = id;
                layoutIdEditedByUser = false;
                ShowMessage(bakeMessage, "success", "Baked '" + id + "' — Ready.", "Levels can now select this layout.", string.Empty);
                SetStatus("success", "Baked '" + id + "'.");
            }
            else
            {
                ShowMessage(
                    bakeMessage,
                    "error",
                    "Bake failed.",
                    "Layout '" + id + "' from " + DisplayName(sourcePath) + ": " + error,
                    "Fix the SVG, then press Bake Layout again. The previous bake (if any) was left in place.");
                SetStatus("error", "Bake failed for '" + id + "'.");
            }

            RefreshAll();
        }

        // One bake path for Bake, Rebake and Rebake All; records the outcome for the status evaluator.
        private bool RunBake(string projectPath, string id, out string error)
        {
            LayoutDefinition definition;
            bool success = LayoutBaker.TryBakeSvg(ToAbsolute(projectPath), id, new PhaseBSvgImportSettings(), out definition, out error);
            library.RecordBakeResult(id, success ? null : error);
            if (success) LayoutBaked?.Invoke();
            if (!success) Debug.LogWarning("[LayoutBake] " + id + ": " + error);
            return success;
        }

        private LayoutBakeEntry FindEntryBySource(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath)) return null;
            for (int i = 0; i < library.Entries.Count; i++)
            {
                LayoutBakeEntry entry = library.Entries[i];
                if (string.Equals(entry.Facts.SourceSvgPath, projectPath, global::System.StringComparison.OrdinalIgnoreCase)) return entry;
            }

            return null;
        }

        private static int CountContours(SE001LevelJson level)
        {
            int count = level.board.wallContours != null ? level.board.wallContours.Count : 0;
            if (level.staticObstacles == null) return count;
            for (int i = 0; i < level.staticObstacles.Count; i++)
                if (level.staticObstacles[i] != null && level.staticObstacles[i].contours != null)
                    count += level.staticObstacles[i].contours.Count;
            return count;
        }

        private static int CountPoints(SE001LevelJson level)
        {
            int count = 0;
            if (level.board.wallContours != null)
            {
                for (int i = 0; i < level.board.wallContours.Count; i++)
                    if (level.board.wallContours[i] != null && level.board.wallContours[i].points != null)
                        count += level.board.wallContours[i].points.Count;
            }

            if (level.staticObstacles == null) return count;
            for (int i = 0; i < level.staticObstacles.Count; i++)
            {
                StaticObstacleData obstacle = level.staticObstacles[i];
                if (obstacle == null || obstacle.contours == null) continue;
                for (int c = 0; c < obstacle.contours.Count; c++)
                    if (obstacle.contours[c] != null && obstacle.contours[c].points != null)
                        count += obstacle.contours[c].points.Count;
            }

            return count;
        }

        private static VisualElement Section(VisualElement parent, string title)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("lb-section");
            Label label = new Label(title);
            label.AddToClassList("lb-section-title");
            section.Add(label);
            parent.Add(section);
            return section;
        }

        private static Label Metric(VisualElement parent, string label)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("lb-metric");
            Label name = new Label(label);
            name.AddToClassList("lb-metric-label");
            Label value = new Label("—");
            value.AddToClassList("lb-metric-value");
            row.Add(name);
            row.Add(value);
            parent.Add(row);
            return value;
        }
    }
}
#endif
