using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Data
{
    [Serializable]
    public sealed class SE001LevelJson
    {
        public int schemaVersion = 5;
        public string levelId = string.Empty;
        public string layoutId = string.Empty;
        public BoardData board = new BoardData();
        [NonSerialized] public List<StaticObstacleData> staticObstacles = new List<StaticObstacleData>();
        public List<SourceData> sources = new List<SourceData>();
        public List<CupData> cups = new List<CupData>();
        public List<RotatingObstacleData> rotatingObstacles = new List<RotatingObstacleData>();
        public float sourceScale = 1f;
        public float bowlScale = 1f;
        public float sourceEmissionRate;
        public int sourceStreamWidth;
        public float drawInkBudget;
        public bool requiresDrawing;

        public static SE001LevelJson FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Level JSON is empty.", nameof(json));
            SchemaProbe probe = JsonUtility.FromJson<SchemaProbe>(json);
            SE001LevelJson level = probe != null && probe.schemaVersion < 3
                ? FromLegacyJson(json)
                : JsonUtility.FromJson<SE001LevelJson>(json);
            if (level == null) throw new FormatException("Level JSON could not be deserialized.");
            level.EnsureCollections();
            return level;
        }

        public string ToJson(bool prettyPrint = true)
        {
            EnsureCollections();
            if (schemaVersion < 3)
            {
                LegacyLevelJson legacy = new LegacyLevelJson
                {
                    schemaVersion = schemaVersion,
                    levelId = levelId,
                    board = new LegacyBoardData { size = board.size, wallContours = board.wallContours },
                    staticObstacles = staticObstacles,
                    sources = sources,
                    cups = cups,
                    drawInkBudget = drawInkBudget,
                    requiresDrawing = requiresDrawing
                };
                return JsonUtility.ToJson(legacy, prettyPrint);
            }

            return JsonUtility.ToJson(this, prettyPrint);
        }

        public void EnsureCollections()
        {
            if (board == null) board = new BoardData();
            if (board.wallContours == null) board.wallContours = new List<PolygonContourData>();
            if (staticObstacles == null) staticObstacles = new List<StaticObstacleData>();
            if (sources == null) sources = new List<SourceData>();
            if (cups == null) cups = new List<CupData>();
            if (rotatingObstacles == null) rotatingObstacles = new List<RotatingObstacleData>();
        }

        private static SE001LevelJson FromLegacyJson(string json)
        {
            LegacyLevelJson legacy = JsonUtility.FromJson<LegacyLevelJson>(json);
            if (legacy == null) throw new FormatException("Legacy level JSON could not be deserialized.");
            return new SE001LevelJson
            {
                schemaVersion = legacy.schemaVersion,
                levelId = legacy.levelId,
                board = new BoardData
                {
                    size = legacy.board != null ? legacy.board.size : Vector2.one,
                    wallContours = legacy.board != null && legacy.board.wallContours != null
                        ? legacy.board.wallContours
                        : new List<PolygonContourData>()
                },
                staticObstacles = legacy.staticObstacles ?? new List<StaticObstacleData>(),
                sources = legacy.sources ?? new List<SourceData>(),
                cups = legacy.cups ?? new List<CupData>(),
                drawInkBudget = legacy.drawInkBudget,
                requiresDrawing = legacy.requiresDrawing
            };
        }

        [Serializable]
        private sealed class SchemaProbe
        {
            public int schemaVersion;
        }

        [Serializable]
        private sealed class LegacyLevelJson
        {
            public int schemaVersion;
            public string levelId = string.Empty;
            public LegacyBoardData board = new LegacyBoardData();
            public List<StaticObstacleData> staticObstacles = new List<StaticObstacleData>();
            public List<SourceData> sources = new List<SourceData>();
            public List<CupData> cups = new List<CupData>();
            public float drawInkBudget;
            public bool requiresDrawing;
        }

        [Serializable]
        private sealed class LegacyBoardData
        {
            public Vector2 size = Vector2.one;
            public List<PolygonContourData> wallContours = new List<PolygonContourData>();
        }
    }

    [Serializable]
    public sealed class BoardData
    {
        public Vector2 size = Vector2.one;
        [NonSerialized] public List<PolygonContourData> wallContours = new List<PolygonContourData>();
    }

    [Serializable]
    public sealed class StaticObstacleData
    {
        public string stableId = string.Empty;
        public List<PolygonContourData> contours = new List<PolygonContourData>();
        public string styleId = string.Empty;
    }

    [Serializable]
    public sealed class PolygonContourData
    {
        public List<Vector2> points = new List<Vector2>();
    }
    [Serializable]
    public sealed class SourceData
    {
        public string stableId = string.Empty;
        public int materialId = 1;
        public Vector2 position;
        public int logicalAmount;
        public bool startsOpen;
    }
    [Serializable]
    public sealed class CupData
    {
        public string stableId = string.Empty;
        public int acceptedMaterialId = 1;
        public Vector2 position;
        public int requiredAmount;
    }

    [Serializable]
    public sealed class RotatingObstacleData
    {
        public string stableId = string.Empty;
        public Vector2 position;
        public float scale = 1f;
        public float barLength = 4f;
        public float initialAngle;
        public float degreesPerSecond = 60f;
    }
}
