using System.Collections.Generic;
using Content.KayMisaZlevels.Shared.Components;
using Content.KayMisaZlevels.Shared.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.KayMisaZlevels.Server.Systems;

// TODO:
// I've had an epiphany
// level transition markers should be steptriggers which then make the client send a ZLevelTransitionRequestEvent
// Server then verifies if the player or mob or whatever is standing in the same tile as marker
// If so - move them up or down depending on the data in ZTransitionMarker comp
// We avoid making the server suffer with yet another MoveEvent subscription

/// <summary>
///     This is responsible for managing transition markers, which take you either up or down if you step in them
/// </summary>
public sealed class ZTransitionSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    //private List<Entity<ZTransitionMarkerComponent>> _markers = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZTransitionMarkerComponent, ComponentInit>(OnMarkerInit);
        SubscribeLocalEvent<ZTransitionMarkerComponent, ComponentStartup>(OnMarkerStartup);
    }

    private void OnMarkerInit(Entity<ZTransitionMarkerComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.DirStr == "down")
            ent.Comp.Dir = Shared.Miscellaneous.ZDirection.Down;
        else if (ent.Comp.DirStr == "up")
            ent.Comp.Dir = Shared.Miscellaneous.ZDirection.Up;
    }

    private void OnMarkerStartup(Entity<ZTransitionMarkerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Position = _transform.GetMapCoordinates(ent.Owner, Comp<TransformComponent>(ent.Owner));
        //_markers.Add(ent);
    }
}

