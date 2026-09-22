using SE001.System.Management;
using UnityEngine;

namespace SE001.Presentation
{
    /// <summary>
    /// Frames the active level's board (board-space XY, origin 0,0) with a "contain" policy:
    /// full board width on phones (9:16 and taller), full board height on wider screens (tablet).
    /// Extra space on tall screens goes above/below according to <see cref="anchor"/>.
    /// Re-fits on LevelReady and when the render target size changes. No per-frame allocation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        public enum VerticalAnchor { Center, Top, Bottom }

        [SerializeField] private LevelManager levelManager;
        [Tooltip("Board-space units kept around the board on every side.")]
        [SerializeField, Min(0f)] private float margin = 0f;
        [Tooltip("Viewport fraction reserved for HUD at the top (0 = HUD overlays the board, like the reference demo).")]
        [SerializeField, Range(0f, 0.4f)] private float topReserve = 0f;
        [SerializeField, Range(0f, 0.4f)] private float bottomReserve = 0f;
        [SerializeField] private VerticalAnchor anchor = VerticalAnchor.Center;

        private Camera targetCamera;
        private int lastWidth;
        private int lastHeight;
        private bool subscribed;

        private void Awake() { targetCamera = GetComponent<Camera>(); }

        private void OnEnable()
        {
            TrySubscribe();
            Fit();
        }

        private void OnDisable()
        {
            if (subscribed && levelManager != null) levelManager.LevelReady -= OnLevelReady;
            subscribed = false;
        }

        private void LateUpdate()
        {
            if (!subscribed) TrySubscribe();
            if (targetCamera.pixelWidth != lastWidth || targetCamera.pixelHeight != lastHeight) Fit();
        }

        private void TrySubscribe()
        {
            if (subscribed) return;
            if (levelManager == null) levelManager = LevelManager.Instance;
            if (levelManager == null) return;
            levelManager.LevelReady += OnLevelReady;
            subscribed = true;
        }

        private void OnLevelReady(LevelContext context) { Fit(); }

        [ContextMenu("Fit Now")]
        public void Fit()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            lastWidth = targetCamera.pixelWidth;
            lastHeight = targetCamera.pixelHeight;

            LevelContext context = levelManager != null ? levelManager.CurrentContext : null;
            if (context == null || context.IsDisposed) return;
            Vector2 board = context.BoardSize;
            if (board.x <= 0f || board.y <= 0f) return;

            float aspect = Mathf.Max(0.01f, targetCamera.aspect);
            float usable = Mathf.Max(0.05f, 1f - topReserve - bottomReserve);
            float framedWidth = board.x + 2f * margin;
            float framedHeight = board.y + 2f * margin;
            float viewHeight = Mathf.Max(framedWidth / aspect, framedHeight / usable);

            float cameraY;
            switch (anchor)
            {
                case VerticalAnchor.Top: cameraY = board.y + margin - (0.5f - topReserve) * viewHeight; break;
                case VerticalAnchor.Bottom: cameraY = -margin + (0.5f - bottomReserve) * viewHeight; break;
                default: cameraY = board.y * 0.5f - (bottomReserve + usable * 0.5f - 0.5f) * viewHeight; break;
            }

            // Board-space is local to BoardRoot; the level hierarchy may sit under an offset parent
            // (scene separator objects), so frame in BoardRoot space and convert to world.
            Transform boardRoot = context.BoardRoot;
            float scale = Mathf.Abs(boardRoot.lossyScale.x);
            float localDistance = 10f; // orthographic: any distance in front of the walls works
            if (targetCamera.orthographic) targetCamera.orthographicSize = viewHeight * 0.5f * scale;
            else localDistance = viewHeight * 0.5f / Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            Transform t = transform;
            t.SetPositionAndRotation(
                boardRoot.TransformPoint(new Vector3(board.x * 0.5f, cameraY, -localDistance)),
                boardRoot.rotation); // looks along BoardRoot +Z at the board plane z = 0
        }
    }
}
