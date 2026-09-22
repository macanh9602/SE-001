using NUnit.Framework;
using SE001.System.Creation;
using UnityEngine;

namespace SE001.System.Management.Tests
{
    public sealed class LevelLifecycleTests
    {
        private GameObject owner;
        private LevelManager manager;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("LevelLifecycleTests");
            manager = owner.AddComponent<LevelManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void BeginLevel_UsesSpawnerOwnedHierarchy()
        {
            manager.BeginLevel("test_level");

            LevelContext context = manager.CurrentContext;
            Assert.That(context, Is.Not.Null);
            Assert.That(manager.Spawner.ActiveContext, Is.SameAs(context));
            Assert.That(context.LevelRoot.parent, Is.EqualTo(owner.transform));
            Assert.That(context.LevelRoot.childCount, Is.EqualTo(7));
            Assert.That(context.BoardRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(context.ObstacleRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(context.SourceRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(context.CupRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(context.DynamicDrawRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(context.SandVisualRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(context.VfxRoot.parent, Is.EqualTo(context.LevelRoot));
            Assert.That(manager.IsReady, Is.True);
        }

        [Test]
        public void Reload_CancelsAndCleansOldContextBeforeNewContextIsReady()
        {
            manager.BeginLevel("test_level");
            LevelContext oldContext = manager.CurrentContext;
            Transform oldRoot = oldContext.LevelRoot;
            var participant = new TestParticipant();
            manager.RegisterParticipant(participant);

            manager.ReloadCurrentLevel();

            Assert.That(oldContext.IsDisposed, Is.True);
            Assert.That(oldContext.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(participant.CleanupCount, Is.EqualTo(1));
            Assert.That(oldRoot == null, Is.True);
            Assert.That(manager.CurrentContext, Is.Not.SameAs(oldContext));
            Assert.That(manager.CurrentContext.Generation, Is.GreaterThan(oldContext.Generation));
            Assert.That(manager.IsReady, Is.True);
        }

        [Test]
        public void TenReloadCycles_DoNotAccumulateContextsOrRoots()
        {
            manager.BeginLevel("test_level");

            for (int i = 0; i < 10; i++)
            {
                LevelContext previous = manager.CurrentContext;
                manager.ReloadCurrentLevel();

                Assert.That(previous.IsDisposed, Is.True);
                Assert.That(owner.transform.childCount, Is.EqualTo(1));
                Assert.That(owner.GetComponents<LevelManager>().Length, Is.EqualTo(1));
                Assert.That(owner.GetComponents<LevelSpawner>().Length, Is.EqualTo(1));
            }

            manager.UnloadCurrentLevel();
            Assert.That(manager.CurrentContext, Is.Null);
            Assert.That(manager.Spawner.ActiveContext, Is.Null);
            Assert.That(owner.transform.childCount, Is.EqualTo(0));
            Assert.That(manager.Readiness.IsOpen, Is.False);
        }

        [Test]
        public void InvalidLevelId_DoesNotUnloadCurrentContext()
        {
            manager.BeginLevel("test_level");
            LevelContext context = manager.CurrentContext;

            Assert.Throws<global::System.ArgumentException>(() => manager.BeginLevel(" "));
            Assert.That(manager.CurrentContext, Is.SameAs(context));
            Assert.That(context.IsDisposed, Is.False);
        }

        private sealed class TestParticipant : ILevelLifecycleParticipant
        {
            public bool IsPendingCleanup { get; private set; }
            public int CleanupCount { get; private set; }

            public void Bind(LevelContext context)
            {
                IsPendingCleanup = true;
            }

            public void CleanupForLevelUnload()
            {
                CleanupCount++;
                IsPendingCleanup = false;
            }
        }
    }
}
