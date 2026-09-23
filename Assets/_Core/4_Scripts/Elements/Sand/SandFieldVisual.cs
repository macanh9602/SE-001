using SE001.Simulation.Sand;
using Unity.Collections;
using UnityEngine;

namespace SE001.Elements.Sand
{
    /// <summary>
    /// Renders the sand grid as one quad + one data texture. Each texel encodes a cell (R material id, G grain tone,
    /// B flags: 1 surface, 2 overhang, 4 airborne, 8 cosmetic stream, 16 stream edge; A coverage) and SE001/SandField
    /// draws round grains in 3 tones with sparkle (Sand Level Lab look, 2026-09-24). Writes straight into the texture's
    /// pixel buffer: no per-frame alloc.
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
        [Header("Grain look")]
        [Tooltip("Round-grain position jitter (0..1 → 0..0.6 cell).")]
        [SerializeField, Range(0f, 1f)] private float jitter = 0.42f;
        [Tooltip("How much lighter the top surface grains are.")]
        [SerializeField, Range(0f, 1f)] private float highlight = 0.37f;
        [Tooltip("Round grains (shader). Off = one square per cell (debug).")]
        [SerializeField] private bool roundGrains = true;
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
        [Tooltip("Stream trail kept per rendered frame (lab 'trail keep'). Bridges sparse falls so the stream never breaks. 0 = off.")]
        [SerializeField, Range(0f, 0.95f)] private float streamTrailKeep = 0.8f;

        private const int PaletteSize = 256;
        private const byte FlagSurface = 1;
        private const byte FlagOverhang = 2;
        private const byte FlagAirborne = 4;
        private const byte FlagStream = 8;
        private const byte FlagStreamEdge = 16;

        // Sliding grains keep v = 1 (SandSimulation slide rule); only real falls shimmer (lab: vel > 1).
        private const float AirborneVelocity = 1.5f;
        private const byte FlagTrail = 32;
        private const byte TrailMin = 10;

        private Texture2D texture;
        private Texture2D paletteTexture;
        private SandSimulation simulation;
        private MaterialPropertyBlock propertyBlock;
        private Mesh generatedSurface;
        private Color32[] palette;
        private Color32 emptyColor;
        private int[] streamGrains;
        private ushort[] streamStamp;
        private ushort streamFrameId;
        private byte[] trailAlpha;
        private byte[] trailMaterial;
        private int renderFrame;

        /// <summary>Index = materialId. Null → single sandColor.</summary>
        public void SetPalette(Color32[] value)
        {
            palette = value;
            if (paletteTexture != null) FillPaletteTexture();
        }

        public void Bind(SandSimulation value)
        {
            simulation = value;
            if (simulation == null) return;
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetFilter == null) targetFilter = GetComponent<MeshFilter>();
            if (texture != null) DestroyOwnedTexture();
            // Data texture: linear (no sRGB decode) + point sampling, the shader reads exact cell codes.
            texture = new Texture2D(simulation.State.Width, simulation.State.Height, TextureFormat.RGBA32, false, true)
            {
                name = "SandFieldTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            emptyColor = new Color32(0, 0, 0, 0);
            EnsurePaletteTexture();
            int cellCount = simulation.State.Width * simulation.State.Height;
            streamGrains = new int[maxStreamGrains];
            streamStamp = new ushort[cellCount];
            streamFrameId = 0;
            trailAlpha = new byte[cellCount];
            trailMaterial = new byte[cellCount];
            renderFrame = 0;
            EnsureSurface();
            if (targetRenderer != null)
            {
                if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
                propertyBlock.Clear();
                propertyBlock.SetTexture("_BaseMap", texture);
                propertyBlock.SetTexture("_MainTex", texture);
                propertyBlock.SetTexture("_Palette", paletteTexture);
                propertyBlock.SetVector("_GridSize", new Vector4(simulation.State.Width, simulation.State.Height, 0f, 0f));
                propertyBlock.SetFloat("_RoundGrains", roundGrains ? 1f : 0f);
                propertyBlock.SetFloat("_GrainJitter", jitter * 0.6f);
                propertyBlock.SetFloat("_SurfaceLift", highlight);
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
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int i = row + x;
                    byte m = cells[i];
                    if (m == 0)
                    {
                        pixels[i] = TrailPixel(state, i, x, y);
                        continue;
                    }

                    byte flags = 0;
                    bool emptyAbove = y == height - 1 || cells[i + width] == 0;
                    bool emptyBelow = y > 0 && cells[i - width] == 0;
                    if (emptyAbove) flags |= FlagSurface;
                    else if (emptyBelow) flags |= FlagOverhang;
                    if (velocity[i] >= AirborneVelocity)
                    {
                        flags |= FlagAirborne;
                        SetTrail(i, m, 255);
                    }
                    else
                    {
                        trailAlpha[i] = 0;
                    }

                    pixels[i] = new Color32(m, shade[i], flags, 255);

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

        private void EnsurePaletteTexture()
        {
            if (paletteTexture == null)
            {
                // sRGB colour lookup (material id → sand colour), sampled with Load in the shader.
                paletteTexture = new Texture2D(PaletteSize, 1, TextureFormat.RGBA32, false, false)
                {
                    name = "SandFieldPalette",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            FillPaletteTexture();
        }

        private void FillPaletteTexture()
        {
            NativeArray<Color32> colors = paletteTexture.GetPixelData<Color32>(0);
            for (int i = 0; i < PaletteSize; i++)
                colors[i] = palette != null && i < palette.Length && palette[i].a > 0 ? palette[i] : sandColor;
            paletteTexture.Apply(false, false);
        }

        private static bool IsGeometry(SandSimulationState state, int i) =>
            !state.ValidMask[i] || state.StaticMask[i] || state.CupWallMask[i] || state.DynamicMask[i] || state.RotatingMask[i];

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
                SetTrail(j, m, 255);
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
                SetTrail(t, m, adx == half ? (byte)120 : (byte)200);
            }
        }

        /// <summary>Fading trail left by the stream on an empty cell (lab trail layer). Presentation only.</summary>
        private Color32 TrailPixel(SandSimulationState state, int i, int x, int y)
        {
            byte alpha = trailAlpha[i];
            if (alpha == 0) return emptyColor;
            int next = (int)(alpha * streamTrailKeep);
            if (next < TrailMin || IsGeometry(state, i))
            {
                trailAlpha[i] = 0;
                return emptyColor;
            }

            trailAlpha[i] = (byte)next;
            byte tone = (byte)(Hash01(x, y) * 255f);
            return new Color32(trailMaterial[i], tone, FlagStream | FlagTrail, (byte)next);
        }

        private void SetTrail(int i, byte m, byte alpha)
        {
            if (streamTrailKeep <= 0f || alpha <= trailAlpha[i]) return;
            trailAlpha[i] = alpha;
            trailMaterial[i] = m;
        }

        private Color32 StreamPixel(byte m, int x, int y, int scroll, float alpha, bool edge, float dim)
        {
            // dim is kept for call-site compatibility; stream tone/darkening is resolved in the shader.
            float r = Hash01(x, y + scroll);
            if (streamGrain > 0f && r < (edge ? 0.45f : 0.22f) * streamGrain) alpha *= edge ? 0.15f : 0.45f;
            byte flags = (byte)(FlagStream | (edge ? FlagStreamEdge : 0));
            return new Color32(m, (byte)(r * 255f), flags, Clamp(alpha * 255f * dim));
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
            if (paletteTexture != null)
            {
                if (Application.isPlaying) Destroy(paletteTexture);
                else DestroyImmediate(paletteTexture);
                paletteTexture = null;
            }

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
