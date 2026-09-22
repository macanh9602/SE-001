using System.Collections.Generic;
using SE001.Data;
using SE001.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace SE001.Elements.Layout
{
    public sealed class LayoutVisualView : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        private Mesh generatedMesh;

        public void Rebuild(IList<Vector2> polygon, LayoutVisualProfile profile, bool wall)
        {
            if (polygon == null || profile == null) throw new global::System.ArgumentNullException();
            EnsureComponents();
            if (generatedMesh != null) DestroyOwnedMesh();
            generatedMesh = ExtrudedBevelMeshBuilder.Build(polygon, wall ? profile.wallHeight : profile.obstacleHeight, profile.bevelRadius, profile.bevelSegments, wall ? "GeneratedWall" : "GeneratedObstacle", profile.uvScale);
            meshFilter.sharedMesh = generatedMesh;
            meshRenderer.sharedMaterial = wall ? profile.wallMaterial : profile.obstacleMaterial;
            meshRenderer.shadowCastingMode = profile.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            meshRenderer.receiveShadows = profile.receiveShadows;
        }

        private void EnsureComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null) throw new MissingComponentException("LayoutVisualView requires MeshFilter and MeshRenderer on its prefab View.");
        }

        private void OnDestroy() { DestroyOwnedMesh(); }
        private void DestroyOwnedMesh() { if (generatedMesh == null) return; if (Application.isPlaying) Destroy(generatedMesh); else DestroyImmediate(generatedMesh); generatedMesh = null; }
    }
}
