using UnityEngine;
using Watermelon;

namespace SE001.HUD
{
    /// <summary>Prefab-backed presentation for the current level ink budget.</summary>
    [DisallowMultipleComponent]
    public sealed class PhaseCInkProgressHudView : MonoBehaviour
    {
        [SerializeField] private SlicedFilledImage fillImage;

        public void Bind()
        {
            if (fillImage == null)
                fillImage = GetComponentInChildren<SlicedFilledImage>(true);

            if (fillImage != null)
                fillImage.raycastTarget = false;
        }

        public void SetInk(float remaining, float budget)
        {
            if (fillImage == null)
                return;

            fillImage.fillAmount = budget > 0f ? Mathf.Clamp01(remaining / budget) : 0f;
        }

        public void ResetForLevelUnload()
        {
            if (fillImage != null)
                fillImage.fillAmount = 1f;
        }
    }
}
