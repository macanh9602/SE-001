using System.Collections.Generic;
using UnityEngine;

namespace SE001.Geometry
{
    /// <summary>
    /// Extrudes a board-space XY contour toward the gameplay camera (-Z). Board plane stays at z = 0,
    /// the cap sits at z = -height and faces -Z; side faces point outward regardless of source winding.
    /// UVs are world-unit based (* uvScale) so a repeat-wrapped texture tiles instead of stretching.
    /// </summary>
    public static class ExtrudedBevelMeshBuilder
    {
        public static Mesh Build(IList<Vector2> polygon, float height, float bevelRadius, int bevelSegments, string meshName, float uvScale = 1f)
        {
            if (polygon == null || polygon.Count < 3) throw new global::System.ArgumentException("A polygon needs at least three points.");

            // Canonical data may be CW or CCW; side winding below assumes CCW.
            List<Vector2> contour = new List<Vector2>(polygon);
            if (PolygonUtility.SignedArea(contour) < 0f) contour.Reverse();

            int[] capIndices = PolygonTriangulator.Triangulate(contour);
            int count = contour.Count;
            int rings = Mathf.Max(1, bevelSegments);
            int ringCount = rings + 2;
            int ringStride = count + 1; // duplicated seam vertex keeps U continuous along the perimeter

            float[] perimeter = new float[ringStride];
            for (int i = 1; i < ringStride; i++) perimeter[i] = perimeter[i - 1] + Vector2.Distance(contour[i - 1], contour[i % count]);

            List<Vector3> vertices = new List<Vector3>(ringCount * ringStride + count);
            List<Vector2> uvs = new List<Vector2>(vertices.Capacity);
            List<int> indices = new List<int>(capIndices.Length + (ringCount - 1) * count * 6);

            // Built in "+Z = up" space, mirrored to -Z at the end.
            for (int ring = 0; ring < ringCount; ring++)
            {
                float z = ring == 0 ? 0f : ring == ringCount - 1 ? height : Mathf.Max(0f, height - bevelRadius * (rings + 1 - ring) / rings);
                for (int i = 0; i < ringStride; i++)
                {
                    Vector2 point = contour[i % count];
                    vertices.Add(new Vector3(point.x, point.y, z));
                    uvs.Add(new Vector2(perimeter[i], z) * uvScale);
                }
            }

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int i = 0; i < count; i++)
                {
                    int a = ring * ringStride + i; int b = a + 1; int c = b + ringStride; int d = a + ringStride;
                    indices.Add(a); indices.Add(b); indices.Add(c); indices.Add(a); indices.Add(c); indices.Add(d);
                }
            }

            // Separate cap vertices -> hard edge between cap and sides.
            int capStart = vertices.Count;
            for (int i = 0; i < count; i++) { Vector2 point = contour[i]; vertices.Add(new Vector3(point.x, point.y, height)); uvs.Add(point * uvScale); }
            for (int i = 0; i < capIndices.Length; i++) indices.Add(capStart + capIndices[i]);

            // Mirror to -Z and flip winding so faces keep pointing outward / toward the camera.
            for (int i = 0; i < vertices.Count; i++) { Vector3 v = vertices[i]; v.z = -v.z; vertices[i] = v; }
            for (int i = 0; i < indices.Count; i += 3) { int t = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = t; }

            Mesh mesh = new Mesh { name = meshName };
            if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
