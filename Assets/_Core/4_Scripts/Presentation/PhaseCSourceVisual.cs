using SE001.Data;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCSourceVisual : MonoBehaviour
    {
        private const float IdleAngle = 180f;
        private const float PourAngle = 0f;

        private static readonly int FillLevelId = Shader.PropertyToID("_FillLevel");
        private static readonly int FillDirectionId = Shader.PropertyToID("_FillDirOS");
        private static readonly int SandColorId = Shader.PropertyToID("_SandColor");
        private static readonly int SparkleId = Shader.PropertyToID("_Sparkle");

        [SerializeField] private Transform pivot;
        [SerializeField] private Renderer shadowRenderer;
        [SerializeField] private Renderer sandFillRenderer;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer mouthRenderer;
        [SerializeField] private float visualDepth = -0.3f;

        private SourceDomain source;
        private ColorProfile palette;
        private PhaseCVisualMaterials materials;
        private JarVisualProfile visualProfile;
        private JuiceProfile juiceProfile;
        private MaterialPropertyBlock fillBlock;
        private Vector3 shadowOffsetWorld;
        private float lastFill = -1f;
        private SourceValveState lastState = (SourceValveState)(-1);
        private float lastAngle = float.NaN;

        public SourceDomain Domain => source;
        public Transform Pivot => pivot;
        public Renderer BodyRenderer => bodyRenderer;
        public Renderer MouthRenderer => mouthRenderer;
        public Renderer NozzleRenderer => mouthRenderer;
        public Renderer ShadowRenderer => shadowRenderer;
        public Renderer SandFillRenderer => sandFillRenderer;

        public void Bind(
            SourceDomain value,
            ColorProfile valuePalette,
            PhaseCVisualMaterials valueMaterials,
            JarVisualProfile valueVisualProfile,
            JuiceProfile juice)
        {
            source = value ?? throw new global::System.ArgumentNullException(nameof(value));
            palette = valuePalette;
            materials = valueMaterials;
            visualProfile = valueVisualProfile;
            juiceProfile = juice;
            fillBlock ??= new MaterialPropertyBlock();

            ResolveAuthoredChildren();
            ConfigureTransform();
            ConfigureMaterials();
            ApplyVisualState(true);
        }

        // Compatibility overload for tools that bind the Phase C visual directly.
        public void Bind(SourceDomain value, ColorProfile valuePalette, PhaseCVisualMaterials valueMaterials, JuiceProfile juice)
        {
            Bind(value, valuePalette, valueMaterials, null, juice);
        }

        private void ResolveAuthoredChildren()
        {
            if (pivot == null) pivot = transform.Find("Pivot");
            if (shadowRenderer == null) shadowRenderer = transform.Find("Pivot/View/Shadow")?.GetComponent<Renderer>();
            if (sandFillRenderer == null) sandFillRenderer = transform.Find("Pivot/View/SandFill")?.GetComponent<Renderer>();
            if (bodyRenderer == null) bodyRenderer = transform.Find("Pivot/View/Body")?.GetComponent<Renderer>();
            if (mouthRenderer == null) mouthRenderer = transform.Find("Pivot/View/Mouth")?.GetComponent<Renderer>();

            if (pivot == null || shadowRenderer == null || sandFillRenderer == null || bodyRenderer == null || mouthRenderer == null)
                throw new MissingComponentException(
                    "SandSource prefab requires Pivot/View/{Shadow,SandFill,Body,Mouth} renderers.");
        }

        private void ConfigureTransform()
        {
            transform.localPosition = new Vector3(
                source.Position.x + source.BodyOffset.x,
                source.Position.y + source.BodyOffset.y,
                0f);
            pivot.localPosition = Vector3.zero;
            float initialAngle = source.IsPouring ? PourAngle : IdleAngle;
            pivot.localRotation = Quaternion.Euler(0f, 0f, initialAngle);
            lastAngle = initialAngle;

            float uniformScale = visualProfile != null
                ? visualProfile.SourceUniformScale(source.Size.y)
                : source.Size.y / 1.92f;
            bodyRenderer.transform.localPosition = Vector3.zero;
            bodyRenderer.transform.localScale = Vector3.one * uniformScale;
            sandFillRenderer.transform.localPosition = Vector3.zero;
            sandFillRenderer.transform.localScale = Vector3.one * uniformScale;
            shadowOffsetWorld = new Vector3(
                visualProfile != null ? visualProfile.shadowOffsetPixels.x * 0.01f * uniformScale : -0.06f * uniformScale,
                visualProfile != null ? visualProfile.shadowOffsetPixels.y * 0.01f * uniformScale : -0.24f * uniformScale,
                0f);
            ApplyShadowWorldOffset();
            shadowRenderer.transform.localScale = Vector3.one * uniformScale;
            mouthRenderer.transform.localPosition = new Vector3(
                0f,
                JarVisualGeometry.SourceMouthLocalY(source.Size.y, visualProfile),
                0f);
            mouthRenderer.transform.localScale = Vector3.one * uniformScale;
        }

        private void ConfigureMaterials()
        {
            if (materials != null)
            {
                bodyRenderer.sharedMaterial = materials.ResolveSourceBody();
                sandFillRenderer.sharedMaterial = materials.sourceFillMaterial;
                shadowRenderer.sharedMaterial = materials.sourceShadowMaterial;
            }

            ColorProfileEntry entry;
            mouthRenderer.sharedMaterial = palette != null && palette.TryGetEntry(source.MaterialId, out entry)
                ? entry.sourceMouthMaterial
                : null;
        }

        private void LateUpdate()
        {
            if (source == null) return;
            float duration = juiceProfile != null ? Mathf.Max(0.01f, juiceProfile.valveOpenDuration) : 0.2f;
            float speed = 180f / duration;
            float targetAngle = source.IsPouring ? PourAngle : IdleAngle;
            float angle = Mathf.MoveTowardsAngle(pivot.localEulerAngles.z, targetAngle, speed * Time.deltaTime);
            bool angleChanged = !Mathf.Approximately(angle, lastAngle);
            if (angleChanged)
            {
                pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
                ApplyShadowWorldOffset();
                lastAngle = angle;
            }

            float fill = source.Initial > 0 ? source.Remaining / (float)source.Initial : 0f;
            if (angleChanged || !Mathf.Approximately(fill, lastFill) || source.State != lastState)
                ApplyVisualState(false);
        }

        private void ApplyShadowWorldOffset()
        {
            if (shadowRenderer == null || pivot == null) return;
            shadowRenderer.transform.localPosition = pivot.InverseTransformVector(shadowOffsetWorld);
        }

        private void ApplyVisualState(bool force)
        {
            float fill = source.Initial > 0 ? source.Remaining / (float)source.Initial : 0f;
            if (!force && Mathf.Approximately(fill, lastFill) && source.State == lastState)
                return;

            float areaCorrectedHeight = JarVisualGeometry.AreaToHeight(
                fill,
                visualProfile != null ? visualProfile.sourceFillAreaLut : null);
            ColorProfileEntry entry;
            Color sandColor = palette != null && palette.TryGetEntry(source.MaterialId, out entry)
                ? entry.sandColor
                : Color.magenta;
            Vector3 worldDownOS = pivot.InverseTransformDirection(Vector3.down).normalized;
            fillBlock.Clear();
            fillBlock.SetFloat(FillLevelId, source.State == SourceValveState.Empty ? 0f : areaCorrectedHeight);
            fillBlock.SetVector(FillDirectionId, worldDownOS);
            fillBlock.SetColor(SandColorId, sandColor);
            fillBlock.SetFloat(SparkleId, source.State == SourceValveState.Empty ? 0f : 1f);
            sandFillRenderer.SetPropertyBlock(fillBlock);
            lastFill = fill;
            lastState = source.State;
        }
    }
}
