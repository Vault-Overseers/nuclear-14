using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared._N14.Radiation.Components;
using Content.Shared.Radiation.Events;
using Robust.Shared.Maths;

namespace Content.Server._N14.Radiation;

public sealed partial class RadiationHealingSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    private static readonly string[] HealableTypes = { "Blunt", "Slash", "Piercing", "Heat" };

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationHealingComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var healing, out var damage))
        {
            var oldExposure = healing.CurrentExposure;

            // Try to heal if we have both radiation and healable damage below the cap.
            var healingNow = false;
            if (healing.CurrentExposure > 0f && damage.TotalDamage < 90f)
            {
                var healable = GetHealableDamage(damage);
                if (healable > 0f)
                {
                    var radUsed = Math.Min(healing.CurrentExposure, healing.CurrentExposure * frameTime);
                    if (radUsed > 0f)
                    {
                        var healAmount = radUsed * healing.HealFactor;

                        DamageSpecifier spec = new();
                        spec.DamageDict["Blunt"] = FixedPoint2.New(-healAmount);
                        spec.DamageDict["Slash"] = FixedPoint2.New(-healAmount);
                        spec.DamageDict["Piercing"] = FixedPoint2.New(-healAmount);
                        spec.DamageDict["Heat"] = FixedPoint2.New(-healAmount);

                        _damageable.TryChangeDamage(uid, spec, interruptsDoAfters: false);
                        healing.CurrentExposure -= radUsed;
                        healingNow = true;
                    }
                }
            }

            // Natural decay of unused radiation.
            if (healing.CurrentExposure > 0f)
                healing.CurrentExposure = Math.Max(0f, healing.CurrentExposure - healing.DecayRate * frameTime);

            if (!MathHelper.CloseTo(oldExposure, healing.CurrentExposure) || healingNow)
            {
                Dirty(uid, healing);
                _movement.RefreshMovementSpeedModifiers(uid);
            }
        }
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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationHealingComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnIrradiated(EntityUid uid, RadiationHealingComponent component, OnIrradiatedEvent args)
    {
        component.CurrentExposure += args.TotalRads;
        Dirty(uid, component);
        _movement.RefreshMovementSpeedModifiers(uid);
    }
}
