using Content.Server.Damage.Components.Reduction;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.EntitySystems.Reduction;

public sealed class HeatDamageProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatDamageProtectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HeatDamageProtectionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, HeatDamageProtectionComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.HeatDamageProtectionModifierSetId, out var modifier))
            return;
        var buffComp = EnsureComp<DamageProtectionBuffComponent>(uid);
        // add the damage modifier if it isn't in the dict yet
        if (!buffComp.Modifiers.ContainsKey(component.HeatDamageProtectionModifierSetId))
            buffComp.Modifiers.Add(component.HeatDamageProtectionModifierSetId, modifier);
    }

    private void OnShutdown(EntityUid uid, HeatDamageProtectionComponent component, ComponentShutdown args)
    {
        if (!TryComp<DamageProtectionBuffComponent>(uid, out var buffComp))
            return;
        // remove the damage modifier from the dict
        buffComp.Modifiers.Remove(component.HeatDamageProtectionModifierSetId);
        // if the dict is empty now, remove the buff component
        if (buffComp.Modifiers.Count == 0)
            RemComp<DamageProtectionBuffComponent>(uid);
    }
}