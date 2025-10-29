using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.DrugProtection;

public sealed class DrugProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadXProtectionComponent, ComponentInit>(OnInit<RadXProtectionComponent>);
        SubscribeLocalEvent<RadXProtectionComponent, ComponentShutdown>(OnShutdown<RadXProtectionComponent>);
        SubscribeLocalEvent<BuffoutProtectionComponent, ComponentInit>(OnInit<BuffoutProtectionComponent>);
        SubscribeLocalEvent<BuffoutProtectionComponent, ComponentShutdown>(OnShutdown<BuffoutProtectionComponent>);
    }

    private void OnInit<T>(EntityUid uid, T component, ComponentInit args) where T : Component, IDrugProtectionComponent
    {
        if (!_prototype.TryIndex(component.ModifierSetId, out var modifier))
            return;
        var buff = EnsureComp<DamageProtectionBuffComponent>(uid);
        if (!buff.Modifiers.ContainsKey(component.ModifierSetId))
            buff.Modifiers.Add(component.ModifierSetId, modifier);
    }

    private void OnShutdown<T>(EntityUid uid, T component, ComponentShutdown args) where T : Component, IDrugProtectionComponent
    {
        if (!TryComp<DamageProtectionBuffComponent>(uid, out var buff))
            return;
        buff.Modifiers.Remove(component.ModifierSetId);
        if (buff.Modifiers.Count == 0)
            RemComp<DamageProtectionBuffComponent>(uid);
    }
}
