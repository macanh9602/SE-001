using System;
using System.Collections.Generic;
using SE001.Data;
using SE001.Geometry;
using SE001.Simulation.Sand;
using SE001.Creation;
using SE001.Elements.Sand;
using SE001.Gameplay;
using SE001.Presentation;
using SE001.System.Management;
using UnityEngine;

namespace SE001.System.Creation
{
    /// <summary>Owns per-level composition, runtime roots, and reverse-order teardown.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelSpawner : MonoBehaviour
    {
        [SerializeField] private SandSimulationProfile sandSimulationProfile;
        [SerializeField] private PrefabProfile prefabProfile;
        [SerializeField] private LayoutVisualProfile layoutVisualProfile;
        [SerializeField] private GameplayRuntimeProfile gameplayProfile;
        [SerializeField, Min(0.001f)] private float simulationCellSize = 0.1f;
        [SerializeField, Min(1)] private int simulationMaxCells = 262144;
        private LevelContext activeContext;

        public LevelContext ActiveContext => activeContext;

        private void Awake()
        {
            if (sandSimulationProfile == null) sandSimulationProfile = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            if (prefabProfile == null) prefabProfile = Resources.Load<PrefabProfile>("Profiles/PhaseBPrefabProfile");
            if (layoutVisualProfile == null) layoutVisualProfile = Resources.Load<LayoutVisualProfile>("Profiles/PhaseBLayoutVisualProfile");
            if (gameplayProfile == null) gameplayProfile = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
        }

        public LevelContext SpawnLevel(string levelId, int generation)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A stable level id is required.", nameof(levelId));
            }

            if (activeContext != null)
            {
                throw new InvalidOperationException("Unload the active level before spawning another level.");
            }

            if (sandSimulationProfile == null) sandSimulationProfile = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            if (prefabProfile == null) prefabProfile = Resources.Load<PrefabProfile>("Profiles/PhaseBPrefabProfile");
            if (layoutVisualProfile == null) layoutVisualProfile = Resources.Load<LayoutVisualProfile>("Profiles/PhaseBLayoutVisualProfile");
            if (gameplayProfile == null) gameplayProfile = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");

            LevelRuntimeState runtimeState = null;
            Transform levelRoot = null;

            try
            {
                SE001LevelJson levelData = null;
                bool hasAuthoredData = Resources.Load<TextAsset>("Levels/" + levelId) != null;
                if (!hasAuthoredData)
                    Debug.LogWarning("[LevelSpawner] No level data at Resources/Levels/" + levelId + ".json; spawning empty roots only.", this);
                if (hasAuthoredData) levelData = LevelDataLoader.Load(levelId);
                runtimeState = new LevelRuntimeState();
                levelRoot = CreateRoot("LevelRoot", transform);
                Transform boardRoot = CreateRoot("BoardRoot", levelRoot);
                Transform obstacleRoot = CreateRoot("ObstacleRoot", levelRoot);
                Transform sourceRoot = CreateRoot("SourceRoot", levelRoot);
                Transform cupRoot = CreateRoot("CupRoot", levelRoot);
                Transform dynamicDrawRoot = CreateRoot("DynamicDrawRoot", levelRoot);
                Transform sandVisualRoot = CreateRoot("SandVisualRoot", levelRoot);
                Transform vfxRoot = CreateRoot("VfxRoot", levelRoot);

                activeContext = new LevelContext(
                    levelId,
                    generation,
                    runtimeState,
                    levelRoot,
                    boardRoot,
                    obstacleRoot,
                    sourceRoot,
                    cupRoot,
                    dynamicDrawRoot,
                    sandVisualRoot,
                    vfxRoot);

                if (levelData != null)
                {
                    activeContext.BoardSize = levelData.board.size;
                    activeContext.DrawInkBudget = levelData.drawInkBudget;
                    SandSimulationProfile profile = gameplayProfile != null && gameplayProfile.sandProfile != null
                        ? gameplayProfile.sandProfile
                        : sandSimulationProfile;
                    if (profile == null) throw new InvalidOperationException("Phase C requires a SandSimulationProfile asset.");
                    LayoutDefinition layout = LevelDataLoader.LoadLayout(levelData.layoutId);
                    LayoutMaskSet masks;
                    string maskError = string.Empty;
                    if (!layout.TryBuildMaskSet(profile.cellSize, profile.maxCells, out masks, out maskError))
                        throw new InvalidOperationException("Layout " + levelData.layoutId + " cannot load: " + maskError);
                    activeContext.AttachSimulation(new SandSimulation(profile, masks, SandSimulation.SeedFrom(levelId)));
                    SpawnLayoutVisuals(layout);
                    SpawnSandField();
                    BindPhaseCGameplay(levelData, profile);
                }

                return activeContext;
            }
            catch
            {
                runtimeState?.Dispose();
                DestroyOwnedRoot(levelRoot);
                throw;
            }
        }

        private void BindPhaseCGameplay(SE001LevelJson levelData, SandSimulationProfile profile)
        {
            GameplayManager gameplay = activeContext.LevelRoot.gameObject.AddComponent<GameplayManager>();
            GameplayInputController input = activeContext.LevelRoot.gameObject.AddComponent<GameplayInputController>();
            activeContext.RegisterParticipant(gameplay);
            activeContext.RegisterParticipant(input);
            if (gameplayProfile == null || gameplayProfile.sourceProfile == null || gameplayProfile.cupProfile == null)
                throw new InvalidOperationException("Phase C requires PhaseCGameplayRuntimeProfile with SourceProfile and CupProfile references.");
            SourceProfile sourceProfile = gameplayProfile.sourceProfile;
            CupProfile cupProfile = gameplayProfile.cupProfile;
            gameplay.ConfigureRuntime(gameplayProfile);
            input.Configure(gameplayProfile.drawPathProfile, sourceProfile.hitPadding);
            var sources = new List<SourceDomain>();
            var cups = new List<CupDomain>();
            int grainsPerUnit = profile != null ? profile.grainsPerUnit : 12;
            for (int i = 0; i < levelData.sources.Count; i++)
                sources.Add(new SourceDomain(levelData.sources[i], sourceProfile, grainsPerUnit, gameplayProfile.jarVisualProfile));
            for (int i = 0; i < levelData.cups.Count; i++) cups.Add(new CupDomain(levelData.cups[i], cupProfile, grainsPerUnit, profile.cellSize));
            gameplay.Configure(sources, cups, profile.cellSize, cupProfile.wallThickness);
            SourceFactory sourceFactory = new SourceFactory();
            CupFactory cupFactory = new CupFactory();
            if (gameplayProfile.prefabProfile == null) throw new InvalidOperationException("Phase C requires PrefabProfile for production visuals.");
            for (int i = 0; i < sources.Count; i++)
                sourceFactory.Create(new SourceCreateParameters(sources[i], activeContext.SourceRoot, gameplayProfile.prefabProfile, gameplayProfile));
            for (int i = 0; i < cups.Count; i++)
                cupFactory.Create(new CupCreateParameters(cups[i], activeContext.CupRoot, gameplayProfile.prefabProfile, gameplayProfile));
            activeContext.RegisterParticipant(activeContext.LevelRoot.gameObject.AddComponent<PhaseCDrawVisualController>());
            // Level sanity (dev): per material, source grains must cover the cups' fill-line volume.
            for (int c = 0; c < cups.Count; c++)
            {
                int need = 0, have = 0;
                for (int k = 0; k < cups.Count; k++) if (cups[k].AcceptedMaterialId == cups[c].AcceptedMaterialId) need += cups[k].Required;
                for (int k = 0; k < sources.Count; k++) if (sources[k].MaterialId == cups[c].AcceptedMaterialId) have += sources[k].Initial;
                if (have < need)
                {
                    string warning = $"[LevelSpawner] {levelData.levelId}: material {cups[c].AcceptedMaterialId} has {have} grains " +
                        $"in sources but cups need {need} to reach the fill line; level is unwinnable.";
                    Debug.LogWarning(warning, this);
                }
            }
            activeContext.RegisterParticipant(
                activeContext.LevelRoot.gameObject.AddComponent<SE001.HUD.PhaseCHudView>());
            activeContext.RegisterParticipant(
                activeContext.LevelRoot.gameObject.AddComponent<SE001.HUD.PhaseCResultHudController>());
            // DebugView is intentionally not part of production presentation ownership.
        }

        private void SpawnLayoutVisuals(LayoutDefinition layout)
        {
            if (layout == null || layout.layoutPrefab == null)
                throw new InvalidOperationException("Layout definition requires a baked layout prefab.");
            Instantiate(layout.layoutPrefab, activeContext.BoardRoot, false);
        }

        private void SpawnSandField()
        {
            if (prefabProfile == null || prefabProfile.sandFieldPrefab == null || activeContext.SandSimulation == null) return;
            GameObject instance = Instantiate(prefabProfile.sandFieldPrefab, activeContext.SandVisualRoot, false);
            SandFieldVisual visual = instance.GetComponent<SandFieldVisual>();
            if (visual == null)
            {
                DestroyOwnedRoot(instance.transform);
                throw new MissingComponentException("SandField prefab requires SandFieldVisual.");
            }
            ColorProfile colorProfile = gameplayProfile != null ? gameplayProfile.colorProfile : null;
            visual.SetPalette(colorProfile != null ? colorProfile.BuildSandLookup() : null);
            visual.Bind(activeContext.SandSimulation);
        }

        public void UnloadLevel(LevelContext context)
        {
            if (context == null)
            {
                return;
            }

            Transform levelRoot = context.LevelRoot;
            context.Dispose();
            PhaseCCupVisual[] cupVisuals =
                context.CupRoot.GetComponentsInChildren<PhaseCCupVisual>();
            for (int i = 0; i < cupVisuals.Length; i++)
                cupVisuals[i].ReleaseForUnload();
            DestroyOwnedRoot(levelRoot);

            if (ReferenceEquals(activeContext, context))
            {
                activeContext = null;
            }
        }

        private void OnDestroy()
        {
            if (activeContext != null)
            {
                UnloadLevel(activeContext);
            }
        }

        private static Transform CreateRoot(string rootName, Transform parent)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(parent, false);
            return root;
        }

        private static void DestroyOwnedRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                // Destroy is deferred in players. Remove the old hierarchy from the active
                // composition immediately so same-frame reloads never expose duplicate roots.
                root.gameObject.SetActive(false);
                root.SetParent(null, false);
                Destroy(root.gameObject);
            }
            else
            {
                DestroyImmediate(root.gameObject);
            }
        }
    }
}
