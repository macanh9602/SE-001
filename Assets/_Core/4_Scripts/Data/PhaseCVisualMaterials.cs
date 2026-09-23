using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "PhaseCVisualMaterials", menuName = "SE001/Profiles/Phase C Visual Materials")]
    public sealed class PhaseCVisualMaterials : ScriptableObject
    {
        // Legacy fields remain serialized so older scenes and tools can migrate without a null reference.
        public Material sourceMaterial;
        public Material cupMaterial;
        public Material cupBackMaterial;
        public Material drawPathMaterial;

        public Material sourceBodyMaterial;
        public Material cupBodyMaterial;
        public Material sourceFillMaterial;
        public Material sourceShadowMaterial;
        public Material cupShadowMaterial;
        public Material bowlSpecMaterial;
        public Material bowlShadowMaterial;

        public Material ResolveSourceBody() => sourceBodyMaterial != null ? sourceBodyMaterial : sourceMaterial;
        public Material ResolveCupBody() => cupBodyMaterial != null ? cupBodyMaterial : cupMaterial;
    }
}
