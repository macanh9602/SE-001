using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCDrawStrokeVisual : MonoBehaviour
    {
        public MeshFilter MeshFilter { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }

        private void Awake()
        {
            MeshFilter = GetComponent<MeshFilter>();
            MeshRenderer = GetComponent<MeshRenderer>();
        }
    }
}
