using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "MaterialPalette", menuName = "SE001/Profiles/Material Palette")]
    public sealed class MaterialPalette : ScriptableObject
    {
        public List<MaterialPaletteEntry> entries = new List<MaterialPaletteEntry>();
        public Color32 GetSandColor(byte id) { for (int i = 0; i < entries.Count; i++) if (entries[i].materialId == id) return entries[i].sandColor; return Color.clear; }
        public bool Contains(int id) { for (int i = 0; i < entries.Count; i++) if (entries[i].materialId == id) return true; return false; }
    }

    [Serializable]
    public struct MaterialPaletteEntry
    {
        public int materialId;
        public Color32 sandColor;
        public Color32 uiColor;
    }
}
