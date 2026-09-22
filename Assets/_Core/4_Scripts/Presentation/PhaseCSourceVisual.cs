using SE001.Data;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCSourceVisual : MonoBehaviour
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer nozzleRenderer;
        [SerializeField] private float nozzleWidthRatio = 0.4f;
        [SerializeField] private float nozzleHeightRatio = 0.18f;
        [SerializeField] private float visualDepth = -0.3f;

        private SourceDomain source;
        private ColorProfile palette;
        private MaterialPropertyBlock block;
        private Material sourceMaterial;
        private Material nozzleMaterial;
        private JuiceProfile juiceProfile;

        public SourceDomain Domain => source;
        public Transform Pivot => pivot;
        public Renderer BodyRenderer => bodyRenderer;
        public Renderer NozzleRenderer => nozzleRenderer;

        public void Bind(SourceDomain value, ColorProfile valuePalette, PhaseCVisualMaterials materials, JuiceProfile juice)
        {
            source = value ?? throw new global::System.ArgumentNullException(nameof(value));
            palette = valuePalette;
            juiceProfile = juice;
            sourceMaterial = materials != null ? materials.sourceMaterial : null;
            nozzleMaterial = sourceMaterial;
            block ??= new MaterialPropertyBlock();
            if (pivot == null) pivot = transform.Find("Pivot");
            if (bodyRenderer == null) bodyRenderer = transform.Find("Pivot/View/Body")?.GetComponent<Renderer>();
            if (nozzleRenderer == null) nozzleRenderer = transform.Find("Pivot/View/Nozzle")?.GetComponent<Renderer>();
            if (pivot == null || bodyRenderer == null || nozzleRenderer == null)
                throw new MissingComponentException("SandSource prefab requires Pivot/View/Body and Pivot/View/Nozzle renderers.");

            transform.localPosition = new Vector3(source.Position.x + source.BodyOffset.x, source.Position.y + source.BodyOffset.y, visualDepth);
            pivot.localPosition = Vector3.zero;
            bodyRenderer.transform.localScale = new Vector3(source.Size.x, source.Size.y, 1f);
            Vector2 nozzleSize = new Vector2(source.Size.x * nozzleWidthRatio, source.Size.y * nozzleHeightRatio);
            nozzleRenderer.transform.localPosition = new Vector3(0f, source.Size.y * 0.5f, 0f);
            nozzleRenderer.transform.localScale = new Vector3(nozzleSize.x, nozzleSize.y, 1f);
            bodyRenderer.sharedMaterial = sourceMaterial;
            nozzleRenderer.sharedMaterial = nozzleMaterial;
            ApplyColor();
        }

        private void LateUpdate()
        {
            if (source == null) return;
            float duration = juiceProfile != null ? Mathf.Max(0.01f, juiceProfile.valveOpenDuration) : 0.2f;
            float speed = 180f / duration;
            float targetAngle = source.IsPouring ? 180f : 0f;
            float angle = Mathf.MoveTowardsAngle(
                pivot.localEulerAngles.z,
                targetAngle,
                speed * Time.deltaTime);
            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            ApplyColor();
        }

        private void ApplyColor()
        {
            Color color = palette != null && palette.Contains(source.MaterialId) ? palette.GetSandColor(source.MaterialId) : Color.magenta;
            if (source.State == SourceValveState.Empty) color = Color.Lerp(color, Color.gray, 0.7f);
            block.SetColor("_BaseColor", color);
            bodyRenderer.SetPropertyBlock(block);
            block.SetColor("_BaseColor", Color.white);
            nozzleRenderer.SetPropertyBlock(block);
        }
    }
}
