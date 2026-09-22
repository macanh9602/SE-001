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
            if (hud == null)
                Debug.LogError("[PhaseCResultHudController] No usable HUDSystem (needs an active scene instance with WinPanel + LosePanel sources). " + DescribeResolve(), this);
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



        private static string DescribeResolve()
        {
            HUDSystem[] systems = Resources.FindObjectsOfTypeAll<HUDSystem>();
            var sb = new global::System.Text.StringBuilder();
            sb.Append("found=").Append(systems.Length);
            for (int i = 0; i < systems.Length; i++)
            {
                HUDSystem c = systems[i];
                sb.Append(" | [").Append(i).Append("] ").Append(c.name)
                  .Append(" sceneValid=").Append(c.gameObject.scene.IsValid())
                  .Append(" sceneLoaded=").Append(c.gameObject.scene.isLoaded)
                  .Append(" activeInHierarchy=").Append(c.gameObject.activeInHierarchy)
                  .Append(" hasWin=").Append(c.HasPanelSource<WinPanel>())
                  .Append(" hasLose=").Append(c.HasPanelSource<LosePanel>())
                  .Append(" rootUI=").Append(c.rootUI == null ? "NULL" : c.rootUI.name);
            }

            return sb.ToString();
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
