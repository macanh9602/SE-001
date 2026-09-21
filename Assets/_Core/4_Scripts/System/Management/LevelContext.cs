using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SE001.System.Management
{
    /// <summary>Owns all transient objects and cancellation state for one loaded level.</summary>
    public sealed class LevelContext : IDisposable
    {
        private readonly List<ILevelLifecycleParticipant> participants = new List<ILevelLifecycleParticipant>();
        private readonly CancellationTokenSource lifetimeSource = new CancellationTokenSource();
        private bool disposed;

        private LevelContext(string levelId, int generation, Transform owner)
        {
            LevelId = levelId;
            Generation = generation;
            LifetimeToken = lifetimeSource.Token;
            RuntimeState = new LevelRuntimeState();

            LevelRoot = CreateChild("LevelRoot", owner);
            BoardRoot = CreateChild("BoardRoot", LevelRoot);
            SimulationRoot = CreateChild("SimulationRoot", LevelRoot);
            VisualRoot = CreateChild("VisualRoot", LevelRoot);
        }

        public string LevelId { get; }
        public int Generation { get; }
        public CancellationToken LifetimeToken { get; }
        public LevelRuntimeState RuntimeState { get; }
        public Transform LevelRoot { get; }
        public Transform BoardRoot { get; }
        public Transform SimulationRoot { get; }
        public Transform VisualRoot { get; }
        public bool IsDisposed => disposed;

        public static LevelContext Create(string levelId, int generation, Transform owner)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A level context requires a stable level id.", nameof(levelId));
            }

            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            return new LevelContext(levelId, generation, owner);
        }

        public void RegisterParticipant(ILevelLifecycleParticipant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            EnsureNotDisposed();
            if (participants.Contains(participant))
            {
                return;
            }

            participants.Add(participant);
            participant.Bind(this);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lifetimeSource.Cancel();

            for (int i = participants.Count - 1; i >= 0; i--)
            {
                participants[i].CleanupForLevelUnload();
            }

            participants.Clear();
            RuntimeState.Dispose();
            lifetimeSource.Dispose();

            if (LevelRoot != null)
            {
                UnityEngine.Object.Destroy(LevelRoot.gameObject);
            }
        }

        private static Transform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(LevelContext));
            }
        }
    }
}
