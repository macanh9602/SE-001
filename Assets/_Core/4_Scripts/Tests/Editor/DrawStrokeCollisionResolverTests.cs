using System.Collections.Generic;
using NUnit.Framework;
using SE001.Data;
using SE001.Gameplay;
using SE001.Geometry;
using SE001.Simulation.Sand;
using SE001.System.Management;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class DrawStrokeCollisionResolverTests
    {
        [Test]
        public void ResolveStroke_StopsAtFirstStaticContact_AndDoesNotIncludeBlockedTail()
        {
            CreateSimulation(out SandSimulationProfile profile, out SandSimulation simulation);
            try
            {
                simulation.State.StaticMask[simulation.State.Index(50, 50)] = true;
                DrawStrokeCollisionResolver resolver = new DrawStrokeCollisionResolver(simulation, new List<SourceDomain>());
                List<Vector2> accepted = new List<Vector2>(8);

                bool resolved = resolver.ResolveStroke(new[] { new Vector2(3f, 5.05f), new Vector2(7f, 5.05f) },
                    0.15f, 20f, accepted, out float length);

                Assert.That(resolved, Is.True);
                Assert.That(accepted, Has.Count.EqualTo(2));
                Assert.That(accepted[1].x, Is.LessThan(5f));
                Assert.That(length, Is.EqualTo(accepted[1].x - accepted[0].x).Within(0.001f));
                Assert.That(simulation.State.StaticMask[simulation.State.Index(50, 50)], Is.True);

                bool clippedByInk = resolver.ResolveStroke(new[] { new Vector2(2f, 2f), new Vector2(8f, 2f) },
                    0.15f, 1f, accepted, out float inkLength);
                Assert.That(clippedByInk, Is.True);
                Assert.That(inkLength, Is.EqualTo(1f).Within(0.001f));
                Assert.That(Vector2.Distance(accepted[0], accepted[1]), Is.EqualTo(1f).Within(0.001f));
            }
            finally { simulation.Dispose(); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void IsPointClear_UsesSchema5ResolvedSourceScaleAndBodyOffset()
        {
            CreateSimulation(out SandSimulationProfile sandProfile, out SandSimulation simulation);
            SourceProfile sourceProfile = ScriptableObject.CreateInstance<SourceProfile>();
            try
            {
                sourceProfile.bodySize = new Vector2(0.8f, 1.2f);
                SE001LevelJson level = new SE001LevelJson { schemaVersion = 5, sourceScale = 2f };
                SourceDomain source = new SourceDomain(new SourceData
                {
                    stableId = "source", position = new Vector2(5f, 5f), logicalAmount = 10
                }, sourceProfile, 1, null, level);
                DrawStrokeCollisionResolver resolver = new DrawStrokeCollisionResolver(simulation,
                    new List<SourceDomain> { source });

                Assert.That(source.Size.x, Is.EqualTo(1.6f).Within(0.001f));
                Assert.That(resolver.IsPointClear(new Vector2(5f, 6.2f), 0.15f), Is.False);
                Assert.That(resolver.IsPointClear(new Vector2(5.96f, 6.2f), 0.15f), Is.True);
            }
            finally
            {
                simulation.Dispose();
                Object.DestroyImmediate(sourceProfile);
                Object.DestroyImmediate(sandProfile);
            }
        }

        [Test]
        public void TryResolveSegment_RejectsBlockedStartAndStopsOnExistingStrokeOrRotor()
        {
            CreateSimulation(out SandSimulationProfile profile, out SandSimulation simulation);
            try
            {
                DrawStrokeCollisionResolver resolver = new DrawStrokeCollisionResolver(simulation, new List<SourceDomain>());
                simulation.State.DynamicMask[simulation.State.Index(40, 50)] = true;
                Assert.That(resolver.TryResolveSegment(new Vector2(4.05f, 5.05f), new Vector2(8f, 5.05f),
                    0.15f, out _), Is.False);

                simulation.State.DynamicMask[simulation.State.Index(40, 50)] = false;
                simulation.State.RotatingMask[simulation.State.Index(60, 50)] = true;
                Assert.That(resolver.TryResolveSegment(new Vector2(3f, 5.05f), new Vector2(8f, 5.05f),
                    0.15f, out Vector2 accepted), Is.False);
                Assert.That(accepted.x, Is.LessThan(6f));

                Assert.That(resolver.TryResolveSegment(new Vector2(6.05f, 5.05f), new Vector2(7f, 5.05f),
                    0.15f, out _), Is.False, "A gesture beginning inside blocked geometry must be rejected.");

                simulation.State.RotatingMask[simulation.State.Index(60, 50)] = false;
                simulation.State.CupWallMask[simulation.State.Index(50, 50)] = true;
                Assert.That(resolver.TryResolveSegment(new Vector2(3f, 5.05f), new Vector2(7f, 5.05f),
                    0.15f, out _), Is.False, "Cup and Bowl walls share the authoritative wall mask.");
                Assert.That(resolver.IsPointClear(new Vector2(5.05f, 5.4f), 0.15f), Is.True,
                    "An open receiver mouth remains drawable when its wall mask is open.");
            }
            finally { simulation.Dispose(); Object.DestroyImmediate(profile); }
        }

        private static void CreateSimulation(out SandSimulationProfile profile, out SandSimulation simulation)
        {
            profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.cellSize = 0.1f;
            profile.maxCells = 10000;
            LayoutMaskSet masks = new LayoutMaskSet(100, 100);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            simulation = new SandSimulation(profile, masks);
        }
    }
}
