using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SE001.Gameplay;

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
        [SerializeField] private float sourceHitRadius = 0.7f;

        private readonly List<Vector2> stroke = new List<Vector2>(128);
        private GameplayManager manager;
        private Camera inputCamera;
        private LevelContext context;
        private Vector2 downScreen;
        private bool held;
        private bool drawing;

        /// <summary>Live preview points (board space) while dragging; for visuals only.</summary>
        public IReadOnlyList<Vector2> PreviewStroke => stroke;
        public bool IsDrawing => drawing;
        public float DrawThickness => drawThickness;
        /// <summary>Screen rect (GUI coords, y down) that should not start input, e.g. a dev overlay.</summary>
        public Rect BlockedGuiRect { get; set; }

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
                if (Input.GetMouseButtonDown(0)) AgentDebugAudit.Event(GameplayManager.DrawAudit, "Input.Down.Ignored", $"manager={(manager != null)} context={(context != null)} camera={(inputCamera != null)}");
                return;
            }
            if (manager.State != GameState.Playing) { held = false; drawing = false; stroke.Clear(); return; }

            Vector2 screen = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                bool blocked = IsBlocked(screen);
                AgentDebugAudit.Event(GameplayManager.DrawAudit, "Input.Down", $"screen={screen.ToString("F0")} screenSize={Screen.width}x{Screen.height} board={ToBoard(screen).ToString("F2")} overUi={overUi} uiObj={(overUi ? CurrentUiName() : "-")} guiRect={BlockedGuiRect} blocked={blocked} state={manager.State}");
                if (blocked) return;
                downScreen = screen;
                held = true;
                drawing = false;
                stroke.Clear();
            }

            if (!held) return;

            if (Input.GetMouseButton(0))
            {
                if (!drawing && (screen - downScreen).sqrMagnitude >= drawStartDeadZonePixels * drawStartDeadZonePixels)
                {
                    drawing = true;
                    stroke.Add(ToBoard(downScreen));
                    AgentDebugAudit.Event(GameplayManager.DrawAudit, "Input.DrawStart", $"screen={screen.ToString("F0")} startBoard={stroke[0].ToString("F2")}");
                }

                if (drawing && stroke.Count < maxPointsPerStroke)
                {
                    Vector2 p = ToBoard(screen);
                    if (Vector2.Distance(stroke[stroke.Count - 1], p) >= minPointDistance) stroke.Add(p);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                held = false;
                AgentDebugAudit.Event(GameplayManager.DrawAudit, "Input.Up", $"drawing={drawing} points={stroke.Count} lastBoard={(stroke.Count > 0 ? stroke[stroke.Count - 1].ToString("F2") : "-")} dragPx={(screen - downScreen).magnitude:F0}");
                if (drawing)
                {
                    drawing = false;
                    if (stroke.Count >= 2) manager.CommitStroke(stroke, drawThickness);
                    stroke.Clear();
                    return;
                }

                TryTapSource(ToBoard(screen));
            }
        }

        private void TryTapSource(Vector2 boardPoint)
        {
            SourceDomain best = null;
            float bestDistance = sourceHitRadius;
            for (int i = 0; i < manager.Sources.Count; i++)
            {
                SourceDomain source = manager.Sources[i];
                float d = Vector2.Distance(boardPoint, source.Position);
                if (d <= bestDistance) { best = source; bestDistance = d; }
            }

            AgentDebugAudit.Event(GameplayManager.DrawAudit, "Input.Tap", $"board={boardPoint.ToString("F2")} hit={(best != null ? best.StableId : "none")}");
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
                if (uiPointer == null || uiPointerOwner != es) { uiPointer = new PointerEventData(es); uiPointerOwner = es; }
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

        private static string CurrentUiName()
        {
            var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            return hits.Count > 0 ? hits[0].gameObject.name : "?(non-raycast)";
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
            stroke.Clear();
        }
    }
}
