using System.Collections.Generic;
using SE001.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCDrawStrokeVisual : MonoBehaviour
    {
        public MeshFilter MeshFilter
        {
            get;
            private set;
        }

        public MeshRenderer MeshRenderer
        {
            get;
            private set;
        }
        public Mesh GeneratedMesh => generatedMesh;

        public void ReleaseForUnload()
        {
            DestroyGeneratedMesh();
        }

        [SerializeField] private Transform view;
        [SerializeField] private LineRenderer preview;
        [SerializeField] private float extrusionDepth = 0.18f;

        private Mesh generatedMesh;

        private void Awake()
        {
            if (view == null)
                view = transform.Find("View");
            if (preview == null)
                preview = transform.Find("Preview")?.GetComponent<LineRenderer>();
            MeshFilter = view != null ? view.GetComponent<MeshFilter>() : GetComponent<MeshFilter>();
            MeshRenderer = view != null
                ? view.GetComponent<MeshRenderer>()
                : GetComponent<MeshRenderer>();
        }

        public void SetPreview(
            IReadOnlyList<Vector2> points,
            bool visible,
            float thickness,
            Material material)
        {
            if (preview == null)
                throw new MissingComponentException(
                    "DrawStroke prefab requires Preview/LineRenderer.");

            preview.sharedMaterial = material;
            if (MeshRenderer != null)
                MeshRenderer.enabled = false;
            preview.widthMultiplier = thickness;
            preview.positionCount = visible ? points.Count : 0;
            for (int i = 0; i < points.Count && visible; i++)
            {
                preview.SetPosition(
                    i,
                    new Vector3(points[i].x, points[i].y, -0.25f));
            }
        }

        public void Rebuild(IList<Vector2> points, float thickness, Material material)
        {
            if (points == null || points.Count < 2)
                throw new global::System.ArgumentException(
                    "A committed stroke needs at least two points.");

            ResolveMeshView();
            List<CombineInstance> parts = new List<CombineInstance>(points.Count * 2);
            List<Vector2> polygon = new List<Vector2>(8);
            float half = thickness * 0.5f;

            for (int i = 0; i < points.Count; i++)
            {
                BuildJoint(points[i], half, polygon, parts);
                if (i == 0)
                    continue;

                Vector2 start = points[i - 1];
                Vector2 end = points[i];
                Vector2 direction = end - start;
                if (direction.sqrMagnitude < 1e-8f)
                    continue;

                BuildSegment(start, end, direction, half, polygon, parts);
            }

            Mesh next = new Mesh
            {
                name = "DrawStrokeMesh",
                indexFormat = IndexFormat.UInt32
            };
            next.CombineMeshes(parts.ToArray(), true, true);
            DestroyPartMeshes(parts);
            DestroyGeneratedMesh();

            generatedMesh = next;
            MeshFilter.sharedMesh = generatedMesh;
            MeshRenderer.sharedMaterial = material;
            MeshRenderer.enabled = true;
            if (preview != null)
                preview.gameObject.SetActive(false);
        }

        private void ResolveMeshView()
        {
            if (view == null)
                view = transform.Find("View");
            if (MeshFilter == null)
                MeshFilter = view != null
                    ? view.GetComponent<MeshFilter>()
                    : GetComponent<MeshFilter>();
            if (MeshRenderer == null)
                MeshRenderer = view != null
                    ? view.GetComponent<MeshRenderer>()
                    : GetComponent<MeshRenderer>();
            if (MeshFilter == null || MeshRenderer == null)
                throw new MissingComponentException(
                    "DrawStroke prefab requires View MeshFilter and MeshRenderer.");
        }

        private void BuildJoint(
            Vector2 point,
            float half,
            List<Vector2> polygon,
            List<CombineInstance> parts)
        {
            polygon.Clear();
            for (int k = 0; k < 8; k++)
            {
                float angle = k * Mathf.PI / 4f;
                polygon.Add(point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * half);
            }

            parts.Add(new CombineInstance
            {
                mesh = ExtrudedBevelMeshBuilder.Build(
                    polygon, extrusionDepth, 0f, 1, "StrokeJoint"),
                transform = Matrix4x4.identity
            });
        }

        private void BuildSegment(
            Vector2 start,
            Vector2 end,
            Vector2 direction,
            float half,
            List<Vector2> polygon,
            List<CombineInstance> parts)
        {
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * half;
            polygon.Clear();
            polygon.Add(start - normal);
            polygon.Add(end - normal);
            polygon.Add(end + normal);
            polygon.Add(start + normal);
            parts.Add(new CombineInstance
            {
                mesh = ExtrudedBevelMeshBuilder.Build(
                    polygon, extrusionDepth, 0f, 1, "StrokeSegment"),
                transform = Matrix4x4.identity
            });
        }

        private void DestroyPartMeshes(List<CombineInstance> parts)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].mesh != null)
                    DestroyOwned(parts[i].mesh);
            }
        }

        private void DestroyGeneratedMesh()
        {
            if (generatedMesh == null)
                return;
            if (MeshFilter != null && MeshFilter.sharedMesh == generatedMesh)
                MeshFilter.sharedMesh = null;
            DestroyOwned(generatedMesh);
            generatedMesh = null;
        }

        private static void DestroyOwned(Object target)
        {
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void OnDisable()
        {
            DestroyGeneratedMesh();
        }

        private void OnDestroy()
        {
            DestroyGeneratedMesh();
        }
    }
}
