using NUnit.Framework;
using SE001.Data;
using SE001.Presentation;
using SE001.System.Management;
using UnityEditor;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class PhaseCLifecycleTests
    {
        private GameObject owner;
        private LevelManager manager;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("PhaseCLifecycleTests");
            manager = owner.AddComponent<LevelManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null) Object.DestroyImmediate(owner);
        }

        [Test]
        public void PhaseC_ReloadTenTimes_NoVisualAccumulation()
        {
            string[] levels = { "phase_c_level_01", "phase_c_level_02", "phase_c_level_03" };
            int baselineMeshes = CountTransientMeshes();
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                manager.BeginLevel(levels[levelIndex]);
                int childCount = manager.CurrentContext.LevelRoot.childCount;
                for (int i = 0; i < 10; i++)
                {
                    LevelContext previous = manager.CurrentContext;
                    manager.ReloadCurrentLevel();
                    Assert.That(previous.IsDisposed, Is.True);
                    Assert.That(owner.transform.childCount, Is.EqualTo(1));
                    Assert.That(manager.CurrentContext.LevelRoot.childCount, Is.EqualTo(childCount));
                    Assert.That(
                        manager.CurrentContext.SourceRoot.GetComponentsInChildren<PhaseCSourceVisual>(),
                        Has.Length.EqualTo(levelIndex == 0 ? 1 : 2));
                    Assert.That(
                        manager.CurrentContext.CupRoot.GetComponentsInChildren<PhaseCCupVisual>(),
                        Has.Length.EqualTo(levelIndex == 0 ? 1 : 2));
                    Assert.That(
                        manager.CurrentContext.DynamicDrawRoot.Find("DrawPreview"),
                        Is.Not.Null);
                    Assert.That(HasDebugView(manager.CurrentContext.LevelRoot), Is.False);
                }
                manager.UnloadCurrentLevel();
                Assert.That(owner.transform.childCount, Is.EqualTo(0));
                Assert.That(CountLiveSandSurfaceFilters(), Is.EqualTo(0));
            }
            Assert.That(CountTransientMeshes(), Is.EqualTo(baselineMeshes));
        }

        [Test]
        public void PhaseC_NextFollowsSequence_DisabledOnLast()
        {
            manager.BeginLevel("phase_c_level_01");
            Assert.That(manager.CanBeginNextLevel(), Is.True);
            Assert.That(manager.BeginNextLevel(), Is.True);
            Assert.That(manager.CurrentContext.LevelId, Is.EqualTo("phase_c_level_02"));
            Assert.That(manager.BeginNextLevel(), Is.True);
            Assert.That(manager.CurrentContext.LevelId, Is.EqualTo("phase_c_level_03"));
            Assert.That(manager.CanBeginNextLevel(), Is.False);
            Assert.That(manager.BeginNextLevel(), Is.False);
        }

        [Test]
        public void PhaseC_VisualMaterialsAssigned()
        {
            GameplayRuntimeProfile runtime = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.visualMaterials, Is.Not.Null);
            Assert.That(runtime.visualMaterials.sourceMaterial, Is.Not.Null);
            Assert.That(runtime.visualMaterials.cupMaterial, Is.Not.Null);
            Assert.That(runtime.visualMaterials.cupBackMaterial, Is.Not.Null);
            Assert.That(runtime.visualMaterials.drawPathMaterial, Is.Not.Null);
            Assert.That(runtime.visualMaterials.sourceMaterial.shader.name, Is.EqualTo("SE001/SpriteSurface"));
            Assert.That(runtime.visualMaterials.cupMaterial.shader.name, Is.EqualTo("SE001/SpriteSurface"));
            Assert.That(runtime.visualMaterials.cupBackMaterial.shader.name, Is.EqualTo("SE001/SpriteSurface"));
            Assert.That(runtime.visualMaterials.drawPathMaterial.shader.name, Is.EqualTo("SE001/LayoutSurface"));
        }

        private static int CountTransientMeshes()
        {
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
            int count = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                if (EditorUtility.IsPersistent(meshes[i])) continue;
                if (meshes[i].name.StartsWith("DebugStroke") || meshes[i].name.StartsWith("PhaseCDebug") ||
                    meshes[i].name.StartsWith("Generated") || meshes[i].name == "SandFieldSurface") continue;
                count++;
            }
            return count;
        }

        private static int CountLiveSandSurfaceFilters()
        {
            MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < filters.Length; i++)
                if (filters[i].sharedMesh != null && filters[i].sharedMesh.name == "SandFieldSurface") count++;
            return count;
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
