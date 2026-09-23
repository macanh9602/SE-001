using NUnit.Framework;
using SE001.Elements.Sand;
using SE001.Geometry;
using SE001.Simulation.Sand;
using UnityEditor;
using UnityEngine;

namespace SE001.Tests
{
    /// <summary>Sand Level Lab decisions (2026-09-24): creep drains flat obstacles, grid still settles, new texel encoding.</summary>
    public sealed class SandFeelTests
    {
        [Test]
        public void Creep_DrainsLayerOffWideFlatLedge()
        {
            LayoutMaskSet masks = Open(60, 40);
            for (int x = 5; x <= 54; x++) masks.StaticMask[masks.Index(x, 10)] = true;
            SandSimulationProfile profile = Profile();
            SandSimulation sim = new SandSimulation(profile, masks);
            try
            {
                for (int x = 20; x <= 39; x++) Assert.That(sim.TryEmit(x, 11, 1), Is.True);
                for (int i = 0; i < 4000; i++) sim.Step();
                int onLedge = 0;
                for (int y = 11; y < 40; y++)
                    for (int x = 5; x <= 54; x++)
                        if (sim.IsOccupied(x, y)) onLedge++;
                Assert.That(onLedge, Is.EqualTo(0), "no grain may stay parked on a flat obstacle");
            }
            finally
            {
                sim.Dispose();
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Creep_Off_KeepsLegacyBehaviour()
        {
            LayoutMaskSet masks = Open(60, 40);
            for (int x = 5; x <= 54; x++) masks.StaticMask[masks.Index(x, 10)] = true;
            SandSimulationProfile profile = Profile();
            profile.creepChance = 0f;
            SandSimulation sim = new SandSimulation(profile, masks);
            try
            {
                for (int x = 20; x <= 39; x++) sim.TryEmit(x, 11, 1);
                for (int i = 0; i < 2000; i++) sim.Step();
                int onLedge = 0;
                for (int x = 5; x <= 54; x++)
                    if (sim.IsOccupied(x, 11)) onLedge++;
                Assert.That(onLedge, Is.GreaterThan(0), "without creep a wide ledge keeps its layer (documents the old bug)");
            }
            finally
            {
                sim.Dispose();
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Creep_GridStillReachesStableState_OnOpenFloor()
        {
            LayoutMaskSet masks = Open(80, 60);
            SandSimulationProfile profile = Profile();
            SandSimulation sim = new SandSimulation(profile, masks);
            try
            {
                for (int step = 0; step < 400; step++)
                {
                    sim.TryEmit(40, 55, 1);
                    sim.Step();
                }

                int quietSteps = 0;
                for (int step = 0; step < 6000 && quietSteps < 60; step++)
                    quietSteps = sim.Step() == 0 ? quietSteps + 1 : 0;
                Assert.That(quietSteps, Is.GreaterThanOrEqualTo(60), "creep must not keep the grid moving forever (lose detection)");
            }
            finally
            {
                sim.Dispose();
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Creep_IsDeterministic()
        {
            byte[] first = RunPour(123u);
            byte[] second = RunPour(123u);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void FieldTexture_EncodesMaterialToneAndFlags()
        {
            LayoutMaskSet masks = Open(10, 10);
            SandSimulationProfile profile = Profile();
            profile.maxCells = 100;
            SandSimulation sim = new SandSimulation(profile, masks);
            GameObject host = new GameObject("SandFieldEncodeTest", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                Assert.That(sim.TryEmit(4, 0, 3), Is.True);
                SandFieldVisual visual = host.AddComponent<SandFieldVisual>();
                visual.Bind(sim);
                visual.UpdateTexture();
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                host.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                Texture2D texture = (Texture2D)block.GetTexture("_BaseMap");
                Color32 cell = texture.GetPixels32()[masks.Index(4, 0)];
                Assert.That(cell.r, Is.EqualTo(3), "R = material id");
                Assert.That(cell.g, Is.EqualTo(sim.State.Shade[masks.Index(4, 0)]), "G = grain tone");
                Assert.That(cell.b & 1, Is.EqualTo(1), "surface flag (nothing above)");
                Assert.That(cell.a, Is.EqualTo(255));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(block.GetTexture("_Palette"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                sim.Dispose();
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SandFieldPrefab_UsesRoundGrains()
        {
            // Movie_009 regression: the prefab kept a legacy "softRender: 0" so the game drew square debug cells.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/3_Prefabs/Gameplay/Sand/SandField.prefab");
            Assert.That(prefab, Is.Not.Null);
            SandFieldVisual visual = prefab.GetComponentInChildren<SandFieldVisual>(true);
            Assert.That(visual, Is.Not.Null);
            SerializedProperty round = new SerializedObject(visual).FindProperty("roundGrains");
            Assert.That(round, Is.Not.Null);
            Assert.That(round.boolValue, Is.True, "Production sand must use the round-grain shader path.");
        }

        [Test]
        public void StreamTrail_BridgesBehindFallingGrain_ThenFadesOut()
        {
            // Movie_010 regression: sparse falls read as a broken stream; the fading trail keeps the column continuous.
            LayoutMaskSet masks = Open(10, 60);
            SandSimulationProfile profile = Profile();
            profile.maxCells = 100;
            SandSimulation sim = new SandSimulation(profile, masks);
            GameObject host = new GameObject("SandTrailTest", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                Assert.That(sim.TryEmit(5, 58, 2), Is.True);
                SandFieldVisual visual = host.AddComponent<SandFieldVisual>();
                visual.Bind(sim);
                for (int frame = 0; frame < 6; frame++)
                {
                    sim.Step();
                    visual.UpdateTexture();
                }

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                host.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                Texture2D texture = (Texture2D)block.GetTexture("_BaseMap");
                Assert.That(texture.GetPixel(5, 57).a, Is.GreaterThan(0f), "the cell the grain left must keep a trail");
                for (int frame = 0; frame < 400; frame++)
                {
                    sim.Step();
                    visual.UpdateTexture();
                }

                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (sim.State.Cells[i] == 0) Assert.That(pixels[i].a, Is.EqualTo(0), "trail must fade out once sand rests");
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
                sim.Dispose();
                Object.DestroyImmediate(profile);
            }
        }

        private static byte[] RunPour(uint seed)
        {
            LayoutMaskSet masks = Open(60, 40);
            for (int x = 5; x <= 54; x++) masks.StaticMask[masks.Index(x, 10)] = true;
            SandSimulationProfile profile = Profile();
            SandSimulation sim = new SandSimulation(profile, masks, seed);
            try
            {
                for (int step = 0; step < 600; step++)
                {
                    if (step < 200) sim.TryEmit(30, 35, 1);
                    sim.Step();
                }

                return (byte[])sim.State.Cells.Clone();
            }
            finally
            {
                sim.Dispose();
                Object.DestroyImmediate(profile);
            }
        }

        private static SandSimulationProfile Profile()
        {
            SandSimulationProfile profile = ScriptableObject.CreateInstance<SandSimulationProfile>();
            profile.maxCells = 100000;
            return profile;
        }

        private static LayoutMaskSet Open(int width, int height)
        {
            LayoutMaskSet masks = new LayoutMaskSet(width, height);
            for (int i = 0; i < masks.ValidMask.Length; i++) masks.ValidMask[i] = true;
            return masks;
        }
    }
}
