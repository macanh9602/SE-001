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
    /// - airborne grains never drift sideways and lose momentum by 'airDrag' each step, so sand leaving an
    ///   obstacle edge drops straight down as a readable stream (sideways spray caused accidental wrong-cup loses);
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
        private readonly byte[] bowlPassStamp;
        private byte bowlPassFrame;
        private int bowlFlowMinX = int.MaxValue, bowlFlowMaxX = -1;
        private int bowlFlowMinY = int.MaxValue, bowlFlowMaxY = -1;
        private uint rng;
        private bool disposed;

        public SandSimulation(SandSimulationProfile profile, LayoutMaskSet masks) : this(profile, masks, DefaultSeed) { }

        public SandSimulation(SandSimulationProfile profile, LayoutMaskSet masks, uint seed)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (masks == null) throw new ArgumentNullException(nameof(masks));
            if ((long)masks.Width * masks.Height > profile.maxCells) throw new InvalidOperationException("Sand grid exceeds profile capacity.");
            int count = masks.Width * masks.Height;
            State = new SandSimulationState(masks.Width, masks.Height, new byte[count],
                masks.ValidMask, masks.StaticMask, new bool[count], new bool[count], new bool[count]);
            stamp = new byte[count];
            bowlPassStamp = new byte[count];
            rng = seed == 0 ? DefaultSeed : seed;
        }

        public SandSimulationState State { get; }
        public float CellSize => profile.cellSize;
        public int BowlSettlingHeadroomCells => profile.bowlSettlingHeadroomCells;
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
            float airDrag = profile.airDrag;
            float creepChance = profile.creepReach > profile.dispersion ? profile.creepChance : 0f;
            int moved = 0;

            LastStepStats = default(SandStepStats);
            // Bottom-up (board y grows upward): grains fall into rows that were already processed.
            for (int y = 0; y < height; y++)
            {
                if (rowCount[y] == 0) continue;
                bool leftToRight = (NextUInt() & 1u) == 0u;
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
                    int rule = 1;
                    int n = v < 1f ? 1 : (int)v;

                    for (int s = 0; s < n; s++)
                    {
                        if (!IsFree(cx, cy - 1)) break;
                        cy--;
                        fell++;
                    }

                    if (fell == 0)
                    {
                        bool overBowl = State.BowlFlowMask[i];
                        bool movedHere = false;
                        // Inside the Bowl, landing energy should settle into the pile instead of kicking grains
                        // across the open lip. Creep below still spreads the surface toward empty sink cells.
                        if (overBowl) m = 0f;
                        else if (v > 1.5f && splash > 0f)
                        {
                            if (Math.Abs(m) < 0.2f) m = NextFloat() < 0.5f ? -0.2f : 0.2f;
                            m += Math.Sign(m) * (v - 1f) * splash * 0.5f;
                        }

                        // Rim assist: a grain resting on a biased solid (Bowl lip) is steered toward the Bowl interior.
                        int bias = cy > 0 ? State.SurfaceBias[(cy - 1) * width + cx] : 0;
                        if (bias != 0 && m * bias < RimAssistMomentum) m = bias * RimAssistMomentum;

                        float am = Math.Abs(m);
                        int d = am > 0.15f ? Math.Sign(m) : (NextFloat() < 0.5f ? -1 : 1);

                        // Diagonal roll (repose), preferring the momentum side.
                        if (NextFloat() < repose || am > 0.5f)
                        {
                            // A rim-assisted grain never rolls away from the Bowl.
                            int tries = bias != 0 ? 1 : 2;
                            for (int q = 0; q < tries && !movedHere; q++)
                            {
                                int dd = q == 0 ? d : -d;
                                if (IsFree(cx + dd, cy - 1) && IsFree(cx + dd, cy) &&
                                    (!overBowl || IsBowlFlowCell(cx + dd, cy - 1)))
                                {
                                    cx += dd;
                                    cy--;
                                    m = m * 0.9f + dd * 0.35f;
                                    movedHere = true;
                                    rule = 2;
                                }
                            }
                        }

                        // Momentum slide along a surface / ramp.
                        if (!movedHere && Math.Abs(m) > 0.3f)
                        {
                            int dd = Math.Sign(m);
                            if (IsFree(cx + dd, cy) && (!overBowl || IsBowlFlowCell(cx + dd, cy)))
                            {
                                cx += dd;
                                m *= slide;
                                movedHere = true;
                                rule = 3;
                            }
                            else m *= -0.2f;
                        }

                        // Avalanche toward the nearest drop (levels the pile, never oscillates on flat ground).
                        if (!movedHere && profile.dispersion > 1 &&
                            TryDisperse(cx, cy, d, bias != 0, overBowl, out int step))
                        {
                            cx += step;
                            movedHere = true;
                            rule = 4;
                        }

                        // Creep: rarely walk toward a far drop so piles keep flattening and nothing parks on a
                        // wide flat obstacle / stroke. Always heads to a lower cell, so the grid still reaches a
                        // stable state (grains on open floor with no drop in reach never creep).
                        float localCreepChance = overBowl ? profile.bowlCreepChance : creepChance;
                        int localCreepReach = overBowl ? profile.bowlCreepReach : profile.creepReach;
                        if (!movedHere && localCreepChance > 0f && NextFloat() < localCreepChance &&
                            TryCreep(cx, cy, d, bias != 0, localCreepReach, overBowl, out int creepStep))
                        {
                            cx += creepStep;
                            movedHere = true;
                            rule = 5;
                        }

                        if (!movedHere) m *= slide * 0.5f;
                        v = movedHere ? Math.Max(1f, v * 0.7f) : 0f;
                    }
                    else
                    {
                        if (fell < n) v = 1f + (v - 1f) * 0.3f;
                        // Airborne: straight vertical fall; momentum only decays (kept for the next landing/slide).
                        m *= airDrag;
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
                        CountMove(rule, x, y, cx, cy);
                    }
                    else
                    {
                        vel[i] = v;
                        mom[i] = m;
                        stamp[i] = frame;
                    }
                }
            }

            for (int pass = 0; pass < profile.bowlLevelingExtraPasses; pass++)
                moved += SettleBowlPass();
            return moved;
        }

        /// <summary>Additional Bowl-only relaxation. Each marked grain moves at most once per pass.</summary>
        private int SettleBowlPass()
        {
            if (bowlFlowMaxY < bowlFlowMinY) return 0;
            if (++bowlPassFrame == 0)
            {
                Array.Clear(bowlPassStamp, 0, bowlPassStamp.Length);
                bowlPassFrame = 1;
            }

            SandSimulationState state = State;
            int width = state.Width;
            int moved = 0;
            for (int y = bowlFlowMinY; y <= bowlFlowMaxY; y++)
            {
                if (state.RowCount[y] == 0) continue;
                bool leftToRight = (NextUInt() & 1u) == 0u;
                int span = bowlFlowMaxX - bowlFlowMinX + 1;
                for (int k = 0; k < span; k++)
                {
                    int x = leftToRight ? bowlFlowMinX + k : bowlFlowMaxX - k;
                    int i = y * width + x;
                    if (!state.BowlFlowMask[i] || state.Cells[i] == 0 || bowlPassStamp[i] == bowlPassFrame ||
                        IsFree(x, y - 1)) continue;

                    int bias = y > 0 ? state.SurfaceBias[i - width] : 0;
                    int preferred = bias != 0 ? bias : (NextUInt() & 1u) == 0u ? -1 : 1;
                    int step;
                    int rule;
                    if (profile.dispersion > 1 && TryDisperse(x, y, preferred, bias != 0, true, out step))
                        rule = 4;
                    else if (profile.bowlCreepChance > 0f && NextFloat() < profile.bowlCreepChance &&
                             TryCreep(x, y, preferred, bias != 0, profile.bowlCreepReach, true, out step))
                        rule = 5;
                    else
                    {
                        bowlPassStamp[i] = bowlPassFrame;
                        continue;
                    }

                    int target = i + step;
                    state.Cells[target] = state.Cells[i];
                    state.Shade[target] = state.Shade[i];
                    state.Velocity[target] = 0f;
                    state.Momentum[target] = 0f;
                    state.Cells[i] = 0;
                    bowlPassStamp[target] = bowlPassFrame;
                    moved++;
                    CountMove(rule, x, y, x + step, y);
                }
            }

            return moved;
        }

        /// <summary>
        /// Resting grain with blocked diagonals: find the nearest drop within 'dispersion' cells along the row
        /// (path must be free) and return a single-cell step toward it.
        /// </summary>
        private bool TryDisperse(int x, int y, int preferred, bool oneSided, bool bowlOnly, out int step)
        {
            int reach = profile.dispersion;
            int sides = oneSided ? 1 : 2;
            for (int distance = 2; distance <= reach; distance++)
            {
                for (int k = 0; k < sides; k++)
                {
                    int dir = k == 0 ? preferred : -preferred;
                    if (!PathFree(x, y, dir, distance)) continue;
                    if (bowlOnly && !IsBowlFlowCell(x + dir * distance, y)) continue;
                    if (IsFree(x + dir * distance, y - 1))
                    {
                        step = dir * (bowlOnly ? Math.Min(distance, Math.Max(1, profile.bowlLevelingCellsPerStep)) : 1);
                        return true;
                    }
                }
            }

            step = 0;
            return false;
        }

        /// <summary>
        /// Walks each direction once along the free row (up to creepReach) and returns a single-cell step toward the
        /// nearest cell with a free drop below. O(reach) per direction.
        /// </summary>
        private bool TryCreep(int x, int y, int preferred, bool oneSided, int reach, bool bowlOnly, out int step)
        {
            int best = int.MaxValue;
            int sides = oneSided ? 1 : 2;
            step = 0;
            for (int k = 0; k < sides; k++)
            {
                int dir = k == 0 ? preferred : -preferred;
                for (int distance = 1; distance <= reach && distance < best; distance++)
                {
                    int nx = x + dir * distance;
                    if (!IsFree(nx, y)) break;
                    if (bowlOnly && !IsBowlFlowCell(nx, y)) break;
                    if (distance >= 2 && IsFree(nx, y - 1))
                    {
                        best = distance;
                        step = dir * (bowlOnly ? Math.Min(distance, Math.Max(1, profile.bowlLevelingCellsPerStep)) : 1);
                        break;
                    }
                }
            }

            return step != 0;
        }

        /// <summary>Moves per rule in the last Step() and the last mover. Diagnostics only (debug-audit 2026-09-24).</summary>
        public SandStepStats LastStepStats;

        public struct SandStepStats
        {
            public int Fall, Roll, Slide, Disperse, Creep;
            public int LastRule, LastX, LastY, LastToX, LastToY;
        }

        private void CountMove(int rule, int x, int y, int toX, int toY)
        {
            if (rule == 1) LastStepStats.Fall++;
            else if (rule == 2) LastStepStats.Roll++;
            else if (rule == 3) LastStepStats.Slide++;
            else if (rule == 4) LastStepStats.Disperse++;
            else LastStepStats.Creep++;
            LastStepStats.LastRule = rule;
            LastStepStats.LastX = x;
            LastStepStats.LastY = y;
            LastStepStats.LastToX = toX;
            LastStepStats.LastToY = toY;
        }

        private bool PathFree(int x, int y, int dir, int distance)
        {
            for (int i = 1; i <= distance; i++)
                if (!IsFree(x + dir * i, y)) return false;
            return true;
        }

        private bool IsFree(int x, int y) => CanOccupy(x, y) && State.Cells[y * State.Width + x] == 0;

        private bool IsBowlFlowCell(int x, int y) => x >= 0 && y >= 0 && x < State.Width &&
            y < State.Height && State.BowlFlowMask[y * State.Width + x];

        public void Dispose()
        {
            disposed = true;
            stamp = null;
        }

        public void SetCupWall(int x, int y, bool value)
        {
            if (x >= 0 && y >= 0 && x < State.Width && y < State.Height) State.CupWallMask[State.Index(x, y)] = value;
        }

        /// <summary>Above 0.5 so the diagonal roll always fires toward the bias side when that side is free.</summary>
        private const float RimAssistMomentum = 0.6f;

        public void SetSurfaceBias(int x, int y, sbyte value)
        {
            if (x >= 0 && y >= 0 && x < State.Width && y < State.Height) State.SurfaceBias[State.Index(x, y)] = value;
        }

        public void SetBowlFlowCell(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < State.Width && y < State.Height)
            {
                State.BowlFlowMask[State.Index(x, y)] = true;
                if (x < bowlFlowMinX) bowlFlowMinX = x;
                if (x > bowlFlowMaxX) bowlFlowMaxX = x;
                if (y < bowlFlowMinY) bowlFlowMinY = y;
                if (y > bowlFlowMaxY) bowlFlowMaxY = y;
            }
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

        public bool TryRelocateGrain(int sourceIndex, int targetIndex, bool[] futureRotatingMask)
        {
            if (sourceIndex < 0 || sourceIndex >= State.Cells.Length || targetIndex < 0 || targetIndex >= State.Cells.Length ||
                State.Cells[sourceIndex] == 0 || State.Cells[targetIndex] != 0 || futureRotatingMask[targetIndex] ||
                !State.ValidMask[targetIndex] || State.StaticMask[targetIndex] || State.CupWallMask[targetIndex] ||
                State.DynamicMask[targetIndex]) return false;
            State.Cells[targetIndex] = State.Cells[sourceIndex];
            State.Shade[targetIndex] = State.Shade[sourceIndex];
            State.Velocity[targetIndex] = State.Velocity[sourceIndex];
            State.Momentum[targetIndex] = State.Momentum[sourceIndex];
            State.Cells[sourceIndex] = 0;
            State.Velocity[sourceIndex] = 0f;
            State.Momentum[sourceIndex] = 0f;
            int fromY = sourceIndex / State.Width;
            int toY = targetIndex / State.Width;
            State.RowCount[fromY]--;
            State.RowCount[toY]++;
            return true;
        }

        private bool CanOccupy(int x, int y)
        {
            if (x < 0 || y < 0 || x >= State.Width || y >= State.Height) return false;
            int i = y * State.Width + x;
            return State.ValidMask[i] && !State.StaticMask[i] && !State.CupWallMask[i] && !State.DynamicMask[i] && !State.RotatingMask[i];
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
