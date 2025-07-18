using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Containers;

namespace Content.Server._N14.Hydroponics;

public sealed class CompostBoxSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerMixerSystem _mixer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CompostBoxComponent, EntInsertedIntoContainerMessage>(OnInserted);
    }

    private void OnInserted(EntityUid uid, CompostBoxComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.MixerSlotId)
            return;

        if (!TryComp<SolutionContainerMixerComponent>(uid, out var mixerComp))
            return;

        _mixer.TryStartMix((uid, mixerComp), null);
    }
}
