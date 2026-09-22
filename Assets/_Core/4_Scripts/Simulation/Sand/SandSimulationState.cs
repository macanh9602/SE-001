namespace SE001.Simulation.Sand
{
    public sealed class SandSimulationState
    {
        internal SandSimulationState(int width, int height, byte[] cells, bool[] validMask, bool[] staticMask, bool[] cupWallMask, bool[] dynamicMask)
        {
            Width = width; Height = height; Cells = cells; ValidMask = validMask; StaticMask = staticMask; CupWallMask = cupWallMask; DynamicMask = dynamicMask;
        }
        public int Width { get; }
        public int Height { get; }
        public byte[] Cells { get; }
        public bool[] ValidMask { get; }
        public bool[] StaticMask { get; }
        public bool[] CupWallMask { get; }
        public bool[] DynamicMask { get; }
        public int OccupiedCount { get; internal set; }
        public int EmittedCount { get; internal set; }
        public int Index(int x, int y) => y * Width + x;
    }
}
