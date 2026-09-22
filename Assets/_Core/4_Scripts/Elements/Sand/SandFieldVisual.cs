using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Elements.Sand
{
    public sealed class SandFieldVisual : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private MeshFilter targetFilter;
        [SerializeField] private Color32 emptyColor = new Color32(0, 0, 0, 0);
        [SerializeField] private Color32 sandColor = new Color32(220, 180, 100, 255);
        private Texture2D texture;
        private Color32[] pixels;
        private SandSimulation simulation;
        private MaterialPropertyBlock propertyBlock;
        private Mesh generatedSurface;

        public void Bind(SandSimulation value)
        {
            simulation = value;
            if (simulation == null) return;
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetFilter == null) targetFilter = GetComponent<MeshFilter>();
            if (texture != null) DestroyOwnedTexture();
            texture = new Texture2D(simulation.State.Width, simulation.State.Height, TextureFormat.RGBA32, false, true) { name = "SandFieldTexture", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            pixels = new Color32[simulation.State.Cells.Length];
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
            if (simulation == null || texture == null) return;
            byte[] cells = simulation.State.Cells;
            for (int i = 0; i < cells.Length; i++) pixels[i] = cells[i] == 0 ? emptyColor : sandColor;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void LateUpdate() { UpdateTexture(); }
        private void OnDestroy() { DestroyOwnedTexture(); DestroyOwnedSurface(); }
        private void EnsureSurface()
        {
            if (targetFilter == null) targetFilter = gameObject.AddComponent<MeshFilter>();
            if (generatedSurface != null) DestroyOwnedSurface();
            Mesh surface = generatedSurface = new Mesh { name = "SandFieldSurface" };
            float width = simulation.State.Width * simulation.CellSize; float height = simulation.State.Height * simulation.CellSize; // cells -> board units
            surface.vertices = new[] { Vector3.zero, new Vector3(width, 0f), new Vector3(width, height), new Vector3(0f, height) };
            surface.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            surface.triangles = new[] { 0, 2, 1, 0, 3, 2 }; // faces -Z (gameplay camera side)
            targetFilter.sharedMesh = surface;
        }
        private void DestroyOwnedSurface() { if (generatedSurface == null) return; if (Application.isPlaying) Destroy(generatedSurface); else DestroyImmediate(generatedSurface); generatedSurface = null; }
        private void DestroyOwnedTexture() { if (texture == null) return; if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture); texture = null; pixels = null; }
    }
}
