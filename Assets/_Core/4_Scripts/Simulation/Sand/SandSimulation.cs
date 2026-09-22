using System;
using SE001.Geometry;

namespace SE001.Simulation.Sand
{
    public sealed class SandSimulation : IDisposable
    {
        private readonly SandSimulationProfile profile;
        private byte[] nextCells;
        private bool disposed;
        private bool reverseScan;

        public SandSimulation(SandSimulationProfile profile, LayoutMaskSet masks)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (masks == null) throw new ArgumentNullException(nameof(masks));
            if ((long)masks.Width * masks.Height > profile.maxCells) throw new InvalidOperationException("Sand grid exceeds profile capacity.");
            State = new SandSimulationState(masks.Width, masks.Height, new byte[masks.Width * masks.Height], masks.ValidMask, masks.StaticMask);
            nextCells = new byte[State.Cells.Length];
        }

        public SandSimulationState State { get; }
        public float CellSize => profile.cellSize;
        public bool IsDisposed => disposed;

        public bool TryEmit(int x, int y, byte materialId)
        {
            EnsureNotDisposed();
            if (materialId == 0 || !CanOccupy(x, y)) return false;
            int index = State.Index(x, y);
            if (State.Cells[index] != 0) return false;
            State.Cells[index] = materialId;
            State.OccupiedCount++;
            State.EmittedCount++;
            return true;
        }

        public int EmitRegion(int minX, int minY, int maxX, int maxY, byte materialId)
        {
            int inserted = 0;
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) if (TryEmit(x, y, materialId)) inserted++;
            return inserted;
        }

        public void Step()
        {
            EnsureNotDisposed();
            Array.Clear(nextCells, 0, nextCells.Length);
            int occupied = 0;
            int startX = reverseScan ? State.Width - 1 : 0;
            int endX = reverseScan ? -1 : State.Width;
            int increment = reverseScan ? -1 : 1;
            for (int y = 0; y < State.Height; y++) for (int x = startX; x != endX; x += increment)
            {
                int source = State.Index(x, y); byte material = State.Cells[source];
                if (material == 0) continue;
                int targetX = x; int targetY = y;
                if (CanOccupy(x, y - 1) && nextCells[State.Index(x, y - 1)] == 0) targetY = y - 1;
                else if (CanOccupy(x - 1, y - 1) && nextCells[State.Index(x - 1, y - 1)] == 0) { targetX = x - 1; targetY = y - 1; }
                else if (CanOccupy(x + 1, y - 1) && nextCells[State.Index(x + 1, y - 1)] == 0) { targetX = x + 1; targetY = y - 1; }
                else if (profile.enableLateralSlide && CanOccupy(x - increment, y) && nextCells[State.Index(x - increment, y)] == 0) targetX = x - increment;
                int target = State.Index(targetX, targetY); if (nextCells[target] == 0) nextCells[target] = material; else nextCells[source] = material;
                occupied++;
            }
            Array.Clear(State.Cells, 0, State.Cells.Length); Buffer.BlockCopy(nextCells, 0, State.Cells, 0, State.Cells.Length); reverseScan = !reverseScan; State.OccupiedCount = occupied;
        }

        public void Dispose() { disposed = true; nextCells = null; }
        private bool CanOccupy(int x, int y) => x >= 0 && y >= 0 && x < State.Width && y < State.Height && State.ValidMask[State.Index(x, y)] && !State.StaticMask[State.Index(x, y)];
        private void EnsureNotDisposed() { if (disposed) throw new ObjectDisposedException(nameof(SandSimulation)); }
    }
}
