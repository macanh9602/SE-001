using UnityEngine;
using SE001.System.Management;

namespace SE001.HUD
{
    /// <summary>Bridges per-level ink accounting to the persistent HUD view.</summary>
    [DisallowMultipleComponent]
    public sealed class PhaseCInkProgressHudController : MonoBehaviour, ILevelLifecycleParticipant
    {
        private GameplayManager gameplay;
        private PhaseCInkProgressHudView view;

        public void Bind(LevelContext context)
        {
            CleanupForLevelUnload();

            if (context == null || context.LevelRoot == null)
                return;

            gameplay = context.LevelRoot.GetComponent<GameplayManager>();
            HUDSystem hud = PhaseCResultHudController.ResolveHudSystem();
            view = hud != null ? hud.GetComponentInChildren<PhaseCInkProgressHudView>(true) : null;

            if (view != null)
                view.Bind();

            if (gameplay == null)
                return;

            gameplay.InkChanged += OnInkChanged;
            view?.SetInk(gameplay.InkRemaining, gameplay.InkBudget);
        }

        public void CleanupForLevelUnload()
        {
            if (gameplay != null)
                gameplay.InkChanged -= OnInkChanged;

            view?.ResetForLevelUnload();
            gameplay = null;
            view = null;
        }

        private void OnInkChanged(float remaining, float budget)
        {
            view?.SetInk(remaining, budget);
        }
    }
}
