namespace SE001.System.Management
{
    /// <summary>Shared input/HUD gate controlled exclusively by the level lifecycle owner.</summary>
    public sealed class LevelReadinessGate
    {
        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }
    }
}
