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
        private Button levelsToggle;
        private Button generalToggle;
        private VisualElement levelsSlot;
        private bool compactLevels;
        private bool levelsDrawerOpen;

        private void BuildChrome()
        {
            VisualElement root = rootVisualElement;
            root.AddToClassList("le-root");

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("le-toolbar");
            toolbar.Add(MakeButton("New", NewDocument, "le-button"));
            toolbar.Add(MakeButton("Open", OpenDocument, "le-button"));
            saveButton = MakeButton("Save", SaveDocument, "le-button-primary");
            saveAsButton = MakeButton("Save As", SaveDocumentAs, "le-button");
            toolbar.Add(saveButton);
            toolbar.Add(saveAsButton);
            toolbar.Add(MakeButton("Undo", Undo.PerformUndo, "le-button"));
            toolbar.Add(MakeButton("Redo", Undo.PerformRedo, "le-button"));
            levelsToggle = MakeButton("Levels", ToggleLevelsPane, "le-button");
            toolbar.Add(levelsToggle);
            generalToggle = MakeButton("General", () => SelectEntity(string.Empty, LevelEditorSelectionKind.None),
                "le-button");
            generalToggle.tooltip = "Level settings, including Ink budget";
            toolbar.Add(generalToggle);
            VisualElement spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);
            playTestButton = MakeButton("Play Test", StartPlayTest, "le-button-primary");
            toolbar.Add(playTestButton);
            root.Add(toolbar);

            if (viewState.levelsPaneWidth > 600f || viewState.levelsPaneWidth < 180f)
                viewState.levelsPaneWidth = 190f;
            if (viewState.inspectorPaneWidth > 600f || viewState.inspectorPaneWidth < 240f)
                viewState.inspectorPaneWidth = 280f;
            outerSplit = new TwoPaneSplitView(0, viewState.levelsPaneWidth, TwoPaneSplitViewOrientation.Horizontal);
            outerSplit.AddToClassList("le-outer-split");
            levelsSlot = new VisualElement();
            levelsSlot.AddToClassList("le-levels-slot");
            levelsPane = new ScrollView(ScrollViewMode.Vertical);
            levelsPane.AddToClassList("le-levels-pane");
            levelsSlot.Add(levelsPane);
            outerSplit.Add(levelsSlot);

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
            outerSplit.Add(innerSplit);
            root.Add(outerSplit);

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
            levelsSlot.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (!compactLevels && evt.newRect.width >= 180f && evt.newRect.width <= 600f)
                    viewState.levelsPaneWidth = evt.newRect.width;
            });
            inspectorPane.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width >= 240f && evt.newRect.width <= 600f)
                    viewState.inspectorPaneWidth = evt.newRect.width;
            });
        }

        private static Button MakeButton(string text, global::System.Action action, string className)
        {
            Button button = action == null ? new Button() : new Button(action);
            button.text = text;
            button.AddToClassList(className);
            return button;
        }

        private void ToggleLevelsPane()
        {
            if (levelsPane == null) return;
            if (compactLevels)
            {
                levelsDrawerOpen = !levelsDrawerOpen;
                levelsPane.EnableInClassList("le-pane-hidden", !levelsDrawerOpen);
            }
            else if (levelsSlot.resolvedStyle.width > 0f)
                outerSplit.CollapseChild(0);
            else
                outerSplit.UnCollapse();
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            bool compact = width < 960f;
            bool narrow = width < 700f;
            if (levelsPane != null && compact != compactLevels)
            {
                compactLevels = compact;
                levelsDrawerOpen = false;
                if (compact)
                {
                    levelsPane.RemoveFromHierarchy();
                    rootVisualElement.Add(levelsPane);
                    levelsPane.AddToClassList("le-levels-overlay");
                    levelsPane.AddToClassList("le-pane-hidden");
                    levelsPane.BringToFront();
                    outerSplit.CollapseChild(0);
                }
                else
                {
                    levelsPane.RemoveFromHierarchy();
                    levelsPane.RemoveFromClassList("le-levels-overlay");
                    levelsPane.RemoveFromClassList("le-pane-hidden");
                    levelsSlot.Add(levelsPane);
                    outerSplit.UnCollapse();
                }
            }
            if (inspectorPane != null) inspectorPane.EnableInClassList("le-inspector-narrow", narrow);
        }

        private void RefreshLayoutList()
        {
            if (levelsPane == null) return;
            levelsPane.Clear();
            levelsPane.Add(new Label("Levels") { name = "levels-title" });
            searchField = new TextField("Find layout or entity") { value = viewState.levelSearch, isDelayed = true };
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
            levelsPane.Add(searchField);
            string filter = viewState.levelSearch.Trim();
            AddSavedLevelRows(filter);
            Button openBake = MakeButton("Open Layout Bake", OpenLayoutBake, "le-button-secondary");
            levelsPane.Add(openBake);
            int visibleLayouts = 0;
            for (int i = 0; i < layoutLibrary.Entries.Count; i++)
                if (MatchesFilter(layoutLibrary.Entries[i].LayoutId, filter)) visibleLayouts++;
            Foldout layoutsGroup = MakeGroup("Layouts", visibleLayouts, viewState.layoutsExpanded,
                value => viewState.layoutsExpanded = value);
            levelsPane.Add(layoutsGroup);
            int shownLayouts = 0;
            for (int i = 0; i < layoutLibrary.Entries.Count; i++)
            {
                LayoutBakeEntry entry = layoutLibrary.Entries[i];
                if (!MatchesFilter(entry.LayoutId, filter)) continue;
                if (shownLayouts++ >= viewState.visibleLayouts) continue;
                AddLayoutRow(layoutsGroup, entry);
            }
            if (shownLayouts > viewState.visibleLayouts)
                AddShowMore(layoutsGroup, () => viewState.visibleLayouts += ListPageSize);
            AddEntityRows(filter);
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
            group.Add(MakeButton("New Level", NewDocument, "le-button-primary"));

            if (matching == 0)
            {
                Label empty = new Label("No saved levels yet. Use New Level to create one.");
                empty.AddToClassList("le-empty-state");
                group.Add(empty);
                levelsPane.Add(group);
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
            levelsPane.Add(group);
        }

        private void AddSavedLevelRow(VisualElement parent, LevelEditorDocumentService.SavedLevelDescriptor descriptor)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("le-list-row");
            bool isCurrent = documentHost != null &&
                string.Equals(documentHost.CurrentPath, descriptor.ProjectPath, StringComparison.OrdinalIgnoreCase);
            if (isCurrent) row.AddToClassList("le-row-selected");

            Button open = MakeButton(descriptor.DisplayName, () => OpenSavedLevel(descriptor.ProjectPath), "le-list-button");
            open.SetEnabled(descriptor.IsValid);
            open.tooltip = descriptor.IsValid ? "Open this saved level." : "This level cannot be opened: " + descriptor.Error;
            row.Add(open);

            Label state = new Label(descriptor.IsValid ? (isCurrent ? "Open" : "Saved") : "Unreadable");
            state.AddToClassList("le-badge");
            state.AddToClassList(descriptor.IsValid ? "le-badge-success" : "le-badge-danger");
            state.tooltip = descriptor.IsValid ? descriptor.ProjectPath : descriptor.Error;
            row.Add(state);

            Button delete = MakeButton("Delete...", () => DeleteSavedLevel(descriptor), "le-button-danger");
            delete.tooltip = "Delete only this saved level JSON. Referenced layouts and assets stay unchanged.";
            row.Add(delete);
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

        private void AddLayoutRow(VisualElement parent, LayoutBakeEntry entry)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("le-list-row");
            Button select = MakeButton(entry.LayoutId, () => SelectLayout(entry.LayoutId), "le-list-button");
            if (Document == null)
            {
                select.SetEnabled(false);
                select.tooltip = "Create or open a level before choosing its layout.";
            }
            row.Add(select);
            Label badge = new Label(entry.Status.Headline);
            badge.AddToClassList("le-badge");
            badge.AddToClassList(StatusClass(entry.Status.State));
            row.Add(badge);
            if (Document != null && entry.LayoutId == Document.layoutId) row.AddToClassList("le-row-selected");
            parent.Add(row);
        }

        private void AddEntityRows(string filter)
        {
            if (Document == null)
            {
                Label empty = new Label("No level open. Use New or Open to begin authoring.");
                empty.AddToClassList("le-empty-state");
                levelsPane.Add(empty);
                levelsPane.Add(MakeButton("New Level", NewDocument, "le-button-primary"));
                levelsPane.Add(MakeButton("Open Level", OpenDocument, "le-button-secondary"));
                return;
            }

            Foldout sourcesGroup = MakeGroup("Sources", Document.sources.Count, viewState.sourcesExpanded,
                value => viewState.sourcesExpanded = value);
            sourcesGroup.Add(MakeButton("Add Source", AddSource, "le-button-secondary"));
            levelsPane.Add(sourcesGroup);
            int matchedSources = 0;
            for (int i = 0; i < Document.sources.Count; i++)
            {
                SourceData source = Document.sources[i];
                if (source == null) continue;
                string label = EntityDisplayName("Source", i + 1);
                if (MatchesFilter(label, filter) || MatchesFilter(EntityColorName(source.materialId), filter) ||
                    MatchesFilter(source.stableId, filter))
                {
                    if (matchedSources++ < viewState.visibleSources)
                        AddEntityRow(sourcesGroup, label, source.stableId, LevelEditorSelectionKind.Source,
                            source.materialId, LevelEditorGeometry.SourceSize(source));
                }
            }
            if (matchedSources > viewState.visibleSources)
                AddShowMore(sourcesGroup, () => viewState.visibleSources += ListPageSize);
            Foldout cupsGroup = MakeGroup("Cups", Document.cups.Count, viewState.cupsExpanded,
                value => viewState.cupsExpanded = value);
            cupsGroup.Add(MakeButton("Add Cup", AddCup, "le-button-secondary"));
            levelsPane.Add(cupsGroup);
            int matchedCups = 0;
            for (int i = 0; i < Document.cups.Count; i++)
            {
                CupData cup = Document.cups[i];
                if (cup == null) continue;
                string label = EntityDisplayName("Cup", i + 1);
                if (MatchesFilter(label, filter) || MatchesFilter(EntityColorName(cup.acceptedMaterialId), filter) ||
                    MatchesFilter(cup.stableId, filter))
                {
                    if (matchedCups++ < viewState.visibleCups)
                        AddEntityRow(cupsGroup, label, cup.stableId, LevelEditorSelectionKind.Cup,
                            cup.acceptedMaterialId, LevelEditorGeometry.CupSize(cup));
                }
            }
            if (matchedCups > viewState.visibleCups)
                AddShowMore(cupsGroup, () => viewState.visibleCups += ListPageSize);

            Foldout obstaclesGroup = MakeGroup("Rotating Obstacles", Document.rotatingObstacles.Count,
                viewState.rotatingObstaclesExpanded, value => viewState.rotatingObstaclesExpanded = value);
            obstaclesGroup.Add(MakeButton("Add Rotating Obstacle", AddRotatingObstacle, "le-button-secondary"));
            levelsPane.Add(obstaclesGroup);
            int matchedObstacles = 0;
            for (int i = 0; i < Document.rotatingObstacles.Count; i++)
            {
                RotatingObstacleData obstacle = Document.rotatingObstacles[i];
                if (obstacle == null) continue;
                string label = EntityDisplayName("Rotating Obstacle", i + 1);
                if (!MatchesFilter(label, filter) && !MatchesFilter(obstacle.stableId, filter)) continue;
                if (matchedObstacles++ < viewState.visibleRotatingObstacles)
                    AddRotatingObstacleRow(obstaclesGroup, label, obstacle);
            }
            if (matchedObstacles > viewState.visibleRotatingObstacles)
                AddShowMore(obstaclesGroup, () => viewState.visibleRotatingObstacles += ListPageSize);
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

        private static string EntityDisplayName(string kind, int number)
        {
            return kind + " " + number;
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
            var previewKind = kind == LevelEditorSelectionKind.Source ? JarPreviewKind.Source : JarPreviewKind.Cup;
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
            if (viewState.selectedStableId == stableId && viewState.selectionKind == kind) row.AddToClassList("le-row-selected");
            parent.Add(row);
        }

        private static string StatusClass(LayoutBakeState state)
        {
            switch (state)
            {
                case LayoutBakeState.Ready: return "le-badge-success";
                case LayoutBakeState.NeedsRebake: return "le-badge-warning";
                default: return "le-badge-danger";
            }
        }
    }
}
#endif
