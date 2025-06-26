using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared._N14.Radiation.Components;
using Content.Shared.Radiation.Events;

namespace Content.Server._N14.Radiation;

public sealed partial class RadiationHealingSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationHealingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentExposure <= 0f)
                continue;

            comp.CurrentExposure = Math.Max(0f, comp.CurrentExposure - comp.DecayRate * frameTime);
            Dirty(uid, comp);
            _movement.RefreshMovementSpeedModifiers(uid);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationHealingComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnIrradiated(EntityUid uid, RadiationHealingComponent component, OnIrradiatedEvent args)
    {
        component.CurrentExposure += args.RadsPerSecond;
        var healAmount = args.RadsPerSecond * component.HealFactor * args.FrameTime;

        if (!TryComp(uid, out DamageableComponent? damage) || healAmount <= 0f)
            return;

        DamageSpecifier spec = new();
        spec.DamageDict["Blunt"] = FixedPoint2.New(-healAmount);
        spec.DamageDict["Slash"] = FixedPoint2.New(-healAmount);
        spec.DamageDict["Piercing"] = FixedPoint2.New(-healAmount);
        spec.DamageDict["Heat"] = FixedPoint2.New(-healAmount);
        spec.DamageDict["Poison"] = FixedPoint2.New(-healAmount);

        _damageable.TryChangeDamage(uid, spec, interruptsDoAfters: false);
    }
}
