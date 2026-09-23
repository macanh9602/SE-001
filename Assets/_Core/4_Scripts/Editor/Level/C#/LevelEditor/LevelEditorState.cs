#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SE001.Data;
using SE001.Geometry;
using UnityEngine;

namespace SE001.Editor.Level
{
    internal enum LevelEditorSelectionKind { None, Source, Cup, RotatingObstacle }

    internal enum LevelEditorIssueSeverity { Blocking, Warning, Info }

    [Serializable]
    internal sealed class LevelEditorViewState
    {
        public LevelEditorSelectionKind selectionKind;
        public string selectedStableId = string.Empty;
        public string activeTool = "Select";
        public float zoom = 1f;
        public Vector2 pan;
        public string levelSearch = string.Empty;
        public bool layoutsExpanded = true;
        public bool sourcesExpanded = true;
        public bool cupsExpanded = true;
        public bool rotatingObstaclesExpanded = true;
        public int visibleLayouts = 25;
        public int visibleSources = 25;
        public int visibleCups = 25;
        public int visibleRotatingObstacles = 25;
        public float levelsPaneWidth = 190f;
        public float inspectorPaneWidth = 280f;
        public bool validationExpanded = true;
    }

    internal sealed class LevelEditorIssue
    {
        public LevelEditorIssueSeverity Severity;
        public string StableId = string.Empty;
        public string FieldKey = string.Empty;
        public string What = string.Empty;
        public string Where = string.Empty;
        public string How = string.Empty;

        public string UserMessage
        {
            get
            {
                string wherePart = string.IsNullOrWhiteSpace(Where) ? string.Empty : " | " + Where;
                string howPart = string.IsNullOrWhiteSpace(How) ? string.Empty : " | " + How;
                return What + wherePart + howPart;
            }
        }
    }

    internal sealed class LevelEditorDerivedState
    {
        public LayoutDefinition Layout;
        public LayoutMaskSet Masks;
        public ColorProfile Colors;
        public float CellSize;
        public int MaxCells;
        public readonly List<LevelEditorIssue> Issues = new List<LevelEditorIssue>();

        public void Clear()
        {
            Layout = null;
            Masks = null;
            Colors = null;
            CellSize = 0f;
            MaxCells = 0;
            Issues.Clear();
        }
    }
}
#endif
