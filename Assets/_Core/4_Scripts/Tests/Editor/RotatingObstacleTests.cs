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
        public void Sweep_PushesGrainWithoutLoss_AndPreservesStrokeMask()
        {
            SandSimulationProfile sandProfile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            sandProfile.cellSize = 0.1f;
            sandProfile.maxCells = 10000;
            LayoutMaskSet masks = new LayoutMaskSet(100, 100);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            SandSimulation simulation = new SandSimulation(sandProfile, masks);
            RotatingObstacleProfile profile = ScriptableObject.CreateInstance<RotatingObstacleProfile>();
            profile.barWidth = 0.6f;
            profile.pushSearchCells = 12;
            RotatingObstacleSystem obstacle = new RotatingObstacleSystem(simulation, profile,
                new List<RotatingObstacleData>
                {
                    new RotatingObstacleData { stableId = "cross", position = new Vector2(5f, 5f),
                        scale = 1f, barLength = 4f, degreesPerSecond = 90f }
                });
            simulation.SetDynamic(20, 20, true);
            Assert.That(simulation.TryEmit(60, 50, 1), Is.True);
            int pushed = obstacle.Advance(0.5f);

            Assert.That(pushed, Is.EqualTo(1));
            Assert.That(simulation.State.OccupiedCount, Is.EqualTo(1));
            Assert.That(simulation.State.Cells[simulation.State.Index(60, 50)], Is.EqualTo(0));
            Assert.That(simulation.State.DynamicMask[simulation.State.Index(20, 20)], Is.True);
            Assert.That(simulation.State.RotatingMask[simulation.State.Index(60, 50)], Is.True);
            simulation.Dispose();
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(sandProfile);
        }

    }
}
