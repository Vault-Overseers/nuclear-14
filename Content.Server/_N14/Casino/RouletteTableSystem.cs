using Content.Shared._N14.Casino;
using Content.Shared.Interaction;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._N14.Casino;

public sealed class RouletteTableSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RouletteTableComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, RouletteTableComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled || comp.Active)
            return;

        args.Handled = true;
        comp.Active = true;
        UpdateAppearance(uid, comp);

        Timer.Spawn(TimeSpan.FromSeconds(comp.SpinTime), () => FinishSpin(uid));
    }

    private void FinishSpin(EntityUid uid)
    {
        if (!TryComp(uid, out RouletteTableComponent? comp))
            return;

        comp.Active = false;
        UpdateAppearance(uid, comp);

        var number = _random.Next(0, 37);
        var color = number == 0 ? Loc.GetString("roulette-color-green") :
            _random.Prob(0.5f) ? Loc.GetString("roulette-color-red") : Loc.GetString("roulette-color-black");

        var message = Loc.GetString("roulette-result", ("number", number), ("color", color));
        var plain = FormattedMessage.RemoveMarkupPermissive(message);
        var coords = _transform.GetMapCoordinates(uid);
        var filter = Filter.Empty().AddInRange(coords, ChatSystem.VoiceRange);
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, false);
        _popup.PopupEntity(plain, uid, filter, true);
    }

    private void UpdateAppearance(EntityUid uid, RouletteTableComponent comp)
    {
        var state = comp.Active ? RouletteTableState.On : RouletteTableState.Off;
        _appearance.SetData(uid, RouletteTableVisuals.State, state);
    }
}
