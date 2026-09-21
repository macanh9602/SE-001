using System;
using System.Collections.Generic;

namespace SE001.System.Management
{
    /// <summary>Per-level, transient runtime index. Feature state is added by later stories.</summary>
    public sealed class LevelRuntimeState : IDisposable
    {
        private readonly HashSet<string> stableIds = new HashSet<string>();
        private bool disposed;

        public bool IsDisposed => disposed;

        public bool RegisterStableId(string stableId)
        {
            EnsureNotDisposed();
            return !string.IsNullOrWhiteSpace(stableId) && stableIds.Add(stableId);
        }

        public bool ContainsStableId(string stableId)
        {
            return !disposed && !string.IsNullOrWhiteSpace(stableId) && stableIds.Contains(stableId);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            stableIds.Clear();
            disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(LevelRuntimeState));
            }
        }
    }
}
