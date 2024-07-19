using Content.Shared.UndecidedLoadout;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.UndecidedLoadout;

[UsedImplicitly]
public sealed class UndecidedLoadoutBackpackBoundUserInterface : BoundUserInterface
{
    private UndecidedLoadoutBackpackMenu? _window;

    public UndecidedLoadoutBackpackBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new UndecidedLoadoutBackpackMenu(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not UndecidedLoadoutBackpackBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new UndecidedLoadoutBackpackChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new UndecidedLoadoutBackpackApproveMessage());
    }
}
