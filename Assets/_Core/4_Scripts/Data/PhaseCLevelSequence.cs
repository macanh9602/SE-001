using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Data
{
    [Serializable] public struct LevelSequenceEntry { public string levelId; }

    [CreateAssetMenu(fileName = "PhaseCLevelSequence", menuName = "SE001/Profiles/Level Sequence")]
    public sealed class PhaseCLevelSequence : ScriptableObject
    {
        public List<LevelSequenceEntry> levels = new List<LevelSequenceEntry>();
    }
}
