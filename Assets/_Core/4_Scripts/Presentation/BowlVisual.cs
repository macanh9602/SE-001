using System;
using SE001.Data;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BowlVisual : MonoBehaviour
    {
        private static readonly int FeedbackId = Shader.PropertyToID("_Feedback");

        [SerializeField] private Transform view;
        [SerializeField] private Renderer shadowRenderer;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private Renderer specRenderer;

        private CupDomain bowl;
        private ColorProfile palette;
        private PhaseCVisualMaterials materials;
        private BowlVisualProfile profile;
        private MaterialPropertyBlock feedbackBlock;
        private bool lastForeign;
        private float lastFeedback = -1f;

        public CupDomain Domain => bowl;
        public Transform View => view;
        public Renderer ShadowRenderer => shadowRenderer;
        public Renderer MainRenderer => mainRenderer;
        public Renderer SpecRenderer => specRenderer;

        public void ReleaseForUnload()
        {
            feedbackBlock?.Clear();
        }

        public void Bind(
            CupDomain value,
            ColorProfile valuePalette,
            PhaseCVisualMaterials valueMaterials,
            BowlVisualProfile valueProfile)
        {
            bowl = value ?? throw new ArgumentNullException(nameof(value));
            palette = valuePalette;
            materials = valueMaterials;
            profile = valueProfile ?? throw new ArgumentNullException(nameof(valueProfile));
            feedbackBlock ??= new MaterialPropertyBlock();
            ResolveAuthoredChildren();
            ConfigureTransform();
            ConfigureMaterials();
            ApplyFeedback(true);
        }

        private void ResolveAuthoredChildren()
        {
            if (view == null) view = transform.Find("View");
            if (shadowRenderer == null) shadowRenderer = transform.Find("View/Shadow")?.GetComponent<Renderer>();
            if (mainRenderer == null) mainRenderer = transform.Find("View/Main")?.GetComponent<Renderer>();
            if (specRenderer == null) specRenderer = transform.Find("View/Spec")?.GetComponent<Renderer>();
            if (view == null || shadowRenderer == null || mainRenderer == null || specRenderer == null)
                throw new MissingComponentException("Bowl prefab requires View/{Shadow,Main,Spec} renderers.");
        }

        private void ConfigureTransform()
        {
            transform.localPosition = new Vector3(bowl.Position.x, bowl.Position.y, 0f);
            // Scale from the width the quad meshes were baked at, not the (GD-tunable) profile width: otherwise changing
            // defaultWorldWidth resized physics and editor but left the in-game Bowl at the old size (Movie_011).
            float widthScale = bowl.Size.x / BakedMeshWidth(mainRenderer, profile.defaultWorldWidth);
            float height = profile.WorldHeightForWidth(bowl.Size.x);
            mainRenderer.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            mainRenderer.transform.localScale = Vector3.one * widthScale;
            shadowRenderer.transform.localPosition = new Vector3(
                profile.shadowOffsetPixels.x * profile.WorldPixelsPerPixel(bowl.Size.x),
                height * 0.5f + profile.shadowOffsetPixels.y * profile.WorldPixelsPerPixel(bowl.Size.x),
                0f);
            shadowRenderer.transform.localScale = Vector3.one * widthScale;
            // BowlSpec mesh is already baked at spec size; it scales with the same factor as Main.
            float specWidthScale = widthScale;
            float specHeightScale = widthScale;
            specRenderer.transform.localPosition = new Vector3(
                (profile.specOffsetPixels.x + profile.specWidthPixels * 0.5f - profile.mainWidthPixels * 0.5f) *
                profile.WorldPixelsPerPixel(bowl.Size.x),
                height * 0.5f - (profile.specOffsetPixels.y + profile.specHeightPixels * 0.5f -
                    profile.mainHeightPixels * 0.5f) * profile.WorldPixelsPerPixel(bowl.Size.x),
                0f);
            specRenderer.transform.localScale = new Vector3(specWidthScale, specHeightScale, 1f);
        }

        private static float BakedMeshWidth(Renderer renderer, float fallback)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            float width = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.size.x : 0f;
            return width > 0.0001f ? width : Mathf.Max(0.001f, fallback);
        }

        private void ConfigureMaterials()
        {
            if (materials != null)
            {
                shadowRenderer.sharedMaterial = materials.bowlShadowMaterial;
                specRenderer.sharedMaterial = materials.bowlSpecMaterial;
            }

            ColorProfileEntry entry;
            mainRenderer.sharedMaterial = palette != null && palette.TryGetEntry(bowl.AcceptedMaterialId, out entry)
                ? entry.bowlMainMaterial : null;
        }

        private void LateUpdate()
        {
            if (bowl == null) return;
            float feedback = bowl.ForeignDetected ? Mathf.PingPong(Time.time * 8f, 1f) : 0f;
            if (bowl.ForeignDetected != lastForeign || !Mathf.Approximately(feedback, lastFeedback))
                ApplyFeedback(false, feedback);
        }

        private void ApplyFeedback(bool force, float feedback = 0f)
        {
            if (!force && !bowl.ForeignDetected && Mathf.Approximately(feedback, lastFeedback)) return;
            feedbackBlock.Clear();
            feedbackBlock.SetFloat(FeedbackId, feedback);
            mainRenderer.SetPropertyBlock(feedbackBlock);
            lastForeign = bowl.ForeignDetected;
            lastFeedback = feedback;
        }
    }
}
