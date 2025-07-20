using Content.Shared.UserInterface;
using Content.Shared.Arcade.SnakeGame;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Arcade.SnakeGame;

public sealed class SnakeArcadeSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnakeArcadeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SnakeArcadeComponent, AfterActivatableUIOpenEvent>(OnUiOpen);

        Subs.BuiEvents<SnakeArcadeComponent>(SnakeGameMessages.SnakeGameUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
            subs.Event<SnakeGameMessages.SnakeGamePlayerActionMessage>(OnAction);
        });
    }

    public override void Update(float frameTime)
    {
        var query = EntityManager.EntityQueryEnumerator<SnakeArcadeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Game?.GameTick(frameTime);
        }
    }

    private void OnInit(EntityUid uid, SnakeArcadeComponent comp, ComponentInit args)
    {
        comp.Game = new SnakeGame(uid);
    }

    private void OnUiOpen(EntityUid uid, SnakeArcadeComponent comp, AfterActivatableUIOpenEvent args)
    {
        if (comp.Player == null)
            comp.Player = args.Actor;
        else
            comp.Spectators.Add(args.Actor);

        comp.Game?.UpdateUi(args.Actor);
    }

    private void OnUiClosed(EntityUid uid, SnakeArcadeComponent comp, BoundUIClosedEvent args)
    {
        if (comp.Player == args.Actor)
        {
            if (comp.Spectators.Count > 0)
            {
                comp.Player = comp.Spectators[0];
                comp.Spectators.RemoveAt(0);
            }
            else
            {
                comp.Player = null;
            }
        }
        else
        {
            comp.Spectators.Remove(args.Actor);
        }
    }

    private void OnAction(EntityUid uid, SnakeArcadeComponent comp, SnakeGameMessages.SnakeGamePlayerActionMessage msg)
    {
        if (msg.Actor != comp.Player)
            return;
        comp.Game?.ProcessInput(msg.Action);
    }
}
