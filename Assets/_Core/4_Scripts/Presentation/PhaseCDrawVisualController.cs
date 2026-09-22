using System.Collections.Generic;
using SE001.Creation;
using SE001.Data;
using SE001.Gameplay;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCDrawVisualController : MonoBehaviour, ILevelLifecycleParticipant
    {
        private GameplayManager manager;
        private GameplayInputController input;
        private LevelContext context;
        private PhaseCDrawStrokeVisual preview;
        private DrawStrokeFactory factory;
        private PrefabProfile prefabs;
        private GameplayRuntimeProfile runtime;

        public void Bind(LevelContext value)
        {
            CleanupForLevelUnload();
            context = value;
            manager = GetComponent<GameplayManager>();
            input = GetComponent<GameplayInputController>();
            runtime = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            prefabs = runtime != null ? runtime.prefabProfile : null;
            factory = new DrawStrokeFactory();
            if (manager == null || input == null)
                throw new MissingComponentException(
                    "Draw visual controller requires GameplayManager and GameplayInputController.");
            preview = factory.CreatePreview(context.DynamicDrawRoot, prefabs, runtime);
            manager.StrokeCommitted += OnStrokeCommitted;
        }

        private void LateUpdate()
        {
            if (preview == null || input == null) return;
            Material material = runtime != null && runtime.visualMaterials != null
                ? runtime.visualMaterials.drawPathMaterial
                : null;
            preview.SetPreview(
                input.PreviewStroke,
                input.IsDrawing,
                input.DrawThickness,
                material);
        }

        private void OnStrokeCommitted(IList<Vector2> points, float thickness)
        {
            factory.Create(new DrawStrokeCreateParameters(points, thickness, context.DynamicDrawRoot, prefabs, runtime));
        }

        public void CleanupForLevelUnload()
        {
            if (context != null && context.DynamicDrawRoot != null)
            {
                PhaseCDrawStrokeVisual[] visuals =
                    context.DynamicDrawRoot.GetComponentsInChildren<PhaseCDrawStrokeVisual>();
                for (int i = 0; i < visuals.Length; i++)
                    visuals[i].ReleaseForUnload();
            }
            if (manager != null) manager.StrokeCommitted -= OnStrokeCommitted;
            manager = null;
            input = null;
            context = null;
            preview = null;
            factory = null;
            prefabs = null;
            runtime = null;
        }
    }
}
