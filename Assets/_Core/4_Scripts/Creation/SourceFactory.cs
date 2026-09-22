using System;
using SE001.Data;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class SourceFactory
    {
        public PhaseCSourceVisual Create(SourceCreateParameters parameters)
        {
            if (parameters == null || parameters.Domain == null || parameters.Parent == null)
                throw new ArgumentException("Source creation parameters are incomplete.");
            GameObject prefab = parameters.Prefabs != null ? parameters.Prefabs.sourcePrefab : null;
            if (prefab == null)
                throw new InvalidOperationException("PrefabProfile.sourcePrefab is required.");
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parameters.Parent, false);
            PhaseCSourceVisual visual = instance.GetComponent<PhaseCSourceVisual>();
            if (visual == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new MissingComponentException("Source prefab requires PhaseCSourceVisual.");
            }
            instance.name = "SandSource_" + parameters.Domain.StableId;
            visual.Bind(
                parameters.Domain,
                parameters.Runtime != null ? parameters.Runtime.colorProfile : null,
                parameters.Runtime != null ? parameters.Runtime.visualMaterials : null,
                parameters.Runtime != null ? parameters.Runtime.juiceProfile : null);
            return visual;
        }
    }
}
