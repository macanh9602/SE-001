using SE001.Simulation.Sand;
using Unity.Collections;
using UnityEngine;

namespace SE001.Elements.Sand
{
    /// <summary>
    /// Renders the sand grid as one quad + one texture (port of sand-feel-lab renderSand, powder preset):
    /// palette color per material, per-grain tone jitter, surface highlight (top lighter, overhang darker),
    /// bilinear filtering for the soft powder look. Writes straight into the texture's pixel buffer: no per-frame alloc.
    /// Presentation only — reads SandSimulationState, never writes it.
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

        private Texture2D texture;
        private SandSimulation simulation;
        private MaterialPropertyBlock propertyBlock;
        private Mesh generatedSurface;
        private Color32[] palette;
        private Color32 emptyColor;

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
                }
            }

            texture.Apply(false, false);
        }

        private static byte Clamp(float v) => (byte)(v < 0f ? 0f : v > 255f ? 255f : v);

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
