using System.Collections.Generic;
using NUnit.Framework;
using SE001.Creation;
using SE001.Data;
using SE001.Gameplay;
using SE001.Presentation;
using SE001.System.Management;
using UnityEditor;
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
            Assert.That(visual.transform.Find("Pivot/View/Mouth"), Is.Not.Null);
            Assert.That(visual.SandFillRenderer, Is.Not.Null);
            Assert.That(visual.ShadowRenderer, Is.Not.Null);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void PrefabAssets_ExposeExactProductionHierarchy()
        {
            GameplayRuntimeProfile runtime = LoadRuntime();
            AssertSourcePrefabContract(runtime.prefabProfile.sourcePrefab);
            AssertCupPrefabContract(runtime.prefabProfile.cupPrefab);
            AssertDrawStrokePrefabContract(runtime.prefabProfile.drawStrokePrefab);
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
                    requiredAmount = 1
                },
                profile,
                runtime.sandProfile.grainsPerUnit,
                runtime.sandProfile.cellSize,
                runtime.jarVisualProfile);
            GameObject parent = new GameObject("CupFactoryParent");

            PhaseCCupVisual visual = new CupFactory().Create(
                new CupCreateParameters(domain, parent.transform, runtime.prefabProfile, runtime));

            Assert.That(visual.Domain, Is.SameAs(domain));
            Assert.That(visual.transform.localPosition, Is.EqualTo((Vector3)domain.Position));
            Assert.That(CountChildren(visual.transform.Find("View")), Is.EqualTo(4));
            Assert.That(visual.transform.Find("View/Shadow").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/Body").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/CapTop").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(visual.transform.Find("View/CapBottom").GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            float bodyHeight = runtime.jarVisualProfile.cupBodyHeightPixels * 0.01f;
            float bodyBottom = visual.BodyRenderer.transform.localPosition.y -
                visual.BodyRenderer.transform.localScale.y * bodyHeight * 0.5f;
            Assert.That(bodyBottom, Is.EqualTo(domain.EffectiveSandBottomY - domain.Position.y).Within(0.0001f));

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
            Assert.That(HasDebugView(context.LevelRoot), Is.False, "PhaseCDebugView must not be part of production presentation.");
            PhaseCSourceVisual source =
                context.SourceRoot.GetComponentInChildren<PhaseCSourceVisual>();
            PhaseCCupVisual cup =
                context.CupRoot.GetComponentInChildren<PhaseCCupVisual>();
            Assert.That(source.transform.Find("Pivot/View/Body"), Is.Not.Null);
            Assert.That(cup.transform.Find("View/Body"), Is.Not.Null);
            Assert.That(cup.transform.Find("View/CapTop"), Is.Not.Null);
            Assert.That(cup.transform.Find("View/CapBottom"), Is.Not.Null);
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
                logicalAmount = 1
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

        private static void AssertSourcePrefabContract(GameObject prefab)
        {
            GameObject root = LoadPrefabContents(prefab);
            try
            {
                AssertRequiredComponent<Renderer>(root, "Pivot/View/Body");
                AssertRequiredComponent<Renderer>(root, "Pivot/View/Shadow");
                AssertRequiredComponent<Renderer>(root, "Pivot/View/SandFill");
                AssertRequiredComponent<Renderer>(root, "Pivot/View/Mouth");
                AssertRequiredTransform(root, "Anchors/EmitPoint");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssertCupPrefabContract(GameObject prefab)
        {
            GameObject root = LoadPrefabContents(prefab);
            try
            {
                AssertRequiredComponent<MeshFilter>(root, "View/Shadow");
                AssertRequiredComponent<MeshFilter>(root, "View/Body");
                AssertRequiredComponent<MeshFilter>(root, "View/CapTop");
                AssertRequiredComponent<MeshFilter>(root, "View/CapBottom");
                AssertRequiredTransform(root, "Anchors/Entry");
                AssertRequiredTransform(root, "Anchors/Feedback");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssertDrawStrokePrefabContract(GameObject prefab)
        {
            GameObject root = LoadPrefabContents(prefab);
            try
            {
                AssertRequiredComponent<MeshFilter>(root, "View");
                AssertRequiredComponent<MeshRenderer>(root, "View");
                AssertRequiredComponent<LineRenderer>(root, "Preview");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject LoadPrefabContents(GameObject prefab)
        {
            Assert.That(prefab, Is.Not.Null);
            string path = AssetDatabase.GetAssetPath(prefab);
            Assert.That(path, Is.Not.Empty);
            return PrefabUtility.LoadPrefabContents(path);
        }

        private static void AssertRequiredTransform(GameObject root, string path)
        {
            Transform target = root.transform.Find(path);
            Assert.That(target, Is.Not.Null, path);
            Assert.That(CountMatchingPaths(root.transform, path), Is.EqualTo(1), path);
        }

        private static void AssertRequiredComponent<T>(GameObject root, string path)
            where T : Component
        {
            Transform target = root.transform.Find(path);
            Assert.That(target, Is.Not.Null, path);
            Assert.That(target.GetComponent<T>(), Is.Not.Null, path);
            Assert.That(CountMatchingPaths(root.transform, path), Is.EqualTo(1), path);
        }

        private static int CountMatchingPaths(Transform root, string expectedPath)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (GetRelativePath(root, transforms[i]) == expectedPath)
                    count++;
            }
            return count;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            List<string> names = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }
    
        private static bool HasDebugView(Transform root)
        {
            Component[] components = root.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
                if (components[i] != null && components[i].GetType().Name == "PhaseCDebugView") return true;
            return false;
        }
}
}
