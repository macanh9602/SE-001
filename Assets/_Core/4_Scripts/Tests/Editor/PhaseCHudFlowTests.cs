using NUnit.Framework;
using SE001.HUD;
using SE001.System.Management;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SE001.Tests
{
    public sealed class PhaseCHudFlowTests
    {
        private GameObject owner;
        private GameObject hudObject;
        private LevelManager manager;

        [SetUp]
        public void SetUp()
        {
            DestroySceneHudSystems();
            owner = new GameObject("PhaseCHudFlowTests");
#if UNITY_EDITOR
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/UI/HUDSystem.prefab");
            hudObject = Object.Instantiate(hudPrefab);
#endif
            manager = owner.AddComponent<LevelManager>();
            HideResultPanels();
        }

        [TearDown]
        public void TearDown()
        {
            if (manager != null && manager.CurrentContext != null)
                manager.UnloadCurrentLevel();
            HideResultPanels();
            if (hudObject != null)
                Object.DestroyImmediate(hudObject);
            if (owner != null)
                Object.DestroyImmediate(owner);
        }

        [Test]
        public void WinState_ShowsWinPanelAndBlocksGameplayInput()
        {
            GameplayManager game = Load("phase_c_level_01");
            game.ToggleSource("source_coral");
            AdvanceUntilFinished(game);

            HUDSystem hud = PhaseCResultHudController.ResolveHudSystem();
            Assert.That(game.State, Is.EqualTo(GameState.Won));
            Assert.That(hud.GetActivePanel<WinPanel>(), Is.Not.Null);
            Assert.That(hud.GetActivePanel<LosePanel>(), Is.Null);
            Assert.That(hud.BlockByPanel(), Is.True);
        }

        [Test]
        public void LoseState_ShowsLosePanelAndBlocksGameplayInput()
        {
            GameplayManager game = Load("phase_c_level_02");
            game.ToggleSource("source_coral");
            game.ToggleSource("source_blue");
            AdvanceUntilFinished(game);

            HUDSystem hud = PhaseCResultHudController.ResolveHudSystem();
            Assert.That(game.State, Is.EqualTo(GameState.Lost));
            Assert.That(hud.GetActivePanel<LosePanel>(), Is.Not.Null);
            Assert.That(hud.GetActivePanel<WinPanel>(), Is.Null);
            Assert.That(hud.BlockByPanel(), Is.True);
        }

        [Test]
        public void Reload_HidesResultPanelBeforeNewLevelIsReady()
        {
            GameplayManager game = Load("phase_c_level_01");
            game.ToggleSource("source_coral");
            AdvanceUntilFinished(game);
            Assert.That(PhaseCResultHudController.ResolveHudSystem().GetActivePanel<WinPanel>(), Is.Not.Null);

            manager.ReloadCurrentLevel();

            HUDSystem hud = PhaseCResultHudController.ResolveHudSystem();
            Assert.That(hud.GetActivePanel<WinPanel>(), Is.Null);
            Assert.That(hud.GetActivePanel<LosePanel>(), Is.Null);
            Assert.That(manager.IsReady, Is.True);
        }

        [Test]
        public void Input_DecorativeUiDoesNotBlock()
        {
            HUDSystem hud = PhaseCResultHudController.ResolveHudSystem();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.BlockByPanel(), Is.False);
        }

        private GameplayManager Load(string levelId)
        {
            manager.BeginLevel(levelId);
            Assert.That(manager.IsReady, Is.True);
            return manager.CurrentContext.LevelRoot.GetComponent<GameplayManager>();
        }

        private static void AdvanceUntilFinished(GameplayManager game)
        {
            int guard = 0;
            while (game.State == GameState.Playing && guard < 10000)
                guard += game.AdvanceSteps(100);
            Assert.That(game.State, Is.Not.EqualTo(GameState.Playing), "Gameplay did not reach a result state.");
        }

        private static void HideResultPanels()
        {
            HUDSystem hud = PhaseCResultHudController.ResolveHudSystem();
            if (hud == null)
                return;
            hud.Hide<WinPanel>();
            hud.Hide<LosePanel>();
        }

        private static void DestroySceneHudSystems()
        {
            HUDSystem[] systems = Resources.FindObjectsOfTypeAll<HUDSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                HUDSystem system = systems[i];
                if (system != null && system.gameObject.scene.IsValid() && system.gameObject.scene.isLoaded)
                    Object.DestroyImmediate(system.gameObject);
            }
        }
    }
}
