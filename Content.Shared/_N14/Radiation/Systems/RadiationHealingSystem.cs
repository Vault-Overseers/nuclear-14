using Content.Shared.Movement.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared._N14.Radiation.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._N14.Radiation;

public sealed partial class RadiationHealingSystem : EntitySystem
{
    private static readonly string[] HealableTypes = { "Blunt", "Slash", "Piercing", "Heat" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationHealingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMove);
    }

    private void OnRefreshMove(EntityUid uid, RadiationHealingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentExposure <= 0f)
            return;

        if (!TryComp(uid, out DamageableComponent? damage) || damage.TotalDamage >= 90f)
            return;

        var healable = GetHealableDamage(damage);
        if (healable <= 0f)
            return;

        var mod = Math.Clamp(1f - component.CurrentExposure * component.SlowFactor, 0.5f, 1f);
        args.ModifySpeed(mod);
    }

    private static float GetHealableDamage(DamageableComponent damage)
    {
        float amount = 0f;
        foreach (var type in HealableTypes)
        {
            if (damage.Damage.DamageDict.TryGetValue(type, out var val) && val > FixedPoint2.Zero)
                amount += val.Float();
        }

        return amount;
    }
}
