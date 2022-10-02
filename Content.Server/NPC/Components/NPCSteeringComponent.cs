using System.Threading;
using Content.Server.CPUJob.JobQueues;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;

namespace Content.Server.NPC.Components;

/// <summary>
/// Added to NPCs that are moving.
/// </summary>
[RegisterComponent]
public sealed class NPCSteeringComponent : Component
{
    /// <summary>
    /// Have we currently requested a path.
    /// </summary>
    [ViewVariables]
    public bool Pathfind => PathfindToken != null;
    [ViewVariables] public CancellationTokenSource? PathfindToken = null;

    /// <summary>
    /// Current path we're following to our coordinates.
    /// </summary>
    [ViewVariables] public Queue<PathPoly> CurrentPath = new();

    /// <summary>
    /// End target that we're trying to move to.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public EntityCoordinates Coordinates;

    /// <summary>
    /// How close are we trying to get to the coordinates before being considered in range.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float Range = 0.2f;

    /// <summary>
    /// How far does the last node in the path need to be before considering re-pathfinding.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public float RepathRange = 1.2f;

    public const int FailedPathLimit = 3;

    /// <summary>
    /// How many times we've failed to pathfind. Once this hits the limit we'll stop steering.
    /// </summary>
    [ViewVariables] public int FailedPathCount;

    [ViewVariables] public SteeringStatus Status = SteeringStatus.Moving;

    [ViewVariables(VVAccess.ReadWrite)] public PathFlags Flags = PathFlags.None;
}

public enum SteeringStatus : byte
{
    /// <summary>
    /// If we can't reach the target (e.g. different map).
    /// </summary>
    NoPath,

    /// <summary>
    /// Are we moving towards our target
    /// </summary>
    Moving,

    /// <summary>
    /// Are we currently in range of our target.
    /// </summary>
    InRange,
}
