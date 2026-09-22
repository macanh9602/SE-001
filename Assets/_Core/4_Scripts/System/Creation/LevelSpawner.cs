using System;
using SE001.System.Management;
using UnityEngine;

namespace SE001.System.Creation
{
    /// <summary>Owns per-level composition, runtime roots, and reverse-order teardown.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelSpawner : MonoBehaviour
    {
        private LevelContext activeContext;

        public LevelContext ActiveContext => activeContext;

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

                return activeContext;
            }
            catch
            {
                runtimeState?.Dispose();
                DestroyOwnedRoot(levelRoot);
                throw;
            }
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
