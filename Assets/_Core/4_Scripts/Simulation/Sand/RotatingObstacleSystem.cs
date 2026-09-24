using System;
using System.Collections.Generic;
using SE001.Data;
using UnityEngine;

namespace SE001.Simulation.Sand
{
    /// <summary>
    /// Passive sand-driven cross rotor.
    /// Sand contributes impact torque; angular velocity carries inertia.
    /// DrawStroke is a hard mechanical stop, while dense sand yields through bounded grain-chain displacement.
    /// </summary>
    public sealed class RotatingObstacleSystem
    {
        private readonly SandSimulation simulation;
        private readonly RotatingObstacleProfile profile;
        private readonly RotatingObstacleState[] obstacles;

        private readonly bool[] nextMask;
        private readonly bool[] candidateMask;
        private readonly bool[] sweptMask;

        private readonly List<int> previousCells;
        private readonly List<int> nextCells;
        private readonly List<int> owners;
        private readonly List<int> nextOwners;
        private readonly List<int> candidateCells;
        private readonly List<int> sweptCells;

        // Bounded, reused BFS scratch for granular yielding.
        private readonly int[] searchQueue;
        private readonly int[] searchParents;
        private readonly int[] searchStamps;
        private int searchStamp;

        // One incoming grain contributes at most once per rotor per step.
        private readonly int[] contactStamps;
        private int contactStamp;

        private readonly float[] contactTorques;
        private readonly float[] previousAngles;

        public RotatingObstacleSystem(
            SandSimulation simulation,
            RotatingObstacleProfile profile,
            IList<RotatingObstacleData> data)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (data == null) throw new ArgumentNullException(nameof(data));

            obstacles = new RotatingObstacleState[data.Count];
            contactTorques = new float[data.Count];
            previousAngles = new float[data.Count];

            int cellCount = simulation.State.Cells.Length;
            int maxSweptCapacity = 0;
            int maxRasterCapacity = 16;

            for (int i = 0; i < data.Count; i++)
            {
                RotatingObstacleData item = data[i];
                obstacles[i] = new RotatingObstacleState(item);

                int boundsCapacity = EstimateBoundsCapacity(
                    item, profile.barWidth, simulation.CellSize, cellCount);

                maxRasterCapacity = Mathf.Max(maxRasterCapacity, boundsCapacity);
                maxSweptCapacity = Mathf.Min(cellCount, maxSweptCapacity + boundsCapacity);
            }

            maxSweptCapacity = Mathf.Max(16, maxSweptCapacity);

            previousCells = new List<int>(maxSweptCapacity);
            nextCells = new List<int>(maxSweptCapacity);
            owners = new List<int>(maxSweptCapacity);
            nextOwners = new List<int>(maxSweptCapacity);
            candidateCells = new List<int>(maxRasterCapacity);
            sweptCells = new List<int>(maxSweptCapacity);

            nextMask = new bool[cellCount];
            candidateMask = new bool[cellCount];
            sweptMask = new bool[cellCount];

            int reach = Mathf.Clamp(profile.pushSearchCells, 1, 64);
            int searchWidth = Mathf.Min(simulation.State.Width, reach * 2 + 1);
            int searchHeight = Mathf.Min(simulation.State.Height, reach * 2 + 1);
            int searchCapacity = Mathf.Max(1, searchWidth * searchHeight);

            searchQueue = new int[searchCapacity];
            searchParents = new int[searchCapacity];
            searchStamps = new int[searchCapacity];
            contactStamps = new int[cellCount];

            BuildNextMask();
            CommitMask();
        }

        public int Count => obstacles.Length;
        public RotatingObstacleState GetState(int index) => obstacles[index];

        /// <summary>
        /// Advances one fixed gameplay step.
        /// Contact torque is sampled from the current rotor raster and the grain motion produced by the prior sand step.
        /// </summary>
        public int Advance(float dt)
        {
            if (dt <= 0f || obstacles.Length == 0) return 0;

            ClearScratch();
            CollectContactTorques();

            bool anyMotion = false;

            for (int i = 0; i < obstacles.Length; i++)
            {
                RotatingObstacleState obstacle = obstacles[i];
                previousAngles[i] = obstacle.Angle;

                float delta = obstacle.Integrate(contactTorques[i], dt, profile);
                if (Mathf.Abs(delta) <= Mathf.Epsilon) continue;

                anyMotion = true;
                SweepObstacle(i, delta);
            }

            if (!anyMotion) return 0;

            BuildNextMask();

            int pushed = 0;
            bool blockedBySand = false;

            // Process swept grains deterministically. A dense pile can yield through a chain of adjacent grains
            // until the bounded search reaches a real empty cell. No grain teleports.
            for (int i = 0; i < sweptCells.Count; i++)
            {
                int source = sweptCells[i];
                if (simulation.State.Cells[source] == 0) continue;

                if (!TryDisplaceGrainChain(source, out int moved))
                {
                    blockedBySand = true;
                    break;
                }

                pushed += moved;
            }

            if (blockedBySand)
            {
                // Sand congestion behaves like friction/pressure, not a rigid brake.
                // Keep the rotor at the last safe angle for this step and retain part of its inertia.
                for (int i = 0; i < obstacles.Length; i++)
                {
                    obstacles[i].SetAngle(previousAngles[i]);
                    obstacles[i].RetainVelocity(profile.sandBlockVelocityRetention, profile.restAngularSpeed);
                }

                BuildNextMask();
                CommitMask();
                ClearSweepMarks();
                return pushed;
            }

            CommitMask();
            ClearSweepMarks();
            return pushed;
        }

        private void SweepObstacle(int owner, float delta)
        {
            RotatingObstacleState obstacle = obstacles[owner];

            float radius = (obstacle.BarLength + profile.barWidth) * obstacle.Scale * 0.5f;
            float arcLimitedDegrees = radius > 0f
                ? simulation.CellSize * 0.5f / radius * Mathf.Rad2Deg
                : profile.collisionSweepStepDegrees;

            float maxStep = Mathf.Max(
                0.01f,
                Mathf.Min(profile.collisionSweepStepDegrees, arcLimitedDegrees));

            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(delta) / maxStep));
            float start = previousAngles[owner];
            float lastValid = start;
            bool blockedByStroke = false;

            for (int step = 1; step <= steps; step++)
            {
                float angle = start + delta * (step / (float)steps);
                RasterCandidate(owner, angle);

                if (CandidateHitsDrawStroke())
                {
                    blockedByStroke = true;
                    break;
                }

                AddCandidateToSweep();
                lastValid = angle;
            }

            obstacle.SetAngle(blockedByStroke ? lastValid : start + delta);

            // DrawStroke is intentionally the hard mechanical stop.
            if (blockedByStroke) obstacle.Stop();
        }

        /// <summary>
        /// Incoming sand motion generates torque. A stationary grain does not act like a perpetual motor.
        /// Each grain is counted at most once per rotor each fixed step.
        /// </summary>
        private void CollectContactTorques()
        {
            SandSimulationState state = simulation.State;
            float cell = simulation.CellSize;

            for (int owner = 0; owner < obstacles.Length; owner++)
            {
                int stamp = NextContactStamp();
                RotatingObstacleState obstacle = obstacles[owner];

                for (int i = 0; i < previousCells.Count; i++)
                {
                    if (owners[i] != owner) continue;

                    int rotorIndex = previousCells[i];
                    int rotorX = rotorIndex % state.Width;
                    int rotorY = rotorIndex / state.Width;

                    for (int side = 0; side < 4; side++)
                    {
                        int dx = side == 0 ? -1 : side == 1 ? 1 : 0;
                        int dy = side == 2 ? -1 : side == 3 ? 1 : 0;

                        int grainX = rotorX + dx;
                        int grainY = rotorY + dy;

                        if (grainX < 0 || grainY < 0 ||
                            grainX >= state.Width || grainY >= state.Height)
                            continue;

                        int grainIndex = grainY * state.Width + grainX;

                        if (state.Cells[grainIndex] == 0 ||
                            contactStamps[grainIndex] == stamp)
                            continue;

                        Vector2 grainMotion = new Vector2(
                            state.Momentum[grainIndex],
                            -state.Velocity[grainIndex]);

                        if (grainMotion.sqrMagnitude <= 0.000001f)
                            continue;

                        // Grain must actually be moving toward this rotor face.
                        Vector2 towardRotor = new Vector2(-dx, -dy);
                        float approachSpeed = Vector2.Dot(grainMotion, towardRotor);

                        if (approachSpeed <= 0.0001f)
                            continue;

                        Vector2 lever = new Vector2(
                            (grainX + 0.5f) * cell - obstacle.Position.x,
                            (grainY + 0.5f) * cell - obstacle.Position.y);

                        // Use real incoming motion as the force direction/magnitude.
                        // Lever arm therefore makes outer-blade impacts naturally stronger.
                        float torque =
                            (lever.x * grainMotion.y - lever.y * grainMotion.x) *
                            profile.sandTorqueScale;

                        if (Mathf.Abs(torque) <= Mathf.Epsilon)
                            continue;

                        contactStamps[grainIndex] = stamp;
                        contactTorques[owner] += torque;
                    }
                }
            }
        }

        private void RasterCandidate(int owner, float angle)
        {
            for (int i = 0; i < candidateCells.Count; i++)
                candidateMask[candidateCells[i]] = false;

            candidateCells.Clear();
            RasterObstacle(owner, angle, candidateMask, candidateCells, null);
        }

        private bool CandidateHitsDrawStroke()
        {
            SandSimulationState state = simulation.State;

            for (int i = 0; i < candidateCells.Count; i++)
                if (state.DynamicMask[candidateCells[i]])
                    return true;

            return false;
        }

        private void AddCandidateToSweep()
        {
            for (int i = 0; i < candidateCells.Count; i++)
            {
                int index = candidateCells[i];
                if (sweptMask[index]) continue;

                sweptMask[index] = true;
                sweptCells.Add(index);
            }
        }

        /// <summary>
        /// Finds the nearest empty cell through a bounded region of movable sand and shifts the whole
        /// grain chain one adjacent cell toward that empty slot. This gives dense powder room to yield
        /// without teleportation and preserves all per-grain simulation data through TryRelocateGrain.
        /// </summary>
        private bool TryDisplaceGrainChain(int source, out int movedCount)
        {
            movedCount = 0;

            SandSimulationState state = simulation.State;
            int sourceX = source % state.Width;
            int sourceY = source / state.Width;
            int reach = Mathf.Clamp(profile.pushSearchCells, 1, 64);

            int minX = Mathf.Max(0, sourceX - reach);
            int maxX = Mathf.Min(state.Width - 1, sourceX + reach);
            int minY = Mathf.Max(0, sourceY - reach);
            int maxY = Mathf.Min(state.Height - 1, sourceY + reach);

            int windowWidth = maxX - minX + 1;
            int stamp = NextSearchStamp();

            int head = 0;
            int tail = 0;

            int rootLocal = (sourceY - minY) * windowWidth + (sourceX - minX);
            searchStamps[rootLocal] = stamp;
            searchParents[rootLocal] = -1;
            searchQueue[tail++] = source;

            int empty = -1;

            while (head < tail)
            {
                int current = searchQueue[head++];
                int x = current % state.Width;
                int y = current / state.Width;

                for (int side = 0; side < 4; side++)
                {
                    int nx = x + (side == 0 ? -1 : side == 1 ? 1 : 0);
                    int ny = y + (side == 2 ? -1 : side == 3 ? 1 : 0);

                    if (nx < minX || nx > maxX || ny < minY || ny > maxY)
                        continue;

                    int local = (ny - minY) * windowWidth + (nx - minX);

                    if (searchStamps[local] == stamp)
                        continue;

                    int index = ny * state.Width + nx;

                    if (!CanTraverseDisplacement(index))
                        continue;

                    searchStamps[local] = stamp;
                    searchParents[local] = current;

                    if (state.Cells[index] == 0)
                    {
                        empty = index;
                        head = tail; // finish outer loop
                        break;
                    }

                    if (tail >= searchQueue.Length)
                        continue;

                    searchQueue[tail++] = index;
                }
            }

            if (empty < 0)
                return false;

            int hole = empty;

            while (hole != source)
            {
                int hx = hole % state.Width;
                int hy = hole / state.Width;
                int local = (hy - minY) * windowWidth + (hx - minX);
                int parent = searchParents[local];

                if (parent < 0)
                    return false;

                if (!simulation.TryRelocateGrain(parent, hole, nextMask))
                    return false;

                movedCount++;
                hole = parent;
            }

            return true;
        }

        private bool CanTraverseDisplacement(int index)
        {
            SandSimulationState state = simulation.State;

            return state.ValidMask[index] &&
                   !state.StaticMask[index] &&
                   !state.CupWallMask[index] &&
                   !state.DynamicMask[index] &&
                   !state.RotatingMask[index] &&
                   !nextMask[index] &&
                   !sweptMask[index];
        }

        private int NextSearchStamp()
        {
            searchStamp++;

            if (searchStamp != int.MaxValue)
                return searchStamp;

            Array.Clear(searchStamps, 0, searchStamps.Length);
            searchStamp = 1;
            return searchStamp;
        }

        private int NextContactStamp()
        {
            contactStamp++;

            if (contactStamp != int.MaxValue)
                return contactStamp;

            Array.Clear(contactStamps, 0, contactStamps.Length);
            contactStamp = 1;
            return contactStamp;
        }

        private void BuildNextMask()
        {
            for (int i = 0; i < nextCells.Count; i++)
                nextMask[nextCells[i]] = false;

            nextCells.Clear();
            nextOwners.Clear();

            for (int i = 0; i < obstacles.Length; i++)
                RasterObstacle(i, obstacles[i].Angle, nextMask, nextCells, nextOwners);
        }

        private void CommitMask()
        {
            SandSimulationState state = simulation.State;

            for (int i = 0; i < previousCells.Count; i++)
                state.RotatingMask[previousCells[i]] = false;

            for (int i = 0; i < nextCells.Count; i++)
                state.RotatingMask[nextCells[i]] = true;

            previousCells.Clear();
            previousCells.AddRange(nextCells);

            owners.Clear();
            owners.AddRange(nextOwners);
        }

        private void RasterObstacle(
            int owner,
            float angle,
            bool[] mask,
            List<int> cells,
            List<int> cellOwners)
        {
            RotatingObstacleState obstacle = obstacles[owner];
            float cell = simulation.CellSize;

            float halfLength = obstacle.BarLength * obstacle.Scale * 0.5f;
            float halfWidth = profile.barWidth * obstacle.Scale * 0.5f;
            float radius = halfLength + halfWidth;

            int minX = Mathf.Max(
                0,
                Mathf.FloorToInt((obstacle.Position.x - radius) / cell));

            int maxX = Mathf.Min(
                simulation.State.Width - 1,
                Mathf.CeilToInt((obstacle.Position.x + radius) / cell));

            int minY = Mathf.Max(
                0,
                Mathf.FloorToInt((obstacle.Position.y - radius) / cell));

            int maxY = Mathf.Min(
                simulation.State.Height - 1,
                Mathf.CeilToInt((obstacle.Position.y + radius) / cell));

            float radians = (angle + 45f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x + 0.5f) * cell - obstacle.Position.x;
                float dy = (y + 0.5f) * cell - obstacle.Position.y;

                float along = dx * cos + dy * sin;
                float across = -dx * sin + dy * cos;

                bool first =
                    Mathf.Abs(along) <= halfLength &&
                    Mathf.Abs(across) <= halfWidth;

                bool second =
                    Mathf.Abs(across) <= halfLength &&
                    Mathf.Abs(along) <= halfWidth;

                if (!first && !second)
                    continue;

                int index = y * simulation.State.Width + x;

                if (mask[index])
                    continue;

                mask[index] = true;
                cells.Add(index);
                cellOwners?.Add(owner);
            }
        }

        private void ClearScratch()
        {
            ClearSweepMarks();
            Array.Clear(contactTorques, 0, contactTorques.Length);
        }

        private void ClearSweepMarks()
        {
            for (int i = 0; i < sweptCells.Count; i++)
                sweptMask[sweptCells[i]] = false;

            sweptCells.Clear();
        }

        private static int EstimateBoundsCapacity(
            RotatingObstacleData item,
            float barWidth,
            float cell,
            int cellCount)
        {
            float radius = (item.barLength + barWidth) * item.scale * 0.5f;
            int side = Mathf.CeilToInt(radius * 2f / cell) + 2;
            return Mathf.Min(cellCount, Mathf.Max(16, side * side));
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
            angle = Mathf.Repeat(data.initialAngle, 360f);

            // Legacy degreesPerSecond is intentionally not used as startup velocity.
            AngularVelocity = 0f;
        }

        public string StableId { get; }
        public Vector2 Position { get; }
        public float Scale { get; }
        public float BarLength { get; }

        private float angle;

        public float Angle => angle;
        public float AngularVelocity { get; private set; }

        public float Integrate(
            float torque,
            float dt,
            RotatingObstacleProfile profile)
        {
            float length = BarLength * Scale;
            float width = Mathf.Max(0.001f, profile.barWidth * Scale);
            float overlapSide = Mathf.Min(length, width);

            float inertia =
                profile.momentOfInertiaScale *
                (
                    2f * length * width *
                    (length * length + width * width) / 12f
                    -
                    overlapSide * overlapSide *
                    overlapSide * overlapSide / 6f
                );

            inertia = Mathf.Max(0.0001f, inertia);

            float angularAccelerationDegrees =
                torque / inertia * Mathf.Rad2Deg;

            float nextVelocity =
                AngularVelocity + angularAccelerationDegrees * dt;

            nextVelocity *= Mathf.Exp(
                -Mathf.Max(0f, profile.angularDamping) * dt);

            AngularVelocity = Mathf.Clamp(
                nextVelocity,
                -profile.maxAngularSpeed,
                profile.maxAngularSpeed);

            if (Mathf.Abs(AngularVelocity) <= profile.restAngularSpeed)
                AngularVelocity = 0f;

            return AngularVelocity * dt;
        }

        public void SetAngle(float value)
        {
            angle = Mathf.Repeat(value, 360f);
        }

        public void Stop()
        {
            AngularVelocity = 0f;
        }

        public void RetainVelocity(float retention, float restThreshold)
        {
            AngularVelocity *= Mathf.Clamp01(retention);

            if (Mathf.Abs(AngularVelocity) <= Mathf.Max(0f, restThreshold))
                AngularVelocity = 0f;
        }
    }
}