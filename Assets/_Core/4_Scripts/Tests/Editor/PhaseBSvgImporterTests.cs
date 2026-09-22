#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using SE001.Editor.Level;
using UnityEngine;

namespace SE001.Editor.Level.Tests
{
    public sealed class PhaseBSvgImporterTests
    {
        [Test]
        public void TestFixture_ImportsDeterministically()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(projectRoot, "TrashStuff", "test_tool.svg");
            string output = Path.Combine(Application.dataPath, "_Core", "Resources", "Levels", "phase_b_svg_test.temp.json");
            PhaseBSvgImportSettings settings = new PhaseBSvgImportSettings { legacyTestFixture = true };
            string error;
            try
            {
                Assert.That(PhaseBSvgImporter.TryImport(source, output, settings, out error), Is.True, error);
                string first = File.ReadAllText(output);
                Assert.That(PhaseBSvgImporter.TryImport(source, output, settings, out error), Is.True, error);
                string second = File.ReadAllText(output);
                Assert.That(second, Is.EqualTo(first));
                Assert.That(SE001.Data.SE001LevelJson.FromJson(first).staticObstacles.Count, Is.GreaterThan(0));
            }
            finally
            {
                if (File.Exists(output)) File.Delete(output);
            }
        }
    }
}
#endif
