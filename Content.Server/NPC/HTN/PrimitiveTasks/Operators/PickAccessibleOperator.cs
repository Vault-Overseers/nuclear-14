using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Chooses a nearby coordinate and puts it into the resulting key.
/// </summary>
public sealed class PickAccessibleOperator : HTNOperator
{
    private PathfindingSystem _pathfinding = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [ViewVariables, DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [ViewVariables, DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }

    /// <inheritdoc/>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        // Very inefficient (should weight each region by its node count) but better than the old system
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        blackboard.TryGetValue<float>(RangeKey, out var maxRange);

        if (maxRange == 0f)
            maxRange = 7f;

        var path = await _pathfinding.GetRandomPath(
            owner,
            1.4f,
            maxRange,
            cancelToken,
            flags: _pathfinding.GetFlags(blackboard));

        if (path.Result != PathResult.Path)
        {
            return (false, null);
        }

        var target = path.Path.Last().Coordinates;

        return (true, new Dictionary<string, object>()
        {
            { TargetKey, target },
            { PathfindKey, path}
        });
    }
}
