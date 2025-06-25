using Content.Shared.Movement.Systems;
using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

public sealed partial class RadiationHealingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationHealingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMove);
    }

    private void OnRefreshMove(EntityUid uid, RadiationHealingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentExposure <= 0f)
            return;

        var mod = Math.Clamp(1f - component.CurrentExposure * component.SlowFactor, 0.25f, 1f);
        args.ModifySpeed(mod);
    }
}
