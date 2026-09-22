namespace SE001.Simulation.Sand
{
    public sealed class SandSimulationState
    {
        internal SandSimulationState(int width, int height, byte[] cells, bool[] validMask, bool[] staticMask)
        {
            Width = width; Height = height; Cells = cells; ValidMask = validMask; StaticMask = staticMask;
        }
        public int Width { get; }
        public int Height { get; }
        public byte[] Cells { get; }
        public bool[] ValidMask { get; }
        public bool[] StaticMask { get; }
        public int OccupiedCount { get; internal set; }
        public int EmittedCount { get; internal set; }
        public int Index(int x, int y) => y * Width + x;
    }
}
