using System.Collections.Generic;
using NUnit.Framework;
using SE001.Data;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class RotatingObstacleTests
    {
        [Test]
        public void Schema4_StoresObstacleAndOmitsGlobalJarSettings()
        {
            SE001LevelJson level = new SE001LevelJson { schemaVersion = 4 };
            level.sources.Add(new SourceData { stableId = "source", materialId = 1, logicalAmount = 10 });
            level.cups.Add(new CupData { stableId = "cup", acceptedMaterialId = 1, requiredAmount = 5 });
            level.rotatingObstacles.Add(new RotatingObstacleData
            {
                stableId = "cross", position = new Vector2(5f, 5f), scale = 1.2f,
                barLength = 4f, initialAngle = 12f, degreesPerSecond = -60f
            });

            string json = level.ToJson();
            Assert.That(level.schemaVersion, Is.EqualTo(4));
            Assert.That(JsonUtility.ToJson(level.sources[0]), Does.Not.Contain("\"size\""));
            Assert.That(JsonUtility.ToJson(level.cups[0]), Does.Not.Contain("\"size\""));
            Assert.That(json, Does.Not.Contain("\"emissionRate\""));
            Assert.That(json, Does.Not.Contain("\"streamWidth\""));
            SE001LevelJson loaded = SE001LevelJson.FromJson(json);
            Assert.That(loaded.rotatingObstacles, Has.Count.EqualTo(1));
            Assert.That(loaded.rotatingObstacles[0].degreesPerSecond, Is.EqualTo(-60f));
        }

        [Test]
        public void PassiveRotor_NoContactOrNearbySand_DoesNotUseLegacyMotor()
        {
            CreatePassiveSetup(out SandSimulationProfile sandProfile, out SandSimulation simulation,
                out RotatingObstacleProfile profile, out RotatingObstacleSystem obstacle);
            try
            {
                RotatingObstacleState state = obstacle.GetState(0);
                float initial = state.Angle;
                Assert.That(obstacle.Advance(0.1f), Is.Zero);
                Assert.That(state.Angle, Is.EqualTo(initial));
                Assert.That(state.AngularVelocity, Is.Zero);

                Assert.That(simulation.TryEmit(80, 80, 1), Is.True);
                Assert.That(obstacle.Advance(0.1f), Is.Zero);
                Assert.That(state.Angle, Is.EqualTo(initial));
                Assert.That(state.AngularVelocity, Is.Zero);
            }
            finally { DestroySetup(sandProfile, simulation, profile); }
        }

        [Test]
        public void PassiveRotor_RealContactProducesSignedTorqueAndFartherLeverIsStronger()
        {
            float near = ContactVelocityAt(58);
            float far = ContactVelocityAt(65);
            float left = ContactVelocityAt(42);

            Assert.That(near, Is.LessThan(0f));
            Assert.That(far, Is.LessThan(near), "The larger lever arm should produce a larger signed response.");
            Assert.That(left, Is.GreaterThan(0f), "Mirroring the contact across the pivot should reverse torque.");
        }

        [Test]
        public void PassiveRotor_SustainedContactContinuesToAccelerate()
        {
            CreatePassiveSetup(out SandSimulationProfile sandProfile, out SandSimulation simulation,
                out RotatingObstacleProfile profile, out RotatingObstacleSystem obstacle);
            try
            {
                profile.angularDamping = 0f;
                profile.restAngularSpeed = 0f;
                Assert.That(simulation.TryEmit(65, 53, 1), Is.True);
                obstacle.Advance(0.01f);
                float firstVelocity = obstacle.GetState(0).AngularVelocity;
                obstacle.Advance(0.01f);
                Assert.That(Mathf.Abs(obstacle.GetState(0).AngularVelocity), Is.GreaterThan(Mathf.Abs(firstVelocity)));
            }
            finally { DestroySetup(sandProfile, simulation, profile); }
        }

        [Test]
        public void PassiveRotor_IntegrationCapsSpeedAndDampsToRest()
        {
            RotatingObstacleProfile profile = ScriptableObject.CreateInstance<RotatingObstacleProfile>();
            RotatingObstacleState state = new RotatingObstacleState(new RotatingObstacleData
            {
                stableId = "cross", position = new Vector2(5f, 5f), scale = 1f, barLength = 4f, initialAngle = 12f,
                degreesPerSecond = 360f
            });
            try
            {
                profile.angularDamping = 0f;
                profile.maxAngularSpeed = 30f;
                profile.restAngularSpeed = 0f;
                state.Integrate(10000f, 0.1f, profile);
                Assert.That(state.AngularVelocity, Is.EqualTo(30f).Within(0.001f));

                profile.angularDamping = 20f;
                for (int i = 0; i < 40; i++) state.Integrate(0f, 0.1f, profile);
                Assert.That(state.AngularVelocity, Is.LessThan(0.01f));
                profile.restAngularSpeed = 1f;
                state.Integrate(0f, 0.1f, profile);
                Assert.That(state.AngularVelocity, Is.Zero);
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void PassiveRotor_AccelerationCap_SpinsUpSmoothly()
        {
            // Movie_006: a stream hit took the rotor from 0 to the 360 deg/s cap within one frame.
            RotatingObstacleProfile profile = ScriptableObject.CreateInstance<RotatingObstacleProfile>();
            RotatingObstacleState state = new RotatingObstacleState(new RotatingObstacleData
            {
                stableId = "cross", position = new Vector2(5f, 5f), scale = 1f, barLength = 4f, initialAngle = 0f
            });
            try
            {
                profile.angularDamping = 0f;
                profile.restAngularSpeed = 0f;
                profile.maxAngularSpeed = 150f;
                profile.maxAngularAcceleration = 600f;
                state.Integrate(10000f, 1f / 60f, profile);
                Assert.That(state.AngularVelocity, Is.EqualTo(10f).Within(0.01f), "600 deg/s^2 x 1/60 s");
                for (int i = 0; i < 60; i++) state.Integrate(10000f, 1f / 60f, profile);
                Assert.That(state.AngularVelocity, Is.EqualTo(150f).Within(0.01f));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void RotorState_TracksPreviousAngleForRenderInterpolation()
        {
            RotatingObstacleState state = new RotatingObstacleState(new RotatingObstacleData
            {
                stableId = "cross", position = new Vector2(5f, 5f), scale = 1f, barLength = 4f, initialAngle = 30f
            });
            Assert.That(state.PreviousAngle, Is.EqualTo(30f));
            state.MarkStepStart();
            state.SetAngle(36f);
            Assert.That(state.PreviousAngle, Is.EqualTo(30f));
            Assert.That(Mathf.LerpAngle(state.PreviousAngle, state.Angle, 0.5f), Is.EqualTo(33f).Within(0.001f));
        }

        [Test]
        public void Sweep_PreservesGrainAccountingAndFeelData_AndDoesNotAlterStrokeMask()
        {
            CreatePassiveSetup(out SandSimulationProfile sandProfile, out SandSimulation simulation,
                out RotatingObstacleProfile profile, out RotatingObstacleSystem obstacle);
            try
            {
                profile.sandTorqueScale = 1000f;
                profile.angularDamping = 0f;
                profile.restAngularSpeed = 0f;
                profile.maxAngularSpeed = 240f;
                simulation.SetDynamic(20, 20, true);
                Assert.That(simulation.TryEmit(65, 53, 1), Is.True);
                int source = simulation.State.Index(65, 53);
                simulation.State.Shade[source] = 211;
                simulation.State.Velocity[source] = 2.5f;
                simulation.State.Momentum[source] = -0.75f;

                obstacle.Advance(0.5f);

                Assert.That(simulation.State.OccupiedCount, Is.EqualTo(1));
                Assert.That(simulation.State.EmittedCount, Is.EqualTo(1));
                Assert.That(simulation.State.DynamicMask[simulation.State.Index(20, 20)], Is.True);
                int grain = FindGrain(simulation.State.Cells);
                Assert.That(grain, Is.GreaterThanOrEqualTo(0));
                Assert.That(simulation.State.Cells[grain], Is.EqualTo(1));
                Assert.That(simulation.State.Shade[grain], Is.EqualTo(211));
                Assert.That(simulation.State.Velocity[grain], Is.EqualTo(2.5f));
                Assert.That(simulation.State.Momentum[grain], Is.EqualTo(-0.75f));
            }
            finally { DestroySetup(sandProfile, simulation, profile); }
        }

        [Test]
        public void Sweep_ClampsBeforeCommittedStrokeAndOppositeTorqueCanMoveAway()
        {
            CreatePassiveSetup(out SandSimulationProfile sandProfile, out SandSimulation simulation,
                out RotatingObstacleProfile profile, out RotatingObstacleSystem obstacle);
            try
            {
                profile.sandTorqueScale = 1000f;
                profile.angularDamping = 0f;
                profile.restAngularSpeed = 0f;
                simulation.SetDynamic(30, 54, true);
                Assert.That(simulation.TryEmit(65, 53, 1), Is.True);
                RotatingObstacleState state = obstacle.GetState(0);
                float initialAngle = state.Angle;

                obstacle.Advance(0.1f);

                Assert.That(state.Angle, Is.LessThan(initialAngle));
                Assert.That(state.Angle, Is.GreaterThan(initialAngle - 6f), "Sweep must clamp near first contact, not tunnel through.");
                Assert.That(state.AngularVelocity, Is.Zero);
                Assert.That(simulation.State.DynamicMask[simulation.State.Index(30, 54)], Is.True);
                for (int i = 0; i < simulation.State.Cells.Length; i++)
                    Assert.That(!(simulation.State.DynamicMask[i] && simulation.State.RotatingMask[i]), Is.True,
                        "Rotor must not overlap the committed stroke.");

                simulation.Remove(65, 53);
                Assert.That(simulation.TryEmit(42, 53, 1), Is.True);
                obstacle.Advance(0.01f);
                Assert.That(state.AngularVelocity, Is.GreaterThan(0f), "Opposite sand torque can rotate away from the stroke.");
                Assert.That(simulation.State.DynamicMask[simulation.State.Index(30, 54)], Is.True);
            }
            finally { DestroySetup(sandProfile, simulation, profile); }
        }

        private static float ContactVelocityAt(int x)
        {
            CreatePassiveSetup(out SandSimulationProfile sandProfile, out SandSimulation simulation,
                out RotatingObstacleProfile profile, out RotatingObstacleSystem obstacle);
            try
            {
                profile.angularDamping = 0f;
                profile.restAngularSpeed = 0f;
                Assert.That(simulation.TryEmit(x, 53, 1), Is.True);
                obstacle.Advance(0.01f);
                return obstacle.GetState(0).AngularVelocity;
            }
            finally { DestroySetup(sandProfile, simulation, profile); }
        }

        private static void CreatePassiveSetup(out SandSimulationProfile sandProfile, out SandSimulation simulation,
            out RotatingObstacleProfile profile, out RotatingObstacleSystem obstacle)
        {
            sandProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            sandProfile.cellSize = 0.1f;
            sandProfile.maxCells = 10000;
            LayoutMaskSet masks = new LayoutMaskSet(100, 100);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            simulation = new SandSimulation(sandProfile, masks);
            profile = ScriptableObject.CreateInstance<RotatingObstacleProfile>();
            profile.barWidth = 0.6f;
            profile.pushSearchCells = 12;
            profile.sandTorqueScale = 18f;
            obstacle = new RotatingObstacleSystem(simulation, profile,
                new List<RotatingObstacleData>
                {
                    new RotatingObstacleData { stableId = "cross", position = new Vector2(5f, 5f),
                        scale = 1f, barLength = 4f, initialAngle = 45f, degreesPerSecond = 90f }
                });
        }

        private static int FindGrain(byte[] cells)
        {
            for (int i = 0; i < cells.Length; i++) if (cells[i] != 0) return i;
            return -1;
        }

        private static void DestroySetup(SandSimulationProfile sandProfile, SandSimulation simulation,
            RotatingObstacleProfile profile)
        {
            simulation.Dispose();
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(sandProfile);
        }

    }
}
