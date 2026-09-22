using System.Collections.Generic;
using NUnit.Framework;
using SE001.Creation;
using SE001.Data;
using SE001.Gameplay;
using SE001.Presentation;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class C1ProductionVisualTests
    {
        private GameObject owner;
        private LevelManager levelManager;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("C1ProductionVisualTests");
            levelManager = owner.AddComponent<LevelManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (levelManager != null && levelManager.CurrentContext != null)
                levelManager.UnloadCurrentLevel();
            if (owner != null)
                Object.DestroyImmediate(owner);
        }

        [Test]
        public void SourceFactory_CreatesPrefabAndBindsDomain()
        {
            GameplayRuntimeProfile runtime = LoadRuntime();
            SourceDomain domain = CreateSource(runtime);
            GameObject parent = new GameObject("SourceFactoryParent");

            PhaseCSourceVisual visual = new SourceFactory().Create(
                new SourceCreateParameters(domain, parent.transform, runtime.prefabProfile, runtime));

            Assert.That(visual.Domain, Is.SameAs(domain));
            Assert.That(visual.BodyRenderer, Is.Not.Null);
            Assert.That(visual.NozzleRenderer, Is.Not.Null);
            Assert.That(visual.transform.Find("Pivot/View/Body"), Is.Not.Null);
            Assert.That(visual.transform.Find("Pivot/View/Nozzle"), Is.Not.Null);
            Assert.That(visual.NozzleRenderer.sharedMaterial, Is.SameAs(visual.BodyRenderer.sharedMaterial));

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void CupFactory_VisualMatchesDomainGeometry()
        {
            GameplayRuntimeProfile runtime = LoadRuntime();
            CupProfile profile = runtime.cupProfile;
            CupDomain domain = new CupDomain(
                new CupData
                {
                    stableId = "test_cup",
                    acceptedMaterialId = 1,
                    position = new Vector2(5.4f, 1f),
                    size = new Vector2(2f, 1.5f),
                    requiredAmount = 1
                },
                profile,
                runtime.sandProfile.grainsPerUnit,
                runtime.sandProfile.cellSize);
            GameObject parent = new GameObject("CupFactoryParent");

            PhaseCCupVisual visual = new CupFactory().Create(
                new CupCreateParameters(domain, parent.transform, runtime.prefabProfile, runtime));

            Assert.That(visual.Domain, Is.SameAs(domain));
            Assert.That(
                visual.transform.Find("FillView").localPosition,
                Is.EqualTo((Vector3)domain.Position));
            Assert.That(CountChildren(visual.transform.Find("View")), Is.EqualTo(6));
            Assert.That(visual.transform.Find("View/WallL").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/WallR").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/WallB").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/Back").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/FillLine").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/Rim").GetComponent<LineRenderer>().positionCount, Is.GreaterThan(0));
            Assert.That(visual.FillView.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void DrawStrokeFactory_BuildsCommittedMesh()
        {
            GameplayRuntimeProfile runtime = LoadRuntime();
            GameObject parent = new GameObject("DrawFactoryParent");
            PhaseCDrawStrokeVisual visual = new DrawStrokeFactory().Create(
                new DrawStrokeCreateParameters(
                    Line(new Vector2(1f, 1f), new Vector2(3f, 2f), 8),
                    0.3f,
                    parent.transform,
                    runtime.prefabProfile,
                    runtime));

            Assert.That(visual.GeneratedMesh, Is.Not.Null);
            Assert.That(visual.MeshFilter.sharedMesh, Is.SameAs(visual.GeneratedMesh));
            Assert.That(visual.GeneratedMesh.vertexCount, Is.GreaterThan(0));
            Assert.That(visual.transform.parent, Is.SameAs(parent.transform));

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void DrawStrokeFactory_GeneratedMeshDestroyedOnUnload()
        {
            GameplayRuntimeProfile runtime = LoadRuntime();
            GameObject parent = new GameObject("DrawCleanupParent");
            PhaseCDrawStrokeVisual visual = new DrawStrokeFactory().Create(
                new DrawStrokeCreateParameters(
                    Line(Vector2.zero, Vector2.one, 4),
                    0.3f,
                    parent.transform,
                    runtime.prefabProfile,
                    runtime));
            Mesh generated = visual.GeneratedMesh;

            visual.ReleaseForUnload();
            Object.DestroyImmediate(parent);

            Assert.That(generated == null, Is.True);
        }

        [Test]
        public void PhaseC_Level02_ProductionVisualHierarchyExists()
        {
            levelManager.BeginLevel("phase_c_level_02");
            LevelContext context = levelManager.CurrentContext;
            GameplayManager gameplay = context.LevelRoot.GetComponent<GameplayManager>();

            Assert.That(context.SourceRoot.GetComponentsInChildren<PhaseCSourceVisual>(), Is.Not.Empty);
            Assert.That(context.CupRoot.GetComponentsInChildren<PhaseCCupVisual>(), Is.Not.Empty);
            Assert.That(context.LevelRoot.GetComponent<SE001.Diagnostics.PhaseCDebugView>(), Is.Null);
            PhaseCSourceVisual source =
                context.SourceRoot.GetComponentInChildren<PhaseCSourceVisual>();
            PhaseCCupVisual cup =
                context.CupRoot.GetComponentInChildren<PhaseCCupVisual>();
            Assert.That(source.transform.Find("Pivot/View/Body"), Is.Not.Null);
            Assert.That(cup.transform.Find("View/WallL"), Is.Not.Null);
            Assert.That(context.DynamicDrawRoot.Find("DrawPreview"), Is.Not.Null);

            Assert.That(gameplay.CommitStroke(
                Line(new Vector2(1f, 1f), new Vector2(3f, 2f), 8),
                0.3f), Is.True);
            Assert.That(
                context.DynamicDrawRoot.GetComponentsInChildren<PhaseCDrawStrokeVisual>(),
                Has.Length.EqualTo(2));
        }

        private static GameplayRuntimeProfile LoadRuntime()
        {
            GameplayRuntimeProfile runtime =
                Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.prefabProfile, Is.Not.Null);
            return runtime;
        }

        private static SourceDomain CreateSource(GameplayRuntimeProfile runtime)
        {
            SourceData data = new SourceData
            {
                stableId = "test_source",
                materialId = 1,
                position = Vector2.one,
                logicalAmount = 1,
                size = new Vector2(1f, 1f)
            };
            return new SourceDomain(data, runtime.sourceProfile, runtime.sandProfile.grainsPerUnit);
        }

        private static List<Vector2> Line(Vector2 start, Vector2 end, int count)
        {
            List<Vector2> points = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
                points.Add(Vector2.Lerp(start, end, i / (float)(count - 1)));
            return points;
        }

        private static int CountChildren(Transform parent)
        {
            return parent == null ? 0 : parent.childCount;
        }
    }
}
