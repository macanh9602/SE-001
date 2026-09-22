using System;
using SE001.Data;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class CupFactory
    {
        public PhaseCCupVisual Create(CupCreateParameters parameters)
        {
            if (parameters == null || parameters.Domain == null || parameters.Parent == null)
                throw new ArgumentException("Cup creation parameters are incomplete.");
            GameObject prefab = parameters.Prefabs != null ? parameters.Prefabs.cupPrefab : null;
            if (prefab == null)
                throw new InvalidOperationException("PrefabProfile.cupPrefab is required.");
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parameters.Parent, false);
            PhaseCCupVisual visual = instance.GetComponent<PhaseCCupVisual>();
            if (visual == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new MissingComponentException("Cup prefab requires PhaseCCupVisual.");
            }
            instance.name = "Cup_" + parameters.Domain.StableId;
            visual.Bind(
                parameters.Domain,
                parameters.Runtime != null ? parameters.Runtime.materialPalette : null,
                parameters.Runtime != null ? parameters.Runtime.visualMaterials : null);
            return visual;
        }
    }
}
