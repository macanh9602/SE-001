using System.Collections.Generic;
using SE001.Data;
using SE001.Elements.Layout;
using UnityEngine;

namespace SE001.Creation
{
    public sealed class LayoutVisualFactory
    {
        public GameObject Create(GameObject prefab, Transform parent, IList<Vector2> polygon, LayoutVisualProfile profile, bool wall)
        {
            if (prefab == null || parent == null || polygon == null || profile == null) throw new global::System.ArgumentNullException();
            GameObject instance = Object.Instantiate(prefab, parent, false);
            LayoutVisualView view = instance.GetComponent<LayoutVisualView>();
            if (view == null) { Object.Destroy(instance); throw new MissingComponentException("Layout prefab requires LayoutVisualView."); }
            view.Rebuild(polygon, profile, wall);
            return instance;
        }
    }
}
