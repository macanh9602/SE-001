using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SE001.Gameplay;
using SE001.Data;

namespace SE001.System.Management
{
    /// <summary>Pointer → TapSource or Draw stroke intents. Never touches the simulation directly.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayInputController : MonoBehaviour, ILevelLifecycleParticipant
    {
        [SerializeField] private float drawStartDeadZonePixels = 18f;
        [SerializeField] private float minPointDistance = 0.08f;
        [SerializeField] private float drawThickness = 0.3f;
        [SerializeField] private int maxPointsPerStroke = 128;
        [SerializeField] private float sourceHitPadding = 0.2f;

        private readonly List<Vector2> stroke = new List<Vector2>(128);
        private GameplayManager manager;
        private Camera inputCamera;
        private LevelContext context;
        private Vector2 downScreen;
        private bool held;
        private bool drawing;
        private bool strokeStopped;
        private float strokeLength;
        private Rect blockedGuiRect;

        /// <summary>Live preview points (board space) while dragging; for visuals only.</summary>
        public IReadOnlyList<Vector2> PreviewStroke => stroke;
        public bool IsDrawing => drawing;
        public float DrawThickness => manager != null ? manager.EffectiveDrawThickness(drawThickness) : drawThickness;
        /// <summary>Screen rect (GUI coords, y down) that should not start input, e.g. a dev overlay.</summary>
        public Rect BlockedGuiRect
        {
            get => blockedGuiRect;
            set => blockedGuiRect = value;
        }

        public void Configure(DrawPathProfile profile, float hitPadding)
        {
            if (profile == null) return;
            drawStartDeadZonePixels = Mathf.Max(0f, profile.drawStartDeadZone);
            minPointDistance = Mathf.Max(0.001f, profile.minPointDistance);
            drawThickness = Mathf.Max(0.001f, profile.drawThickness);
            maxPointsPerStroke = Mathf.Max(2, profile.maxPointsPerStroke);
            sourceHitPadding = Mathf.Max(0f, hitPadding);
        }

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            manager = GetComponent<GameplayManager>();
            inputCamera = Camera.main;
        }

        private void Update()
        {
            if (manager == null || context == null || inputCamera == null)
            {
                return;
            }
            if (HUDSystem.Instance != null && HUDSystem.Instance.BlockByPanel())
            {
                held = false;
                drawing = false;
                strokeStopped = false;
                strokeLength = 0f;
                stroke.Clear();
                return;
            }
            if (manager.State != GameState.Playing)
            {
                held = false;
                drawing = false;
                strokeStopped = false;
                strokeLength = 0f;
                stroke.Clear();
                return;
            }

            Vector2 screen = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                bool blocked = IsBlocked(screen);
                if (blocked) return;
                downScreen = screen;
                held = true;
                drawing = false;
                strokeStopped = false;
                strokeLength = 0f;
                stroke.Clear();
            }

            if (!held) return;

            if (Input.GetMouseButton(0))
            {
                if (!drawing && (screen - downScreen).sqrMagnitude >= drawStartDeadZonePixels * drawStartDeadZonePixels)
                {
                    drawing = true;
                    Vector2 start = ToBoard(downScreen);
                    if (manager.CanBeginDraw(start, drawThickness)) stroke.Add(start);
                    else strokeStopped = true;
                }

                if (drawing && !strokeStopped && stroke.Count < maxPointsPerStroke)
                {
                    Vector2 p = ToBoard(screen);
                    Vector2 last = stroke[stroke.Count - 1];
                    if (Vector2.Distance(last, p) >= minPointDistance)
                    {
                        bool fullyAccepted = manager.TryAcceptDrawSegment(last, p, strokeLength, drawThickness,
                            out Vector2 accepted);
                        float acceptedDistance = Vector2.Distance(last, accepted);
                        if (acceptedDistance > Mathf.Epsilon)
                        {
                            stroke.Add(accepted);
                            strokeLength += acceptedDistance;
                        }
                        if (!fullyAccepted) strokeStopped = true;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                held = false;
                if (drawing)
                {
                    drawing = false;
                    if (stroke.Count >= 2) manager.CommitStroke(stroke, drawThickness);
                    stroke.Clear();
                    strokeLength = 0f;
                    strokeStopped = false;
                    return;
                }

                TryTapSource(ToBoard(screen));
            }
        }

        private void TryTapSource(Vector2 boardPoint)
        {
            // Hit-test the visible jar body (emit point + BodyOffset), not the emit point itself.
            SourceDomain best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < manager.Sources.Count; i++)
            {
                SourceDomain source = manager.Sources[i];
                if (!source.HitTest(boardPoint, sourceHitPadding)) continue;
                float d = Vector2.Distance(boardPoint, source.Position + source.BodyOffset);
                if (d < bestDistance)
                {
                    best = source;
                    bestDistance = d;
                }
            }

            if (best != null) manager.ToggleSource(best.StableId);
        }

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
        private PointerEventData uiPointer;
        private EventSystem uiPointerOwner;

        /// <summary>
        /// Blocks only when the pointer is over an INTERACTIVE UI element (button, slider, drag handler...).
        /// Decorative full-screen graphics (e.g. background "BG" with Raycast Target on) must not eat gameplay input.
        /// </summary>
        private bool IsBlocked(Vector2 screen)
        {
            EventSystem es = EventSystem.current;
            if (es != null)
            {
                if (uiPointer == null || uiPointerOwner != es)
                {
                    uiPointer = new PointerEventData(es);
                    uiPointerOwner = es;
                }
                uiPointer.position = screen;
                uiHits.Clear();
                es.RaycastAll(uiPointer, uiHits);
                for (int i = 0; i < uiHits.Count; i++)
                {
                    GameObject go = uiHits[i].gameObject;
                    if (go.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
                    if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(go) != null) return true;
                    if (ExecuteEvents.GetEventHandler<IDragHandler>(go) != null) return true;
                }
            }

            Vector2 gui = new Vector2(screen.x, Screen.height - screen.y);
            return BlockedGuiRect.width > 0f && BlockedGuiRect.Contains(gui);
        }

        /// <summary>Screen → BoardRoot local XY on the z = 0 board plane.</summary>
        public Vector2 ToBoard(Vector2 screen)
        {
            Ray ray = inputCamera.ScreenPointToRay(screen);
            Transform board = context.BoardRoot;
            Plane plane = new Plane(board.forward, board.position);
            if (!plane.Raycast(ray, out float enter)) return Vector2.zero;
            Vector3 local = board.InverseTransformPoint(ray.GetPoint(enter));
            return new Vector2(local.x, local.y);
        }

        public void CleanupForLevelUnload()
        {
            manager = null;
            context = null;
            inputCamera = null;
            held = false;
            drawing = false;
            strokeStopped = false;
            strokeLength = 0f;
            stroke.Clear();
        }
    }
}
