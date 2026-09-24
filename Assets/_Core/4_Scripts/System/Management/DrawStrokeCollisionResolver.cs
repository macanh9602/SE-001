using System.Collections.Generic;
using SE001.Gameplay;
using SE001.Simulation.Sand;
using UnityEngine;

namespace SE001.System.Management
{
    /// <summary>Shared first-contact resolver for draw preview and committed stroke paths.</summary>
    public sealed class DrawStrokeCollisionResolver
    {
        private readonly SandSimulation simulation;
        private readonly IList<SourceDomain> sources;

        public DrawStrokeCollisionResolver(SandSimulation simulation, IList<SourceDomain> sources)
        {
            this.simulation = simulation;
            this.sources = sources;
        }

        public float EffectiveRadius(float thickness) => Mathf.Max(thickness * 0.5f, simulation.CellSize * 1.5f);

        public bool IsPointClear(Vector2 point, float radius)
        {
            SandSimulationState state = simulation.State;
            float cell = simulation.CellSize;
            if (point.x - radius < 0f || point.y - radius < 0f ||
                point.x + radius >= state.Width * cell || point.y + radius >= state.Height * cell)
                return false;

            int minX = Mathf.FloorToInt((point.x - radius) / cell);
            int maxX = Mathf.FloorToInt((point.x + radius) / cell);
            int minY = Mathf.FloorToInt((point.y - radius) / cell);
            int maxY = Mathf.FloorToInt((point.y + radius) / cell);
            float radiusSq = radius * radius;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int index = state.Index(x, y);
                float dx = (x + 0.5f) * cell - point.x;
                float dy = (y + 0.5f) * cell - point.y;
                if (dx * dx + dy * dy > radiusSq) continue;
                if (!state.ValidMask[index] || state.StaticMask[index] || state.CupWallMask[index] ||
                    state.DynamicMask[index] || state.RotatingMask[index]) return false;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                SourceDomain source = sources[i];
                Vector2 center = source.Position + source.BodyOffset;
                Vector2 half = source.Size * 0.5f;
                float closestX = Mathf.Clamp(point.x, center.x - half.x, center.x + half.x);
                float closestY = Mathf.Clamp(point.y, center.y - half.y, center.y + half.y);
                float dx = point.x - closestX;
                float dy = point.y - closestY;
                if (dx * dx + dy * dy <= radiusSq) return false;
            }

            return true;
        }

        /// <summary>Accepts points up to, but not beyond, the first sampled contact on this segment.</summary>
        public bool TryResolveSegment(Vector2 start, Vector2 requestedEnd, float radius, out Vector2 acceptedEnd)
        {
            acceptedEnd = start;
            if (!IsPointClear(start, radius)) return false;

            Vector2 delta = requestedEnd - start;
            float length = delta.magnitude;
            if (length <= Mathf.Epsilon) return true;
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / (simulation.CellSize * 0.5f)));
            for (int step = 1; step <= steps; step++)
            {
                Vector2 candidate = start + delta * (step / (float)steps);
                if (!IsPointClear(candidate, radius)) return false;
                acceptedEnd = candidate;
            }

            return true;
        }

        public bool ResolveStroke(IList<Vector2> input, float radius, float maxLength,
            List<Vector2> output, out float acceptedLength)
        {
            output.Clear();
            acceptedLength = 0f;
            if (input == null || input.Count < 2 || !IsPointClear(input[0], radius)) return false;
            output.Add(input[0]);

            for (int i = 1; i < input.Count && acceptedLength < maxLength; i++)
            {
                Vector2 start = output[output.Count - 1];
                Vector2 requested = input[i];
                Vector2 delta = requested - start;
                float distance = delta.magnitude;
                if (distance <= Mathf.Epsilon) continue;

                float remainingInk = maxLength - acceptedLength;
                if (distance > remainingInk) requested = start + delta * (remainingInk / distance);
                bool clear = TryResolveSegment(start, requested, radius, out Vector2 accepted);
                float acceptedSegmentLength = Vector2.Distance(start, accepted);
                acceptedLength += acceptedSegmentLength;
                if (acceptedSegmentLength > Mathf.Epsilon) output.Add(accepted);
                if (!clear || acceptedLength >= maxLength) break;
            }

            return output.Count >= 2 && acceptedLength > Mathf.Epsilon;
        }
    }
}
