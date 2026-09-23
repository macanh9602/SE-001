using NUnit.Framework;
using SE001.Data;
using SE001.Creation;
using SE001.Editor;
using SE001.Gameplay;
using SE001.Presentation;
using UnityEditor;
using UnityEngine;

namespace SE001.Tests
{
    public sealed class JarVisualTests
    {
        [Test]
        public void ColorProfile_HasSevenEntries_WithBothMaterials()
        {
            ColorProfile profile = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.entries, Has.Count.EqualTo(7));
            for (int i = 1; i <= 7; i++) Assert.That(profile.HasJarMaterials(i), Is.True, "colorId " + i);
        }

        [Test]
        public void ColorProfile_LegacyIds1And2_StillResolve()
        {
            ColorProfile profile = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            ColorProfileEntry first;
            ColorProfileEntry second;
            Assert.That(profile.TryGetEntry(1, out first), Is.True);
            Assert.That(profile.TryGetEntry(2, out second), Is.True);
            Assert.That(first.displayName, Is.EqualTo("Red"));
            Assert.That(second.displayName, Is.EqualTo("Cobalt"));
        }

        [Test]
        public void JarMaterials_Rebuild_IsIdempotent_PreservesGuids()
        {
            const string capPath = "Assets/_Core/1_Materials/Jar/MAT_CupCap_Red.mat";
            const string mouthPath = "Assets/_Core/1_Materials/Jar/MAT_SourceMouth_Red.mat";
            string capGuid = AssetDatabase.AssetPathToGUID(capPath);
            string mouthGuid = AssetDatabase.AssetPathToGUID(mouthPath);
            Assert.That(capGuid, Is.Not.Empty);
            Assert.That(mouthGuid, Is.Not.Empty);

            JarVisualAssetSetup.RebuildJarMaterials();

            Assert.That(AssetDatabase.AssetPathToGUID(capPath), Is.EqualTo(capGuid));
            Assert.That(AssetDatabase.AssetPathToGUID(mouthPath), Is.EqualTo(mouthGuid));
        }

        [Test]
        public void SourceFill_LevelIsAreaCorrect()
        {
            JarVisualProfile profile = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            float heightAtHalfArea = JarVisualGeometry.AreaToHeight(0.5f, profile.sourceFillAreaLut);
            Assert.That(Mathf.Abs(heightAtHalfArea - 0.5f), Is.GreaterThan(0.005f));
        }

        [Test]
        public void SourceFill_SettlesWorldDown_AtIdleAndPouring()
        {
            GameObject pivot = new GameObject("JarVisualDirectionTest");
            try
            {
                pivot.transform.localRotation = Quaternion.identity;
                Assert.That(pivot.transform.InverseTransformDirection(Vector3.down).y, Is.LessThan(0f));
                pivot.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                Assert.That(pivot.transform.InverseTransformDirection(Vector3.down).y, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(pivot);
            }
        }

        [Test]
        public void Cup_Taper0_GeometryMatchesArtRect()
        {
            CupProfile profile = Resources.Load<CupProfile>("Profiles/PhaseCCupProfile");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.taper, Is.EqualTo(0f));
        }

        [Test]
        public void Cup_NineSlice_CapThicknessConstantAcrossHeights()
        {
            GameplayRuntimeProfile runtime = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            CupProfile profile = runtime.cupProfile;
            GameObject parent = new GameObject("JarNineSliceTest");
            try
            {
                PhaseCCupVisual shortCup = new CupFactory().Create(new CupCreateParameters(
                    new CupDomain(
                        new CupData
                        {
                            stableId = "short",
                            acceptedMaterialId = 1,
                            position = Vector2.zero,
                            size = new Vector2(2f, 1.5f),
                            requiredAmount = 1
                        }, profile, 1, 0.1f),
                    parent.transform, runtime.prefabProfile, runtime));
                PhaseCCupVisual tallCup = new CupFactory().Create(new CupCreateParameters(
                    new CupDomain(
                        new CupData
                        {
                            stableId = "tall",
                            acceptedMaterialId = 1,
                            position = new Vector2(3f, 0f),
                            size = new Vector2(2f, 3f),
                            requiredAmount = 1
                        }, profile, 1, 0.1f),
                    parent.transform, runtime.prefabProfile, runtime));
                Assert.That(shortCup.CapTopRenderer.transform.localScale.y, Is.EqualTo(tallCup.CapTopRenderer.transform.localScale.y));
                Assert.That(shortCup.CapBottomRenderer.transform.localScale.y, Is.EqualTo(tallCup.CapBottomRenderer.transform.localScale.y));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Source_UniformScale_HitTestMatchesVisualBounds()
        {
            SourceProfile sourceProfile = Resources.Load<SourceProfile>("Profiles/PhaseCSourceProfile");
            JarVisualProfile visuals = Resources.Load<JarVisualProfile>("Profiles/JarVisualProfile");
            SourceDomain source = new SourceDomain(
                new SourceData { stableId = "source", materialId = 1, position = new Vector2(2f, 3f), size = new Vector2(2f, 2f), logicalAmount = 1 },
                sourceProfile,
                1,
                visuals);
            Assert.That(source.BodyOffset.y, Is.EqualTo(2f * (212f / 192f - 0.5f)).Within(0.0001f));
            Assert.That(source.HitTest(source.Position + source.BodyOffset + new Vector2(0.99f, 0.99f), 0f), Is.True);
            Assert.That(source.HitTest(source.Position + source.BodyOffset + new Vector2(1.1f, 0f), 0f), Is.False);
        }

        [Test]
        public void Source_ClosedPose_IsMouthUp_AboveBody()
        {
            GameplayRuntimeProfile runtime = Resources.Load<GameplayRuntimeProfile>("Profiles/PhaseCGameplayRuntimeProfile");
            SourceDomain source = new SourceDomain(
                new SourceData
                {
                    stableId = "closed_source",
                    materialId = 1,
                    position = new Vector2(2f, 3f),
                    size = new Vector2(1f, 2f),
                    logicalAmount = 1
                },
                runtime.sourceProfile,
                runtime.sandProfile.grainsPerUnit,
                runtime.jarVisualProfile);
            GameObject parent = new GameObject("SourcePoseTest");
            try
            {
                PhaseCSourceVisual visual = new SourceFactory().Create(new SourceCreateParameters(
                    source, parent.transform, runtime.prefabProfile, runtime));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(visual.Pivot.localEulerAngles.z, 180f)), Is.LessThan(0.01f));
                Assert.That(visual.MouthRenderer.transform.position.y, Is.GreaterThan(visual.BodyRenderer.transform.position.y));
                // Emit point is the mouth tip in the POUR pose (see SourcePourPoseTests); idle only needs mouth-up.
                Assert.That(visual.MouthRenderer.transform.position.y, Is.GreaterThan(source.Position.y));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void JarPreview_Cache_NoRebuildWithoutChange()
        {
            JarPreviewUtility.ClearCache();
            int before = JarPreviewUtility.BuildCount;
            Texture2D first = JarPreviewUtility.GetPreview(JarPreviewKind.Cup, 1, new Vector2(1.5f, 1.5f), 0f);
            Texture2D second = JarPreviewUtility.GetPreview(JarPreviewKind.Cup, 1, new Vector2(1.5f, 1.5f), 0f);
            Assert.That(second, Is.SameAs(first));
            Assert.That(JarPreviewUtility.BuildCount, Is.EqualTo(before + 1));
            JarPreviewUtility.ClearCache();
        }

        [Test]
        public void JarPreview_CpuMirror_MatchesGpu()
        {
            ColorProfile profile = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            Assert.That(profile.TryGetEntry(1, out ColorProfileEntry entry), Is.True);
            Material sourceMaterial = entry.cupCapMaterial;
            Texture2D sourceTexture = sourceMaterial.GetTexture("_BaseMap") as Texture2D;
            Assert.That(sourceTexture, Is.Not.Null);

            Color sample = sourceTexture.GetPixelBilinear(0.5f, 0.1f);
            Assert.That(sample.a, Is.GreaterThan(0.5f));
            Texture2D onePixel = new Texture2D(1, 1, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Point };
            onePixel.SetPixel(0, 0, sample);
            onePixel.Apply(false, false);
            Material gpuMaterial = Object.Instantiate(sourceMaterial);
            gpuMaterial.SetTexture("_BaseMap", onePixel);
            RenderTexture renderTarget = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Texture2D readback = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(Texture2D.whiteTexture, renderTarget, gpuMaterial);
                RenderTexture.active = renderTarget;
                readback.ReadPixels(new Rect(0f, 0f, 1f, 1f), 0, 0);
                readback.Apply(false, false);
                Color cpu = JarPreviewUtility.EvaluateMaterialColor(sample, sourceMaterial);
                Color gpu = readback.GetPixel(0, 0);
                Color expectedPremultiplied = new Color(cpu.r * cpu.a, cpu.g * cpu.a, cpu.b * cpu.a, cpu.a);
                float error = Vector3.Distance(
                    new Vector3(expectedPremultiplied.r, expectedPremultiplied.g, expectedPremultiplied.b),
                    new Vector3(gpu.r, gpu.g, gpu.b));
                Assert.That(error, Is.LessThan(0.12f), "CPU mirror must follow the GPU tint path within editor readback tolerance.");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(renderTarget);
                Object.DestroyImmediate(gpuMaterial);
                Object.DestroyImmediate(onePixel);
            }
        }
    }
}
