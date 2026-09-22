using System;
using SE001.Data;
using SE001.Geometry;
using SE001.System.Management;
using SE001.Simulation.Sand;
using SE001.Creation;
using SE001.Elements.Sand;
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
        [SerializeField, Min(0.001f)] private float simulationCellSize = 0.1f;
        [SerializeField, Min(1)] private int simulationMaxCells = 262144;
        private LevelContext activeContext;

        public LevelContext ActiveContext => activeContext;

        private void Awake()
        {
            if (sandSimulationProfile == null) sandSimulationProfile = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            if (prefabProfile == null) prefabProfile = Resources.Load<PrefabProfile>("Profiles/PhaseBPrefabProfile");
            if (layoutVisualProfile == null) layoutVisualProfile = Resources.Load<LayoutVisualProfile>("Profiles/PhaseBLayoutVisualProfile");
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

            LevelRuntimeState runtimeState = null;
            Transform levelRoot = null;

            try
            {
                SE001LevelJson levelData = null;
                bool hasAuthoredData = Resources.Load<TextAsset>("Levels/" + levelId) != null;
                if (!hasAuthoredData) Debug.LogWarning("[LevelSpawner] No level data at Resources/Levels/" + levelId + ".json; spawning empty roots only.", this);
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
                    SandSimulationProfile profile = sandSimulationProfile;
                    SandSimulationProfile ownedProfile = null;
                    if (profile == null)
                    {
                        profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
                        ownedProfile = profile;
                        profile.cellSize = simulationCellSize;
                        profile.maxCells = simulationMaxCells;
                    }
                    LayoutMaskSet masks = LayoutRasterizer.Rasterize(levelData, profile.cellSize, profile.maxCells);
                    activeContext.AttachSimulation(new SandSimulation(profile, masks), ownedProfile);
                    SpawnLayoutVisuals(levelData);
                    SpawnSandField();
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

        private void SpawnLayoutVisuals(SE001LevelJson levelData)
        {
            if (prefabProfile == null) prefabProfile = Resources.Load<PrefabProfile>("Profiles/PhaseBPrefabProfile");
            if (layoutVisualProfile == null) layoutVisualProfile = Resources.Load<LayoutVisualProfile>("Profiles/PhaseBLayoutVisualProfile");
            if (prefabProfile == null || layoutVisualProfile == null) return;
            LayoutVisualFactory factory = new LayoutVisualFactory();
            for (int i = 0; i < levelData.board.wallContours.Count; i++) factory.Create(prefabProfile.boardWallPrefab, activeContext.BoardRoot, levelData.board.wallContours[i].points, layoutVisualProfile, true);
            for (int i = 0; i < levelData.staticObstacles.Count; i++) for (int c = 0; c < levelData.staticObstacles[i].contours.Count; c++) factory.Create(prefabProfile.staticObstaclePrefab, activeContext.ObstacleRoot, levelData.staticObstacles[i].contours[c].points, layoutVisualProfile, false);
        }

        private void SpawnSandField()
        {
            if (prefabProfile == null || prefabProfile.sandFieldPrefab == null || activeContext.SandSimulation == null) return;
            GameObject instance = Instantiate(prefabProfile.sandFieldPrefab, activeContext.SandVisualRoot, false);
            SandFieldVisual visual = instance.GetComponent<SandFieldVisual>();
            if (visual == null) { DestroyOwnedRoot(instance.transform); throw new MissingComponentException("SandField prefab requires SandFieldVisual."); }
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
