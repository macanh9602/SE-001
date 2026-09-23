#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;
using SE001.Geometry;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    public sealed partial class LevelEditorWindow : EditorWindow
    {
        private const string StylePath = "Assets/_Core/4_Scripts/Editor/Level/USS/LevelEditorWindow.uss";

        [SerializeField] private LevelEditorDocumentHost documentHost;
        [SerializeField] private LevelEditorViewState viewState = new LevelEditorViewState();

        private readonly LevelEditorDerivedState derivedState = new LevelEditorDerivedState();
        private string cachedMaskLayoutId = string.Empty;
        private readonly Dictionary<string, VisualElement> inspectorFields = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private LayoutBakeLibrary layoutLibrary;
        private LevelEditorCanvas boardCanvas;
        private ScrollView levelsPane;
        private ScrollView inspectorPane;
        private TwoPaneSplitView outerSplit;
        private TwoPaneSplitView innerSplit;
        private Label documentStatus;
        private Label validationStatus;
        private Button saveButton;
        private Button saveAsButton;
        private Button playTestButton;
        private TextField searchField;
        private string dragId = string.Empty;
        private LevelEditorSelectionKind dragKind;
        private Vector2 dragStartPosition;
        private bool dragPreviewValid;

        [MenuItem("SE001/Level Editor")]
        public static void Open()
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            layoutLibrary = new LayoutBakeLibrary();
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (boardCanvas != null) boardCanvas.DisposeDerivedTextures();
        }

        public void CreateGUI()
        {
            EnsureDocumentHost();
            layoutLibrary.Refresh();
            if (boardCanvas != null) boardCanvas.DisposeDerivedTextures();
            rootVisualElement.Clear();
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null) rootVisualElement.styleSheets.Add(style);
            BuildChrome();
            RefreshAll();
        }

        private void EnsureDocumentHost()
        {
            if (documentHost != null) return;
            documentHost = CreateInstance<LevelEditorDocumentHost>();
            documentHost.hideFlags = HideFlags.HideAndDontSave;
        }

        private SE001LevelJson Document => documentHost != null ? documentHost.Level : null;
        private PhaseCLevelSequence LevelSequence => Resources.Load<PhaseCLevelSequence>("Profiles/PhaseCLevelSequence");

        private void OnUndoRedo()
        {
            if (documentHost == null) return;
            derivedState.Clear();
            RefreshAll();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            LevelPlayTestOverride.Clear();
            EditorApplication.delayCall += RefreshAfterPlayTest;
        }

        private void RefreshAfterPlayTest()
        {
            if (this == null || rootVisualElement == null) return;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (documentHost == null) return;
            if (!documentHost.HasDocument)
            {
                derivedState.Clear();
                cachedMaskLayoutId = string.Empty;
                RefreshLayoutList();
                RefreshCanvas();
                RefreshInspector();
                RefreshValidation();
                RefreshDocumentStatus();
                return;
            }

            documentHost.Level.EnsureCollections();
            RebuildDerivedState();
            RefreshLayoutList();
            RefreshCanvas();
            RefreshInspector();
            RefreshValidation();
            RefreshDocumentStatus();
        }

        private void RebuildDerivedState()
        {
            if (Document == null)
            {
                derivedState.Clear();
                cachedMaskLayoutId = string.Empty;
                return;
            }

            LayoutDefinition cachedLayout = derivedState.Layout;
            LayoutMaskSet cachedMasks = derivedState.Masks;
            float cachedCellSize = derivedState.CellSize;
            int cachedMaxCells = derivedState.MaxCells;
            derivedState.Clear();
            ColorProfile colors = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            derivedState.Colors = colors;
            if (!LayoutBaker.TryGetRuntimeGrid(out derivedState.CellSize, out derivedState.MaxCells, out _))
            {
                derivedState.CellSize = 0f;
                derivedState.MaxCells = 0;
            }

            if (!string.IsNullOrWhiteSpace(Document.layoutId))
            {
                LayoutBakeEntry entry = layoutLibrary.Find(Document.layoutId);
                if (entry != null)
                {
                    derivedState.Layout = entry.Definition;
                    if (derivedState.CellSize > 0f && cachedMasks != null &&
                        cachedMaskLayoutId == Document.layoutId && cachedLayout == entry.Definition &&
                        cachedCellSize == derivedState.CellSize && cachedMaxCells == derivedState.MaxCells)
                        derivedState.Masks = cachedMasks;
                    else if (derivedState.CellSize > 0f)
                    {
                        string error;
                        LayoutMaskSet masks;
                        if (entry.Definition.TryBuildMaskSet(derivedState.CellSize, derivedState.MaxCells, out masks, out error))
                            derivedState.Masks = masks;
                    }
                    cachedMaskLayoutId = Document.layoutId;
                }
            }

            LevelEditorValidation.Rebuild(Document, derivedState.Layout, derivedState.Masks, derivedState.Colors,
                derivedState.CellSize, derivedState.Issues);
        }

        private void ApplyEdit(Action<SE001LevelJson> change, string undoName)
        {
            if (Document == null || change == null) return;
            Undo.RecordObject(documentHost, undoName);
            change(Document);
            Document.EnsureCollections();
            documentHost.MarkDirty();
            RefreshAll();
        }

        private bool HasBlockingIssues()
        {
            if (Document == null) return true;
            for (int i = 0; i < derivedState.Issues.Count; i++)
                if (derivedState.Issues[i].Severity == LevelEditorIssueSeverity.Blocking) return true;
            return false;
        }

        private void SelectEntity(string stableId, LevelEditorSelectionKind kind)
        {
            viewState.selectedStableId = stableId ?? string.Empty;
            viewState.selectionKind = kind;
            if (boardCanvas != null) boardCanvas.SetSelected(viewState.selectedStableId, kind);
            RefreshLayoutList();
            RefreshInspector();
        }

        private void StartDrag(string stableId, LevelEditorSelectionKind kind)
        {
            SelectEntity(stableId, kind);
            dragId = stableId ?? string.Empty;
            dragKind = kind;
            dragStartPosition = LevelEditorGeometry.PositionFor(Document, dragId, dragKind);
            dragPreviewValid = true;
            Undo.RecordObject(documentHost, "Move " + kind);
        }

        private void MoveDrag(string stableId, LevelEditorSelectionKind kind, Vector2 position)
        {
            if (stableId != dragId || kind != dragKind) return;
            SetEntityPosition(stableId, kind, position);
            dragPreviewValid = LevelEditorValidation.IsPositionValid(Document, derivedState.Masks, derivedState.CellSize, stableId, kind, position);
            if (boardCanvas != null) boardCanvas.UpdateEntityPosition(stableId, kind, position);
            if (validationStatus != null) validationStatus.text = dragPreviewValid ? "Placement: valid" : "Placement: blocked";
        }

        private void EndDrag(string stableId, LevelEditorSelectionKind kind, Vector2 position)
        {
            if (stableId != dragId || kind != dragKind) return;
            if (!dragPreviewValid)
            {
                SetEntityPosition(stableId, kind, dragStartPosition);
                if (boardCanvas != null) boardCanvas.UpdateEntityPosition(stableId, kind, dragStartPosition);
            }
            else SetEntityPosition(stableId, kind, position);
            documentHost.MarkDirty();
            dragId = string.Empty;
            RefreshAll();
        }

        private void SetEntityPosition(string stableId, LevelEditorSelectionKind kind, Vector2 position)
        {
            if (Document == null) return;
            if (kind == LevelEditorSelectionKind.Source)
                for (int i = 0; i < Document.sources.Count; i++)
                    if (Document.sources[i] != null && Document.sources[i].stableId == stableId) Document.sources[i].position = position;
            if (kind == LevelEditorSelectionKind.Cup)
                for (int i = 0; i < Document.cups.Count; i++)
                    if (Document.cups[i] != null && Document.cups[i].stableId == stableId) Document.cups[i].position = position;
            if (kind == LevelEditorSelectionKind.RotatingObstacle)
                for (int i = 0; i < Document.rotatingObstacles.Count; i++)
                    if (Document.rotatingObstacles[i] != null && Document.rotatingObstacles[i].stableId == stableId)
                        Document.rotatingObstacles[i].position = position;
        }

        private void RefreshLayoutLibrary()
        {
            layoutLibrary.Refresh();
            derivedState.Clear();
            RefreshAll();
        }
    }
}
#endif
