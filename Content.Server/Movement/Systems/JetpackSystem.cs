using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;

namespace Content.Server.Movement.Systems;

public sealed class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;

    private const float UpdateCooldown = 0.5f;

    protected override bool CanEnable(JetpackComponent component)
    {
        return base.CanEnable(component) &&  TryComp<GasTankComponent>(component.Owner, out var gasTank) && !(gasTank.Air.TotalMoles < component.MoleUsage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDisable = new ValueList<JetpackComponent>();

        foreach (var (active, comp, gasTank) in EntityQuery<ActiveJetpackComponent, JetpackComponent, GasTankComponent>())
        {
            active.Accumulator += frameTime;
            if (active.Accumulator < UpdateCooldown) continue;

            active.Accumulator -= UpdateCooldown;
            var air = _gasTank.RemoveAir(gasTank, comp.MoleUsage);

            if (air == null || !MathHelper.CloseTo(air.TotalMoles, comp.MoleUsage, 0.001f))
            {
                toDisable.Add(comp);
                continue;
            }

            _gasTank.UpdateUserInterface(gasTank);
        }

        foreach (var comp in toDisable)
        {
            SetEnabled(comp, false);
        }
    }
}
