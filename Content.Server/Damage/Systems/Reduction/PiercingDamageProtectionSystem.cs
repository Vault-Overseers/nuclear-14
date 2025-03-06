using Content.Server.Damage.Components.Reduction;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.EntitySystems.Reduction;

public sealed class PiercingDamageProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PiercingDamageProtectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PiercingDamageProtectionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, PiercingDamageProtectionComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.PiercingDamageProtectionModifierSetId, out var modifier))
            return;
        var buffComp = EnsureComp<DamageProtectionBuffComponent>(uid);
        // add the damage modifier if it isn't in the dict yet
        if (!buffComp.Modifiers.ContainsKey(component.PiercingDamageProtectionModifierSetId))
            buffComp.Modifiers.Add(component.PiercingDamageProtectionModifierSetId, modifier);
    }

    private void OnShutdown(EntityUid uid, PiercingDamageProtectionComponent component, ComponentShutdown args)
    {
        if (!TryComp<DamageProtectionBuffComponent>(uid, out var buffComp))
            return;
        // remove the damage modifier from the dict
        buffComp.Modifiers.Remove(component.PiercingDamageProtectionModifierSetId);
        // if the dict is empty now, remove the buff component
        if (buffComp.Modifiers.Count == 0)
            RemComp<DamageProtectionBuffComponent>(uid);
    }
}