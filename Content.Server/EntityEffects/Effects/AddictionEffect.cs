using Content.Server.Chemistry.Addiction;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Applies addiction mechanics to the target.
/// </summary>
public sealed partial class AddictionEffect : EntityEffect
{
    [DataField("drugId", required: true)]
    public string DrugId = string.Empty;

    [DataField]
    public float AddictionChance = 0.05f;

    [DataField]
    public float ToleranceIncrease = 1f;

    [DataField(required: true)]
    public string AddictionStatus = string.Empty;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs)
            return;

        var sys = args.EntityManager.System<DrugAddictionSystem>();
        sys.OnDrugUsed(args.TargetEntity, DrugId, AddictionChance, ToleranceIncrease, AddictionStatus);
    }
}
