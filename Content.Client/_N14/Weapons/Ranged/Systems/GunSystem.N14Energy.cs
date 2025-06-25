using Content.Shared._N14.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.Systems;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeN14Energy()
    {
        base.InitializeN14Energy();
        SubscribeLocalEvent<N14EnergyWeaponComponent, AmmoCounterControlEvent>(OnN14EnergyControl);
        SubscribeLocalEvent<N14EnergyWeaponComponent, UpdateAmmoCounterEvent>(OnN14EnergyUpdate);
    }

    private void OnN14EnergyUpdate(EntityUid uid, N14EnergyWeaponComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes) return;
        boxes.Update(component.Shots, component.Capacity);
    }

    private void OnN14EnergyControl(EntityUid uid, N14EnergyWeaponComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }
}
