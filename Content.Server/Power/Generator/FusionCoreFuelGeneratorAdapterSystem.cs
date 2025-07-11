using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Generator;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;

namespace Content.Server.Power.Generator;

/// <summary>
/// Handles fusion core insertion and supplies fuel accordingly.
/// </summary>
public sealed partial class FusionCoreFuelGeneratorAdapterSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, EntRemovedFromContainerMessage>(OnContainerModified);

        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, GeneratorGetFuelEvent>(OnGetFuel);
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, GeneratorUseFuel>(OnUseFuel);
        SubscribeLocalEvent<FusionCoreFuelGeneratorAdapterComponent, GeneratorEmpty>(OnEmpty);
    }

    private void OnMapInit(EntityUid uid, FusionCoreFuelGeneratorAdapterComponent comp, MapInitEvent args)
    {
        UpdateAppearance(uid, comp);
    }

    private void OnContainerModified(EntityUid uid, FusionCoreFuelGeneratorAdapterComponent comp, ContainerModifiedMessage args)
    {
        if (args.Container.ID != comp.SlotId)
            return;

        UpdateAppearance(uid, comp);
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
        {
            _slots.TryEject(uid, slot, null, out _);
            UpdateAppearance(uid, comp);
        }
    }

    private void UpdateAppearance(EntityUid uid, FusionCoreFuelGeneratorAdapterComponent comp)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        var hasCore = _slots.TryGetSlot(uid, comp.SlotId, out var slot) && slot.Item != null;
        _appearance.SetData(uid, GeneratorVisuals.HasCore, hasCore, appearance);
    }
}

