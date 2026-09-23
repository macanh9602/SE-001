using SE001.Data;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCSourceVisual : MonoBehaviour
    {
        // Authored art is mouth-down. Pour pose = art orientation (mouth tip on the emit point);
        // idle = rotated 180 degrees around the body center (mouth up).
        private const float IdleAngle = 180f;
        private const float PourAngle = 0f;
        private const float SettledAngle = 0.05f;
        private const float SettledVelocity = 0.5f;

        private static readonly int FillLevelId = Shader.PropertyToID("_FillLevel");
        private static readonly int FillDirectionId = Shader.PropertyToID("_FillDirOS");
        private static readonly int FillThresholdId = Shader.PropertyToID("_FillThreshold");
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
        private SourceSandFillSolver sandSolver;
        private Color sandColor;
        private Vector3 shadowOffsetWorld;
        private Vector3 shadowArtOffsetLocal;
        private float lastFill = -1f;
        private SourceValveState lastState = (SourceValveState)(-1);
        private float lastAngle = float.NaN;

        // Sand surface lag: angle (deg) between the "down" the sand currently feels and true world down.
        private float sandTilt;
        private float sandTiltVelocity;

        public SourceDomain Domain => source;
        public Transform Pivot => pivot;
        public Renderer BodyRenderer => bodyRenderer;
        public Renderer MouthRenderer => mouthRenderer;
        public Renderer NozzleRenderer => mouthRenderer;
        public Renderer ShadowRenderer => shadowRenderer;
        public Renderer SandFillRenderer => sandFillRenderer;
        public float SandTilt => sandTilt;

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
            sandSolver = SourceSandFillSolver.Create(visualProfile);
            sandTilt = 0f;
            sandTiltVelocity = 0f;

            ResolveAuthoredChildren();
            ConfigureTransform();
            ConfigureMaterials();
            ApplyVisualState();
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
            // The silhouette quad covers body + mouth (composite), so its center sits below the body center
            // in authored (mouth-down) orientation.
            float compositeExtraPixels = visualProfile != null
                ? visualProfile.sourceCompositeHeightPixels - visualProfile.sourceBodyHeightPixels
                : 20f;
            shadowArtOffsetLocal = new Vector3(0f, -compositeExtraPixels * 0.5f * 0.01f * uniformScale, 0f);
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

            ColorProfileEntry entry = default;
            bool hasEntry = palette != null && palette.TryGetEntry(source.MaterialId, out entry);
            mouthRenderer.sharedMaterial = hasEntry ? entry.sourceMouthMaterial : null;
            sandColor = hasEntry ? (Color)entry.sandColor : Color.magenta;
        }

        private void LateUpdate()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>Advances rotation + sand motion by dt. LateUpdate drives it; tests call it with a fixed step.</summary>
        public void Tick(float dt)
        {
            if (source == null) return;
            float duration = juiceProfile != null ? Mathf.Max(0.01f, juiceProfile.valveOpenDuration) : 0.2f;
            float speed = 180f / duration;
            float targetAngle = source.IsPouring ? PourAngle : IdleAngle;
            float angle = Mathf.MoveTowardsAngle(pivot.localEulerAngles.z, targetAngle, speed * dt);
            float jarDelta = float.IsNaN(lastAngle) ? 0f : Mathf.DeltaAngle(lastAngle, angle);
            bool angleChanged = !Mathf.Approximately(jarDelta, 0f);
            if (angleChanged)
            {
                pivot.localRotation = Quaternion.Euler(0f, 0f, angle);
                ApplyShadowWorldOffset();
                lastAngle = angle;
            }

            bool sandMoving = StepSandTilt(jarDelta, dt);
            float fill = CurrentFill();
            if (angleChanged || sandMoving || !Mathf.Approximately(fill, lastFill) || source.State != lastState)
                ApplyVisualState();
        }

        // Tilt -> slide -> settle: the sand is carried by the jar, then springs back toward level,
        // never leaning past the repose angle. Returns true while the surface is still moving.
        private bool StepSandTilt(float jarDelta, float dt)
        {
            if (sandSolver == null || dt <= 0f)
            {
                sandTilt = 0f;
                sandTiltVelocity = 0f;
                return false;
            }

            float carry = visualProfile != null ? visualProfile.sandCarry : 0.85f;
            float omega = visualProfile != null ? visualProfile.sandSettleFrequency : 14f;
            float zeta = visualProfile != null ? visualProfile.sandSettleDamping : 0.55f;
            float repose = visualProfile != null ? visualProfile.sandReposeAngle : 32f;

            sandTilt += jarDelta * carry;
            float acceleration = -omega * omega * sandTilt - 2f * zeta * omega * sandTiltVelocity;
            sandTiltVelocity += acceleration * dt;
            sandTilt += sandTiltVelocity * dt;
            if (Mathf.Abs(sandTilt) > repose)
            {
                sandTilt = Mathf.Sign(sandTilt) * repose;
                if (sandTiltVelocity * sandTilt > 0f) sandTiltVelocity = 0f;
            }

            bool settled = Mathf.Abs(sandTilt) < SettledAngle && Mathf.Abs(sandTiltVelocity) < SettledVelocity;
            if (!settled) return true;
            bool wasMoving = sandTilt != 0f || sandTiltVelocity != 0f;
            sandTilt = 0f;
            sandTiltVelocity = 0f;
            return wasMoving;
        }

        private float CurrentFill()
        {
            if (source.State == SourceValveState.Empty) return 0f;
            return source.Initial > 0 ? source.Remaining / (float)source.Initial : 0f;
        }

        private void ApplyShadowWorldOffset()
        {
            if (shadowRenderer == null || pivot == null) return;
            shadowRenderer.transform.localPosition = shadowArtOffsetLocal + pivot.InverseTransformVector(shadowOffsetWorld);
        }

        private void ApplyVisualState()
        {
            float fill = CurrentFill();
            Vector3 feltDownWorld = Quaternion.Euler(0f, 0f, sandTilt) * Vector3.down;
            Vector3 downOS = sandFillRenderer.transform.InverseTransformDirection(feltDownWorld);
            Vector2 down2 = new Vector2(downOS.x, downOS.y);
            down2 = down2.sqrMagnitude > 0.000001f ? down2.normalized : Vector2.down;

            float threshold;
            if (sandSolver != null)
            {
                threshold = sandSolver.SolveThreshold(down2, fill);
            }
            else
            {
                // No baked spans: level along the jar axis only (legacy approximation).
                float height = JarVisualGeometry.AreaToHeight(fill, visualProfile != null ? visualProfile.sourceFillAreaLut : null);
                threshold = fill <= 0f ? SourceSandFillSolver.NoSand : 0.96f - height * 1.92f;
                down2 = Vector2.down;
            }

            fillBlock.Clear();
            fillBlock.SetFloat(FillLevelId, fill);
            fillBlock.SetVector(FillDirectionId, new Vector4(down2.x, down2.y, 0f, 0f));
            fillBlock.SetFloat(FillThresholdId, threshold);
            fillBlock.SetColor(SandColorId, sandColor);
            fillBlock.SetFloat(SparkleId, fill > 0f ? 1f : 0f);
            sandFillRenderer.SetPropertyBlock(fillBlock);
            lastFill = fill;
            lastState = source.State;
        }
    }
}
