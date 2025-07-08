using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Generator;
using Robust.Shared.Containers;

namespace Content.Server.Power.Generator;

/// <summary>
/// Handles fusion core insertion and supplies fuel accordingly.
/// </summary>
public sealed partial class FusionCoreFuelGeneratorAdapterSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, GeneratorGetFuelEvent>(OnGetFuel);
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, GeneratorUseFuel>(OnUseFuel);
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, GeneratorEmpty>(OnEmpty);
    }

    private void OnGetFuel(EntityUid uid, FusionCoreFuelGeneratorAdapterComponent comp, ref GeneratorGetFuelEvent args)
    {
        if (_slots.TryGetSlot(uid, comp.SlotId, out var slot) && slot.Item != null)
            args.Fuel += 1f;
    }

    private void OnUseFuel(EntityUid uid, FusionCoreFuelGeneratorAdapterComponent comp, GeneratorUseFuel args)
    {
        // Fusion cores are not consumed in the current implementation.
    }

    private void OnEmpty(EntityUid uid, FusionCoreFuelGeneratorAdapterComponent comp, GeneratorEmpty args)
    {
        if (_slots.TryGetSlot(uid, comp.SlotId, out var slot))
            _slots.TryEject(uid, slot);
    }
}

