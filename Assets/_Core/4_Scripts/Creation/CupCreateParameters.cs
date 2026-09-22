using SE001.Commons;
using SE001.Data;
using SE001.Gameplay;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class CupCreateParameters : ICreateParameters
    {
        public CupCreateParameters(CupDomain domain, Transform parent, PrefabProfile prefabs, GameplayRuntimeProfile runtime)
        {
            Domain = domain;
            Parent = parent;
            Prefabs = prefabs;
            Runtime = runtime;
        }
        public CupDomain Domain { get; }
        public Transform Parent { get; }
        public PrefabProfile Prefabs { get; }
        public GameplayRuntimeProfile Runtime { get; }
    }
}
