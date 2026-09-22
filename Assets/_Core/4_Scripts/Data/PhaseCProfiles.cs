using System;
using System.Collections.Generic;
using UnityEngine;
using SE001.Simulation.Sand;

namespace SE001.Data
{
    [Serializable] public struct MaterialPaletteEntry { public int materialId; public Color32 sandColor; public Color32 uiColor; }

    [CreateAssetMenu(fileName = "MaterialPalette", menuName = "SE001/Profiles/Material Palette")]
    public sealed class MaterialPalette : ScriptableObject
    {
        public List<MaterialPaletteEntry> entries = new List<MaterialPaletteEntry>();
        public Color32 GetSandColor(byte id) { for (int i = 0; i < entries.Count; i++) if (entries[i].materialId == id) return entries[i].sandColor; return new Color32(220, 180, 100, 255); }
        public bool Contains(int id) { for (int i = 0; i < entries.Count; i++) if (entries[i].materialId == id) return true; return false; }
    }

    [CreateAssetMenu(fileName = "SourceProfile", menuName = "SE001/Profiles/Source")]
    public sealed class SourceProfile : ScriptableObject { public float emissionRate = 16f; public int streamWidth = 6; public float valveOpenDelay = 0.2f; public float valveCloseRotateTime = 0.15f; public float hitPadding = 0.2f; }
    [CreateAssetMenu(fileName = "CupProfile", menuName = "SE001/Profiles/Cup")]
    public sealed class CupProfile : ScriptableObject { public float wallThickness = 0.12f; }
    [CreateAssetMenu(fileName = "DrawPathProfile", menuName = "SE001/Profiles/Draw Path")]
    public sealed class DrawPathProfile : ScriptableObject { public float drawThickness = 0.12f; public float minPointDistance = 0.08f; public int maxPointsPerStroke = 128; public int maxStrokes = 16; public float defaultInkBudget = 8f; public float drawStartDeadZone = 18f; public float extrudeHeight = 0.18f; }
    [CreateAssetMenu(fileName = "GameplayRuntimeProfile", menuName = "SE001/Profiles/Gameplay Runtime")]
    public sealed class GameplayRuntimeProfile : ScriptableObject
    {
        public MaterialPalette materialPalette;
        public SourceProfile sourceProfile;
        public CupProfile cupProfile;
        public DrawPathProfile drawPathProfile;
        public SandSimulationProfile sandProfile;
        public PrefabProfile prefabProfile;
        public int grainsPerUnit = 12;
        public int stableStepsForLose = 30;
        public float fixedStepHz = 60f;
    }
}
