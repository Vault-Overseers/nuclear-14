using Content.Server.Damage.Components.Reduction;
using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.EntitySystems.Reduction;

public sealed class BluntDamageProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BluntDamageProtectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BluntDamageProtectionComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, BluntDamageProtectionComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.BluntDamageProtectionModifierSetId, out var modifier))
            return;
        var buffComp = EnsureComp<DamageProtectionBuffComponent>(uid);
        // add the damage modifier if it isn't in the dict yet
        if (!buffComp.Modifiers.ContainsKey(component.BluntDamageProtectionModifierSetId))
            buffComp.Modifiers.Add(component.BluntDamageProtectionModifierSetId, modifier);
    }

    private void OnShutdown(EntityUid uid, BluntDamageProtectionComponent component, ComponentShutdown args)
    {
        if (!TryComp<DamageProtectionBuffComponent>(uid, out var buffComp))
            return;
        // remove the damage modifier from the dict
        buffComp.Modifiers.Remove(component.BluntDamageProtectionModifierSetId);
        // if the dict is empty now, remove the buff component
        if (buffComp.Modifiers.Count == 0)
            RemComp<DamageProtectionBuffComponent>(uidBlunt);
    }
}