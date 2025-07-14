using Content.Server.CartridgeLoader;
using Content.Server.Power.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Computer;
using Content.Shared.Power;
using Content.Shared.Terminal;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Terminal;

public sealed class TerminalSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminalComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<TerminalComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<TerminalComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<TerminalComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<TerminalComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnUiOpened(EntityUid uid, TerminalComponent component, BoundUIOpenedEvent args)
    {
        if (!TerminalUiKey.Key.Equals(args.UiKey))
            return;

        UpdateTerminalUi(uid, component);
    }

    private void OnContainerModified(EntityUid uid, TerminalComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID != CartridgeLoaderComponent.CartridgeSlotId && args.Container.ID != SharedCartridgeLoaderSystem.InstalledContainerId)
            return;

        UpdateTerminalUi(uid, component);
    }

    private void OnCompInit(EntityUid uid, TerminalComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var power))
            _appearance.SetData(uid, ComputerVisuals.Powered, power.Powered);
    }

    private void OnPowerChanged(EntityUid uid, TerminalComponent component, ref PowerChangedEvent args)
    {
        _appearance.SetData(uid, ComputerVisuals.Powered, args.Powered);
    }

    public void UpdateTerminalUi(EntityUid uid, TerminalComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(uid, out CartridgeLoaderComponent? loader))
            return;

        if (!_ui.HasUi(uid, TerminalUiKey.Key))
            return;

        var programs = _cartridgeLoader.GetAvailablePrograms(uid, loader);
        var state = new TerminalUpdateState(programs, GetNetEntity(loader.ActiveProgram));
        _ui.SetUiState(uid, TerminalUiKey.Key, state);
    }
}
