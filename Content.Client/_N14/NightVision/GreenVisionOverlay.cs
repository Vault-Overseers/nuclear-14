using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared._N14.NightVision;

namespace Content.Client._N14.NightVision;

public sealed partial class GreenVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] IEntityManager _entityManager = default!;


    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _greenVisionShader;

    public GreenVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _greenVisionShader = _prototypeManager.Index<ShaderPrototype>("GreenVision").Instance().Duplicate();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;
        if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
            return;
        if (!_entityManager.HasComponent<GreenVisionComponent>(player))
            return;

        _greenVisionShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);


        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_greenVisionShader);
        worldHandle.DrawRect(viewport, Color.White);
    }
}