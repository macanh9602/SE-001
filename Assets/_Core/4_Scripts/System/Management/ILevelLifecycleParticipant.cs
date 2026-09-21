using SE001.Commons;

namespace SE001.System.Management
{
    public interface ILevelLifecycleParticipant : IPendingCleanup
    {
        void Bind(LevelContext context);
    }
}
