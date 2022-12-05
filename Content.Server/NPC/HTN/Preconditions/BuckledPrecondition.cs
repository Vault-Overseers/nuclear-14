using Content.Server.Buckle.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is buckled or not
/// </summary>
public sealed class BuckledPrecondition : HTNPrecondition
{
    private BuckleSystem _buckle = default!;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("isBuckled")] public bool IsBuckled = true;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _buckle = sysManager.GetEntitySystem<BuckleSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        return IsBuckled && _buckle.IsBuckled(owner) ||
               !IsBuckled && !_buckle.IsBuckled(owner);
    }
}
