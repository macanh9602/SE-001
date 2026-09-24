using System;
using UnityEngine;
using SE001.Data;
using SE001.Simulation.Sand;

namespace SE001.Gameplay
{
    public enum SourceValveState { Closed, Opening, Open, Empty }

    /// <summary>Finite sand source with a tap-toggled valve. Semantic only: no renderer access.</summary>
    public sealed class SourceDomain
    {
        private readonly float openDelay;
        private readonly float grainsPerSecond;
        private readonly int streamWidthCells;
        private float openingTimer;
        private float emitAccumulator;

        public SourceDomain(SourceData data, SourceProfile profile, int grainsPerUnit, JarVisualProfile visualProfile = null,
            SE001LevelJson level = null)
        {
            StableId = data.stableId;
            MaterialId = (byte)data.materialId;
            Initial = Mathf.Max(0, data.logicalAmount) * Mathf.Max(1, grainsPerUnit);
            remaining = Initial;
            openDelay = profile.valveOpenDelay;
            // emissionRate is grains per 60 Hz step (sand-feel-lab 'rate'); converted to grains/second.
            grainsPerSecond = LevelTuning.EmissionRate(level, profile) * 60f;
            streamWidthCells = LevelTuning.StreamWidth(level, profile);
            Position = data.position;
            Size = LevelTuning.SourceSize(level, profile);
            BodyOffset = new Vector2(0f, JarVisualGeometry.SourceBodyOffsetY(Size.y, profile, visualProfile));
            state = remaining == 0 ? SourceValveState.Empty : data.startsOpen ? SourceValveState.Open : SourceValveState.Closed;
        }

        public string StableId { get; }
        public byte MaterialId { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public Vector2 BodyOffset { get; }
        public int Initial { get; }
        private int remaining;
        private SourceValveState state;
        public int Remaining => remaining;
        public SourceValveState State => state;
        public bool IsPouring => State == SourceValveState.Open || State == SourceValveState.Opening;

        /// <summary>Tap area: the visible jar body (not the emit point), grown by padding.</summary>
        public bool HitTest(Vector2 boardPoint, float padding)
        {
            Vector2 d = boardPoint - (Position + BodyOffset);
            return Mathf.Abs(d.x) <= Size.x * 0.5f + padding && Mathf.Abs(d.y) <= Size.y * 0.5f + padding;
        }

        public void Toggle()
        {
            switch (State)
            {
                case SourceValveState.Empty: return;
                case SourceValveState.Open:
                case SourceValveState.Opening:
                    state = SourceValveState.Closed;
                    openingTimer = 0f;
                    emitAccumulator = 0f;
                    return;
                default:
                    state = SourceValveState.Opening;
                    openingTimer = 0f;
                    return;
            }
        }

        /// <summary>Returns grains actually inserted. Blocked cells keep the amount for later steps.</summary>
        public int Emit(SandSimulation sim, float dt)
        {
            if (State == SourceValveState.Empty || State == SourceValveState.Closed) return 0;
            if (State == SourceValveState.Opening)
            {
                openingTimer += dt;
                if (openingTimer < openDelay) return 0;
                state = SourceValveState.Open;
            }

            emitAccumulator += grainsPerSecond * dt;
            int budget = Mathf.Min(Mathf.FloorToInt(emitAccumulator), Remaining);
            if (budget <= 0) return 0;

            int cx = Mathf.FloorToInt(Position.x / sim.CellSize);
            int cy = Mathf.FloorToInt(Position.y / sim.CellSize);
            int half = streamWidthCells / 2;
            int inserted = 0;
            // Two rows so the stream stays dense when the first row is still occupied.
            for (int row = 0; row < 2 && inserted < budget; row++)
                for (int x = cx - half; x <= cx + half && inserted < budget; x++)
                    if (sim.TryEmit(x, cy - row, MaterialId)) inserted++;

            emitAccumulator -= inserted;
            if (emitAccumulator > grainsPerSecond) emitAccumulator = grainsPerSecond; // do not bank unlimited backlog
            remaining -= inserted;
            if (remaining == 0) state = SourceValveState.Empty;
            return inserted;
        }
    }

    /// <summary>
    /// Receiver geometry from authored profile data only (never from the mesh).
    /// position = bottom-center, size = outer width (at the mouth) / height. Tapered bucket:
    /// bottom width = size.x * (1 - taper). Slanted side walls + bottom block sand; top open until full.
    /// Full = the authored logical target converted through SandSimulationProfile.grainsPerUnit.
    /// Geometry still determines the physical Capacity; authoring validation blocks targets above it.
    /// </summary>
    public sealed class CupDomain
    {
        private readonly ReceiverStyle receiverStyle;
        private readonly BowlVisualProfile bowlProfile;
        private readonly float taper;
        private readonly float fillLine;
        private float wall;
        private float geometryOuterWidth;
        private float geometryBottom;
        private int outerMinY, outerMaxY;
        private int[] rowMinX, rowMaxX; // sink span per row, index = y - MinY (min > max = empty row)

        public CupDomain(
            CupData data,
            CupProfile profile,
            int grainsPerUnit,
            float cellSize,
            JarVisualProfile visualProfile = null)
            : this(data, profile, grainsPerUnit, cellSize, visualProfile, ReceiverStyle.Cup, null)
        {
        }

        public CupDomain(
            CupData data,
            CupProfile profile,
            int grainsPerUnit,
            float cellSize,
            JarVisualProfile visualProfile,
            ReceiverStyle style,
            BowlVisualProfile valueBowlProfile,
            SE001LevelJson level = null)
        {
            StableId = data.stableId;
            AcceptedMaterialId = (byte)data.acceptedMaterialId;
            Position = data.position;
            receiverStyle = style;
            bowlProfile = valueBowlProfile;
            Size = style == ReceiverStyle.Bowl && valueBowlProfile != null
                ? LevelTuning.BowlSize(level, valueBowlProfile) : profile.bodySize;
            RequiredLogical = Mathf.Max(1, data.requiredAmount);
            taper = profile.taper;
            fillLine = profile.fillLine;
            if (style == ReceiverStyle.Bowl)
            {
                if (valueBowlProfile == null || !valueBowlProfile.IsBaked)
                    throw new InvalidOperationException("BowlVisualProfile must contain a baked Bowl contour.");
                BuildBowlCells(cellSize);
            }
            else BuildCells(profile.wallThickness, cellSize, visualProfile);
            long targetGrains = (long)RequiredLogical * Mathf.Max(1, grainsPerUnit);
            required = targetGrains > int.MaxValue ? int.MaxValue : (int)targetGrains;
        }

        public string StableId { get; }
        public byte AcceptedMaterialId { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public int RequiredLogical { get; }
        private int required;
        private int capacity;
        private int collected;
        private bool foreignDetected;
        private byte foreignMaterialId;
        private float fillLineY;
        private int minY;
        private int maxY;
        public int Required => required;
        public int Capacity => capacity;
        public int SafeCapacity => receiverStyle == ReceiverStyle.Bowl && bowlProfile != null
            ? Mathf.FloorToInt(capacity * Mathf.Clamp01(bowlProfile.fullFillFraction))
            : capacity;
        public int Collected => collected;
        public bool Full => Collected >= Required;
        public bool ForeignDetected => foreignDetected;
        public byte ForeignMaterialId => foreignMaterialId;
        public float FillRatio => Mathf.Clamp01(Collected / (float)Required);
        public int CollectedLogical => Mathf.Min(RequiredLogical, Mathf.FloorToInt(FillRatio * RequiredLogical + 0.0001f));
        /// <summary>Wall thickness actually used (board units), shared by mask and visuals.</summary>
        public float EffectiveWall => wall;
        /// <summary>Playable sand width, aligned to the authored cup body inner area.</summary>
        public float EffectiveInnerWidth => Mathf.Max(0f, geometryOuterWidth - wall * 2f);
        public float EffectiveBottomY => geometryBottom;
        public float EffectiveSandBottomY => geometryBottom + wall;
        public float Taper => receiverStyle == ReceiverStyle.Bowl ? 0f : taper;
        public float FillLineY => fillLineY;
        public int MinY => minY;
        public int MaxY => maxY;
        public ReceiverStyle Style => receiverStyle;

        private float OuterHalfWidthAt(float y)
        {
            float geometryHeight = Mathf.Max(0.001f, Position.y + Size.y - geometryBottom);
            float t = Mathf.Clamp01((y - geometryBottom) / geometryHeight);
            float outerWidth = geometryOuterWidth > 0f ? geometryOuterWidth : Size.x;
            return Mathf.Lerp(outerWidth * (1f - taper) * 0.5f, outerWidth * 0.5f, t);
        }

        private void BuildCells(float wallThickness, float cell, JarVisualProfile visualProfile)
        {
            wall = Mathf.Max(wallThickness, cell * 2.5f); // >= 2 cells everywhere: no diagonal leaks through slanted walls
            float visualInnerWidth = visualProfile != null
                ? Size.x * visualProfile.CupBodyInnerWidthPixels /
                    Mathf.Max(0.001f, visualProfile.cupHeadWidthPixels)
                : Size.x - wall * 2f;
            geometryOuterWidth = visualProfile != null
                ? visualInnerWidth + wall * 2f
                : Size.x;
            geometryBottom = Position.y + (visualProfile != null
                ? visualProfile.CupBodyBottomOffset(Size.x) +
                    visualProfile.CupSandBottomOffset(Size.x) - wall
                : 0f);
            outerMinY = Mathf.FloorToInt(geometryBottom / cell);
            outerMaxY = Mathf.CeilToInt((Position.y + Size.y) / cell) - 1;
            // First row whose cell center is above the bottom wall (must match RegisterWalls' 'bottom' test exactly).
            minY = Mathf.CeilToInt((geometryBottom + wall) / cell - 0.5f);
            maxY = outerMaxY;
            int rows = Mathf.Max(0, maxY - minY + 1);
            rowMinX = new int[rows];
            rowMaxX = new int[rows];
            float interiorTop = Position.y + Size.y;
            fillLineY = geometryBottom + wall + (interiorTop - geometryBottom - wall) * fillLine;
            capacity = 0;
            for (int r = 0; r < rows; r++)
            {
                int y = minY + r;
                float yc = (y + 0.5f) * cell;
                float inner = OuterHalfWidthAt(yc) - wall;
                rowMinX[r] = Mathf.CeilToInt((Position.x - inner) / cell - 0.5f);
                rowMaxX[r] = Mathf.FloorToInt((Position.x + inner) / cell - 0.5f);
                int span = Mathf.Max(0, rowMaxX[r] - rowMinX[r] + 1);
                capacity += span;
            }
        }

        private void BuildBowlCells(float cell)
        {
            float pixelsToWorld = bowlProfile.WorldPixelsPerPixel(Size.x);
            wall = Mathf.Max(bowlProfile.wallThicknessPixels * pixelsToWorld, cell * 1.5f);
            geometryOuterWidth = Size.x;
            geometryBottom = Position.y;
            outerMinY = Mathf.FloorToInt(geometryBottom / cell);
            outerMaxY = Mathf.CeilToInt((Position.y + Size.y) / cell) - 1;

            float interiorTop = Position.y + bowlProfile.rimPixelY * pixelsToWorld;
            minY = Mathf.CeilToInt((geometryBottom + wall) / cell - 0.5f);
            maxY = Mathf.CeilToInt(interiorTop / cell) - 1;
            int rows = Mathf.Max(0, maxY - minY + 1);
            rowMinX = new int[rows];
            rowMaxX = new int[rows];
            fillLineY = interiorTop;
            capacity = 0;

            for (int r = 0; r < rows; r++)
                capacity += BowlSinkRow(bowlProfile, Position, Size.x, cell, minY + r, out rowMinX[r], out rowMaxX[r]);
        }

        /// <summary>
        /// Grains per logical unit. Bowl: measured Bowl capacity x fullFillFraction / unitsPerFullBowl at the current
        /// profile width, so "units to fill a Bowl" is a GD knob and exact at any width (decision 2026-09-24).
        /// Cup: the SandSimulationProfile value, unchanged.
        /// </summary>
        public static int GrainsPerUnitFor(int baseGrainsPerUnit, ReceiverStyle style, BowlVisualProfile bowl, float cell)
        {
            int fallback = Mathf.Max(1, baseGrainsPerUnit);
            if (style != ReceiverStyle.Bowl || bowl == null) return fallback;
            int capacity = MeasureBowlCapacity(bowl, cell);
            if (capacity <= 0) return fallback;
            float full = capacity * Mathf.Clamp01(bowl.fullFillFraction) / Mathf.Max(0.1f, bowl.unitsPerFullBowl);
            return Mathf.Max(1, Mathf.FloorToInt(full));
        }

        /// <summary>Canonical Bowl capacity in grains (Bowl at the origin), on the same sink rows the game collects.</summary>
        public static int MeasureBowlCapacity(BowlVisualProfile bowl, float cell)
        {
            if (bowl == null || !bowl.IsBaked || cell <= 0f) return 0;
            float width = bowl.defaultWorldWidth;
            float pixelsToWorld = bowl.WorldPixelsPerPixel(width);
            float wallWorld = Mathf.Max(bowl.wallThicknessPixels * pixelsToWorld, cell * 1.5f);
            int first = Mathf.CeilToInt(wallWorld / cell - 0.5f);
            int last = Mathf.CeilToInt(bowl.rimPixelY * pixelsToWorld / cell) - 1;
            int total = 0;
            for (int y = first; y <= last; y++)
                total += BowlSinkRow(bowl, Vector2.zero, width, cell, y, out _, out _);
            return total;
        }

        private static int BowlSinkRow(BowlVisualProfile bowl, Vector2 position, float width, float cell, int y,
            out int minX, out int maxX)
        {
            minX = 1;
            maxX = 0;
            float pixelsToWorld = bowl.WorldPixelsPerPixel(width);
            float localY = (y + 0.5f) * cell - position.y;
            int pixelY = bowl.PixelRowForLocalY(localY, width);
            BowlRowSpan span;
            if (pixelY >= bowl.rimPixelY || !bowl.TryGetInnerSpan(pixelY, out span)) return 0;
            minX = PixelToCellMin(bowl, position.x, span.minX, pixelsToWorld, cell);
            maxX = PixelToCellMax(bowl, position.x, span.maxX, pixelsToWorld, cell);
            return Mathf.Max(0, maxX - minX + 1);
        }

        private static int PixelToCellMin(BowlVisualProfile bowl, float x, float pixel, float pixelsToWorld, float cell)
        {
            float world = x + (pixel - bowl.mainWidthPixels * 0.5f) * pixelsToWorld;
            return Mathf.CeilToInt(world / cell - 0.5f);
        }

        private static int PixelToCellMax(BowlVisualProfile bowl, float x, float pixel, float pixelsToWorld, float cell)
        {
            float world = x + (pixel + 1f - bowl.mainWidthPixels * 0.5f) * pixelsToWorld;
            return Mathf.FloorToInt(world / cell - 0.5f);
        }

        public void RegisterWalls(SandSimulation sim, float cell, float wallThickness)
        {
            if (receiverStyle == ReceiverStyle.Bowl)
            {
                RegisterBowlWalls(sim, cell);
                return;
            }

            for (int y = outerMinY; y <= outerMaxY; y++)
            {
                float yc = (y + 0.5f) * cell;
                float outer = OuterHalfWidthAt(yc);
                int x0 = Mathf.FloorToInt((Position.x - outer) / cell);
                int x1 = Mathf.CeilToInt((Position.x + outer) / cell) - 1;
                bool bottom = yc < geometryBottom + wall;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) * cell - Position.x);
                    if (dx > outer) continue;
                    if (bottom || dx > outer - wall) sim.SetCupWall(x, y, true);
                }
            }
        }

        private void RegisterBowlWalls(SandSimulation sim, float cell)
        {
            float pixelsToWorld = bowlProfile.WorldPixelsPerPixel(Size.x);
            for (int y = outerMinY; y <= outerMaxY; y++)
            {
                float localY = (y + 0.5f) * cell - Position.y;
                int pixelY = bowlProfile.PixelRowForLocalY(localY, Size.x);
                BowlRowSpan outer;
                if (!bowlProfile.TryGetOuterSpan(pixelY, out outer)) continue;
                int x0 = PixelToCellMin(bowlProfile, Position.x, outer.minX, pixelsToWorld, cell);
                int x1 = PixelToCellMax(bowlProfile, Position.x, outer.maxX, pixelsToWorld, cell);
                BowlRowSpan inner;
                bool hasInner = bowlProfile.TryGetInnerSpan(pixelY, out inner);
                int innerMin = hasInner ? PixelToCellMin(bowlProfile, Position.x, inner.minX, pixelsToWorld, cell) : 1;
                int innerMax = hasInner ? PixelToCellMax(bowlProfile, Position.x, inner.maxX, pixelsToWorld, cell) : 0;
                for (int x = x0; x <= x1; x++)
                    if (x < innerMin || x > innerMax) sim.SetCupWall(x, y, true);
            }

            SealBowlInterior(sim);
            if (bowlProfile.rimAssist) MarkRimAssist(sim, cell);
        }

        /// <summary>
        /// Movie_013: a stream landing on the lip split both ways and half fell outside. Top-surface wall cells of the
        /// lip band steer resting grains toward the Bowl centre, so sand that touches the rim goes in.
        /// </summary>
        private void MarkRimAssist(SandSimulation sim, float cell)
        {
            SandSimulationState state = sim.State;
            int centerX = Mathf.FloorToInt(Position.x / cell);
            int fromY = Mathf.Max(0, maxY - 1);
            BuildCatchShelf(sim, cell, centerX, fromY);
            for (int y = fromY; y <= outerMaxY && y < state.Height - 1; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    int i = state.Index(x, y);
                    if (!state.CupWallMask[i] || state.CupWallMask[i + state.Width]) continue;
                    if (Mathf.Abs(x - centerX) * cell > Size.x * 0.5f + cell * (2 + bowlProfile.rimCatchCells)) continue;
                    sim.SetSurfaceBias(x, y, (sbyte)(x < centerX ? 1 : -1));
                }
            }
        }

        /// <summary>
        /// Movie_014: a stream grazing the outside of the rounded lip fell past the Bowl. The top wall row becomes a
        /// flat shelf from (outermost lip wall - rimCatchCells) to the inner edge of the top-row wall, on both sides.
        /// MarkRimAssist then biases the shelf inward, so a grain landing on it slides into the opening.
        /// </summary>
        private void BuildCatchShelf(SandSimulation sim, float cell, int centerX, int fromY)
        {
            SandSimulationState state = sim.State;
            int halfCells = Mathf.CeilToInt(Size.x * 0.5f / cell) + 2;
            int minX = Mathf.Max(0, centerX - halfCells);
            int maxX = Mathf.Min(state.Width - 1, centerX + halfCells);
            int outerLeft = int.MaxValue;
            int outerRight = int.MinValue;
            int top = -1;
            for (int y = fromY; y <= outerMaxY && y < state.Height; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!state.CupWallMask[state.Index(x, y)]) continue;
                    outerLeft = Mathf.Min(outerLeft, x);
                    outerRight = Mathf.Max(outerRight, x);
                    top = Mathf.Max(top, y);
                }
            }

            // Never lid the opening: the top row must be open at the Bowl centre.
            if (top < 0 || state.CupWallMask[state.Index(centerX, top)]) return;
            int leftWallEnd = -1;
            for (int x = minX; x < centerX; x++)
                if (state.CupWallMask[state.Index(x, top)]) leftWallEnd = x;
            int rightWallStart = -1;
            for (int x = maxX; x > centerX; x--)
                if (state.CupWallMask[state.Index(x, top)]) rightWallStart = x;
            int catchCells = Mathf.Max(0, bowlProfile.rimCatchCells);
            if (leftWallEnd >= 0)
                for (int x = Mathf.Max(0, outerLeft - catchCells); x <= leftWallEnd; x++) sim.SetCupWall(x, top, true);
            if (rightWallStart >= 0)
                for (int x = rightWallStart; x <= Mathf.Min(state.Width - 1, outerRight + catchCells); x++) sim.SetCupWall(x, top, true);
        }

        /// <summary>
        /// Leak guard (Movie_011): at small Bowl widths the glass is ~1 cell thick, so the sampled staircase can leave
        /// diagonal or missing wall cells. Every 8-neighbour of a sink cell that is not itself a sink cell becomes wall,
        /// except upward into a row with no sink (the mouth stays open). A grain inside can then only leave over the rim.
        /// </summary>
        private void SealBowlInterior(SandSimulation sim)
        {
            for (int r = 0; r < rowMinX.Length; r++)
            {
                int y = minY + r;
                for (int x = rowMinX[r]; x <= rowMaxX[r]; x++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        // Open mouth: never cap a sink cell from above with a row that holds no sink (rim rows sampled
                        // at pixelY >= rimPixelY are empty). Capping it made a lid at width 4 (sand piled on the rim).
                        if (ny > maxY || (dy > 0 && !RowHasSink(ny))) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if ((dx != 0 || dy != 0) && !IsSinkCell(x + dx, ny)) sim.SetCupWall(x + dx, ny, true);
                        }
                    }
                }
            }
        }

        private bool RowHasSink(int y)
        {
            int r = y - minY;
            return r >= 0 && r < rowMinX.Length && rowMinX[r] <= rowMaxX[r];
        }

        private bool IsSinkCell(int x, int y)
        {
            int r = y - minY;
            if (r < 0 || r >= rowMinX.Length) return false;
            return x >= rowMinX[r] && x <= rowMaxX[r];
        }

        /// <summary>
        /// Sand physically fills the cup: grains stay in the simulation and pile inside the walls.
        /// Collected = accepted grains currently inside the sink. Returns the change since last call.
        /// </summary>
        public int Collect(SandSimulation sim)
        {
            if (Full || foreignDetected) return 0;
            int count = 0;
            for (int r = 0; r < rowMinX.Length; r++)
            {
                int y = minY + r;
                for (int x = rowMinX[r]; x <= rowMaxX[r]; x++)
                {
                    if (!sim.IsOccupied(x, y)) continue;
                    byte material = sim.State.Cells[sim.State.Index(x, y)];
                    if (material != AcceptedMaterialId)
                    {
                        foreignDetected = true;
                        foreignMaterialId = material;
                        return 0;
                    }

                    count++;
                }
            }

            int delta = count - collected;
            collected = Mathf.Min(count, required);
            // Bowl keeps its mouth open when full (decision 2026-09-24): extra sand fills the pockets and spills over
            // the lip instead of resting on an invisible lid above a gap. Counting still stops at Full.
            if (Full && receiverStyle != ReceiverStyle.Bowl) CloseMouth(sim);
            return delta;
        }

        /// <summary>Full cup: the open top row becomes wall so further sand piles on top.</summary>
        private void CloseMouth(SandSimulation sim)
        {
            int r = rowMinX.Length - 1;
            if (r < 0) return;
            for (int x = rowMinX[r]; x <= rowMaxX[r]; x++) sim.SetCupWall(x, maxY, true);
        }
    }
}
