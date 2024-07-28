using Content.Shared.Abilities;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared._N14.NightVision;

namespace Content.Client._N14.NightVision;

public sealed partial class GreenVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private GreenVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GreenVisionComponent, ComponentInit>(OnGreenVisionInit);
        SubscribeLocalEvent<GreenVisionComponent, ComponentShutdown>(OnGreenVisionShutdown);

        _overlay = new();
    }

    private void OnGreenVisionInit(EntityUid uid, GreenVisionComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnGreenVisionShutdown(EntityUid uid, GreenVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}