#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;
using SE001.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LevelEditorWindow
    {
        private void RefreshCanvas()
        {
            if (boardCanvas == null) return;
            boardCanvas.SetContent(Document, derivedState.Layout, derivedState.Masks,
                viewState.selectedStableId, viewState.selectionKind);
        }

        private void RefreshInspector()
        {
            if (inspectorPane == null) return;
            if (generalToggle != null)
                generalToggle.EnableInClassList("le-button-selected",
                    viewState.selectionKind == LevelEditorSelectionKind.None);
            inspectorFields.Clear();
            inspectorPane.Clear();
            inspectorPane.Add(new Label("Inspector") { name = "inspector-title" });
            if (Document == null)
            {
                Label empty = new Label("No level is open. Create a new level or open an existing level.");
                empty.AddToClassList("le-empty-state");
                inspectorPane.Add(empty);
                inspectorPane.Add(MakeButton("New Level", NewDocument, "le-button-primary"));
                inspectorPane.Add(MakeButton("Open Level", OpenDocument, "le-button-secondary"));
                return;
            }

            if (viewState.selectionKind == LevelEditorSelectionKind.Source)
            {
                SourceData source = FindSource(viewState.selectedStableId);
                if (source != null) BuildSourceInspector(source);
                else BuildLevelInspector();
            }
            else if (viewState.selectionKind == LevelEditorSelectionKind.Cup)
            {
                CupData cup = FindCup(viewState.selectedStableId);
                if (cup != null) BuildCupInspector(cup);
                else BuildLevelInspector();
            }
            else if (viewState.selectionKind == LevelEditorSelectionKind.RotatingObstacle)
            {
                RotatingObstacleData obstacle = FindRotatingObstacle(viewState.selectedStableId);
                if (obstacle != null) BuildRotatingObstacleInspector(obstacle);
                else BuildLevelInspector();
            }
            else BuildLevelInspector();
            BuildValidationPanel();
        }

        private void BuildLevelInspector()
        {
            inspectorPane.Add(new Label("General · Level settings") { name = "level-settings-header" });
            bool canRename = string.IsNullOrWhiteSpace(documentHost.CurrentPath);
            TextField levelId = new TextField("Level name")
            {
                value = Document.levelId,
                isReadOnly = !canRename,
                isDelayed = true
            };
            inspectorPane.Add(levelId);
            inspectorFields["levelId"] = levelId;
            levelId.tooltip = canRename
                ? "Name this unsaved level. Save As will use the same name for the JSON file."
                : "Saved level names are read-only here. Rename is a separate operation.";
            if (canRename)
                levelId.RegisterValueChangedCallback(evt =>
                    ApplyEdit(level => level.levelId = evt.newValue != null ? evt.newValue.Trim() : string.Empty,
                        "Rename Level"));

            AddFloatField("Ink budget", Document.drawInkBudget, "drawInkBudget", value => ApplyEdit(level => level.drawInkBudget = value, "Edit Ink Budget"));

            inspectorPane.Add(new Label("Layout") { name = "layout-header" });
            List<string> readyIds = new List<string>();
            int selectedIndex = -1;
            for (int i = 0; i < layoutLibrary.Entries.Count; i++)
            {
                LayoutBakeEntry entry = layoutLibrary.Entries[i];
                if (entry.Status.State != LayoutBakeState.Ready) continue;
                readyIds.Add(entry.LayoutId);
                if (entry.LayoutId == Document.layoutId) selectedIndex = readyIds.Count - 1;
            }
            if (readyIds.Count > 0)
            {
                // The level's layout is missing/not Ready: show it as a placeholder instead of silently displaying the
                // first Ready layout. Otherwise picking that layout raised no change event and the level kept the
                // missing id (2026-09-24: test_2 shown, level still on test_tool).
                string missingChoice = null;
                if (selectedIndex < 0)
                {
                    missingChoice = string.IsNullOrEmpty(Document.layoutId)
                        ? "(none selected)"
                        : Document.layoutId + " (missing)";
                    readyIds.Insert(0, missingChoice);
                    selectedIndex = 0;
                }

                PopupField<string> layoutField = new PopupField<string>("Ready layout", readyIds, selectedIndex);
                inspectorPane.Add(layoutField);
                inspectorFields["layoutId"] = layoutField;
                layoutField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue != missingChoice) SelectLayout(evt.newValue);
                });
            }
            else inspectorPane.Add(new Label("No Ready layout is available. Open Layout Bake to create one.") { name = "layout-empty" });
            inspectorPane.Add(MakeButton("Open Layout Bake", OpenLayoutBake, "le-button-secondary"));
        }

        private void BuildSourceInspector(SourceData source)
        {
            AddEntityHeader(EntityDisplayName("Source", Document.sources.IndexOf(source) + 1), source.stableId);
            AddInspectorSectionHeader("Appearance");
            AddColorField(JarPreviewKind.Source, LevelEditorGeometry.SourceSize(source), source.materialId, "Color", "materialId", value => ApplyEdit(
                level => FindSource(level, source.stableId).materialId = value, "Edit Source Color"));

            AddInspectorSectionHeader("Gameplay");
            AddFloatField("Amount", source.logicalAmount, "amount", value => ApplyEdit(
                level => FindSource(level, source.stableId).logicalAmount = Mathf.Max(0, Mathf.RoundToInt(value)), "Edit Source Amount"));

            AddInspectorSectionHeader("Placement");
            AddFloatPair("Position", source.position, "position", (x, y) => ApplyEdit(
                level => FindSource(level, source.stableId).position = new Vector2(x, y), "Edit Source Position"));
            Toggle startsOpen = new Toggle("Starts open") { value = source.startsOpen };
            inspectorPane.Add(startsOpen);
            startsOpen.RegisterValueChangedCallback(evt => ApplyEdit(
                level => FindSource(level, source.stableId).startsOpen = evt.newValue, "Edit Source State"));
            inspectorPane.Add(MakeButton("Delete Source", DeleteSelected, "le-button-danger"));
        }

        private void BuildCupInspector(CupData cup)
        {
            AddEntityHeader(EntityDisplayName("Cup", Document.cups.IndexOf(cup) + 1), cup.stableId);
            AddInspectorSectionHeader("Appearance");
            AddColorField(LevelEditorGeometry.ReceiverPreviewKind(), LevelEditorGeometry.CupSize(cup), cup.acceptedMaterialId,
                "Accepted color", "acceptedMaterialId", value => ApplyEdit(
                level => FindCup(level, cup.stableId).acceptedMaterialId = value, "Edit Cup Color"));

            AddInspectorSectionHeader("Gameplay");
            AddFloatField("Required amount", cup.requiredAmount, "requiredAmount", value => ApplyEdit(
                level => FindCup(level, cup.stableId).requiredAmount = Mathf.Max(0, Mathf.RoundToInt(value)), "Edit Required Amount"));

            AddInspectorSectionHeader("Placement");
            AddFloatPair("Position", cup.position, "position", (x, y) => ApplyEdit(
                level => FindCup(level, cup.stableId).position = new Vector2(x, y), "Edit Cup Position"));
            Label taperNote = new Label("Taper is fixed by the visual contract and is not editable here.");
            taperNote.AddToClassList("le-inspector-note");
            inspectorPane.Add(taperNote);
            inspectorPane.Add(MakeButton("Delete Cup", DeleteSelected, "le-button-danger"));
        }

        private void BuildRotatingObstacleInspector(RotatingObstacleData obstacle)
        {
            AddEntityHeader(EntityDisplayName("Rotating Obstacle",
                Document.rotatingObstacles.IndexOf(obstacle) + 1), obstacle.stableId);
            AddInspectorSectionHeader("Placement");
            AddFloatPair("Position", obstacle.position, "position", (x, y) => ApplyEdit(level =>
                FindRotatingObstacle(level, obstacle.stableId).position = new Vector2(x, y), "Edit Obstacle Position"));
            AddFloatField("Scale", obstacle.scale, "scale", value => ApplyEdit(level =>
                FindRotatingObstacle(level, obstacle.stableId).scale = Mathf.Max(0.01f, value), "Edit Obstacle Scale"));

            AddInspectorSectionHeader("Motion");
            AddFloatField("Bar length", obstacle.barLength, "barLength", value => ApplyEdit(level =>
                FindRotatingObstacle(level, obstacle.stableId).barLength = Mathf.Max(0.01f, value), "Edit Bar Length"));
            AddFloatField("Initial angle", obstacle.initialAngle, "initialAngle", value => ApplyEdit(level =>
                FindRotatingObstacle(level, obstacle.stableId).initialAngle = value, "Edit Initial Angle"));
            AddFloatField("Rotation °/s", obstacle.degreesPerSecond, "degreesPerSecond", value => ApplyEdit(level =>
                FindRotatingObstacle(level, obstacle.stableId).degreesPerSecond = value, "Edit Rotation Speed"));
            inspectorPane.Add(MakeButton("Delete Rotating Obstacle", DeleteSelected, "le-button-danger"));
        }

        private void AddEntityHeader(string label, string stableId)
        {
            inspectorPane.Add(new Label(label) { name = "entity-header" });
            Foldout technical = new Foldout { text = "Technical ID", value = false };
            Label id = new Label(stableId);
            id.AddToClassList("le-muted");
            technical.Add(id);
            inspectorPane.Add(technical);
        }

        private void AddInspectorSectionHeader(string text)
        {
            Label section = new Label(text);
            section.AddToClassList("le-inspector-section-header");
            inspectorPane.Add(section);
        }

        private void AddFloatField(string label, float value, string key, Action<float> onCommit)
        {
            FloatField field = new FloatField(label) { value = value, isDelayed = true };
            inspectorPane.Add(field);
            inspectorFields[key] = field;
            field.RegisterValueChangedCallback(evt => onCommit(evt.newValue));
        }

        private void AddFloatPair(string label, Vector2 value, string key, Action<float, float> onCommit)
        {
            Label title = new Label(label);
            title.AddToClassList("le-coordinate-title");
            inspectorPane.Add(title);
            VisualElement grid = new VisualElement();
            grid.AddToClassList("le-coordinate-grid");
            VisualElement xContainer = CreateCoordinateField("X", value.x, out FloatField x);
            VisualElement yContainer = CreateCoordinateField("Y", value.y, out FloatField y);
            grid.Add(xContainer);
            grid.Add(yContainer);
            inspectorPane.Add(grid);
            inspectorFields[key] = grid;
            x.RegisterValueChangedCallback(evt => onCommit(evt.newValue, y.value));
            y.RegisterValueChangedCallback(evt => onCommit(x.value, evt.newValue));
        }

        private static VisualElement CreateCoordinateField(string axis, float value, out FloatField field)
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("le-coordinate-field");
            Label axisLabel = new Label(axis);
            axisLabel.AddToClassList("le-coordinate-label");
            container.Add(axisLabel);
            field = new FloatField { value = value, isDelayed = true };
            field.AddToClassList("le-coordinate-input");
            container.Add(field);
            return container;
        }

        private void AddColorField(JarPreviewKind kind, Vector2 size, int colorId, string label, string key, Action<int> onCommit)
        {
            inspectorPane.Add(new Label(label));
            VisualElement palette = new VisualElement();
            palette.AddToClassList("le-color-palette");
            inspectorPane.Add(palette);
            inspectorFields[key] = palette;
            if (derivedState.Colors != null)
            {
                for (int i = 0; i < derivedState.Colors.entries.Count; i++)
                {
                    ColorProfileEntry entry = derivedState.Colors.entries[i];
                    int choiceId = entry.colorId;
                    Button choice = new Button(() => onCommit(choiceId));
                    choice.tooltip = string.IsNullOrWhiteSpace(entry.displayName) ? "Color " + choiceId : entry.displayName;
                    choice.AddToClassList("le-color-choice");
                    choice.EnableInClassList("le-color-choice-selected", choiceId == colorId);
                    Image preview = new Image
                    {
                        image = JarPreviewUtility.GetPreview(kind, choiceId, size, 1f),
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };
                    preview.AddToClassList("le-color-preview");
                    choice.Add(preview);
                    palette.Add(choice);
                }
            }
            if (palette.childCount == 0) palette.Add(new Label("No colors available."));
        }

        private void BuildValidationPanel()
        {
            VisualElement panel = new VisualElement();
            panel.AddToClassList("le-validation-panel");
            panel.Add(new Label("Validation") { name = "validation-header" });
            if (derivedState.Issues.Count == 0)
            {
                Label valid = new Label("No issues found.");
                valid.AddToClassList("le-validation-info");
                panel.Add(valid);
            }
            else
            {
                for (int i = 0; i < derivedState.Issues.Count; i++)
                {
                    LevelEditorIssue issue = derivedState.Issues[i];
                    Button row = new Button(() => SelectIssue(issue)) { text = SeverityLabel(issue.Severity) + " | " + issue.UserMessage };
                    row.AddToClassList("le-validation-row");
                    row.AddToClassList(SeverityClass(issue.Severity));
                    panel.Add(row);
                }
            }
            inspectorPane.Add(panel);
        }

        private void RefreshValidation()
        {
            if (validationStatus == null) return;
            if (Document == null)
            {
                validationStatus.text = "No level open";
                validationStatus.EnableInClassList("le-status-blocking", false);
                validationStatus.EnableInClassList("le-status-warning", false);
                return;
            }

            int blocking = 0;
            int warnings = 0;
            for (int i = 0; i < derivedState.Issues.Count; i++)
            {
                if (derivedState.Issues[i].Severity == LevelEditorIssueSeverity.Blocking) blocking++;
                if (derivedState.Issues[i].Severity == LevelEditorIssueSeverity.Warning) warnings++;
            }
            validationStatus.text = blocking > 0
                ? "Validation: " + blocking + " blocking"
                : (warnings > 0 ? "Validation: " + warnings + " warning(s)" : "Validation: ready");
            validationStatus.EnableInClassList("le-status-blocking", blocking > 0);
            validationStatus.EnableInClassList("le-status-warning", blocking == 0 && warnings > 0);
        }

        private static string SeverityLabel(LevelEditorIssueSeverity severity)
        {
            return severity == LevelEditorIssueSeverity.Blocking ? "BLOCK" : severity == LevelEditorIssueSeverity.Warning ? "WARN" : "INFO";
        }

        private static string SeverityClass(LevelEditorIssueSeverity severity)
        {
            return severity == LevelEditorIssueSeverity.Blocking
                ? "le-validation-blocking"
                : severity == LevelEditorIssueSeverity.Warning ? "le-validation-warning" : "le-validation-info";
        }

        private SourceData FindSource(string id)
        {
            return FindSource(Document, id);
        }

        private CupData FindCup(string id)
        {
            return FindCup(Document, id);
        }

        private RotatingObstacleData FindRotatingObstacle(string id)
        {
            return FindRotatingObstacle(Document, id);
        }

        private static SourceData FindSource(SE001LevelJson level, string id)
        {
            for (int i = 0; i < level.sources.Count; i++)
                if (level.sources[i] != null && level.sources[i].stableId == id) return level.sources[i];
            return null;
        }

        private static CupData FindCup(SE001LevelJson level, string id)
        {
            for (int i = 0; i < level.cups.Count; i++)
                if (level.cups[i] != null && level.cups[i].stableId == id) return level.cups[i];
            return null;
        }

        private static RotatingObstacleData FindRotatingObstacle(SE001LevelJson level, string id)
        {
            for (int i = 0; i < level.rotatingObstacles.Count; i++)
                if (level.rotatingObstacles[i] != null && level.rotatingObstacles[i].stableId == id)
                    return level.rotatingObstacles[i];
            return null;
        }
    }
}
#endif
