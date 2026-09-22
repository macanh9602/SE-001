using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SE001.Simulation.Sand;

namespace SE001.System.Management
{
    /// <summary>Owns one level's lifetime state and receives its hierarchy from LevelSpawner.</summary>
    public sealed class LevelContext : IDisposable
    {
        private readonly List<ILevelLifecycleParticipant> participants = new List<ILevelLifecycleParticipant>();
        private readonly CancellationTokenSource lifetimeSource = new CancellationTokenSource();
        private bool disposed;
        private SandSimulationProfile ownedSimulationProfile;

        internal LevelContext(
            string levelId,
            int generation,
            LevelRuntimeState runtimeState,
            Transform levelRoot,
            Transform boardRoot,
            Transform obstacleRoot,
            Transform sourceRoot,
            Transform cupRoot,
            Transform dynamicDrawRoot,
            Transform sandVisualRoot,
            Transform vfxRoot)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                throw new ArgumentException("A level context requires a stable level id.", nameof(levelId));
            }

            LevelId = levelId;
            Generation = generation;
            LifetimeToken = lifetimeSource.Token;
            RuntimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            LevelRoot = levelRoot != null ? levelRoot : throw new ArgumentNullException(nameof(levelRoot));
            BoardRoot = boardRoot != null ? boardRoot : throw new ArgumentNullException(nameof(boardRoot));
            ObstacleRoot = obstacleRoot != null ? obstacleRoot : throw new ArgumentNullException(nameof(obstacleRoot));
            SourceRoot = sourceRoot != null ? sourceRoot : throw new ArgumentNullException(nameof(sourceRoot));
            CupRoot = cupRoot != null ? cupRoot : throw new ArgumentNullException(nameof(cupRoot));
            DynamicDrawRoot = dynamicDrawRoot != null ? dynamicDrawRoot : throw new ArgumentNullException(nameof(dynamicDrawRoot));
            SandVisualRoot = sandVisualRoot != null ? sandVisualRoot : throw new ArgumentNullException(nameof(sandVisualRoot));
            VfxRoot = vfxRoot != null ? vfxRoot : throw new ArgumentNullException(nameof(vfxRoot));
        }

        internal void AttachSimulation(SandSimulation simulation, SandSimulationProfile ownedProfile = null)
        {
            EnsureNotDisposed();
            SandSimulation = simulation;
            ownedSimulationProfile = ownedProfile;
        }

        public string LevelId { get; }
        public int Generation { get; }
        public CancellationToken LifetimeToken { get; }
        public LevelRuntimeState RuntimeState { get; }
        public Transform LevelRoot { get; }
        public Transform BoardRoot { get; }
        public Transform ObstacleRoot { get; }
        public Transform SourceRoot { get; }
        public Transform CupRoot { get; }
        public Transform DynamicDrawRoot { get; }
        public Transform SandVisualRoot { get; }
        public Transform VfxRoot { get; }
        public SandSimulation SandSimulation { get; private set; }
        /// <summary>Board bounds in board-space units, origin at (0,0). Zero when the level has no authored data.</summary>
        public Vector2 BoardSize { get; internal set; }
        public float DrawInkBudget { get; internal set; }
        public bool IsDisposed => disposed;

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
            SandSimulation?.Dispose();
            if (ownedSimulationProfile != null)
            {
                if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(ownedSimulationProfile);
                else UnityEngine.Object.DestroyImmediate(ownedSimulationProfile);
                ownedSimulationProfile = null;
            }
            RuntimeState.Dispose();
            lifetimeSource.Dispose();
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
