#if UNITY_EDITOR
using SE001.Data;
using SE001.Gameplay;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.Editor.Level
{
    /// <summary>Authoring-only fill target recalculation after a Bowl Scale edit.</summary>
    internal static class BowlScaleRequiredAmount
    {
        public static void Apply(SE001LevelJson level, float scale, CupProfile cupProfile,
            BowlVisualProfile bowlProfile, SandSimulationProfile sandProfile)
        {
            if (level == null) return;
            level.bowlScale = scale;
            level.EnsureCollections();
            if (cupProfile == null || bowlProfile == null || !bowlProfile.IsBaked || sandProfile == null ||
                !FinitePositive(scale) || !FinitePositive(sandProfile.cellSize) || level.board == null ||
                !FinitePositive(level.board.size.x) || !FinitePositive(level.board.size.y)) return;

            Vector2 size = LevelTuning.BowlSize(level, bowlProfile);
            if (!FinitePositive(size.x) || !FinitePositive(size.y) ||
                size.x > level.board.size.x || size.y > level.board.size.y) return;

            int grainsPerUnit = CupDomain.GrainsPerUnitFor(sandProfile.grainsPerUnit, ReceiverStyle.Bowl,
                bowlProfile, sandProfile.cellSize);
            for (int i = 0; i < level.cups.Count; i++)
            {
                CupData cup = level.cups[i];
                if (cup == null) continue;
                CupDomain measured = new CupDomain(cup, cupProfile, grainsPerUnit, sandProfile.cellSize,
                    null, ReceiverStyle.Bowl, bowlProfile, level);
                // A Bowl smaller than one logical unit stays at 1 so existing capacity validation blocks it.
                cup.requiredAmount = Mathf.Max(1, measured.Capacity / grainsPerUnit);
            }
        }

        private static bool FinitePositive(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
#endif
