using Content.Server.Chemistry.Addiction;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Removes all drug addictions from the target.
/// </summary>
public sealed partial class CureAddiction : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var sys = args.EntityManager.System<DrugAddictionSystem>();
        sys.CureAllAddictions(args.TargetEntity);
    }
}
