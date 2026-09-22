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
            State = new SandSimulationState(masks.Width, masks.Height, new byte[masks.Width * masks.Height], masks.ValidMask, masks.StaticMask, new bool[masks.Width * masks.Height], new bool[masks.Width * masks.Height]);
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

        public int Step()
        {
            EnsureNotDisposed();
            Array.Clear(nextCells, 0, nextCells.Length);
            int occupied = 0; int moved = 0;
            int startX = reverseScan ? State.Width - 1 : 0;
            int endX = reverseScan ? -1 : State.Width;
            int increment = reverseScan ? -1 : 1;
            for (int y = 0; y < State.Height; y++) for (int x = startX; x != endX; x += increment)
            {
                int source = State.Index(x, y); byte material = State.Cells[source];
                if (material == 0) continue;
                int targetX = x; int targetY = y;
                int first = increment; // alternate diagonal preference with scan direction → no left bias
                if (IsFreeNext(x, y - 1)) targetY = y - 1;
                else if (IsFreeNext(x + first, y - 1)) { targetX = x + first; targetY = y - 1; }
                else if (IsFreeNext(x - first, y - 1)) { targetX = x - first; targetY = y - 1; }
                else if (profile.dispersion > 1 && TryDisperse(x, y, first, out int dx)) targetX = x + (dx > 0 ? 1 : -1); // walk 1 cell/step toward the drop (smooth, no teleport)
                int target = State.Index(targetX, targetY); if (nextCells[target] == 0) { nextCells[target] = material; if (target != source) moved++; } else nextCells[source] = material;
                occupied++;
            }
            Array.Clear(State.Cells, 0, State.Cells.Length); Buffer.BlockCopy(nextCells, 0, State.Cells, 0, State.Cells.Length); reverseScan = !reverseScan; State.OccupiedCount = occupied; return moved;
        }

        /// <summary>
        /// Avalanche / levelling: a resting grain whose diagonals are blocked slides to the nearest drop up to
        /// profile.dispersion cells away along its row (path must be free). Slope settles at ~1/dispersion instead
        /// of 45°, piles spread flat like powder, and it only ever moves toward a lower cell → no endless jitter.
        /// </summary>
        private bool TryDisperse(int x, int y, int first, out int dx)
        {
            int reach = profile.dispersion;
            for (int d = 2; d <= reach; d++)
            {
                for (int k = 0; k < 2; k++)
                {
                    int dir = k == 0 ? first : -first;
                    if (!PathFree(x, y, dir, d)) continue;
                    if (IsFreeNext(x + dir * d, y - 1)) { dx = dir * d; return true; }
                }
            }

            dx = 0;
            return false;
        }

        private bool PathFree(int x, int y, int dir, int distance)
        {
            for (int i = 1; i <= distance; i++)
            {
                int px = x + dir * i;
                if (!CanOccupy(px, y)) return false;
                int idx = State.Index(px, y);
                if (State.Cells[idx] != 0 || nextCells[idx] != 0) return false;
            }

            return true;
        }

        private bool IsFreeNext(int x, int y) => CanOccupy(x, y) && nextCells[State.Index(x, y)] == 0;

        public void Dispose() { disposed = true; nextCells = null; }
        public void SetCupWall(int x, int y, bool value) { if (x >= 0 && y >= 0 && x < State.Width && y < State.Height) State.CupWallMask[State.Index(x, y)] = value; }
        public void SetDynamic(int x, int y, bool value) { if (x >= 0 && y >= 0 && x < State.Width && y < State.Height) State.DynamicMask[State.Index(x, y)] = value; }
        public bool IsOccupied(int x, int y) => x >= 0 && y >= 0 && x < State.Width && y < State.Height && State.Cells[State.Index(x, y)] != 0;
        public byte Remove(int x, int y) { if (!IsOccupied(x, y)) return 0; int i = State.Index(x, y); byte value = State.Cells[i]; State.Cells[i] = 0; State.OccupiedCount--; return value; }
        private bool CanOccupy(int x, int y) => x >= 0 && y >= 0 && x < State.Width && y < State.Height && State.ValidMask[State.Index(x, y)] && !State.StaticMask[State.Index(x, y)] && !State.CupWallMask[State.Index(x, y)] && !State.DynamicMask[State.Index(x, y)];
        private void EnsureNotDisposed() { if (disposed) throw new ObjectDisposedException(nameof(SandSimulation)); }
    }
}
