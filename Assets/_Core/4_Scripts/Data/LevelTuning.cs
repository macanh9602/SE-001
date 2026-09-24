using UnityEngine;

namespace SE001.Data
{
    /// <summary>Resolves authored level tuning against the shared art and simulation profiles.</summary>
    public static class LevelTuning
    {
        public static Vector2 SourceSize(SE001LevelJson level, SourceProfile profile) =>
            profile.bodySize * (level == null || level.schemaVersion < 5 ? 1f : level.sourceScale);

        public static Vector2 BowlSize(SE001LevelJson level, BowlVisualProfile profile) =>
            profile.WorldSize * (level == null || level.schemaVersion < 5 ? 1f : level.bowlScale);

        public static float EmissionRate(SE001LevelJson level, SourceProfile profile) =>
            level == null || level.schemaVersion < 5 ? profile.emissionRate : level.sourceEmissionRate;

        public static int StreamWidth(SE001LevelJson level, SourceProfile profile) =>
            level == null || level.schemaVersion < 5 ? profile.streamWidth : level.sourceStreamWidth;

        public static void UpgradeToSchema5(SE001LevelJson level, SourceProfile profile)
        {
            if (level.schemaVersion >= 5) return;
            level.sourceScale = 1f;
            level.bowlScale = 1f;
            level.sourceEmissionRate = profile.emissionRate;
            level.sourceStreamWidth = profile.streamWidth;
            level.schemaVersion = 5;
        }
    }
}
