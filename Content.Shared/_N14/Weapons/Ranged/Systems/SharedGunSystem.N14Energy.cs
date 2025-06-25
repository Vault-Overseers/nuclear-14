using Content.Shared._N14.Weapons.Ranged.Components;
using Content.Server.Power.Components; // only needed for parameter type
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeN14Energy()
    {
        SubscribeLocalEvent<N14EnergyWeaponComponent, MapInitEvent>(OnN14EnergyInit);
        SubscribeLocalEvent<N14EnergyWeaponComponent, TakeAmmoEvent>(OnN14EnergyTakeAmmo);
        SubscribeLocalEvent<N14EnergyWeaponComponent, GetAmmoCountEvent>(OnN14EnergyAmmoCount);
        SubscribeLocalEvent<N14EnergyWeaponComponent, EntInsertedIntoContainerMessage>(OnN14EnergySlotChange);
        SubscribeLocalEvent<N14EnergyWeaponComponent, EntRemovedFromContainerMessage>(OnN14EnergySlotChange);
    }

    private void OnN14EnergyInit(EntityUid uid, N14EnergyWeaponComponent component, MapInitEvent args)
    {
        UpdateN14EnergyShots(uid, component);
    }

    private void OnN14EnergySlotChange(EntityUid uid, N14EnergyWeaponComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID != MagazineSlot)
            return;
        UpdateN14EnergyShots(uid, component);
    }

    private void OnN14EnergyAmmoCount(EntityUid uid, N14EnergyWeaponComponent component, ref GetAmmoCountEvent args)
    {
        UpdateN14EnergyShots(uid, component);
        args.Count = component.Shots;
        args.Capacity = component.Capacity;
    }

    private void OnN14EnergyTakeAmmo(EntityUid uid, N14EnergyWeaponComponent component, TakeAmmoEvent args)
    {
        var cell = GetMagazineEntity(uid);
        if (cell == null || !TryComp<BatteryComponent>(cell.Value, out var battery))
            return;

        for (var i = 0; i < args.Shots; i++)
        {
            if (battery.CurrentCharge < component.FireCost)
                break;

            if (component.Hitscan)
            {
                var hitscan = ProtoManager.Index<HitscanPrototype>(component.HitscanProto!);
                args.Ammo.Add((null, hitscan));
            }
            else
            {
                var ent = Spawn(component.ProjectileProto!, args.Coordinates);
                args.Ammo.Add((ent, EnsureShootable(ent)));
            }

            TakeCellCharge(uid, cell.Value, component.FireCost, battery);
        }

        UpdateN14EnergyShots(uid, component);
    }

    protected virtual void TakeCellCharge(EntityUid gunUid, EntityUid cellUid, float fireCost, BatteryComponent? battery = null) {}

    private void UpdateN14EnergyShots(EntityUid uid, N14EnergyWeaponComponent component)
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

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            Appearance.SetData(uid, AmmoVisuals.HasAmmo, component.Shots != 0, appearance);
            Appearance.SetData(uid, AmmoVisuals.AmmoCount, component.Shots, appearance);
            Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.Capacity, appearance);
        }
    }
}
