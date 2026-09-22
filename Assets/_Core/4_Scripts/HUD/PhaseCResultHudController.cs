using SE001.System.Management;
using UnityEngine;

namespace SE001.HUD
{
    /// <summary>Bridges the gameplay state event to the shared prefab-backed result panels.</summary>
    [DisallowMultipleComponent]
    public sealed class PhaseCResultHudController : MonoBehaviour, ILevelLifecycleParticipant
    {
        private GameplayManager gameplay;
        private HUDSystem hud;

        public void Bind(LevelContext context)
        {
            CleanupForLevelUnload();
            gameplay = context.LevelRoot.GetComponent<GameplayManager>();
            hud = ResolveHudSystem();
            HideResults();
            gameplay.GameStateChanged += OnGameStateChanged;
        }

        public static HUDSystem ResolveHudSystem()
        {
            HUDSystem[] systems = Resources.FindObjectsOfTypeAll<HUDSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                HUDSystem candidate = systems[i];
                if (!candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded ||
                    !candidate.gameObject.activeInHierarchy)
                    continue;

                if (candidate.HasPanelSource<WinPanel>() && candidate.HasPanelSource<LosePanel>())
                    return candidate;
            }

            return null;
        }

        public void CleanupForLevelUnload()
        {
            if (gameplay != null)
                gameplay.GameStateChanged -= OnGameStateChanged;
            HideResults();
            gameplay = null;
            hud = null;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (hud == null)
                return;

            if (state == GameState.Won)
            {
                hud.Hide<LosePanel>();
                hud.Show<WinPanel>(ShowType.DissmissCurrent);
                return;
            }

            if (state == GameState.Lost)
            {
                hud.Hide<WinPanel>();
                LosePanel panel = hud.Show<LosePanel>(ShowType.DissmissCurrent);
                if (panel != null && gameplay.Context != null)
                    panel.SetResultData(gameplay.Context.LevelId, gameplay.LastLoseReason);
                return;
            }

            HideResults();
        }

        private void HideResults()
        {
            if (hud == null)
                return;
            hud.Hide<WinPanel>();
            hud.Hide<LosePanel>();
        }
    }
}
