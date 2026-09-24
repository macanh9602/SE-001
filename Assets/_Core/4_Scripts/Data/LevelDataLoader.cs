using System;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Data
{
    public static class LevelDataLoader
    {
        public static SE001LevelJson Load(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level id is required.", nameof(levelId));
            string overrideJson;
            SE001LevelJson level;
            if (LevelPlayTestOverride.TryConsume(levelId, out overrideJson))
            {
                level = SE001LevelJson.FromJson(overrideJson);
            }
            else
            {
                TextAsset asset = Resources.Load<TextAsset>("Levels/" + levelId);
                if (asset == null) throw new InvalidOperationException("Canonical level JSON was not found: " + levelId);
                level = SE001LevelJson.FromJson(asset.text);
            }
            if (level.schemaVersion != 3 && level.schemaVersion != 4 && level.schemaVersion != 5)
                throw new FormatException("Level " + levelId + " uses unsupported schema " + level.schemaVersion + ". Open it in the Level Editor and select a baked layout.");
            ColorProfile colorProfile = Resources.Load<ColorProfile>("Profiles/PhaseCColorProfile");
            if (colorProfile == null) throw new InvalidOperationException("Phase C ColorProfile asset is missing.");
            SandSimulationProfile sandProfile = Resources.Load<SandSimulationProfile>("Profiles/PhaseBSandSimulationProfile");
            if (sandProfile == null) throw new InvalidOperationException("Phase C SandSimulationProfile asset is missing.");
            LayoutDefinition layout = LoadLayout(level.layoutId);
            LevelDataValidator.Validate(level, colorProfile, sandProfile.cellSize, sandProfile.maxCells, layout);
            if (!string.Equals(level.levelId, levelId, StringComparison.Ordinal)) throw new FormatException("Level id does not match its resource path.");
            return level;
        }

        public static LayoutDefinition LoadLayout(string layoutId)
        {
            if (string.IsNullOrWhiteSpace(layoutId)) throw new FormatException("Level layoutId is required.");
            LayoutDefinition layout = Resources.Load<LayoutDefinition>("Layouts/" + layoutId);
            if (layout == null) throw new InvalidOperationException("Layout definition was not found: " + layoutId);
            if (!string.Equals(layout.layoutId, layoutId, StringComparison.Ordinal))
                throw new FormatException("Layout definition id does not match its resource path: " + layoutId);
            return layout;
        }
    }
}
