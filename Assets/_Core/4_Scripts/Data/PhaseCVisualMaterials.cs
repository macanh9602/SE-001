using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "PhaseCVisualMaterials", menuName = "SE001/Profiles/Phase C Visual Materials")]
    public sealed class PhaseCVisualMaterials : ScriptableObject
    {
        public Material sourceMaterial;
        public Material cupMaterial;
        public Material cupBackMaterial;
        public Material drawPathMaterial;
    }
}
