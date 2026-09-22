using System;
using SE001.Geometry;

namespace SE001.Simulation.Sand
{
    /// <summary>
    /// Authoritative falling-sand simulation. Model ported from Docs/visualizers/sand-feel-lab.html (stepSand),
    /// adapted to board space (y up) and made deterministic:
    /// - gravity + max fall speed, multi-cell fall per step;
    /// - landing splash converts impact speed into sideways momentum;
    /// - momentum carries grains along shallow ramps / drawn lines (pure CA would stick below 45°);
    /// - diagonal roll (repose) + avalanche 'dispersion' toward the nearest drop levels piles like powder.
    /// The lab's random 'flow' is intentionally replaced by dispersion: grains only move toward lower cells or
    /// while they still have momentum, so a pile always reaches a stable state (no endless shimmer).
    /// All randomness comes from a seeded xorshift PRNG advanced in scan order → same seed + same commands = same grid.
    /// </summary>
    public sealed class SandSimulation : IDisposable
    {
        private const uint DefaultSeed = 0x9E3779B9u;

        private readonly SandSimulationProfile profile;
        private byte[] stamp;
        private byte frame;
        private uint rng;
        private bool disposed;

        public SandSimulation(SandSimulationProfile profile, LayoutMaskSet masks) : this(profile, masks, DefaultSeed) { }

        public SandSimulation(SandSimulationProfile profile, LayoutMaskSet masks, uint seed)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (masks == null) throw new ArgumentNullException(nameof(masks));
            if ((long)masks.Width * masks.Height > profile.maxCells) throw new InvalidOperationException("Sand grid exceeds profile capacity.");
            int count = masks.Width * masks.Height;
            State = new SandSimulationState(masks.Width, masks.Height, new byte[count], masks.ValidMask, masks.StaticMask, new bool[count], new bool[count]);
            stamp = new byte[count];
            rng = seed == 0 ? DefaultSeed : seed;
        }

        public SandSimulationState State { get; }
        public float CellSize => profile.cellSize;
        public bool IsDisposed => disposed;

        /// <summary>Stable 32-bit seed from a level id (FNV-1a; independent of runtime string hashing).</summary>
        public static uint SeedFrom(string text)
        {
            uint hash = 2166136261u;
            if (text != null)
                for (int i = 0; i < text.Length; i++) { hash ^= text[i]; hash *= 16777619u; }
            return hash == 0 ? DefaultSeed : hash;
        }

        public bool TryEmit(int x, int y, byte materialId)
        {
            EnsureNotDisposed();
            if (materialId == 0 || !CanOccupy(x, y)) return false;
            int index = State.Index(x, y);
            if (State.Cells[index] != 0) return false;
            State.Cells[index] = materialId;
            State.Shade[index] = (byte)(NextUInt() & 0xFF);
            State.Velocity[index] = NextFloat() * 0.6f;
            State.Momentum[index] = (NextFloat() - 0.5f) * 0.3f;
            State.RowCount[y]++;
            State.OccupiedCount++;
            State.EmittedCount++;
            return true;
        }

        public int EmitRegion(int minX, int minY, int maxX, int maxY, byte materialId)
        {
            int inserted = 0;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (TryEmit(x, y, materialId)) inserted++;
            return inserted;
        }

        /// <summary>Advances one step. Returns the number of grains that changed cell (0 = stable).</summary>
        public int Step()
        {
            EnsureNotDisposed();
            frame = frame == 1 ? (byte)2 : (byte)1;
            int width = State.Width;
            int height = State.Height;
            byte[] cells = State.Cells;
            float[] vel = State.Velocity;
            float[] mom = State.Momentum;
            int[] rowCount = State.RowCount;
            float g = profile.gravityCellsPerStep2;
            float vmax = profile.maxFallCellsPerStep;
            float repose = profile.repose;
            float slide = profile.slide;
            float splash = profile.splash;
            int moved = 0;

            // Bottom-up (board y grows upward): grains fall into rows that were already processed.
            for (int y = 0; y < height; y++)
            {
                if (rowCount[y] == 0) continue;
                bool leftToRight = (NextUInt() & 1u) == 0u;
                if (profile.flowOrderedRows)
                {
                    float rowMomentum = 0f;
                    int rowStart = y * width;
                    for (int k = 0; k < width; k++)
                        if (cells[rowStart + k] != 0) rowMomentum += mom[rowStart + k];
                    // Process the leading grain first so a sliding train advances together.
                    if (Math.Abs(rowMomentum) > profile.flowOrderThreshold) leftToRight = rowMomentum < 0f;
                }
                for (int k = 0; k < width; k++)
                {
                    int x = leftToRight ? k : width - 1 - k;
                    int i = y * width + x;
                    byte material = cells[i];
                    if (material == 0 || stamp[i] == frame) continue;

                    float v = vel[i] + g;
                    if (v > vmax) v = vmax;
                    float m = mom[i];
                    int cx = x, cy = y;
                    int fell = 0;
                    int n = v < 1f ? 1 : (int)v;

                    for (int s = 0; s < n; s++)
                    {
                        if (!IsFree(cx, cy - 1)) break;
                        cy--;
                        fell++;
                    }

                    if (fell == 0)
                    {
                        bool movedHere = false;
                        if (profile.momentumCarry > 0f && cy > 0)
                        {
                            // Resting on a faster-sliding grain: ride along so the layer flows as a sheet.
                            int below = i - width;
                            if (cells[below] != 0 && Math.Abs(mom[below]) > Math.Abs(m))
                                m = m * (1f - profile.momentumCarry) + mom[below] * profile.momentumCarry;
                        }

                        int downhill = profile.slopeAccel > 0f ? DownhillSide(cx, cy) : 0;
                        if (v > 1.5f && splash > 0f)
                        {
                            if (Math.Abs(m) < 0.2f)
                                m = downhill != 0 && NextFloat() < profile.splashDownhillBias
                                    ? downhill * 0.2f
                                    : (NextFloat() < 0.5f ? -0.2f : 0.2f);
                            m += Math.Sign(m) * (v - 1f) * splash * 0.5f;
                        }

                        if (downhill != 0) m += downhill * profile.slopeAccel;

                        float am = Math.Abs(m);
                        int d = am > 0.15f ? Math.Sign(m) : (NextFloat() < 0.5f ? -1 : 1);

                        // Diagonal roll (repose), preferring the momentum side.
                        if (NextFloat() < repose || am > 0.5f)
                        {
                            for (int q = 0; q < 2 && !movedHere; q++)
                            {
                                int dd = q == 0 ? d : -d;
                                if (IsFree(cx + dd, cy - 1) && IsFree(cx + dd, cy))
                                {
                                    cx += dd;
                                    cy--;
                                    m = m * 0.9f + dd * 0.35f;
                                    movedHere = true;
                                }
                            }
                        }

                        // Momentum slide along a surface / ramp.
                        if (!movedHere && Math.Abs(m) > 0.3f)
                        {
                            int dd = Math.Sign(m);
                            int maxCells = profile.maxSlideCells;
                            if (maxCells > 1)
                            {
                                float speed = Math.Abs(m);
                                int cellsToMove = (int)speed + (NextFloat() < speed - (int)speed ? 1 : 0);
                                if (cellsToMove < 1) cellsToMove = 1;
                                if (cellsToMove > maxCells) cellsToMove = maxCells;
                                for (int slideStep = 0; slideStep < cellsToMove; slideStep++)
                                {
                                    if (!IsFree(cx + dd, cy)) break;
                                    cx += dd;
                                    movedHere = true;
                                    // Stepped past a lip into open air: stop here and let it drop straight (no walking off the edge).
                                    if (profile.airDropProbe > 0 && IsOpenBelow(cx, cy, profile.airDropProbe)) break;
                                    if (IsFree(cx, cy - 1)) cy--; // follow the ramp step down
                                }

                                m *= movedHere ? slide : -0.2f;
                            }
                            else if (IsFree(cx + dd, cy)) { cx += dd; m *= slide; movedHere = true; }
                            else m *= -0.2f;
                        }

                        // Avalanche toward the nearest drop (levels the pile, never oscillates on flat ground).
                        if (!movedHere && profile.dispersion > 1 && TryDisperse(cx, cy, d, out int step))
                        {
                            cx += step;
                            movedHere = true;
                        }

                        if (!movedHere) m *= slide * 0.5f;
                        v = movedHere ? Math.Max(1f, v * 0.7f) : 0f;
                    }
                    else
                    {
                        if (fell < n) v = 1f + (v - 1f) * 0.3f;
                        bool released = profile.airDropProbe > 0 && IsOpenBelow(cx, cy, profile.airDropProbe);
                        if (released) m *= profile.airMomentumRetention; // left the edge: drop straight, no sideways drift
                        float am = Math.Abs(m);
                        if (am > 0.4f && NextFloat() < am * 0.35f)
                        {
                            int dd = Math.Sign(m);
                            if (IsFree(cx + dd, cy)) cx += dd;
                        }

                        // Only damp when truly airborne; 1-cell staircase drops on a ramp keep slide momentum.
                        if (!released && v >= profile.airborneMinSpeed) m *= profile.airMomentumRetention;
                    }

                    if (m > 2f) m = 2f;
                    else if (m < -2f) m = -2f;

                    int target = cy * width + cx;
                    if (target != i)
                    {
                        cells[target] = material;
                        State.Shade[target] = State.Shade[i];
                        vel[target] = v;
                        mom[target] = m;
                        stamp[target] = frame;
                        cells[i] = 0;
                        rowCount[y]--;
                        rowCount[cy]++;
                        moved++;
                    }
                    else
                    {
                        vel[i] = v;
                        mom[i] = m;
                        stamp[i] = frame;
                    }
                }
            }

            return moved;
        }

        /// <summary>
        /// Resting grain with blocked diagonals: find the nearest drop within 'dispersion' cells along the row
        /// (path must be free) and return a single-cell step toward it.
        /// </summary>
        private bool TryDisperse(int x, int y, int preferred, out int step)
        {
            int reach = profile.dispersion;
            for (int distance = 2; distance <= reach; distance++)
            {
                for (int k = 0; k < 2; k++)
                {
                    int dir = k == 0 ? preferred : -preferred;
                    if (!PathFree(x, y, dir, distance)) continue;
                    if (IsFree(x + dir * distance, y - 1)) { step = dir; return true; }
                }
            }

            step = 0;
            return false;
        }

        /// <summary>Side (-1/+1) whose nearest drop along the row is closer within slopeProbe; 0 = flat/ambiguous.</summary>
        private int DownhillSide(int x, int y)
        {
            int reach = profile.slopeProbe;
            int left = int.MaxValue, right = int.MaxValue;
            for (int d = 1; d <= reach; d++)
            {
                if (!IsFree(x - d, y)) break;
                if (IsFree(x - d, y - 1)) { left = d; break; }
            }

            for (int d = 1; d <= reach; d++)
            {
                if (!IsFree(x + d, y)) break;
                if (IsFree(x + d, y - 1)) { right = d; break; }
            }

            if (left == right) return 0;
            return left < right ? -1 : 1;
        }

        /// <summary>No surface within 'depth' cells below: only empty cells or grains that are themselves falling (v ≥ 1).
        /// Falling grains count as air so the upper layer of a sheet also releases at the lip.</summary>
        private bool IsOpenBelow(int x, int y, int depth)
        {
            for (int d = 1; d <= depth; d++)
            {
                int yy = y - d;
                if (!CanOccupy(x, yy)) return false;
                int j = yy * State.Width + x;
                if (State.Cells[j] != 0 && State.Velocity[j] < 1f) return false;
            }

            return true;
        }

        private bool PathFree(int x, int y, int dir, int distance)
        {
            for (int i = 1; i <= distance; i++)
                if (!IsFree(x + dir * i, y)) return false;
            return true;
        }

        private bool IsFree(int x, int y) => CanOccupy(x, y) && State.Cells[y * State.Width + x] == 0;

        public void Dispose()
        {
            disposed = true;
            stamp = null;
        }

        public void SetCupWall(int x, int y, bool value)
        {
            if (x >= 0 && y >= 0 && x < State.Width && y < State.Height) State.CupWallMask[State.Index(x, y)] = value;
        }

        public void SetDynamic(int x, int y, bool value)
        {
            if (x >= 0 && y >= 0 && x < State.Width && y < State.Height) State.DynamicMask[State.Index(x, y)] = value;
        }

        public bool IsOccupied(int x, int y) => x >= 0 && y >= 0 && x < State.Width && y < State.Height && State.Cells[State.Index(x, y)] != 0;

        public byte Remove(int x, int y)
        {
            if (!IsOccupied(x, y)) return 0;
            int i = State.Index(x, y);
            byte value = State.Cells[i];
            State.Cells[i] = 0;
            State.Velocity[i] = 0f;
            State.Momentum[i] = 0f;
            State.RowCount[y]--;
            State.OccupiedCount--;
            return value;
        }

        private bool CanOccupy(int x, int y)
        {
            if (x < 0 || y < 0 || x >= State.Width || y >= State.Height) return false;
            int i = y * State.Width + x;
            return State.ValidMask[i] && !State.StaticMask[i] && !State.CupWallMask[i] && !State.DynamicMask[i];
        }

        // xorshift32 — deterministic, allocation-free.
        private uint NextUInt()
        {
            uint x = rng;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            rng = x;
            return x;
        }

        private float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        private void EnsureNotDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(SandSimulation));
        }
    }
}
