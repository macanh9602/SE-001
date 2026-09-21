using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SE001.System.Management
{
    /// <summary>Owns the lifecycle of the active level; gameplay rules live in feature owners.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class LevelManager : MonoBehaviour
    {
        private const string GameSceneName = "GameScene";
        private const string FoundationSmokeLevelId = "foundation_smoke";

        private static LevelManager instance;

        private int nextGeneration;
        private LevelContext currentContext;

        public event Action<LevelContext> LevelReady;
        public event Action<LevelContext> LevelWillUnload;

        public static LevelManager Instance => instance;
        public LevelContext CurrentContext => currentContext;
        public LevelReadinessGate Readiness { get; } = new LevelReadinessGate();
        public bool IsLoading { get; private set; }
        public bool IsReady => currentContext != null && !currentContext.IsDisposed && Readiness.IsOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameSceneRuntime()
        {
            if (SceneManager.GetActiveScene().name != GameSceneName || instance != null)
            {
                return;
            }

            var root = new GameObject(nameof(LevelManager));
            root.AddComponent<LevelManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BeginLevel(FoundationSmokeLevelId);
        }

        public void BeginLevel(string levelId)
        {
            IsLoading = true;
            Readiness.Close();
            UnloadCurrentLevel();

            try
            {
                currentContext = LevelContext.Create(levelId, ++nextGeneration, transform);
                Readiness.Open();
                LevelReady?.Invoke(currentContext);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ReloadCurrentLevel()
        {
            BeginLevel(currentContext == null ? FoundationSmokeLevelId : currentContext.LevelId);
        }

        public void UnloadCurrentLevel()
        {
            Readiness.Close();

            if (currentContext == null)
            {
                return;
            }

            LevelContext context = currentContext;
            currentContext = null;
            LevelWillUnload?.Invoke(context);
            context.Dispose();
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

            UnloadCurrentLevel();
            instance = null;
        }
    }
}
