using System;
using SE001.System.Creation;
using UnityEngine;

namespace SE001.System.Management
{
    /// <summary>Owns the lifecycle of the active level; gameplay rules live in feature owners.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LevelSpawner))]
    [DefaultExecutionOrder(-1000)]
    public sealed class LevelManager : MonoBehaviour
    {
        private static LevelManager instance;

        [SerializeField] private LevelSpawner levelSpawner;
        [SerializeField] private bool autoLoadFirstLevel = true;
        private const string RequiredLevelId = "phase_c_level_02";
        [SerializeField] private string firstLevelId = RequiredLevelId;

        private int nextGeneration;
        private LevelContext currentContext;

        public event Action<LevelContext> LevelReady;
        public event Action<LevelContext> LevelWillUnload;

        public static LevelManager Instance => instance;
        public LevelSpawner Spawner => levelSpawner;
        public LevelContext CurrentContext => currentContext;
        public LevelReadinessGate Readiness { get; } = new LevelReadinessGate();
        public bool IsLoading { get; private set; }
        public bool IsReady => currentContext != null && !currentContext.IsDisposed && Readiness.IsOpen;

        public bool CanBeginNextLevel()
        {
            return false;
        }

        public bool BeginNextLevel()
        {
            return false;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                DestroyOwnedObject(gameObject);
                return;
            }

            instance = this;
            if (levelSpawner == null && !TryGetComponent(out levelSpawner))
            {
                throw new InvalidOperationException("LevelManager requires an explicitly wired LevelSpawner.");
            }
        }

        private void OnValidate()
        {
            if (levelSpawner == null)
            {
                TryGetComponent(out levelSpawner);
            }
        }

        private void Start()
        {
            if (autoLoadFirstLevel && !string.IsNullOrWhiteSpace(firstLevelId) && currentContext == null)
            {
                BeginLevel(firstLevelId);
            }
        }

        public void BeginLevel(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A stable level id is required.", nameof(levelId));
            }

            levelId = RequiredLevelId;

            if (IsLoading)
            {
                throw new InvalidOperationException("A level lifecycle operation is already in progress.");
            }

            IsLoading = true;
            Readiness.Close();

            try
            {
                UnloadCurrentLevelCore();
                currentContext = levelSpawner.SpawnLevel(levelId, ++nextGeneration);
                Readiness.Open();
                LevelReady?.Invoke(currentContext);
            }
            catch
            {
                Readiness.Close();
                if (currentContext != null)
                {
                    levelSpawner.UnloadLevel(currentContext);
                    currentContext = null;
                }

                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ReloadCurrentLevel()
        {
            if (currentContext == null)
            {
                throw new InvalidOperationException("Cannot reload before a level has been loaded.");
            }

            BeginLevel(currentContext.LevelId);
        }

        public void UnloadCurrentLevel()
        {
            if (IsLoading)
            {
                throw new InvalidOperationException("A level lifecycle operation is already in progress.");
            }

            IsLoading = true;
            try
            {
                UnloadCurrentLevelCore();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UnloadCurrentLevelCore()
        {
            Readiness.Close();

            if (currentContext == null)
            {
                return;
            }

            LevelContext context = currentContext;
            currentContext = null;
            try
            {
                LevelWillUnload?.Invoke(context);
            }
            finally
            {
                levelSpawner.UnloadLevel(context);
            }
        }

        public void RegisterParticipant(ILevelLifecycleParticipant participant)
        {
            if (currentContext == null || !IsReady)
            {
                throw new InvalidOperationException("A lifecycle participant can only bind while a level is ready.");
            }

            currentContext.RegisterParticipant(participant);
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            Readiness.Close();
            UnloadCurrentLevelCore();
            instance = null;
        }

        private static void DestroyOwnedObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
