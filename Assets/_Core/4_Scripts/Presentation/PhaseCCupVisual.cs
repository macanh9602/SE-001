using SE001.Data;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCCupVisual : MonoBehaviour
    {
        private static readonly int FeedbackId = Shader.PropertyToID("_Feedback");

        [SerializeField] private Transform view;
        [SerializeField] private Renderer shadowRenderer;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer capTopRenderer;
        [SerializeField] private Renderer capBottomRenderer;
        [SerializeField] private float visualDepth = -0.3f;

        private CupDomain cup;
        private ColorProfile palette;
        private PhaseCVisualMaterials materials;
        private JarVisualProfile visualProfile;
        private MaterialPropertyBlock capBlock;
        private bool lastForeign;
        private float lastFeedback = -1f;

        public CupDomain Domain => cup;
        public Transform View => view;
        public Renderer ShadowRenderer => shadowRenderer;
        public Renderer BodyRenderer => bodyRenderer;
        public Renderer CapTopRenderer => capTopRenderer;
        public Renderer CapBottomRenderer => capBottomRenderer;

        public void ReleaseForUnload()
        {
            // All meshes are authored assets. There is no generated per-bind mesh to destroy.
            capBlock?.Clear();
        }

        public void Bind(
            CupDomain value,
            ColorProfile valuePalette,
            PhaseCVisualMaterials valueMaterials,
            JarVisualProfile valueVisualProfile)
        {
            cup = value ?? throw new global::System.ArgumentNullException(nameof(value));
            palette = valuePalette;
            materials = valueMaterials;
            visualProfile = valueVisualProfile;
            capBlock ??= new MaterialPropertyBlock();
            ResolveAuthoredChildren();
            ConfigureTransform();
            ConfigureMaterials();
            ApplyFeedback(true);
        }

        public void Bind(CupDomain value, ColorProfile valuePalette, PhaseCVisualMaterials valueMaterials)
        {
            Bind(value, valuePalette, valueMaterials, null);
        }

        private void ResolveAuthoredChildren()
        {
            if (view == null) view = transform.Find("View");
            if (shadowRenderer == null) shadowRenderer = transform.Find("View/Shadow")?.GetComponent<Renderer>();
            if (bodyRenderer == null) bodyRenderer = transform.Find("View/Body")?.GetComponent<Renderer>();
            if (capTopRenderer == null) capTopRenderer = transform.Find("View/CapTop")?.GetComponent<Renderer>();
            if (capBottomRenderer == null) capBottomRenderer = transform.Find("View/CapBottom")?.GetComponent<Renderer>();
            if (view == null || shadowRenderer == null || bodyRenderer == null || capTopRenderer == null || capBottomRenderer == null)
                throw new MissingComponentException("Cup prefab requires View/{Shadow,Body,CapTop,CapBottom} renderers.");
        }

        private void ConfigureTransform()
        {
            transform.localPosition = new Vector3(cup.Position.x, cup.Position.y, 0f);
            float widthScale = visualProfile != null
                ? visualProfile.CupWidthScale(cup.Size.x)
                : cup.Size.x / 1.82f;
            float topCapHeight = (visualProfile != null ? visualProfile.cupCapTopHeightPixels : 44f) * 0.01f * widthScale;
            float bottomCapHeight = (visualProfile != null ? visualProfile.cupCapBottomHeightPixels : 49f) * 0.01f * widthScale;
            float bodyBottomOffset = visualProfile != null
                ? visualProfile.CupBodyBottomOffset(cup.Size.x)
                : bottomCapHeight;
            float bodyHeight = Mathf.Max(0.001f, cup.Size.y - topCapHeight - bodyBottomOffset);
            float bodyNativeHeight = (visualProfile != null ? visualProfile.cupBodyHeightPixels : 130f) * 0.01f;

            bodyRenderer.transform.localPosition = new Vector3(0f, bodyBottomOffset + bodyHeight * 0.5f, 0f);
            bodyRenderer.transform.localScale = new Vector3(widthScale, bodyHeight / bodyNativeHeight, 1f);
            capBottomRenderer.transform.localPosition = new Vector3(0f, bottomCapHeight * 0.5f, 0f);
            capBottomRenderer.transform.localScale = Vector3.one * widthScale;
            capTopRenderer.transform.localPosition = new Vector3(0f, cup.Size.y - topCapHeight * 0.5f, 0f);
            capTopRenderer.transform.localScale = Vector3.one * widthScale;

            shadowRenderer.transform.localPosition = new Vector3(
                (visualProfile != null ? visualProfile.shadowOffsetPixels.x : -6f) * 0.01f * widthScale,
                cup.Size.y * 0.5f + (visualProfile != null ? visualProfile.shadowOffsetPixels.y : -24f) * 0.01f * widthScale,
                0f);
            shadowRenderer.transform.localScale = new Vector3(widthScale, cup.Size.y / 2.17f, 1f);
        }

        private void ConfigureMaterials()
        {
            if (materials != null)
            {
                bodyRenderer.sharedMaterial = materials.ResolveCupBody();
                shadowRenderer.sharedMaterial = materials.cupShadowMaterial;
            }

            ColorProfileEntry entry;
            Material capMaterial = palette != null && palette.TryGetEntry(cup.AcceptedMaterialId, out entry)
                ? entry.cupCapMaterial
                : null;
            capTopRenderer.sharedMaterial = capMaterial;
            capBottomRenderer.sharedMaterial = capMaterial;
        }

        private void LateUpdate()
        {
            if (cup == null) return;
            float feedback = cup.ForeignDetected ? Mathf.PingPong(Time.time * 8f, 1f) : 0f;
            if (cup.ForeignDetected != lastForeign || !Mathf.Approximately(feedback, lastFeedback))
                ApplyFeedback(false, feedback);
        }

        private void ApplyFeedback(bool force, float feedback = 0f)
        {
            if (!force && !cup.ForeignDetected && Mathf.Approximately(feedback, lastFeedback)) return;
            capBlock.Clear();
            capBlock.SetFloat(FeedbackId, feedback);
            capTopRenderer.SetPropertyBlock(capBlock);
            capBottomRenderer.SetPropertyBlock(capBlock);
            lastForeign = cup.ForeignDetected;
            lastFeedback = feedback;
        }
    }
}
