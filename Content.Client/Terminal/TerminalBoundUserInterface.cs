using Content.Client.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.Terminal;
using Robust.Client.UserInterface;

namespace Content.Client.Terminal;

public sealed class TerminalBoundUserInterface : CartridgeLoaderBoundUserInterface
{
    [ViewVariables]
    private TerminalMenu? _menu;

    public TerminalBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (_menu == null)
            CreateMenu();
    }

    private void CreateMenu()
    {
        _menu = this.CreateWindow<TerminalMenu>();
        _menu.OpenCenteredLeft();

        _menu.OnProgramItemPressed += ActivateCartridge;
        _menu.OnInstallButtonPressed += InstallCartridge;
        _menu.OnUninstallButtonPressed += UninstallCartridge;
        _menu.ProgramCloseButton.OnPressed += _ => DeactivateActiveCartridge();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
    }

    protected override void AttachCartridgeUI(Control cartridgeUIFragment, string? title)
    {
        _menu?.ProgramView.AddChild(cartridgeUIFragment);
        _menu?.ToProgramView(title ?? Loc.GetString("comp-pda-io-program-fallback-title"));
    }

    protected override void DetachCartridgeUI(Control cartridgeUIFragment)
    {
        if (_menu is null)
            return;

        _menu.ToProgramList();
        _menu.HideProgramHeader();
        _menu.ProgramView.RemoveChild(cartridgeUIFragment);
    }

    protected override void UpdateAvailablePrograms(List<(EntityUid, CartridgeComponent)> programs)
    {
        _menu?.UpdateAvailablePrograms(programs);
    }
}
