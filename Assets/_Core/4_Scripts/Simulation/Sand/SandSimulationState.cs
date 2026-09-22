namespace SE001.Simulation.Sand
{
    /// <summary>
    /// Authoritative sand grid (structure of arrays, allocated once per level).
    /// Cells = material id (0 = empty). Shade/Velocity/Momentum are per-grain feel data that move with the grain.
    /// </summary>
    public sealed class SandSimulationState
    {
        internal SandSimulationState(int width, int height, byte[] cells, bool[] validMask, bool[] staticMask, bool[] cupWallMask, bool[] dynamicMask)
        {
            Width = width;
            Height = height;
            Cells = cells;
            ValidMask = validMask;
            StaticMask = staticMask;
            CupWallMask = cupWallMask;
            DynamicMask = dynamicMask;
            int count = width * height;
            Shade = new byte[count];
            Velocity = new float[count];
            Momentum = new float[count];
            RowCount = new int[height];
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Cells { get; }
        /// <summary>Per-grain random tone (0..255) assigned at emission; used by the renderer for texture.</summary>
        public byte[] Shade { get; }
        /// <summary>Per-grain fall speed in cells/step.</summary>
        public float[] Velocity { get; }
        /// <summary>Per-grain horizontal momentum (signed, cells/step-ish).</summary>
        public float[] Momentum { get; }
        /// <summary>Occupied cells per row; rows with 0 are skipped by Step.</summary>
        public int[] RowCount { get; }
        public bool[] ValidMask { get; }
        public bool[] StaticMask { get; }
        public bool[] CupWallMask { get; }
        public bool[] DynamicMask { get; }
        public int OccupiedCount { get; internal set; }
        public int EmittedCount { get; internal set; }
        public int Index(int x, int y) => y * Width + x;
    }
}
