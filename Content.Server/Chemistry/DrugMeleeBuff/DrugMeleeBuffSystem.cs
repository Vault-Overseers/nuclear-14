using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.DrugMeleeBuff;

public sealed class DrugMeleeBuffSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsychoMeleeBuffComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PsychoMeleeBuffComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, PsychoMeleeBuffComponent component, ComponentInit args)
    {
        if (!_prototype.TryIndex(component.ModifierSetId, out var modifier))
            return;

        var comp = EnsureComp<BonusMeleeDamageComponent>(uid);
        comp.DamageModifierSet = modifier;
        Dirty(uid, comp);
    }

    private void OnShutdown(EntityUid uid, PsychoMeleeBuffComponent component, ComponentShutdown args)
    {
        if (TryComp<BonusMeleeDamageComponent>(uid, out var comp))
        {
            comp.DamageModifierSet = null;
            if (comp.BonusDamage == null && comp.HeavyDamageFlatModifier == FixedPoint2.Zero && comp.HeavyDamageMultiplier.Equals(1f))
                RemComp<BonusMeleeDamageComponent>(uid);
            else
                Dirty(uid, comp);
        }
    }
}
