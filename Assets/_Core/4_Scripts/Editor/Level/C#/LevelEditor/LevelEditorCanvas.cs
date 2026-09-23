#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;
using SE001.Editor;
using SE001.Geometry;
using UnityEngine;
using UnityEngine.UIElements;

namespace SE001.Editor.Level
{
    internal sealed class LevelEditorCanvas : VisualElement
    {
        private readonly VisualElement surface;
        private readonly Dictionary<VisualElement, EntityHandle> handles = new Dictionary<VisualElement, EntityHandle>();
        private LayoutDefinition layout;
        private LayoutMaskSet masks;
        private Texture2D maskTexture;
        private LayoutMaskSet previewMask;
        private Vector2 boardSize;
        private float boardScale = 1f;
        private VisualElement boardMask;
        private Label emptyState;
        private string selectedId = string.Empty;
        private LevelEditorSelectionKind selectedKind;
        private EntityHandle activeDrag;
        private bool dragging;
        private int dragPointerId = -1;

        public event Action<string, LevelEditorSelectionKind> EntitySelected;
        public event Action<string, LevelEditorSelectionKind> DragStarted;
        public event Action<string, LevelEditorSelectionKind, Vector2> DragMoved;
        public event Action<string, LevelEditorSelectionKind, Vector2> DragEnded;

        public LevelEditorCanvas()
        {
            AddToClassList("le-canvas");
            pickingMode = PickingMode.Position;
            surface = new VisualElement();
            surface.AddToClassList("le-board-surface");
            surface.style.position = Position.Absolute;
            Add(surface);
            emptyState = new Label("Choose a Ready layout, then add an entity.");
            emptyState.AddToClassList("le-empty-state");
            Add(emptyState);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        public void SetContent(
            SE001LevelJson level,
            LayoutDefinition selectedLayout,
            LayoutMaskSet selectedMasks,
            string currentSelectedId,
            LevelEditorSelectionKind currentSelectedKind)
        {
            layout = selectedLayout;
            masks = selectedMasks;
            selectedId = currentSelectedId ?? string.Empty;
            selectedKind = currentSelectedKind;
            boardSize = level != null && level.board != null ? level.board.size : Vector2.zero;
            emptyState.style.display = boardSize.x > 0f && boardSize.y > 0f ? DisplayStyle.None : DisplayStyle.Flex;
            RebuildMaskTexture();
            RebuildEntities(level);
            UpdateGeometry();
        }

        public Vector2 GetBoardPoint(Vector2 panelPosition)
        {
            Rect bounds = surface.worldBound;
            if (boardScale <= 0.0001f) return Vector2.zero;
            return new Vector2(
                (panelPosition.x - bounds.xMin) / boardScale,
                (bounds.yMax - panelPosition.y) / boardScale);
        }

        public Vector2 GetBoardCenter()
        {
            return boardSize * 0.5f;
        }

        public void SetSelected(string stableId, LevelEditorSelectionKind kind)
        {
            selectedId = stableId ?? string.Empty;
            selectedKind = kind;
            RefreshSelectionClasses();
        }

        public void DisposeDerivedTextures()
        {
            if (maskTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(maskTexture);
                maskTexture = null;
            }
            previewMask = null;
        }

        private void RebuildEntities(SE001LevelJson level)
        {
            surface.Clear();
            handles.Clear();
            boardMask = null;
            if (level == null || boardSize.x <= 0f || boardSize.y <= 0f) return;

            if (maskTexture != null)
            {
                boardMask = new Image { image = maskTexture, scaleMode = ScaleMode.StretchToFill };
                boardMask.AddToClassList("le-board-mask");
                boardMask.pickingMode = PickingMode.Ignore;
                boardMask.style.position = Position.Absolute;
                surface.Add(boardMask);
            }

            for (int i = 0; i < level.sources.Count; i++)
            {
                SourceData source = level.sources[i];
                if (source == null) continue;
                AddEntity(JarPreviewKind.Source, source.stableId, source.materialId, source.position,
                    ResolveSourceSize(source), ResolveSourceFill(source));
            }

            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData cup = level.cups[i];
                if (cup == null) continue;
                AddEntity(JarPreviewKind.Cup, cup.stableId, cup.acceptedMaterialId, cup.position,
                    ResolveCupSize(cup), 0f);
            }

            for (int i = 0; i < level.rotatingObstacles.Count; i++)
            {
                RotatingObstacleData obstacle = level.rotatingObstacles[i];
                if (obstacle == null) continue;
                LevelEditorCanvasData.SetPosition(obstacle.stableId, LevelEditorSelectionKind.RotatingObstacle,
                    obstacle.position);
                CrossPreviewElement preview = new CrossPreviewElement(obstacle.barLength, obstacle.scale,
                    obstacle.initialAngle, LevelEditorGeometry.RotatingBarWidth());
                preview.AddToClassList("le-entity");
                preview.userData = new EntityHandle(obstacle.stableId,
                    LevelEditorSelectionKind.RotatingObstacle, LevelEditorGeometry.RotatingSize(obstacle));
                preview.RegisterCallback<PointerDownEvent>(OnEntityPointerDown);
                surface.Add(preview);
                handles.Add(preview, (EntityHandle)preview.userData);
            }

            RefreshSelectionClasses();
        }

        private void AddEntity(JarPreviewKind kind, string stableId, int colorId, Vector2 position, Vector2 size, float fill)
        {
            LevelEditorSelectionKind selectionKind = kind == JarPreviewKind.Source
                ? LevelEditorSelectionKind.Source
                : LevelEditorSelectionKind.Cup;
            LevelEditorCanvasData.SetPosition(stableId, selectionKind, position);
            Texture2D preview = JarPreviewUtility.GetPreview(kind, colorId, size, fill);
            Image image = new Image { image = preview, scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList("le-entity");
            image.userData = new EntityHandle(stableId, selectionKind, size);
            image.RegisterCallback<PointerDownEvent>(OnEntityPointerDown);
            surface.Add(image);
            handles.Add(image, (EntityHandle)image.userData);
        }

        private void OnEntityPointerDown(PointerDownEvent evt)
        {
            VisualElement target = (VisualElement)evt.currentTarget;
            EntityHandle handle;
            if (!handles.TryGetValue(target, out handle)) return;
            selectedId = handle.StableId;
            selectedKind = handle.Kind;
            RefreshSelectionClasses();
            EntitySelected?.Invoke(handle.StableId, handle.Kind);
            if (evt.button != 0) return;
            activeDrag = handle;
            dragging = true;
            dragPointerId = evt.pointerId;
            this.CapturePointer(evt.pointerId);
            DragStarted?.Invoke(handle.StableId, handle.Kind);
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0 && evt.target == this)
            {
                EntitySelected?.Invoke(string.Empty, LevelEditorSelectionKind.None);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || evt.pointerId != dragPointerId) return;
            Vector2 point = GetBoardPoint(evt.position);
            DragMoved?.Invoke(activeDrag.StableId, activeDrag.Kind, point);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging || evt.pointerId != dragPointerId) return;
            dragging = false;
            dragPointerId = -1;
            this.ReleasePointer(evt.pointerId);
            Vector2 point = GetBoardPoint(evt.position);
            DragEnded?.Invoke(activeDrag.StableId, activeDrag.Kind, point);
            activeDrag = default(EntityHandle);
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!dragging) return;
            dragging = false;
            dragPointerId = -1;
            DragEnded?.Invoke(activeDrag.StableId, activeDrag.Kind,
                LevelEditorCanvasData.PositionFor(activeDrag.StableId, activeDrag.Kind));
            activeDrag = default(EntityHandle);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            if (boardSize.x <= 0f || boardSize.y <= 0f || contentRect.width <= 0f || contentRect.height <= 0f) return;
            float availableWidth = Mathf.Max(1f, contentRect.width - 32f);
            float availableHeight = Mathf.Max(1f, contentRect.height - 32f);
            boardScale = Mathf.Min(availableWidth / boardSize.x, availableHeight / boardSize.y);
            float width = boardSize.x * boardScale;
            float height = boardSize.y * boardScale;
            surface.style.width = width;
            surface.style.height = height;
            surface.style.left = (contentRect.width - width) * 0.5f;
            surface.style.top = (contentRect.height - height) * 0.5f;
            if (boardMask != null)
            {
                boardMask.style.left = 0f;
                boardMask.style.top = 0f;
                boardMask.style.width = width;
                boardMask.style.height = height;
            }

            foreach (KeyValuePair<VisualElement, EntityHandle> pair in handles)
            {
                VisualElement image = pair.Key;
                EntityHandle handle = pair.Value;
                Vector2 size = handle.Size;
                image.style.position = Position.Absolute;
                float imageWidth = Mathf.Max(12f, size.x * boardScale);
                float imageHeight = Mathf.Max(12f, size.y * boardScale);
                image.style.width = imageWidth;
                image.style.height = imageHeight;
                Vector2 position = FindEntityPosition(handle.StableId, handle.Kind);
                image.style.left = position.x * boardScale - imageWidth * 0.5f;
                image.style.top = (boardSize.y - position.y) * boardScale - imageHeight * 0.5f;
            }
        }

        private Vector2 FindEntityPosition(string stableId, LevelEditorSelectionKind kind)
        {
            return LevelEditorCanvasData.PositionFor(stableId, kind);
        }

        public void UpdateEntityPosition(string stableId, LevelEditorSelectionKind kind, Vector2 position)
        {
            LevelEditorCanvasData.SetPosition(stableId, kind, position);
            foreach (KeyValuePair<VisualElement, EntityHandle> pair in handles)
            {
                if (pair.Value.StableId != stableId || pair.Value.Kind != kind) continue;
                pair.Key.style.left = position.x * boardScale - pair.Key.resolvedStyle.width * 0.5f;
                pair.Key.style.top = (boardSize.y - position.y) * boardScale - pair.Key.resolvedStyle.height * 0.5f;
                break;
            }
        }

        private void RefreshSelectionClasses()
        {
            foreach (KeyValuePair<VisualElement, EntityHandle> pair in handles)
            {
                bool selected = pair.Value.StableId == selectedId && pair.Value.Kind == selectedKind;
                pair.Key.EnableInClassList("le-entity-selected", selected);
            }
        }

        private void RebuildMaskTexture()
        {
            if (ReferenceEquals(previewMask, masks) && maskTexture != null) return;
            if (maskTexture != null) UnityEngine.Object.DestroyImmediate(maskTexture);
            maskTexture = null;
            previewMask = masks;
            if (masks == null || masks.Width <= 0 || masks.Height <= 0) return;
            maskTexture = new Texture2D(masks.Width, masks.Height, TextureFormat.RGBA32, false, true)
            {
                name = "LevelEditorMaskPreview",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[masks.Width * masks.Height];
            for (int y = 0; y < masks.Height; y++)
            for (int x = 0; x < masks.Width; x++)
            {
                int index = masks.Index(x, y);
                pixels[index] = masks.StaticMask[index]
                    ? new Color32(140, 68, 68, 180)
                    : (masks.ValidMask[index] ? new Color32(54, 69, 82, 90) : new Color32(25, 28, 34, 150));
            }
            maskTexture.SetPixels32(pixels);
            maskTexture.Apply(false, false);
        }

        private static Vector2 ResolveSourceSize(SourceData source)
        {
            SourceProfile profile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            Vector2 fallback = profile != null ? profile.bodySize : new Vector2(0.8f, 1.2f);
            return fallback;
        }

        private static Vector2 ResolveCupSize(CupData cup)
        {
            CupProfile profile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            return profile != null ? profile.bodySize : new Vector2(2f, 1.5f);
        }

        private static float ResolveSourceFill(SourceData source)
        {
            return source.logicalAmount > 0 ? 1f : 0f;
        }

        private readonly struct EntityHandle
        {
            public EntityHandle(string stableId, LevelEditorSelectionKind kind, Vector2 size)
            {
                StableId = stableId ?? string.Empty;
                Kind = kind;
                Size = size;
            }

            public string StableId { get; }
            public LevelEditorSelectionKind Kind { get; }
            public Vector2 Size { get; }
        }
    }

    internal static class LevelEditorCanvasData
    {
        private static readonly Dictionary<string, Vector2> Positions = new Dictionary<string, Vector2>();

        public static void SetPosition(string stableId, LevelEditorSelectionKind kind, Vector2 position)
        {
            Positions[Key(stableId, kind)] = position;
        }

        public static Vector2 PositionFor(string stableId, LevelEditorSelectionKind kind)
        {
            Vector2 position;
            return Positions.TryGetValue(Key(stableId, kind), out position) ? position : Vector2.zero;
        }

        private static string Key(string stableId, LevelEditorSelectionKind kind)
        {
            return (int)kind + ":" + stableId;
        }
    }
}
#endif
