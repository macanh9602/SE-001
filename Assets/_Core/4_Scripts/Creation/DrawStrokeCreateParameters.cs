using System.Collections.Generic;
using SE001.Commons;
using SE001.Data;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class DrawStrokeCreateParameters : ICreateParameters
    {
        public DrawStrokeCreateParameters(IList<Vector2> points, float thickness, Transform parent, PrefabProfile prefabs, GameplayRuntimeProfile runtime)
        {
            Points = points;
            Thickness = thickness;
            Parent = parent;
            Prefabs = prefabs;
            Runtime = runtime;
        }
        public IList<Vector2> Points { get; }
        public float Thickness { get; }
        public Transform Parent { get; }
        public PrefabProfile Prefabs { get; }
        public GameplayRuntimeProfile Runtime { get; }
    }
}
