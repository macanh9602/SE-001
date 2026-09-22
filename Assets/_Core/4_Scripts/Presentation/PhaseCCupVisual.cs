using System.Collections.Generic;
using SE001.Data;
using SE001.Gameplay;
using SE001.Geometry;
using UnityEngine;

namespace SE001.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PhaseCCupVisual : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private Transform fillView;
        [SerializeField] private float extrusionDepth = 0.25f;
        [SerializeField] private float fillThickness = 0.04f;
        [SerializeField] private float rimWidth = 0.06f;
        [SerializeField] private int rimSegments = 32;
        [SerializeField] private float rimDepth = -0.27f;

        private readonly List<Mesh> generatedMeshes = new List<Mesh>();
        private CupDomain cup;
        private ColorProfile palette;
        private PhaseCVisualMaterials materials;
        private MaterialPropertyBlock block;
        private MeshFilter fillFilter;
        private MeshRenderer fillRenderer;
        private float shownFill;

        public CupDomain Domain => cup;
        public Transform FillView => fillView;

        public void ReleaseForUnload()
        {
            DestroyGeneratedMeshes();
        }

        public void Bind(CupDomain value, ColorProfile valuePalette, PhaseCVisualMaterials valueMaterials)
        {
            cup = value ?? throw new global::System.ArgumentNullException(nameof(value));
            palette = valuePalette;
            materials = valueMaterials;
            block ??= new MaterialPropertyBlock();
            ResolveAuthoredChildren();
            DestroyGeneratedMeshes();
            BuildGeometryInPlace();
        }

        private void ResolveAuthoredChildren()
        {
            if (view == null) view = transform.Find("View");
            if (fillView == null) fillView = transform.Find("FillView");
            if (view == null || fillView == null)
                throw new MissingComponentException("Cup prefab requires authored View and FillView children.");

            RequireMeshFilter("WallL");
            RequireMeshFilter("WallR");
            RequireMeshFilter("WallB");
            RequireMeshFilter("Back");
            RequireMeshFilter("FillLine");
            RequireLineRenderer("Rim");
            fillFilter = fillView.GetComponent<MeshFilter>();
            fillRenderer = fillView.GetComponent<MeshRenderer>();
            if (fillFilter == null || fillRenderer == null)
                throw new MissingComponentException("Cup prefab FillView requires MeshFilter and MeshRenderer.");
        }

        private void LateUpdate()
        {
            if (cup == null || fillView == null) return;
            shownFill = Mathf.Lerp(shownFill, cup.FillRatio, 1f - Mathf.Exp(-10f * Time.deltaTime));
            fillView.localScale = new Vector3(Mathf.Max(0.0001f, shownFill), 1f, 1f);
            Color color = GetMaterialColor(cup.AcceptedMaterialId);
            if (cup.ForeignDetected) color = Color.Lerp(color, Color.red, Mathf.PingPong(Time.time * 8f, 1f));
            block.SetColor("_BaseColor", color);
            fillRenderer.SetPropertyBlock(block);
        }

        private void BuildGeometryInPlace()
        {
            float px = cup.Position.x;
            float y0 = cup.Position.y;
            float y1 = y0 + cup.Size.y;
            float topHalf = cup.Size.x * 0.5f;
            float bottomHalf = cup.Size.x * (1f - cup.Taper) * 0.5f;
            float wall = cup.EffectiveWall;
            Material wallMaterial = materials != null ? materials.cupMaterial : null;
            Material backMaterial = materials != null && materials.cupBackMaterial != null
                ? materials.cupBackMaterial
                : wallMaterial;

            SetExtruded("WallL", new List<Vector2>
            {
                new Vector2(px - bottomHalf, y0),
                new Vector2(px - bottomHalf + wall, y0),
                new Vector2(px - topHalf + wall, y1),
                new Vector2(px - topHalf, y1)
            }, wallMaterial);
            SetExtruded("WallR", new List<Vector2>
            {
                new Vector2(px + bottomHalf - wall, y0),
                new Vector2(px + bottomHalf, y0),
                new Vector2(px + topHalf, y1),
                new Vector2(px + topHalf - wall, y1)
            }, wallMaterial);
            SetExtruded("WallB", new List<Vector2>
            {
                new Vector2(px - bottomHalf, y0),
                new Vector2(px + bottomHalf, y0),
                new Vector2(px + bottomHalf - wall * 0.2f, y0 + wall),
                new Vector2(px - bottomHalf + wall * 0.2f, y0 + wall)
            }, wallMaterial);
            SetFlat("Back", new[]
            {
                new Vector2(px - bottomHalf, y0),
                new Vector2(px + bottomHalf, y0),
                new Vector2(px + topHalf, y1),
                new Vector2(px - topHalf, y1)
            }, 0.015f, backMaterial);

            float lineHalf = Mathf.Lerp(bottomHalf, topHalf, (cup.FillLineY - y0) / cup.Size.y) - wall;
            SetFlat("FillLine", new[]
            {
                new Vector2(px - lineHalf, cup.FillLineY - fillThickness * 0.5f),
                new Vector2(px + lineHalf, cup.FillLineY - fillThickness * 0.5f),
                new Vector2(px + lineHalf, cup.FillLineY + fillThickness * 0.5f),
                new Vector2(px - lineHalf, cup.FillLineY + fillThickness * 0.5f)
            }, -0.26f, wallMaterial);
            SetRim(px, y1, topHalf, wallMaterial);
            SetFillView(cup.Position, topHalf, cup.FillLineY - y0);
        }

        private void SetExtruded(string childName, IList<Vector2> polygon, Material material)
        {
            MeshFilter filter = RequireMeshFilter(childName);
            Mesh mesh = ExtrudedBevelMeshBuilder.Build(polygon, extrusionDepth, 0f, 1, childName);
            generatedMeshes.Add(mesh);
            filter.sharedMesh = mesh;
            filter.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void SetFlat(string childName, Vector2[] points, float z, Material material)
        {
            MeshFilter filter = RequireMeshFilter(childName);
            Mesh mesh = new Mesh { name = childName };
            mesh.vertices = new[]
            {
                (Vector3)points[0] + Vector3.forward * z,
                (Vector3)points[1] + Vector3.forward * z,
                (Vector3)points[2] + Vector3.forward * z,
                (Vector3)points[3] + Vector3.forward * z
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            filter.sharedMesh = mesh;
            filter.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void SetRim(float x, float y, float halfWidth, Material material)
        {
            LineRenderer line = RequireLineRenderer("Rim");
            line.useWorldSpace = false;
            line.loop = true;
            line.alignment = LineAlignment.TransformZ;
            line.widthMultiplier = rimWidth;
            line.sharedMaterial = material;
            line.positionCount = rimSegments;
            for (int i = 0; i < rimSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / rimSegments;
                line.SetPosition(i, new Vector3(
                    x + Mathf.Cos(angle) * halfWidth,
                    y + Mathf.Sin(angle) * halfWidth * 0.18f,
                    rimDepth));
            }
        }

        private void SetFillView(Vector2 position, float halfWidth, float height)
        {
            fillView.localPosition = new Vector3(position.x, position.y, 0f);
            fillView.localScale = new Vector3(0.0001f, 1f, 1f);
            fillFilter.sharedMesh = CreateTrackedQuad(halfWidth, height);
            fillRenderer.sharedMaterial = materials != null ? materials.cupMaterial : null;
            block.SetColor("_BaseColor", GetMaterialColor(cup.AcceptedMaterialId));
            fillRenderer.SetPropertyBlock(block);
        }

        private Mesh CreateTrackedQuad(float halfWidth, float height)
        {
            Mesh mesh = new Mesh { name = "CupFillQuad" };
            float safeHeight = Mathf.Max(0.01f, height);
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, -0.1f),
                new Vector3(halfWidth, 0f, -0.1f),
                new Vector3(halfWidth, safeHeight, -0.1f),
                new Vector3(-halfWidth, safeHeight, -0.1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            return mesh;
        }

        private MeshFilter RequireMeshFilter(string childName)
        {
            Transform child = view.Find(childName);
            MeshFilter filter = child != null ? child.GetComponent<MeshFilter>() : null;
            if (filter == null)
                throw new MissingComponentException("Cup prefab requires View/" + childName + " MeshFilter.");
            if (child.GetComponent<MeshRenderer>() == null)
                throw new MissingComponentException("Cup prefab requires View/" + childName + " MeshRenderer.");
            return filter;
        }

        private LineRenderer RequireLineRenderer(string childName)
        {
            Transform child = view.Find(childName);
            LineRenderer line = child != null ? child.GetComponent<LineRenderer>() : null;
            if (line == null)
                throw new MissingComponentException("Cup prefab requires View/" + childName + " LineRenderer.");
            return line;
        }

        private Color GetMaterialColor(byte materialId)
        {
            return palette != null && palette.Contains(materialId)
                ? palette.GetSandColor(materialId)
                : Color.magenta;
        }

        private void DestroyGeneratedMeshes()
        {
            for (int i = 0; i < generatedMeshes.Count; i++)
            {
                if (generatedMeshes[i] == null) continue;
                if (Application.isPlaying) Destroy(generatedMeshes[i]);
                else DestroyImmediate(generatedMeshes[i]);
            }
            generatedMeshes.Clear();
        }

        private void OnDestroy()
        {
            DestroyGeneratedMeshes();
        }
    }
}
