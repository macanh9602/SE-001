using SE001.Simulation.Sand;
using Unity.Collections;
using UnityEngine;

namespace SE001.Elements.Sand
{
    /// <summary>
    /// Renders the sand grid as one quad + one texture (port of sand-feel-lab renderSand, powder preset):
    /// palette color per material, per-grain tone jitter, surface highlight (top lighter, overhang darker),
    /// bilinear filtering for the soft powder look. Writes straight into the texture's pixel buffer: no per-frame alloc.
    /// Falling stream (Docs/visualizers/sand-stream-lab.html, panel B+): airborne grains get a streak above them, gaps to the
    /// next same-material grain in the column are bridged, and the result is widened ±streamHalfWidth cells with a grain
    /// texture that scrolls down with the flow — so a sparse falling trickle reads as one continuous sandy stream.
    /// Stream pixels only ever fill empty, non-geometry cells.
    /// Presentation only — reads SandSimulationState, never writes it (no effect on collision or cup counting).
    /// </summary>
    public sealed class SandFieldVisual : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private MeshFilter targetFilter;
        [SerializeField] private Color32 sandColor = new Color32(220, 180, 100, 255);
        [Header("Powder look (lab 'Bột mịn')")]
        [SerializeField, Range(0f, 1f)] private float jitter = 0.35f;
        [SerializeField, Range(0f, 1f)] private float highlight = 0.3f;
        [SerializeField] private bool softRender = true;
        [Header("Falling stream")]
        [Tooltip("Streak length above an airborne grain = ceil(fall speed * streak) cells.")]
        [SerializeField, Range(0f, 2f)] private float streamStreak = 1f;
        [Tooltip("Bridge the empty gap to the next same-material grain above if gap <= streak + this many cells.")]
        [SerializeField, Range(0, 16)] private int streamBridgeCells = 10;
        [Tooltip("Visual widening of the stream, cells each side. Collision is unaffected.")]
        [SerializeField, Range(0, 2)] private int streamHalfWidth = 1;
        [Tooltip("Grain texture on the stream: tone jitter, darker gaps, ragged edge. 0 = flat bar (rejected look).")]
        [SerializeField, Range(0f, 1f)] private float streamGrain = 0.8f;
        [Tooltip("Grain pattern scroll speed (cells per rendered frame) so the texture flows down with the sand.")]
        [SerializeField, Range(0, 8)] private int streamScrollCellsPerFrame = 2;
        [Tooltip("Max airborne grains decorated per frame; beyond this the rest render as plain grains.")]
        [SerializeField, Min(0)] private int maxStreamGrains = 8192;

        private Texture2D texture;
        private SandSimulation simulation;
        private MaterialPropertyBlock propertyBlock;
        private Mesh generatedSurface;
        private Color32[] palette;
        private Color32 emptyColor;
        private int[] streamGrains;
        private ushort[] streamStamp;
        private ushort streamFrameId;
        private int renderFrame;

        /// <summary>Index = materialId. Null → single sandColor.</summary>
        public void SetPalette(Color32[] value) { palette = value; }

        public void Bind(SandSimulation value)
        {
            simulation = value;
            if (simulation == null) return;
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetFilter == null) targetFilter = GetComponent<MeshFilter>();
            if (texture != null) DestroyOwnedTexture();
            texture = new Texture2D(simulation.State.Width, simulation.State.Height, TextureFormat.RGBA32, false, false)
            {
                name = "SandFieldTexture",
                filterMode = softRender ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            // Empty texels keep a sand-like RGB with alpha 0 so bilinear edges fade instead of darkening.
            emptyColor = new Color32(sandColor.r, sandColor.g, sandColor.b, 0);
            int cellCount = simulation.State.Width * simulation.State.Height;
            streamGrains = new int[maxStreamGrains];
            streamStamp = new ushort[cellCount];
            streamFrameId = 0;
            renderFrame = 0;
            EnsureSurface();
            if (targetRenderer != null)
            {
                if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
                propertyBlock.Clear();
                propertyBlock.SetTexture("_BaseMap", texture);
                propertyBlock.SetTexture("_MainTex", texture);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            UpdateTexture();
        }

        public void UpdateTexture()
        {
            if (simulation == null || texture == null || simulation.IsDisposed) return;
            SandSimulationState state = simulation.State;
            byte[] cells = state.Cells;
            byte[] shade = state.Shade;
            float[] velocity = state.Velocity;
            bool drawStream = streamGrains != null && streamGrains.Length > 0 && (streamStreak > 0f || streamBridgeCells > 0 || streamHalfWidth > 0);
            int streamCount = 0;
            int width = state.Width;
            int height = state.Height;
            NativeArray<Color32> pixels = texture.GetPixelData<Color32>(0);
            float jitterAmount = jitter * 26f;
            float highlightUp = highlight * 20f;
            float highlightDown = highlight * 24f;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int i = row + x;
                    byte m = cells[i];
                    if (m == 0) { pixels[i] = emptyColor; continue; }

                    Color32 baseColor = palette != null && m < palette.Length ? palette[m] : sandColor;
                    float l = (shade[i] - 128) / 128f * jitterAmount;
                    bool emptyAbove = y == height - 1 || cells[i + width] == 0;
                    bool emptyBelow = y > 0 && cells[i - width] == 0;
                    if (emptyAbove) l += highlightUp;
                    else if (emptyBelow) l -= highlightDown;

                    pixels[i] = new Color32(Clamp(baseColor.r + l), Clamp(baseColor.g + l), Clamp(baseColor.b + l * 0.95f), 255);

                    if (drawStream && streamCount < streamGrains.Length && y > 0 && velocity[i] >= 1f)
                    {
                        int below = i - width;
                        if (!IsGeometry(state, below) && (cells[below] == 0 || velocity[below] >= 1f)) streamGrains[streamCount++] = i;
                    }
                }
            }

            if (drawStream && streamCount > 0) DrawStream(state, pixels, streamCount);
            renderFrame++;
            texture.Apply(false, false);
        }

        private static byte Clamp(float v) => (byte)(v < 0f ? 0f : v > 255f ? 255f : v);

        private static bool IsGeometry(SandSimulationState state, int i) =>
            !state.ValidMask[i] || state.StaticMask[i] || state.CupWallMask[i] || state.DynamicMask[i];

        /// <summary>Overlay for airborne grains collected by the base pass. Only paints empty, non-geometry cells.</summary>
        private void DrawStream(SandSimulationState state, NativeArray<Color32> pixels, int count)
        {
            if (++streamFrameId == 0)
            {
                global::System.Array.Clear(streamStamp, 0, streamStamp.Length);
                streamFrameId = 1;
            }

            byte[] cells = state.Cells;
            float[] velocity = state.Velocity;
            int width = state.Width;
            int height = state.Height;
            int scroll = renderFrame * streamScrollCellsPerFrame;

            for (int n = 0; n < count; n++)
            {
                int i = streamGrains[n];
                byte m = cells[i];
                int x = i % width;
                int y = i / width;
                int len = (int)global::System.Math.Ceiling(velocity[i] * streamStreak);

                // Streak: straight up (board y+) until a grain or geometry.
                for (int k = 1; k <= len && y + k < height; k++)
                {
                    int j = i + k * width;
                    if (cells[j] != 0 || IsGeometry(state, j)) break;
                    MarkStreamCell(state, pixels, j, x, y + k, m, scroll);
                }

                // Bridge: fill the gap to the next same-material grain above within reach.
                if (streamBridgeCells > 0)
                {
                    int reach = len + streamBridgeCells;
                    int k = 1;
                    while (k <= reach && y + k < height && cells[i + k * width] == 0 && !IsGeometry(state, i + k * width)) k++;
                    if (k <= reach && y + k < height && cells[i + k * width] == m)
                        for (int q = 1; q < k; q++) MarkStreamCell(state, pixels, i + q * width, x, y + q, m, scroll);
                }

                // The grain itself seeds the widening.
                WidenStream(state, pixels, i, x, y, m, scroll);
            }
        }

        private void MarkStreamCell(SandSimulationState state, NativeArray<Color32> pixels, int j, int x, int y, byte m, int scroll)
        {
            if (streamStamp[j] != streamFrameId)
            {
                streamStamp[j] = streamFrameId;
                pixels[j] = StreamPixel(m, x, y, scroll, 0.95f, false, 1f);
            }

            WidenStream(state, pixels, j, x, y, m, scroll);
        }

        private void WidenStream(SandSimulationState state, NativeArray<Color32> pixels, int j, int x, int y, byte m, int scroll)
        {
            int half = streamHalfWidth;
            if (half <= 0) return;
            byte[] cells = state.Cells;
            int width = state.Width;
            for (int dx = -half; dx <= half; dx++)
            {
                if (dx == 0) continue;
                int xx = x + dx;
                if (xx < 0 || xx >= width) continue;
                int t = j + dx;
                // A core stream cell always wins; a cell that becomes core later simply overwrites this pixel.
                if (cells[t] != 0 || streamStamp[t] == streamFrameId || IsGeometry(state, t)) continue;
                int adx = dx < 0 ? -dx : dx;
                pixels[t] = StreamPixel(m, xx, y, scroll, 0.92f - 0.25f * (adx - 1), adx == half, 0.97f);
            }
        }

        private Color32 StreamPixel(byte m, int x, int y, int scroll, float alpha, bool edge, float dim)
        {
            Color32 c = palette != null && m < palette.Length ? palette[m] : sandColor;
            float l = 0f;
            if (streamGrain > 0f)
            {
                float r = Hash01(x, y + scroll);
                l = (r - 0.5f) * 2f * 26f * streamGrain;
                if (r < (edge ? 0.45f : 0.22f) * streamGrain) alpha *= edge ? 0.15f : 0.45f;
            }

            return new Color32(Clamp((c.r + l) * dim), Clamp((c.g + l) * dim), Clamp((c.b + l * 0.95f) * dim), Clamp(alpha * 255f));
        }

        /// <summary>Stable integer hash → [0,1). Deterministic, no UnityEngine.Random.</summary>
        private static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h >> 8) * (1f / 16777216f);
            }
        }

        private void LateUpdate() { UpdateTexture(); }

        private void OnDestroy()
        {
            DestroyOwnedTexture();
            DestroyOwnedSurface();
        }

        private void EnsureSurface()
        {
            if (targetFilter == null) targetFilter = gameObject.AddComponent<MeshFilter>();
            if (generatedSurface != null) DestroyOwnedSurface();
            Mesh surface = generatedSurface = new Mesh { name = "SandFieldSurface" };
            float width = simulation.State.Width * simulation.CellSize; // cells -> board units
            float height = simulation.State.Height * simulation.CellSize;
            surface.vertices = new[] { Vector3.zero, new Vector3(width, 0f), new Vector3(width, height), new Vector3(0f, height) };
            surface.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            surface.triangles = new[] { 0, 2, 1, 0, 3, 2 }; // faces -Z (gameplay camera side)
            targetFilter.sharedMesh = surface;
        }

        private void DestroyOwnedSurface()
        {
            if (generatedSurface == null) return;
            if (Application.isPlaying) Destroy(generatedSurface);
            else DestroyImmediate(generatedSurface);
            generatedSurface = null;
        }

        private void DestroyOwnedTexture()
        {
            if (texture == null) return;
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
            texture = null;
        }
    }
}
