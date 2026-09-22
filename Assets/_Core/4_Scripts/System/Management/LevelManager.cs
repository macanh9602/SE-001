using System;
using System.Collections.Generic;
using SE001.Data;
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
        [SerializeField] private string firstLevelId = "phase_c_level_01";
        [SerializeField] private PhaseCLevelSequence levelSequence;

        private int nextGeneration;
        private LevelContext currentContext;
        private readonly LevelReadinessGate readiness = new LevelReadinessGate();
        private bool isLoading;

        public event Action<LevelContext> LevelReady;
        public event Action<LevelContext> LevelWillUnload;

        public static LevelManager Instance => instance;
        public LevelSpawner Spawner => levelSpawner;
        public LevelContext CurrentContext => currentContext;
        public LevelReadinessGate Readiness => readiness;
        public bool IsLoading
        {
            get => isLoading;
            private set => isLoading = value;
        }
        public bool IsReady => currentContext != null && !currentContext.IsDisposed && Readiness.IsOpen;

        public bool CanBeginNextLevel()
        {
            EnsureLevelSequence();
            if (!IsReady || levelSequence == null || levelSequence.levels == null)
                return false;
            int index = FindCurrentLevelIndex();
            return index >= 0 && index + 1 < levelSequence.levels.Count;
        }

        public bool BeginNextLevel()
        {
            if (!CanBeginNextLevel())
                return false;
            string nextLevelId = levelSequence.levels[FindCurrentLevelIndex() + 1].levelId;
            BeginLevel(nextLevelId);
            return true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                DestroyOwnedObject(gameObject);
                return;
            }

            instance = this;
            EnsureLevelSequence();
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
            EnsureLevelSequence();
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A stable level id is required.", nameof(levelId));
            }

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

        private int FindCurrentLevelIndex()
        {
            if (currentContext == null || levelSequence == null || levelSequence.levels == null)
                return -1;
            for (int i = 0; i < levelSequence.levels.Count; i++)
                if (levelSequence.levels[i].levelId == currentContext.LevelId)
                    return i;
            return -1;
        }

        public IReadOnlyList<LevelSequenceEntry> GetLevelSequence()
        {
            EnsureLevelSequence();
            return levelSequence != null ? levelSequence.levels : Array.Empty<LevelSequenceEntry>();
        }

        private void EnsureLevelSequence()
        {
            if (levelSequence != null)
                return;
            GameplayRuntimeProfile runtime =
                Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            levelSequence = runtime != null ? runtime.levelSequence : null;
        }
    }
}
