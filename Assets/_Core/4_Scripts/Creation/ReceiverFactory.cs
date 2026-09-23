using System;
using SE001.Data;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class ReceiverFactory
    {
        private readonly CupFactory cupFactory = new CupFactory();

        public MonoBehaviour Create(CupCreateParameters parameters)
        {
            if (parameters == null || parameters.Domain == null || parameters.Parent == null)
                throw new ArgumentException("Receiver creation parameters are incomplete.");

            if (parameters.Runtime == null || parameters.Runtime.receiverStyle == ReceiverStyle.Cup)
                return cupFactory.Create(parameters);

            GameObject prefab = parameters.Prefabs != null ? parameters.Prefabs.bowlPrefab : null;
            if (prefab == null)
                throw new InvalidOperationException("PrefabProfile.bowlPrefab is required when Receiver Style is Bowl.");
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parameters.Parent, false);
            BowlVisual visual = instance.GetComponent<BowlVisual>();
            if (visual == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new MissingComponentException("Bowl prefab requires BowlVisual.");
            }

            instance.name = "Bowl_" + parameters.Domain.StableId;
            visual.Bind(
                parameters.Domain,
                parameters.Runtime.colorProfile,
                parameters.Runtime.visualMaterials,
                parameters.Runtime.bowlVisualProfile);
            return visual;
        }
    }
}
