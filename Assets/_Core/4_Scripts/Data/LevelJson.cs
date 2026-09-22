using System;
using System.Collections.Generic;
using UnityEngine;

namespace SE001.Data
{
    [Serializable]
    public sealed class SE001LevelJson
    {
        public int schemaVersion = 2;
        public string levelId = string.Empty;
        public BoardData board = new BoardData();
        public List<StaticObstacleData> staticObstacles = new List<StaticObstacleData>();
        public List<SourceData> sources = new List<SourceData>();
        public List<CupData> cups = new List<CupData>();
        public float drawInkBudget;

        public static SE001LevelJson FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Level JSON is empty.", nameof(json));
            SE001LevelJson level = JsonUtility.FromJson<SE001LevelJson>(json);
            if (level == null) throw new FormatException("Level JSON could not be deserialized.");
            level.EnsureCollections();
            return level;
        }

        public string ToJson(bool prettyPrint = true)
        {
            EnsureCollections();
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public void EnsureCollections()
        {
            if (board == null) board = new BoardData();
            if (board.wallContours == null) board.wallContours = new List<PolygonContourData>();
            if (staticObstacles == null) staticObstacles = new List<StaticObstacleData>();
            if (sources == null) sources = new List<SourceData>();
            if (cups == null) cups = new List<CupData>();
        }
    }

    [Serializable] public sealed class BoardData { public Vector2 size = Vector2.one; public List<PolygonContourData> wallContours = new List<PolygonContourData>(); }
    [Serializable] public sealed class StaticObstacleData { public string stableId = string.Empty; public List<PolygonContourData> contours = new List<PolygonContourData>(); public string styleId = string.Empty; }
    [Serializable] public sealed class PolygonContourData { public List<Vector2> points = new List<Vector2>(); }
    [Serializable]
    public sealed class SourceData
    {
        public string stableId = string.Empty;
        public int materialId = 1;
        public Vector2 position;
        public Vector2 size;
        public int logicalAmount;
        public bool startsOpen;
        public float emissionRate;
        public float streamWidth;
    }
    [Serializable] public sealed class CupData { public string stableId = string.Empty; public int acceptedMaterialId = 1; public Vector2 position; public Vector2 size = Vector2.one; public int requiredAmount; }
}
