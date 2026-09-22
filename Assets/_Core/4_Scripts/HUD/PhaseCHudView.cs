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
        [SerializeField] private int buttonFontSize = 30;

        private readonly List<TextMeshProUGUI> cupLabels = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> sourceLabels = new List<TextMeshProUGUI>();
        private LevelContext context;
        private GameplayManager gameplay;
        private LevelManager levelManager;
        private Canvas canvas;
        private TextMeshProUGUI title;
        private TextMeshProUGUI state;
        private Button retryButton;
        private Button nextButton;
        private bool retryRequested;
        private bool nextRequested;

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            gameplay = value.LevelRoot.GetComponent<GameplayManager>();
            levelManager = LevelManager.Instance;
            BuildUi();
            gameplay.GameStateChanged += OnGameStateChanged;
            gameplay.SourceStateChanged += OnSourceChanged;
            gameplay.CupChanged += OnCupChanged;
            gameplay.StrokeCommitted += OnStrokeCommitted;
            Refresh();
        }

        private void Update()
        {
            if (retryRequested)
            {
                retryRequested = false;
                if (levelManager != null && levelManager.IsReady) levelManager.ReloadCurrentLevel();
            }
            if (nextRequested)
            {
                nextRequested = false;
                if (levelManager != null && levelManager.IsReady) levelManager.BeginNextLevel();
            }
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("PhaseCHudCanvas");
            canvasObject.transform.SetParent(null, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panel = CreateRect("Panel", canvas.transform, new Vector2(28f, -28f), new Vector2(430f, 410f), new Color(0.05f, 0.07f, 0.11f, 0.88f));
            title = CreateLabel("Title", panel.transform, "Phase C", titleFontSize, TextAlignmentOptions.Center, new Vector2(0f, -28f), new Vector2(390f, 58f));
            state = CreateLabel("State", panel.transform, string.Empty, bodyFontSize, TextAlignmentOptions.Left, new Vector2(18f, -92f), new Vector2(390f, 48f));
            CreateLists(panel.transform);
            retryButton = CreateButton("Retry", panel.transform, "Retry", new Vector2(18f, 24f), new Vector2(180f, 54f));
            nextButton = CreateButton("Next", panel.transform, "Next", new Vector2(212f, 24f), new Vector2(180f, 54f));
            retryButton.onClick.AddListener(RequestRetry);
            nextButton.onClick.AddListener(RequestNext);
        }

        private void CreateLists(Transform parent)
        {
            float y = -145f;
            for (int i = 0; i < gameplay.Sources.Count; i++)
            {
                sourceLabels.Add(CreateLabel("Source" + i, parent, string.Empty, bodyFontSize, TextAlignmentOptions.Left, new Vector2(18f, y), new Vector2(390f, 38f)));
                y -= 36f;
            }
            for (int i = 0; i < gameplay.Cups.Count; i++)
            {
                cupLabels.Add(CreateLabel("Cup" + i, parent, string.Empty, bodyFontSize, TextAlignmentOptions.Left, new Vector2(18f, y), new Vector2(390f, 38f)));
                y -= 36f;
            }
        }

        private void Refresh()
        {
            if (gameplay == null) return;
            title.text = context.LevelId;
            state.text = "State: " + gameplay.State;
            for (int i = 0; i < gameplay.Sources.Count && i < sourceLabels.Count; i++)
            {
                SourceDomain source = gameplay.Sources[i];
                sourceLabels[i].text = source.StableId + ": " + source.State + " " + source.Remaining + "/" + source.Initial;
            }
            for (int i = 0; i < gameplay.Cups.Count && i < cupLabels.Count; i++)
            {
                CupDomain cup = gameplay.Cups[i];
                cupLabels[i].text = cup.StableId + ": " + cup.CollectedLogical + "/" + cup.RequiredLogical + (cup.ForeignDetected ? " WRONG" : cup.Full ? " FULL" : string.Empty);
            }
            nextButton.interactable = gameplay.State == GameState.Won && levelManager != null && levelManager.CanBeginNextLevel();
        }

        private void OnGameStateChanged(GameState value) { Refresh(); }
        private void OnSourceChanged(string id) { Refresh(); }
        private void OnCupChanged(string id) { Refresh(); }
        private void OnStrokeCommitted(IList<Vector2> points, float thickness) { Refresh(); }
        private void RequestRetry() { retryRequested = true; }
        private void RequestNext() { nextRequested = true; }

        public void CleanupForLevelUnload()
        {
            if (gameplay != null)
            {
                gameplay.GameStateChanged -= OnGameStateChanged;
                gameplay.SourceStateChanged -= OnSourceChanged;
                gameplay.CupChanged -= OnCupChanged;
                gameplay.StrokeCommitted -= OnStrokeCommitted;
            }
            if (canvas != null) DestroyOwned(canvas.gameObject);
            canvas = null;
            gameplay = null;
            context = null;
            levelManager = null;
            cupLabels.Clear();
            sourceLabels.Clear();
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
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

        private TextMeshProUGUI CreateLabel(string name, Transform parent, string value, int size, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 dimensions)
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
            return label;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = CreateRect(name, parent, anchoredPosition, size, new Color(0.15f, 0.25f, 0.35f, 1f));
            Button button = go.AddComponent<Button>();
            TextMeshProUGUI text = CreateLabel("Label", go.transform, label, buttonFontSize, TextAlignmentOptions.Center, Vector2.zero, size);
            text.raycastTarget = false;
            return button;
        }

        private static void DestroyOwned(GameObject target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
