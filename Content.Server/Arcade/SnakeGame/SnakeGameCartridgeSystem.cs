using Content.Server.CartridgeLoader;
using Content.Shared.Arcade.SnakeGame;
using Content.Shared.CartridgeLoader;
using Robust.Server.GameObjects;

namespace Content.Server.Arcade.SnakeGame;

public sealed class SnakeGameCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _loader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnakeGameCartridgeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SnakeGameCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<SnakeGameCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    public override void Update(float frameTime)
    {
        var query = EntityManager.EntityQueryEnumerator<SnakeGameCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Game?.GameTick(frameTime);
        }
    }

    private void OnInit(EntityUid uid, SnakeGameCartridgeComponent comp, ComponentInit args)
    {
        comp.Game = new SnakeGame(uid);
    }

    private void OnUiReady(EntityUid uid, SnakeGameCartridgeComponent comp, CartridgeUiReadyEvent args)
    {
        UpdateUi(uid, args.Loader, comp);
    }

    private void OnMessage(EntityUid uid, SnakeGameCartridgeComponent comp, CartridgeMessageEvent args)
    {
        if (args is not SnakeGameUiMessageEvent msg)
            return;

        comp.Game?.ProcessInput(msg.Action);
        UpdateUi(uid, GetEntity(args.LoaderUid), comp);
    }

    private void UpdateUi(EntityUid uid, EntityUid loader, SnakeGameCartridgeComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        var state = new SnakeGameMessages.SnakeGameUiState(comp.Game?.Board ?? string.Empty, comp.Game?.Score ?? 0, comp.Game?.GameOver ?? false);
        _loader.UpdateCartridgeUiState(loader, state);
    }
}
