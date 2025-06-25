using Content.Shared._N14.Weapons.Ranged.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;

    protected override void InitializeN14Energy()
    {
        base.InitializeN14Energy();
    }

    protected override void TakeCellCharge(EntityUid gunUid, EntityUid cellUid, float fireCost, BatteryComponent? battery = null)
    {
        _battery.UseCharge(cellUid, fireCost, battery);
    }
}
