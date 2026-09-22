using System;
using UnityEngine;

namespace SE001.Data
{
    public static class LevelDataLoader
    {
        public static SE001LevelJson Load(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level id is required.", nameof(levelId));
            TextAsset asset = Resources.Load<TextAsset>("Levels/" + levelId);
            if (asset == null) throw new InvalidOperationException("Canonical level JSON was not found: " + levelId);
            SE001LevelJson level = SE001LevelJson.FromJson(asset.text);
            MaterialPalette palette = Resources.Load<MaterialPalette>("Profiles/PhaseCMaterialPalette");
            if (palette == null) throw new InvalidOperationException("Phase C MaterialPalette asset is missing.");
            LevelDataValidator.Validate(level, palette);
            if (!string.Equals(level.levelId, levelId, StringComparison.Ordinal)) throw new FormatException("Level id does not match its resource path.");
            return level;
        }
    }
}
