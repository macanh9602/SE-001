using System;
using SE001.Data;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class DrawStrokeFactory
    {
        public PhaseCDrawStrokeVisual Create(DrawStrokeCreateParameters parameters)
        {
            if (parameters == null || parameters.Points == null || parameters.Parent == null)
                throw new ArgumentException("Draw stroke creation parameters are incomplete.");
            GameObject prefab = parameters.Prefabs != null ? parameters.Prefabs.drawStrokePrefab : null;
            if (prefab == null)
                throw new InvalidOperationException("PrefabProfile.drawStrokePrefab is required.");
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parameters.Parent, false);
            PhaseCDrawStrokeVisual visual = instance.GetComponent<PhaseCDrawStrokeVisual>();
            if (visual == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new MissingComponentException("DrawStroke prefab requires PhaseCDrawStrokeVisual.");
            }
            instance.name = "DrawStroke_" + parameters.Parent.childCount;
            Material material = parameters.Runtime != null && parameters.Runtime.visualMaterials != null
                ? parameters.Runtime.visualMaterials.drawPathMaterial
                : null;
            visual.Rebuild(parameters.Points, parameters.Thickness, material);
            return visual;
        }

        public PhaseCDrawStrokeVisual CreatePreview(Transform parent, PrefabProfile prefabs, GameplayRuntimeProfile runtime)
        {
            if (parent == null || prefabs == null || prefabs.drawStrokePrefab == null)
                throw new InvalidOperationException("Preview requires PrefabProfile.drawStrokePrefab.");
            PhaseCDrawStrokeVisual visual = UnityEngine.Object.Instantiate(prefabs.drawStrokePrefab, parent, false).GetComponent<PhaseCDrawStrokeVisual>();
            if (visual == null) throw new MissingComponentException("DrawStroke prefab requires PhaseCDrawStrokeVisual.");
            visual.name = "DrawPreview";
            return visual;
        }
    }
}
