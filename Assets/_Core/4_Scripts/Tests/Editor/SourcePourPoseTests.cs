using NUnit.Framework;
using SE001.Creation;
using SE001.Data;
using SE001.Gameplay;
using SE001.Presentation;
using UnityEngine;

namespace SE001.Tests
{
    /// <summary>Source emits from the mouth tip in the pour pose; sand inside moves with the jar rotation.</summary>
    public sealed class SourcePourPoseTests
    {
        [Test]
        public void Source_PourPose_MouthTipSitsOnEmitPoint()
        {
            GameplayRuntimeProfile runtime = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            SourceDomain source = CreateSource(runtime, true);
            GameObject parent = new GameObject("SourcePourPoseTest");
            try
            {
                PhaseCSourceVisual visual = new SourceFactory().Create(new SourceCreateParameters(
                    source, parent.transform, runtime.prefabProfile, runtime));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(visual.Pivot.localEulerAngles.z, 0f)), Is.LessThan(0.01f));
                float scale = runtime.jarVisualProfile.SourceUniformScale(source.Size.y);
                float mouthHalfHeight = 0.14f * scale;
                float mouthTipY = visual.MouthRenderer.transform.position.y - mouthHalfHeight;
                Assert.That(mouthTipY, Is.EqualTo(source.Position.y).Within(0.01f), "Sand must leave from the mouth, not the jar base.");
                Assert.That(visual.MouthRenderer.transform.position.x, Is.EqualTo(source.Position.x).Within(0.001f));
                Assert.That(visual.BodyRenderer.transform.position.y, Is.GreaterThan(source.Position.y));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SandSolver_HalfFill_UprightAndInverted_SplitArea()
        {
            JarVisualProfile profile = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            SourceSandFillSolver solver = SourceSandFillSolver.Create(profile);
            Assert.That(solver, Is.Not.Null, "JarVisualProfile needs baked sourceFillRowSpans.");
            float down = solver.SolveThreshold(Vector2.down, 0.5f);
            float up = solver.SolveThreshold(Vector2.up, 0.5f);
            // Same plane seen from both sides: t_down ≈ -t_up when exactly half is filled.
            Assert.That(down, Is.EqualTo(-up).Within(0.03f));
        }

        [Test]
        public void SandSolver_IsMonotonic_AndHandlesEmptyFull()
        {
            JarVisualProfile profile = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            SourceSandFillSolver solver = SourceSandFillSolver.Create(profile);
            Vector2 tilted = new Vector2(0.5f, -0.866f);
            float previous = float.MaxValue;
            for (int i = 1; i < 10; i++)
            {
                float threshold = solver.SolveThreshold(tilted, i / 10f);
                Assert.That(threshold, Is.LessThanOrEqualTo(previous), "More sand must lower the plane offset.");
                previous = threshold;
            }

            Assert.That(solver.SolveThreshold(tilted, 0f), Is.EqualTo(SourceSandFillSolver.NoSand));
            Assert.That(solver.SolveThreshold(tilted, 1f), Is.EqualTo(SourceSandFillSolver.FullSand));
        }

        [Test]
        public void SandSolver_WideEndHoldsMoreThanNeck()
        {
            // Authored art is mouth-down: object-space down points to the neck. 30% sand fills a taller band at
            // the narrow neck than at the wide end, which is what makes the level area-correct.
            JarVisualProfile profile = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            SourceSandFillSolver solver = SourceSandFillSolver.Create(profile);
            float neckSide = solver.SolveThreshold(Vector2.down, 0.3f);
            float wideSide = solver.SolveThreshold(Vector2.up, 0.3f);
            float neckBand = 0.96f - neckSide;
            float wideBand = 0.96f - wideSide;
            Assert.That(neckBand, Is.GreaterThan(wideBand));
        }

        [Test]
        public void SandTilt_FollowsRotation_ThenSettlesLevel()
        {
            GameplayRuntimeProfile runtime = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            SourceDomain source = CreateSource(runtime, false);
            GameObject parent = new GameObject("SourceSandTiltTest");
            try
            {
                PhaseCSourceVisual visual = new SourceFactory().Create(new SourceCreateParameters(
                    source, parent.transform, runtime.prefabProfile, runtime));
                source.Toggle();
                float maxTilt = 0f;
                for (int frame = 0; frame < 6; frame++)
                {
                    visual.Tick(1f / 60f);
                    maxTilt = Mathf.Max(maxTilt, Mathf.Abs(visual.SandTilt));
                }

                Assert.That(maxTilt, Is.GreaterThan(1f), "Sand should lag the jar while it rotates.");
                Assert.That(maxTilt, Is.LessThanOrEqualTo(runtime.jarVisualProfile.sandReposeAngle + 0.001f));
                for (int frame = 0; frame < 600; frame++) visual.Tick(1f / 60f);
                Assert.That(visual.SandTilt, Is.EqualTo(0f), "Sand must settle level once the jar stops.");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static SourceDomain CreateSource(GameplayRuntimeProfile runtime, bool startsOpen)
        {
            return new SourceDomain(
                new SourceData
                {
                    stableId = "pour_pose_source",
                    materialId = 1,
                    position = new Vector2(2f, 3f),
                    size = new Vector2(1f, 1.2f),
                    logicalAmount = 4,
                    startsOpen = startsOpen
                },
                runtime.sourceProfile,
                runtime.sandProfile.grainsPerUnit,
                runtime.jarVisualProfile);
        }
    }
}
