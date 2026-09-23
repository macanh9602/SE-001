using System;
using System.Collections.Generic;
using SE001.Data;
using UnityEngine;

namespace SE001.Simulation.Sand
{
    /// <summary>Owns moving Cross occupancy and conservative sand displacement on the simulation grid.</summary>
    public sealed class RotatingObstacleSystem
    {
        private readonly SandSimulation simulation;
        private readonly RotatingObstacleProfile profile;
        private readonly RotatingObstacleState[] obstacles;
        private readonly bool[] nextMask;
        private readonly List<int> previousCells;
        private readonly List<int> nextCells;
        private readonly List<int> owners;

        public RotatingObstacleSystem(SandSimulation simulation, RotatingObstacleProfile profile,
            IList<RotatingObstacleData> data)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (data == null) throw new ArgumentNullException(nameof(data));
            obstacles = new RotatingObstacleState[data.Count];
            int estimatedCells = 0;
            for (int i = 0; i < data.Count; i++)
            {
                RotatingObstacleData item = data[i];
                obstacles[i] = new RotatingObstacleState(item);
                float length = item.barLength * item.scale;
                float width = profile.barWidth * item.scale;
                estimatedCells += Mathf.CeilToInt(2f * (length + simulation.CellSize) *
                    (width + simulation.CellSize) / (simulation.CellSize * simulation.CellSize));
            }
            int capacity = Mathf.Min(simulation.State.Cells.Length, Mathf.Max(16, estimatedCells));
            previousCells = new List<int>(capacity);
            nextCells = new List<int>(capacity);
            owners = new List<int>(capacity);
            nextMask = new bool[simulation.State.Cells.Length];
            Advance(0f);
        }

        public int Count => obstacles.Length;
        public RotatingObstacleState GetState(int index) => obstacles[index];

        public int Advance(float dt)
        {
            SandSimulationState state = simulation.State;
            for (int i = 0; i < previousCells.Count; i++) nextMask[previousCells[i]] = false;
            nextCells.Clear();
            owners.Clear();
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacles[i].Advance(dt);
                Raster(i);
            }

            int pushed = 0;
            for (int i = 0; i < nextCells.Count; i++)
            {
                int index = nextCells[i];
                if (state.Cells[index] != 0 && Push(index, obstacles[owners[i]])) pushed++;
            }
            for (int i = 0; i < previousCells.Count; i++) state.RotatingMask[previousCells[i]] = false;
            for (int i = 0; i < nextCells.Count; i++) state.RotatingMask[nextCells[i]] = true;
            previousCells.Clear();
            previousCells.AddRange(nextCells);
            return pushed;
        }

        private void Raster(int owner)
        {
            RotatingObstacleState obstacle = obstacles[owner];
            float cell = simulation.CellSize;
            float halfLength = obstacle.BarLength * obstacle.Scale * 0.5f;
            float halfWidth = profile.barWidth * obstacle.Scale * 0.5f;
            float radius = halfLength + halfWidth;
            int minX = Mathf.Max(0, Mathf.FloorToInt((obstacle.Position.x - radius) / cell));
            int maxX = Mathf.Min(simulation.State.Width - 1, Mathf.CeilToInt((obstacle.Position.x + radius) / cell));
            int minY = Mathf.Max(0, Mathf.FloorToInt((obstacle.Position.y - radius) / cell));
            int maxY = Mathf.Min(simulation.State.Height - 1, Mathf.CeilToInt((obstacle.Position.y + radius) / cell));
            float angle = (obstacle.Angle + 45f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x + 0.5f) * cell - obstacle.Position.x;
                float dy = (y + 0.5f) * cell - obstacle.Position.y;
                float along = dx * cos + dy * sin;
                float across = -dx * sin + dy * cos;
                bool first = Mathf.Abs(along) <= halfLength && Mathf.Abs(across) <= halfWidth;
                bool second = Mathf.Abs(across) <= halfLength && Mathf.Abs(along) <= halfWidth;
                if (!first && !second) continue;
                int index = stateIndex(x, y);
                if (nextMask[index]) continue;
                nextMask[index] = true;
                nextCells.Add(index);
                owners.Add(owner);
            }
        }

        private int stateIndex(int x, int y) => y * simulation.State.Width + x;

        private bool Push(int sourceIndex, RotatingObstacleState obstacle)
        {
            SandSimulationState state = simulation.State;
            int width = state.Width;
            int sourceX = sourceIndex % width;
            int sourceY = sourceIndex / width;
            float relativeX = (sourceX + 0.5f) * simulation.CellSize - obstacle.Position.x;
            float relativeY = (sourceY + 0.5f) * simulation.CellSize - obstacle.Position.y;
            float sign = Mathf.Sign(obstacle.DegreesPerSecond);
            float tangentX = -relativeY * sign;
            float tangentY = relativeX * sign;
            int limit = profile.pushSearchCells;
            for (int radius = 1; radius <= limit; radius++)
            {
                int chosen = -1;
                float best = float.NegativeInfinity;
                for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                    int x = sourceX + dx;
                    int y = sourceY + dy;
                    if (x < 0 || y < 0 || x >= width || y >= state.Height) continue;
                    int target = stateIndex(x, y);
                    if (nextMask[target] || !state.ValidMask[target] || state.StaticMask[target] ||
                        state.CupWallMask[target] || state.DynamicMask[target] || state.Cells[target] != 0) continue;
                    float score = dx * tangentX + dy * tangentY;
                    if (score <= best) continue;
                    best = score;
                    chosen = target;
                }
                if (chosen >= 0) return simulation.TryRelocateGrain(sourceIndex, chosen, nextMask);
            }
            return false;
        }
    }

    public sealed class RotatingObstacleState
    {
        public RotatingObstacleState(RotatingObstacleData data)
        {
            StableId = data.stableId;
            Position = data.position;
            Scale = data.scale;
            BarLength = data.barLength;
            angle = data.initialAngle;
            DegreesPerSecond = data.degreesPerSecond;
        }

        public string StableId { get; }
        public Vector2 Position { get; }
        public float Scale { get; }
        public float BarLength { get; }
        private float angle;
        public float Angle => angle;
        public float DegreesPerSecond { get; }
        public void Advance(float dt) => angle = Mathf.Repeat(angle + DegreesPerSecond * dt, 360f);
    }
}
