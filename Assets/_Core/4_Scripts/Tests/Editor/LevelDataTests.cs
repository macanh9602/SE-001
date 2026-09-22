using System.Collections.Generic;
using NUnit.Framework;
using SE001.Data;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Data.Tests
{
    public sealed class LevelDataTests
    {
        [Test]
        public void RoundTrip_IsDeterministicForCanonicalData()
        {
            SE001LevelJson source = CreateFixture();
            string first = source.ToJson();
            string second = SE001LevelJson.FromJson(first).ToJson();
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DuplicateIds_AreRejected()
        {
            SE001LevelJson level = CreateFixture();
            level.staticObstacles.Add(new StaticObstacleData { stableId = "wall", contours = new List<PolygonContourData> { Triangle() } });
            level.staticObstacles.Add(new StaticObstacleData { stableId = "wall", contours = new List<PolygonContourData> { Triangle() } });
            List<string> errors = new List<string>();
            Assert.That(LevelDataValidator.TryValidate(level, errors), Is.False);
        }

        [Test]
        public void DegenerateContour_IsRejected()
        {
            SE001LevelJson level = CreateFixture();
            level.board.wallContours[0].points = new List<Vector2> { Vector2.zero, Vector2.right, Vector2.right };
            Assert.Throws<global::System.FormatException>(() => LevelDataValidator.Validate(level));
        }

        [Test]
        public void ConcavePolygon_Triangulates()
        {
            List<Vector2> polygon = new List<Vector2> { new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f), new Vector2(1f, 1f), new Vector2(0f, 2f) };
            Assert.That(PolygonTriangulator.Triangulate(polygon).Length, Is.EqualTo(9));
        }

        [Test]
        public void Rasterizer_IsDeterministicAndSeparatesStaticCells()
        {
            SE001LevelJson level = CreateFixture();
            level.board.size = new Vector2(4f, 4f);
            level.board.wallContours = new List<PolygonContourData> { Square(0f, 0f, 1f, 4f) };
            level.staticObstacles.Add(new StaticObstacleData { stableId = "obstacle", contours = new List<PolygonContourData> { Square(2f, 2f, 1f, 1f) } });
            LayoutMaskSet first = LayoutRasterizer.Rasterize(level, 1f, 100);
            LayoutMaskSet second = LayoutRasterizer.Rasterize(level, 1f, 100);
            Assert.That(first.ValidMask, Is.EqualTo(second.ValidMask));
            Assert.That(first.StaticMask[0], Is.True);
            Assert.That(first.ValidMask[first.Index(3, 3)], Is.True);
        }

        [Test]
        public void SandSimulation_FallsAndConservesOccupiedCount()
        {
            LayoutMaskSet masks = new LayoutMaskSet(4, 4);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            try
            {
                profile.maxCells = 100;
                SandSimulation simulation = new SandSimulation(profile, masks);
                Assert.That(simulation.TryEmit(1, 3, 1), Is.True);
                simulation.Step();
                Assert.That(simulation.State.OccupiedCount, Is.EqualTo(1));
                Assert.That(simulation.State.Cells[simulation.State.Index(1, 2)], Is.EqualTo(1));
                simulation.Dispose();
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void ExtrudedMesh_HasTopSideAndBevelTopology()
        {
            List<Vector2> polygon = new List<Vector2> { new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f), new Vector2(0f, 2f) };
            Mesh mesh = ExtrudedBevelMeshBuilder.Build(polygon, 1f, 0.1f, 2, "test_mesh");
            try
            {
                // 4 side rings x (4 + seam) + 4 separate cap vertices.
                Assert.That(mesh.vertexCount, Is.EqualTo(24));
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
                Assert.That(mesh.bounds.min.z, Is.EqualTo(-1f).Within(0.0001f), "Extrusion must go toward the camera (-Z).");
                Assert.That(mesh.bounds.max.z, Is.EqualTo(0f).Within(0.0001f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExtrudedMesh_CapFacesCameraAndSidesFaceOutward(bool clockwiseSource)
        {
            List<Vector2> polygon = new List<Vector2> { new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f), new Vector2(0f, 2f) };
            if (clockwiseSource) polygon.Reverse();
            Mesh mesh = ExtrudedBevelMeshBuilder.Build(polygon, 1f, 0.1f, 1, "test_mesh");
            try
            {
                Vector3[] v = mesh.vertices; int[] t = mesh.triangles; Vector3 center = new Vector3(1f, 1f, -0.5f);
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    Vector3 outward = ((a + b + c) / 3f - center);
                    Assert.That(Vector3.Dot(normal, outward), Is.GreaterThan(0f), "Triangle " + (i / 3) + " faces inward.");
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void GeneratedFixture_LoadsThroughCanonicalResourceProvider()
        {
            SE001LevelJson level = LevelDataLoader.Load("phase_c_level_02");
            Assert.That(level.board.wallContours.Count, Is.GreaterThan(0));
            Assert.That(level.staticObstacles.Count, Is.GreaterThan(0));
        }

        private static SE001LevelJson CreateFixture()
        {
            return new SE001LevelJson
            {
                levelId = "phase_b_data_test",
                board = new BoardData { size = new Vector2(4f, 3f), wallContours = new List<PolygonContourData> { Triangle() } },
                staticObstacles = new List<StaticObstacleData>(),
                sources = new List<SourceData>(),
                cups = new List<CupData>()
            };
        }

        private static PolygonContourData Triangle()
        {
            return new PolygonContourData { points = new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f) } };
        }

        private static PolygonContourData Square(float x, float y, float width, float height)
        {
            return new PolygonContourData { points = new List<Vector2> { new Vector2(x, y), new Vector2(x + width, y), new Vector2(x + width, y + height), new Vector2(x, y + height) } };
        }
    }
}
