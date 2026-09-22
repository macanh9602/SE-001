using NUnit.Framework;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class PhaseBIntegrationTests
    {
        private GameObject owner;

        [SetUp]
        public void SetUp() { owner = new GameObject("PhaseBIntegrationTests"); owner.AddComponent<LevelManager>(); }

        [TearDown]
        public void TearDown() { if (owner != null) Object.DestroyImmediate(owner); }

        [Test]
        public void Fixture_LoadsThroughNormalManagerSpawnerPath()
        {
            Assert.That(Resources.Load<SE001.Data.PrefabProfile>("Profiles/PhaseBPrefabProfile"), Is.Not.Null);
            Assert.That(Resources.Load<SE001.Data.LayoutVisualProfile>("Profiles/PhaseBLayoutVisualProfile"), Is.Not.Null);
            LevelManager manager = owner.GetComponent<LevelManager>();
                manager.BeginLevel("phase_c_level_02");
            Assert.That(manager.IsReady, Is.True);
            Assert.That(manager.CurrentContext.SandSimulation, Is.Not.Null);
            Assert.That(manager.CurrentContext.BoardRoot.childCount, Is.GreaterThan(0));
            Assert.That(manager.CurrentContext.ObstacleRoot.childCount, Is.GreaterThan(0));
            Assert.That(manager.CurrentContext.SandVisualRoot.childCount, Is.EqualTo(1));
            Assert.That(manager.CurrentContext.SandSimulation.State.ValidMask.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Fixture_ReloadsTenTimesWithoutAccumulatingRuntimeRoots()
        {
            LevelManager manager = owner.GetComponent<LevelManager>();
                manager.BeginLevel("phase_c_level_02");
            for (int i = 0; i < 10; i++)
            {
                LevelContext previous = manager.CurrentContext;
                manager.ReloadCurrentLevel();
                Assert.That(previous.IsDisposed, Is.True);
                Assert.That(owner.transform.childCount, Is.EqualTo(1));
                Assert.That(manager.CurrentContext.LevelRoot.childCount, Is.EqualTo(7));
            }
            manager.UnloadCurrentLevel();
            Assert.That(owner.transform.childCount, Is.EqualTo(0));
        }
    }
}
