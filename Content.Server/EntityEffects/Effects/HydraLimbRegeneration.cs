using Content.Shared.Body.Part;
using Content.Shared.EntityEffects;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Regenerates or repairs a limb when Hydra is applied to a specific body part.
/// Only works on arms, hands, legs and feet.
/// </summary>
public sealed partial class HydraLimbRegeneration : EntityEffect
{
    [DataField]
    public float HealAmount = 25f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs || reagentArgs.OrganEntity is null)
            return;

        var entMan = args.EntityManager;
        if (!entMan.TryGetComponent<BodyPartComponent>(reagentArgs.OrganEntity.Value, out var part) || part.Body == null)
            return;

        if (part.PartType is BodyPartType.Torso or BodyPartType.Head)
            return;

        // heal damage on the part
        if (entMan.TryGetComponent<DamageableComponent>(reagentArgs.OrganEntity.Value, out var damage))
        {
            entMan.System<DamageableSystem>().TryChangeDamage(reagentArgs.OrganEntity.Value,
                new DamageSpecifier(damage.DamageTypes, damage.DamageGroups) * -1);
        }

        if (!part.Enabled && part.CanEnable)
        {
            part.Enabled = true;
            entMan.Dirty(reagentArgs.OrganEntity.Value, part);
        }
    }
}
