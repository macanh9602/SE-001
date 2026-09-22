using SE001.Gameplay;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Diagnostics
{
    /// <summary>Optional diagnostic overlay only. Production Source/Cup/Draw visuals live in factories and prefabs.</summary>
    [DisallowMultipleComponent]
    public sealed class PhaseCDebugView : MonoBehaviour, ILevelLifecycleParticipant
    {
        [SerializeField] private bool showOverlay;
        private GameplayManager manager;
        private LevelContext context;

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            manager = GetComponent<GameplayManager>();
        }

        private void OnGUI()
        {
            if (!showOverlay || manager == null || context == null) return;
            float height = 160f + 22f * (manager.Sources.Count + manager.Cups.Count);
            GUILayout.BeginArea(new Rect(8f, 120f, 280f, height), GUI.skin.box);
            GUILayout.Label($"[DEV] {context.LevelId}  step {manager.StepCount}");
            GUILayout.Label($"State: {manager.State}");
            GUILayout.Label($"Ink: {manager.InkRemaining:0.00} / {manager.InkBudget:0.00}");
            for (int i = 0; i < manager.Sources.Count; i++)
            {
                SourceDomain source = manager.Sources[i];
                GUILayout.Label($"{source.StableId}: {source.State} {source.Remaining}/{source.Initial}");
            }
            for (int i = 0; i < manager.Cups.Count; i++)
            {
                CupDomain cup = manager.Cups[i];
                GUILayout.Label($"{cup.StableId}: {cup.CollectedLogical}/{cup.RequiredLogical}");
            }
            GUILayout.EndArea();
        }

        public void CleanupForLevelUnload()
        {
            manager = null;
            context = null;
        }
    }
}
