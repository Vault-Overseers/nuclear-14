using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Damage.Systems;

public sealed partial class StaminaSystem
{
    private void InitializeSlowdown()
    {
        SubscribeLocalEvent<StaminaComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshModifiers);
    }

    private void OnRefreshModifiers(Entity<StaminaComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var damage = ent.Comp.StaminaDamage;
        var threshold = 0.5f * ent.Comp.CritThreshold;
        if (damage < threshold)
            return;

        var factor = damage / ent.Comp.CritThreshold;
        args.ModifySpeed(factor, factor);
    }
}
