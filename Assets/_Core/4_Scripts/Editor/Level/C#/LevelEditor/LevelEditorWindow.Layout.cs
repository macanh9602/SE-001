#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LevelEditorWindow
    {
        private const int ListPageSize = 25;
        private Button editTab;
        private Button levelBrowserTab;
        private Button generalToggle;
        private VisualElement editWorkspace;
        private Button addSourceButton;
        private Button addCupButton;
        private Button addObstacleButton;
        private Button entitiesToggle;
        private ScrollView entitiesPane;
        private VisualElement editBody;
        private VisualElement levelBrowserContent;

        private void BuildChrome()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("le-root");

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("le-toolbar");
            toolbar.Add(MakeButton("New", NewDocument, "le-button"));
            toolbar.Add(MakeButton("Open", OpenDocument, "le-button"));
            editTab = MakeButton("Edit", () => SetWorkspaceTab(false), "le-button");
            editTab.AddToClassList("le-workspace-tab");
            toolbar.Add(editTab);
            saveButton = MakeButton("Save", SaveDocument, "le-button-primary");
            saveAsButton = MakeButton("Save As", SaveDocumentAs, "le-button");
            toolbar.Add(saveButton);
            toolbar.Add(saveAsButton);
            levelBrowserTab = MakeButton("Level Browser", () => SetWorkspaceTab(true), "le-button");
            levelBrowserTab.AddToClassList("le-workspace-tab");
            toolbar.Add(levelBrowserTab);
            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);
            playTestButton = MakeButton("Play Test", StartPlayTest, "le-button-primary");
            toolbar.Add(playTestButton);
            root.Add(toolbar);

            if (viewState.inspectorPaneWidth > 600f || viewState.inspectorPaneWidth < 240f)
                viewState.inspectorPaneWidth = 280f;

            VisualElement workspaceHost = new VisualElement();
            workspaceHost.AddToClassList("le-workspace-host");

            editWorkspace = new VisualElement();
            editWorkspace.AddToClassList("le-edit-workspace");
            VisualElement editActions = new VisualElement();
            editActions.AddToClassList("le-edit-actions");
            generalToggle = MakeButton("General", () => SelectEntity(string.Empty, LevelEditorSelectionKind.None),
                "le-button-secondary");
            generalToggle.tooltip = "Level settings, including Ink budget";
            editActions.Add(generalToggle);
            addSourceButton = MakeButton("Add Source", AddSource, "le-button-secondary");
            addCupButton = MakeButton("Add Cup", AddCup, "le-button-secondary");
            addObstacleButton = MakeButton("Add Obstacle", AddRotatingObstacle, "le-button-secondary");
            editActions.Add(addSourceButton);
            editActions.Add(addCupButton);
            editActions.Add(addObstacleButton);
            entitiesToggle = MakeButton("Entities", ToggleEntitiesDrawer, "le-button-secondary");
            entitiesToggle.tooltip = "Show entities in the current level.";
            editActions.Add(entitiesToggle);
            editWorkspace.Add(editActions);

            levelsPane = new ScrollView(ScrollViewMode.Vertical);
            levelsPane.AddToClassList("le-levels-pane");
            levelsPane.AddToClassList("le-level-browser-pane");
            levelBrowserContent = new VisualElement();
            levelBrowserContent.AddToClassList("le-level-browser-content");
            levelsPane.Add(levelBrowserContent);
            workspaceHost.Add(levelsPane);

            innerSplit = new TwoPaneSplitView(1, viewState.inspectorPaneWidth, TwoPaneSplitViewOrientation.Horizontal);
            innerSplit.AddToClassList("le-inner-split");
            boardCanvas = new LevelEditorCanvas();
            boardCanvas.EntitySelected += SelectEntity;
            boardCanvas.DragStarted += StartDrag;
            boardCanvas.DragMoved += MoveDrag;
            boardCanvas.DragEnded += EndDrag;
            innerSplit.Add(boardCanvas);
            inspectorPane = new ScrollView(ScrollViewMode.Vertical);
            inspectorPane.AddToClassList("le-inspector-pane");
            innerSplit.Add(inspectorPane);

            editBody = new VisualElement();
            editBody.AddToClassList("le-edit-body");
            editBody.Add(innerSplit);
            entitiesPane = new ScrollView(ScrollViewMode.Vertical);
            entitiesPane.AddToClassList("le-entities-drawer");
            entitiesPane.AddToClassList("le-pane-hidden");
            editBody.Add(entitiesPane);
            editWorkspace.Add(editBody);
            workspaceHost.Add(editWorkspace);
            root.Add(workspaceHost);

            VisualElement statusBar = new VisualElement();
            statusBar.AddToClassList("le-statusbar");
            documentStatus = new Label();
            validationStatus = new Label();
            statusBar.Add(documentStatus);
            VisualElement statusSpacer = new VisualElement();
            statusSpacer.style.flexGrow = 1f;
            statusBar.Add(statusSpacer);
            statusBar.Add(validationStatus);
            root.Add(statusBar);

            root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            inspectorPane.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width >= 240f && evt.newRect.width <= 600f)
                    viewState.inspectorPaneWidth = evt.newRect.width;
            });
            editBody.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (entitiesPane != null)
                    entitiesPane.EnableInClassList("le-entities-drawer-narrow", evt.newRect.width < 700f);
            });
            SetWorkspaceTab(false);
        }

        private static Button MakeButton(string text, global::System.Action action, string className)
        {
            Button button = action == null ? new Button() : new Button(action);
            button.text = text;
            button.AddToClassList(className);
            return button;
        }

        private void SetWorkspaceTab(bool showBrowser)
        {
            if (editWorkspace != null) editWorkspace.EnableInClassList("le-pane-hidden", showBrowser);
            if (levelsPane != null) levelsPane.EnableInClassList("le-pane-hidden", !showBrowser);
            if (editTab != null) editTab.EnableInClassList("le-button-selected", !showBrowser);
            if (levelBrowserTab != null) levelBrowserTab.EnableInClassList("le-button-selected", showBrowser);
            if (playTestButton != null) playTestButton.EnableInClassList("le-pane-hidden", showBrowser);
            if (showBrowser) RefreshLayoutList();
        }

        private void ToggleEntitiesDrawer()
        {
            if (entitiesPane == null) return;
            bool show = entitiesPane.ClassListContains("le-pane-hidden");
            entitiesPane.EnableInClassList("le-pane-hidden", !show);
            if (entitiesToggle != null) entitiesToggle.EnableInClassList("le-button-selected", show);
            if (show)
            {
                entitiesPane.BringToFront();
                RefreshEntityList();
            }
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            bool narrow = evt.newRect.width < 700f;
            if (inspectorPane != null) inspectorPane.EnableInClassList("le-inspector-narrow", narrow);
            if (levelsPane != null) levelsPane.EnableInClassList("le-level-browser-narrow", narrow);
        }

        private void RefreshLayoutList()
        {
            if (levelBrowserContent == null) return;
            levelBrowserContent.Clear();
            levelBrowserContent.Add(new Label("Level Browser") { name = "levels-title" });
            searchField = new TextField("Find level") { value = viewState.levelSearch, isDelayed = true };
            searchField.RegisterValueChangedCallback(evt =>
            {
                viewState.levelSearch = evt.newValue ?? string.Empty;
                viewState.visibleSavedLevels = ListPageSize;
                viewState.visibleLayouts = ListPageSize;
                viewState.visibleSources = ListPageSize;
                viewState.visibleCups = ListPageSize;
                viewState.visibleRotatingObstacles = ListPageSize;
                RefreshLayoutList();
            });
            levelBrowserContent.Add(searchField);
            string filter = viewState.levelSearch.Trim();
            AddSavedLevelRows(filter);
            Button openBake = MakeButton("Open Layout Bake", OpenLayoutBake, "le-button-secondary");
            levelBrowserContent.Add(openBake);
        }

        private void AddSavedLevelRows(string filter)
        {
            List<LevelEditorDocumentService.SavedLevelDescriptor> savedLevels =
                LevelEditorDocumentService.EnumerateSavedLevels(LevelSequence);
            int matching = 0;
            for (int i = 0; i < savedLevels.Count; i++)
            {
                LevelEditorDocumentService.SavedLevelDescriptor descriptor = savedLevels[i];
                if (MatchesFilter(descriptor.DisplayName, filter) || MatchesFilter(descriptor.LevelId, filter)) matching++;
            }

            Foldout group = MakeGroup("Saved Levels", matching, viewState.savedLevelsExpanded,
                value => viewState.savedLevelsExpanded = value);

            if (matching == 0)
            {
                Label empty = new Label("No saved levels yet. Use New in the toolbar to create one.");
                empty.AddToClassList("le-empty-state");
                group.Add(empty);
                levelBrowserContent.Add(group);
                return;
            }

            int shown = 0;
            for (int i = 0; i < savedLevels.Count; i++)
            {
                LevelEditorDocumentService.SavedLevelDescriptor descriptor = savedLevels[i];
                if (!MatchesFilter(descriptor.DisplayName, filter) && !MatchesFilter(descriptor.LevelId, filter)) continue;
                if (shown++ >= viewState.visibleSavedLevels) continue;
                AddSavedLevelRow(group, descriptor);
            }

            if (shown > viewState.visibleSavedLevels)
                AddShowMore(group, () => viewState.visibleSavedLevels += ListPageSize);
            levelBrowserContent.Add(group);
        }

        private void AddSavedLevelRow(VisualElement parent, LevelEditorDocumentService.SavedLevelDescriptor descriptor)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("le-saved-level-row");
            bool isCurrent = documentHost != null &&
                string.Equals(documentHost.CurrentPath, descriptor.ProjectPath, StringComparison.OrdinalIgnoreCase);
            if (isCurrent) row.AddToClassList("le-row-selected");

            Button open = MakeButton(descriptor.DisplayName, () => OpenSavedLevel(descriptor.ProjectPath), "le-list-button");
            open.AddToClassList("le-saved-level-open");
            open.SetEnabled(descriptor.IsValid);
            open.tooltip = descriptor.IsValid ? "Open this saved level." : "This level cannot be opened: " + descriptor.Error;
            row.Add(open);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("le-saved-level-actions");
            Label state = new Label(descriptor.IsValid ? (isCurrent ? "Open" : "Saved") : "Unreadable");
            state.AddToClassList("le-badge");
            state.AddToClassList(descriptor.IsValid ? "le-badge-success" : "le-badge-danger");
            state.tooltip = descriptor.IsValid ? descriptor.ProjectPath : descriptor.Error;
            actions.Add(state);

            Button delete = MakeButton("Delete", () => DeleteSavedLevel(descriptor), "le-button-danger");
            delete.AddToClassList("le-saved-level-delete");
            delete.tooltip = "Delete only this saved level JSON. Referenced layouts and assets stay unchanged.";
            actions.Add(delete);
            row.Add(actions);
            parent.Add(row);
        }

        private static Foldout MakeGroup(string label, int count, bool expanded, Action<bool> onChanged)
        {
            Foldout group = new Foldout { text = label + " (" + count + ")", value = expanded };
            group.AddToClassList("le-entity-group");
            group.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            return group;
        }

        private static bool MatchesFilter(string text, string filter)
        {
            return string.IsNullOrEmpty(filter) ||
                (!string.IsNullOrEmpty(text) && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void AddShowMore(VisualElement group, Action reveal)
        {
            group.Add(MakeButton("Show more", () =>
            {
                reveal();
                RefreshLayoutList();
            }, "le-button-secondary"));
        }

        private void RefreshEntityList()
        {
            if (entitiesPane == null) return;
            entitiesPane.Clear();
            entitiesPane.Add(new Label("Entities") { name = "entities-title" });
            if (Document == null)
            {
                Label empty = new Label("No level is open.");
                empty.AddToClassList("le-empty-state");
                entitiesPane.Add(empty);
                return;
            }

            Foldout sourcesGroup = MakeGroup("Sources", Document.sources.Count, viewState.sourcesExpanded,
                value => viewState.sourcesExpanded = value);
            entitiesPane.Add(sourcesGroup);
            int shownSources = 0;
            for (int i = 0; i < Document.sources.Count; i++)
            {
                SourceData source = Document.sources[i];
                if (source == null || shownSources >= viewState.visibleSources) continue;
                AddEntityRow(sourcesGroup, EntityDisplayName("Source", i + 1), source.stableId,
                    LevelEditorSelectionKind.Source, source.materialId, LevelEditorGeometry.SourceSize(source));
                shownSources++;
            }
            if (Document.sources.Count > viewState.visibleSources)
                AddEntityShowMore(sourcesGroup, () => viewState.visibleSources += ListPageSize);

            Foldout cupsGroup = MakeGroup("Cups", Document.cups.Count, viewState.cupsExpanded,
                value => viewState.cupsExpanded = value);
            entitiesPane.Add(cupsGroup);
            int shownCups = 0;
            for (int i = 0; i < Document.cups.Count; i++)
            {
                CupData cup = Document.cups[i];
                if (cup == null || shownCups >= viewState.visibleCups) continue;
                AddEntityRow(cupsGroup, EntityDisplayName("Cup", i + 1), cup.stableId,
                    LevelEditorSelectionKind.Cup, cup.acceptedMaterialId, LevelEditorGeometry.CupSize(cup));
                shownCups++;
            }
            if (Document.cups.Count > viewState.visibleCups)
                AddEntityShowMore(cupsGroup, () => viewState.visibleCups += ListPageSize);

            Foldout obstaclesGroup = MakeGroup("Rotating Obstacles", Document.rotatingObstacles.Count,
                viewState.rotatingObstaclesExpanded, value => viewState.rotatingObstaclesExpanded = value);
            entitiesPane.Add(obstaclesGroup);
            int shownObstacles = 0;
            for (int i = 0; i < Document.rotatingObstacles.Count; i++)
            {
                RotatingObstacleData obstacle = Document.rotatingObstacles[i];
                if (obstacle == null || shownObstacles >= viewState.visibleRotatingObstacles) continue;
                AddRotatingObstacleRow(obstaclesGroup, EntityDisplayName("Rotating Obstacle", i + 1), obstacle);
                shownObstacles++;
            }
            if (Document.rotatingObstacles.Count > viewState.visibleRotatingObstacles)
                AddEntityShowMore(obstaclesGroup, () => viewState.visibleRotatingObstacles += ListPageSize);
        }

        private void AddEntityShowMore(VisualElement group, Action reveal)
        {
            group.Add(MakeButton("Show more", () =>
            {
                reveal();
                RefreshEntityList();
            }, "le-button-secondary"));
        }

        private void AddRotatingObstacleRow(VisualElement parent, string label, RotatingObstacleData obstacle)
        {
            Button row = MakeButton(string.Empty,
                () => SelectEntity(obstacle.stableId, LevelEditorSelectionKind.RotatingObstacle), "le-list-button");
            CrossPreviewElement preview = new CrossPreviewElement(obstacle.barLength, obstacle.scale,
                obstacle.initialAngle, LevelEditorGeometry.RotatingBarWidth());
            preview.AddToClassList("le-entity-thumbnail");
            preview.pickingMode = PickingMode.Ignore;
            row.Add(preview);
            row.Add(new Label(label) { pickingMode = PickingMode.Ignore });
            if (viewState.selectedStableId == obstacle.stableId) row.AddToClassList("le-row-selected");
            parent.Add(row);
        }

        private string EntityColorName(int colorId)
        {
            ColorProfileEntry color;
            return derivedState.Colors != null && derivedState.Colors.TryGetEntry(colorId, out color) &&
                !string.IsNullOrWhiteSpace(color.displayName)
                ? color.displayName : "Color " + colorId;
        }

        private void AddEntityRow(VisualElement parent, string label, string stableId,
            LevelEditorSelectionKind kind, int colorId, Vector2 size)
        {
            Button row = MakeButton(string.Empty, () => SelectEntity(stableId, kind), "le-list-button");
            JarPreviewKind previewKind = kind == LevelEditorSelectionKind.Source
                ? JarPreviewKind.Source : LevelEditorGeometry.ReceiverPreviewKind();
            Image preview = new Image
            {
                image = JarPreviewUtility.GetPreview(previewKind, colorId, size, 1f),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            preview.AddToClassList("le-entity-thumbnail");
            row.Add(preview);
            row.Add(new Label(label));
            row.tooltip = EntityColorName(colorId) + " | " + stableId;
            if (viewState.selectedStableId == stableId && viewState.selectionKind == kind)
                row.AddToClassList("le-row-selected");
            parent.Add(row);
        }

        private static string EntityDisplayName(string kind, int number)
        {
            return kind + " " + number;
        }
    }
}
#endif
