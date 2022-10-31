using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Is the specified key within the specified range of us.
/// </summary>
public sealed class TargetInRangePrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [ViewVariables, DataField("targetKey", required: true)] public string TargetKey = default!;

    [ViewVariables, DataField("rangeKey", required: true)]
    public string RangeKey = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates))
            return false;

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target) ||
            !_entManager.TryGetComponent<TransformComponent>(target, out var targetXform))
            return false;

        return coordinates.InRange(_entManager, targetXform.Coordinates, blackboard.GetValueOrDefault<float>(RangeKey));
    }
}
