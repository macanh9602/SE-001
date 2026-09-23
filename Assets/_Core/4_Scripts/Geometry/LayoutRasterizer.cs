using System;
using System.Collections.Generic;
using SE001.Data;
using UnityEngine;

namespace SE001.Geometry
{
    public sealed class LayoutMaskSet
    {
        public LayoutMaskSet(int width, int height)
        {
            Width = width;
            Height = height;
            ValidMask = new bool[width * height];
            StaticMask = new bool[width * height];
        }
        public int Width { get; }
        public int Height { get; }
        public bool[] ValidMask { get; }
        public bool[] StaticMask { get; }
        public int Index(int x, int y) => y * Width + x;
    }

    public static class LayoutRasterizer
    {
        private static int rasterizeCallCount;

        public static int RasterizeCallCount => rasterizeCallCount;

        public static void ResetCallCount()
        {
            rasterizeCallCount = 0;
        }

        public static LayoutMaskSet Rasterize(SE001LevelJson level, float cellSize, int maxCells)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            rasterizeCallCount++;
            int width = Mathf.Max(1, Mathf.CeilToInt(level.board.size.x / cellSize));
            int height = Mathf.Max(1, Mathf.CeilToInt(level.board.size.y / cellSize));
            long count = (long)width * height;
            if (count > maxCells) throw new InvalidOperationException($"Layout grid {width}x{height} exceeds capacity {maxCells}.");
            LayoutMaskSet result = new LayoutMaskSet(width, height);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                bool blockedByWall = ContainsAny(level.board.wallContours, point);
                bool blockedByObstacle = false;
                for (int o = 0; o < level.staticObstacles.Count && !blockedByObstacle; o++)
                {
                    blockedByObstacle = ContainsAny(level.staticObstacles[o].contours, point);
                }
                int index = result.Index(x, y);
                result.StaticMask[index] = blockedByWall || blockedByObstacle;
                result.ValidMask[index] = point.x >= 0f && point.x <= level.board.size.x &&
                    point.y >= 0f && point.y <= level.board.size.y && !result.StaticMask[index];
            }
            return result;
        }

        private static bool ContainsAny(List<PolygonContourData> contours, Vector2 point)
        {
            if (contours == null) return false;
            for (int i = 0; i < contours.Count; i++)
            {
                if (contours[i] != null && PolygonUtility.ContainsPoint(contours[i].points, point))
                    return true;
            }
            return false;
        }
    }
}
