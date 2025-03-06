using Content.Server.Damage.Components.Reduction;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.EntitySystems.Reduction;

public sealed class ColdDamageProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ColdDamageProtectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ColdDamageProtectionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, ColdDamageProtectionComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.ColdDamageProtectionModifierSetId, out var modifier))
            return;
        var buffComp = EnsureComp<DamageProtectionBuffComponent>(uid);
        // add the damage modifier if it isn't in the dict yet
        if (!buffComp.Modifiers.ContainsKey(component.ColdDamageProtectionModifierSetId))
            buffComp.Modifiers.Add(component.ColdDamageProtectionModifierSetId, modifier);
    }

    private void OnShutdown(EntityUid uid, ColdDamageProtectionComponent component, ComponentShutdown args)
    {
        if (!TryComp<DamageProtectionBuffComponent>(uid, out var buffComp))
            return;
        // remove the damage modifier from the dict
        buffComp.Modifiers.Remove(component.ColdDamageProtectionModifierSetId);
        // if the dict is empty now, remove the buff component
        if (buffComp.Modifiers.Count == 0)
            RemComp<DamageProtectionBuffComponent>(uid);
    }
}