#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SE001.Gameplay;
using SE001.System.Management;
using UnityEditor;
using UnityEngine;

namespace SE001.Editor
{
    public static class PhaseCPerfCapture
    {
        private const int ChunkSize = 60;
        private const int MaxSteps = 100000;

        [MenuItem("SE001/Phase C/Perf capture")]
        private static void Capture()
        {
            GameObject owner = new GameObject("PhaseCPerfCapture");
            LevelManager manager = owner.AddComponent<LevelManager>();
            StringBuilder report = new StringBuilder();
            report.AppendLine("# Phase C C-R1 performance capture");
            report.AppendLine();
            report.AppendLine("EditMode capture using `GameplayManager.AdvanceSteps` in chunks of 60. GC delta is approximate.");
            report.AppendLine();
            report.AppendLine("| Level | Grid | Steps | Max grains | Avg ms/step | Max ms/step | GC delta bytes | Result |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");

            try
            {
                string[] levels = { "phase_c_level_02" };
                for (int i = 0; i < levels.Length; i++) report.AppendLine(CaptureLevel(manager, levels[i]));
                string path = Path.Combine(Directory.GetCurrentDirectory(), "handoff/phase-C-complete-playable-core/perf-C-R1.md");
                File.WriteAllText(path, report.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("SE001 Phase C performance capture written to " + path);
            }
            finally
            {
                if (manager.CurrentContext != null) manager.UnloadCurrentLevel();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static string CaptureLevel(LevelManager manager, string levelId)
        {
            manager.BeginLevel(levelId);
            GameplayManager gameplay = manager.CurrentContext.LevelRoot.GetComponent<GameplayManager>();
            for (int i = 0; i < gameplay.Sources.Count; i++)
                if (!gameplay.Sources[i].IsPouring) gameplay.ToggleSource(gameplay.Sources[i].StableId);

            int gcBefore = (int)GC.GetTotalMemory(false);
            int steps = 0;
            int maxGrains = 0;
            long elapsedTicks = 0;
            long maxChunkTicks = 0;
            Stopwatch stopwatch = new Stopwatch();
            while (gameplay.State == GameState.Playing && steps < MaxSteps)
            {
                stopwatch.Restart();
                gameplay.AdvanceSteps(ChunkSize);
                stopwatch.Stop();
                long chunkTicks = stopwatch.ElapsedTicks;
                elapsedTicks += chunkTicks;
                maxChunkTicks = Math.Max(maxChunkTicks, chunkTicks);
                steps += ChunkSize;
                maxGrains = Math.Max(maxGrains, CountGrains(manager.CurrentContext.SandSimulation.State.Cells));
            }

            float tickToMs = 1000f / Stopwatch.Frequency;
            float avgMsPerStep = steps > 0 ? elapsedTicks * tickToMs / steps : 0f;
            float maxMsPerStep = maxChunkTicks * tickToMs / ChunkSize;
            int gcDelta = (int)GC.GetTotalMemory(false) - gcBefore;
            string result = gameplay.State == GameState.Playing ? "STEP_CAP" : gameplay.State.ToString();
            Vector2Int grid = new Vector2Int(manager.CurrentContext.SandSimulation.State.Width, manager.CurrentContext.SandSimulation.State.Height);
            manager.UnloadCurrentLevel();
            return $"| {levelId} | {grid.x}x{grid.y} | {steps} | {maxGrains} | {avgMsPerStep:0.000} | {maxMsPerStep:0.000} | {gcDelta} | {result} |";
        }

        private static int CountGrains(byte[] cells)
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++) if (cells[i] != 0) count++;
            return count;
        }
    }
}
#endif
