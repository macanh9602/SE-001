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

        public SourceDomain(SourceData data, SourceProfile profile, int grainsPerUnit)
        {
            StableId = data.stableId;
            MaterialId = (byte)data.materialId;
            Initial = Mathf.Max(0, data.logicalAmount) * Mathf.Max(1, grainsPerUnit);
            Remaining = Initial;
            openDelay = profile.valveOpenDelay;
            // emissionRate is grains per 60 Hz step (sand-feel-lab 'rate'); converted to grains/second.
            grainsPerSecond = (data.emissionRate > 0f ? data.emissionRate : profile.emissionRate) * 60f;
            streamWidthCells = Mathf.Max(1, Mathf.RoundToInt(data.streamWidth > 0f ? data.streamWidth : profile.streamWidth));
            Position = data.position;
            Size = data.size.x > 0f && data.size.y > 0f ? data.size : profile.bodySize;
            BodyOffset = new Vector2(0f, Size.y * 0.5f);
            State = Remaining == 0 ? SourceValveState.Empty : data.startsOpen ? SourceValveState.Open : SourceValveState.Closed;
        }

        public string StableId { get; }
        public byte MaterialId { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public Vector2 BodyOffset { get; }
        public int Initial { get; }
        public int Remaining { get; private set; }
        public SourceValveState State { get; private set; }
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
                    State = SourceValveState.Closed;
                    openingTimer = 0f;
                    emitAccumulator = 0f;
                    return;
                default:
                    State = SourceValveState.Opening;
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
                State = SourceValveState.Open;
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
            Remaining -= inserted;
            if (Remaining == 0) State = SourceValveState.Empty;
            return inserted;
        }
    }

    /// <summary>
    /// Cup geometry from CupData + CupProfile only (never from the mesh).
    /// position = bottom-center, size = outer width (at the mouth) / height. Tapered bucket:
    /// bottom width = size.x * (1 - taper). Slanted side walls + bottom block sand; top open until full.
    /// Full = sand volume reaches the fill line (fillLine * interior height) → Required grains derive
    /// from geometry; GD requiredAmount is the displayed logical target.
    /// </summary>
    public sealed class CupDomain
    {
        private readonly float taper;
        private readonly float fillLine;
        private float wall;
        private int outerMinY, outerMaxY;
        private int[] rowMinX, rowMaxX; // sink span per row, index = y - MinY (min > max = empty row)

        public CupDomain(CupData data, CupProfile profile, int grainsPerUnit, float cellSize)
        {
            StableId = data.stableId;
            AcceptedMaterialId = (byte)data.acceptedMaterialId;
            Position = data.position;
            Size = data.size;
            RequiredLogical = Mathf.Max(1, data.requiredAmount);
            taper = profile.taper;
            fillLine = profile.fillLine;
            BuildCells(profile.wallThickness, cellSize);
        }

        public string StableId { get; }
        public byte AcceptedMaterialId { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public int RequiredLogical { get; }
        public int Required { get; private set; }
        public int Capacity { get; private set; }
        public int Collected { get; private set; }
        public bool Full => Collected >= Required;
        public bool ForeignDetected { get; private set; }
        public byte ForeignMaterialId { get; private set; }
        public float FillRatio => Mathf.Clamp01(Collected / (float)Required);
        public int CollectedLogical => Mathf.Min(RequiredLogical, Mathf.FloorToInt(FillRatio * RequiredLogical + 0.0001f));
        /// <summary>Wall thickness actually used (board units), shared by mask and visuals.</summary>
        public float EffectiveWall => wall;
        public float Taper => taper;
        public float FillLineY { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        private float OuterHalfWidthAt(float y)
        {
            float t = Mathf.Clamp01((y - Position.y) / Size.y);
            return Mathf.Lerp(Size.x * (1f - taper) * 0.5f, Size.x * 0.5f, t);
        }

        private void BuildCells(float wallThickness, float cell)
        {
            wall = Mathf.Max(wallThickness, cell * 2.5f); // >= 2 cells everywhere: no diagonal leaks through slanted walls
            outerMinY = Mathf.FloorToInt(Position.y / cell);
            outerMaxY = Mathf.CeilToInt((Position.y + Size.y) / cell) - 1;
            // First row whose cell center is above the bottom wall (must match RegisterWalls' 'bottom' test exactly).
            MinY = Mathf.CeilToInt((Position.y + wall) / cell - 0.5f);
            MaxY = outerMaxY;
            int rows = Mathf.Max(0, MaxY - MinY + 1);
            rowMinX = new int[rows];
            rowMaxX = new int[rows];
            float interiorTop = Position.y + Size.y;
            FillLineY = Position.y + wall + (interiorTop - Position.y - wall) * fillLine;
            Capacity = 0;
            Required = 0;
            for (int r = 0; r < rows; r++)
            {
                int y = MinY + r;
                float yc = (y + 0.5f) * cell;
                float inner = OuterHalfWidthAt(yc) - wall;
                rowMinX[r] = Mathf.CeilToInt((Position.x - inner) / cell - 0.5f);
                rowMaxX[r] = Mathf.FloorToInt((Position.x + inner) / cell - 0.5f);
                int span = Mathf.Max(0, rowMaxX[r] - rowMinX[r] + 1);
                Capacity += span;
                if (yc <= FillLineY) Required += span;
            }

            Required = Mathf.Max(1, Required);
        }

        public void RegisterWalls(SandSimulation sim, float cell, float wallThickness)
        {
            for (int y = outerMinY; y <= outerMaxY; y++)
            {
                float yc = (y + 0.5f) * cell;
                float outer = OuterHalfWidthAt(yc);
                int x0 = Mathf.FloorToInt((Position.x - outer) / cell);
                int x1 = Mathf.CeilToInt((Position.x + outer) / cell) - 1;
                bool bottom = yc < Position.y + wall;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) * cell - Position.x);
                    if (dx > outer) continue;
                    if (bottom || dx > outer - wall) sim.SetCupWall(x, y, true);
                }
            }
        }

        /// <summary>
        /// Sand physically fills the cup: grains stay in the simulation and pile inside the walls.
        /// Collected = accepted grains currently inside the sink. Returns the change since last call.
        /// </summary>
        public int Collect(SandSimulation sim)
        {
            if (Full || ForeignDetected) return 0;
            int count = 0;
            for (int r = 0; r < rowMinX.Length; r++)
            {
                int y = MinY + r;
                for (int x = rowMinX[r]; x <= rowMaxX[r]; x++)
                {
                    if (!sim.IsOccupied(x, y)) continue;
                    byte material = sim.State.Cells[sim.State.Index(x, y)];
                    if (material != AcceptedMaterialId)
                    {
                        ForeignDetected = true;
                        ForeignMaterialId = material;
                        return 0;
                    }

                    count++;
                }
            }

            int delta = count - Collected;
            Collected = Mathf.Min(count, Required);
            if (Full) CloseMouth(sim);
            return delta;
        }

        /// <summary>Full cup: the open top row becomes wall so further sand piles on top.</summary>
        private void CloseMouth(SandSimulation sim)
        {
            int r = rowMinX.Length - 1;
            if (r < 0) return;
            for (int x = rowMinX[r]; x <= rowMaxX[r]; x++) sim.SetCupWall(x, MaxY, true);
        }
    }
}
