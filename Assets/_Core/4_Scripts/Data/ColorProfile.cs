using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Data
{
    [CreateAssetMenu(fileName = "PhaseCColorProfile", menuName = "SE001/Profiles/Color Profile")]
    public sealed class ColorProfile : ScriptableObject
    {
        public List<ColorProfileEntry> entries = new List<ColorProfileEntry>();

        public Color32 GetSandColor(byte colorId)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].colorId == colorId)
                    return entries[i].sandColor;
            return Color.clear;
        }

        public Color32 GetUiColor(byte colorId)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].colorId == colorId)
                    return entries[i].uiColor;
            return Color.clear;
        }

        public bool Contains(int colorId)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].colorId == colorId)
                    return true;
            return false;
        }

        public Color32[] BuildSandLookup()
        {
            int maxId = 0;
            for (int i = 0; i < entries.Count; i++)
                maxId = Mathf.Max(maxId, entries[i].colorId);

            Color32[] lookup = new Color32[maxId + 1];
            for (int i = 0; i < entries.Count; i++)
            {
                ColorProfileEntry entry = entries[i];
                if (entry.colorId >= 0 && entry.colorId < lookup.Length)
                    lookup[entry.colorId] = entry.sandColor;
            }
            return lookup;
        }
    }

    [Serializable]
    public struct ColorProfileEntry
    {
        public int colorId;
        public Color32 sandColor;
        public Color32 uiColor;
        public string displayName;
    }
}
