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

    protected override void TakeCellCharge(EntityUid gunUid, N14EnergyWeaponComponent component, EntityUid cellUid, float fireCost)
    {
        _battery.UseCharge(cellUid, fireCost);
        UpdateN14EnergyShots(gunUid, component);
    }

    protected override void UpdateN14EnergyShots(EntityUid uid, N14EnergyWeaponComponent component)
    {
        var cell = GetMagazineEntity(uid);

        if (cell == null || !TryComp<BatteryComponent>(cell.Value, out var battery))
        {
            component.Shots = 0;
            component.Capacity = 0;
        }
        else
        {
            component.Shots = (int)(battery.CurrentCharge / component.FireCost);
            component.Capacity = (int)(battery.MaxCharge / component.FireCost);
        }

        UpdateN14EnergyAppearance(uid, component);
        Dirty(uid, component);
    }
}
