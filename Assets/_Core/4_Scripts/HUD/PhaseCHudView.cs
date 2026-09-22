using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SE001.Gameplay;
using SE001.System.Management;

namespace SE001.HUD
{
    /// <summary>Phase C runtime HUD. It observes gameplay events and defers lifecycle commands to Update.</summary>
    [DisallowMultipleComponent]
    public sealed class PhaseCHudView : MonoBehaviour, ILevelLifecycleParticipant
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private int titleFontSize = 42;
        [SerializeField] private int bodyFontSize = 28;

        private readonly List<TextMeshProUGUI> cupLabels = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> sourceLabels = new List<TextMeshProUGUI>();
        private LevelContext context;
        private GameplayManager gameplay;
        private Canvas canvas;
        private TextMeshProUGUI title;
        private TextMeshProUGUI state;
        private RectTransform inkFill;
        private TextMeshProUGUI inkLabel;
        private float inkTrackWidth;

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            gameplay = value.LevelRoot.GetComponent<GameplayManager>();
            BuildUi();
            gameplay.GameStateChanged += OnGameStateChanged;
            gameplay.SourceStateChanged += OnSourceChanged;
            gameplay.CupChanged += OnCupChanged;
            gameplay.StrokeCommitted += OnStrokeCommitted;
            Refresh();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("PhaseCHudCanvas");
            Transform canvasParent = context.VfxRoot != null ? context.VfxRoot : transform;
            canvasObject.transform.SetParent(canvasParent, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panel = CreateRect(
                "Panel",
                canvas.transform,
                new Vector2(28f, -28f),
                new Vector2(430f, 410f),
                new Color(0.05f, 0.07f, 0.11f, 0.88f));
            title = CreateLabel(
                "Title",
                panel.transform,
                "Phase C",
                titleFontSize,
                TextAlignmentOptions.Center,
                new Vector2(0f, -28f),
                new Vector2(390f, 58f));
            state = CreateLabel(
                "State",
                panel.transform,
                string.Empty,
                bodyFontSize,
                TextAlignmentOptions.Left,
                new Vector2(18f, -92f),
                new Vector2(390f, 48f));
            BuildInkBar(panel.transform);
            CreateLists(panel.transform);
        }

        private void CreateLists(Transform parent)
        {
            float y = -160f;
            for (int i = 0; i < gameplay.Sources.Count; i++)
            {
                sourceLabels.Add(CreateLabel(
                    "Source" + i,
                    parent,
                    string.Empty,
                    bodyFontSize,
                    TextAlignmentOptions.Left,
                    new Vector2(18f, y),
                    new Vector2(390f, 38f)));
                y -= 36f;
            }
            for (int i = 0; i < gameplay.Cups.Count; i++)
            {
                cupLabels.Add(CreateLabel(
                    "Cup" + i,
                    parent,
                    string.Empty,
                    bodyFontSize,
                    TextAlignmentOptions.Left,
                    new Vector2(18f, y),
                    new Vector2(390f, 38f)));
                y -= 36f;
            }
        }

        /// <summary>Ink/energy bar: track + fill + remaining/budget label.</summary>
        private void BuildInkBar(Transform parent)
        {
            inkLabel = CreateLabel("InkLabel", parent, "Ink", bodyFontSize, TextAlignmentOptions.Left,
                new Vector2(18f, -100f), new Vector2(390f, 32f));
            inkTrackWidth = 390f;
            GameObject track = CreateRect("InkTrack", parent, new Vector2(18f, -132f),
                new Vector2(inkTrackWidth, 16f), new Color(1f, 1f, 1f, 0.16f));
            GameObject fill = CreateRect("InkFill", track.transform, Vector2.zero,
                new Vector2(inkTrackWidth, 16f), new Color(0.35f, 0.78f, 1f, 0.95f));
            inkFill = fill.GetComponent<RectTransform>();
        }

        private void RefreshInk()
        {
            if (inkFill == null || gameplay == null) return;
            float budget = gameplay.InkBudget;
            float ratio = budget > 0f ? Mathf.Clamp01(gameplay.InkRemaining / budget) : 0f;
            inkFill.sizeDelta = new Vector2(inkTrackWidth * ratio, inkFill.sizeDelta.y);
            if (inkLabel != null)
                inkLabel.text = "Ink: " + gameplay.InkRemaining.ToString("0.0") + " / " + budget.ToString("0.0");
        }

        private void Refresh()
        {
            if (gameplay == null) return;
            title.text = context.LevelId;
            state.text = "State: " + gameplay.State +
                (gameplay.State == GameState.Lost ? " (" + gameplay.LastLoseReason + ")" : string.Empty);
            RefreshInk();
            for (int i = 0; i < gameplay.Sources.Count && i < sourceLabels.Count; i++)
            {
                SourceDomain source = gameplay.Sources[i];
                sourceLabels[i].text = source.StableId + ": " + source.State + " " + source.Remaining + "/" + source.Initial;
            }
            for (int i = 0; i < gameplay.Cups.Count && i < cupLabels.Count; i++)
            {
                CupDomain cup = gameplay.Cups[i];
                string suffix = cup.ForeignDetected ? " WRONG" : cup.Full ? " FULL" : string.Empty;
                cupLabels[i].text = cup.StableId + ": " + cup.CollectedLogical + "/" +
                    cup.RequiredLogical + suffix;
            }
        }

        private void OnGameStateChanged(GameState value) { Refresh(); }
        private void OnSourceChanged(string id) { Refresh(); }
        private void OnCupChanged(string id) { Refresh(); }
        private void OnStrokeCommitted(IList<Vector2> points, float thickness) { Refresh(); }
        public void CleanupForLevelUnload()
        {
            if (gameplay != null)
            {
                gameplay.GameStateChanged -= OnGameStateChanged;
                gameplay.SourceStateChanged -= OnSourceChanged;
                gameplay.CupChanged -= OnCupChanged;
                gameplay.StrokeCommitted -= OnStrokeCommitted;
            }
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
                DestroyOwned(canvas.gameObject);
            }
            canvas = null;
            gameplay = null;
            context = null;
            cupLabels.Clear();
            sourceLabels.Clear();
        }

        private static GameObject CreateRect(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private TextMeshProUGUI CreateLabel(
            string name,
            Transform parent,
            string value,
            int size,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = dimensions;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static void DestroyOwned(GameObject target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
