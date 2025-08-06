using System.Numerics;
using Content.Server.Ghost.Components;
using Content.Server.Popups;
using Content.Server.Warps;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._N14.Warps;

/// <summary>
/// Handles entities that warp players to named warp points.
/// </summary>
public sealed class WarperSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarperComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, WarperComponent component, InteractHandEvent args)
    {
        if (component.ID is null)
        {
            Logger.DebugS("warper", "Warper has no destination");
            _popupSystem.PopupEntity(Loc.GetString("warper-goes-nowhere", ("warper", args.Target)), args.User, Filter.Entities(args.User), true);
            return;
        }

        var dest = FindWarpPoint(component.ID);
        if (dest is null)
        {
            Logger.DebugS("warper", $"Warp destination '{component.ID}' not found");
            _popupSystem.PopupEntity(Loc.GetString("warper-goes-nowhere", ("warper", args.Target)), args.User, Filter.Entities(args.User), true);
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();
        TransformComponent? destXform;
        entMan.TryGetComponent(dest.Value, out destXform);
        if (destXform is null)
        {
            Logger.DebugS("warper", $"Warp destination '{component.ID}' has no transform");
            _popupSystem.PopupEntity(Loc.GetString("warper-goes-nowhere", ("warper", args.Target)), args.User, Filter.Entities(args.User), true);
            return;
        }

        var mapMgr = IoCManager.Resolve<IMapManager>();
        var destMap = destXform.MapID;
        if (!mapMgr.IsMapInitialized(destMap) || mapMgr.IsMapPaused(destMap))
        {
            if (!entMan.HasComponent<GhostComponent>(args.User))
            {
                Logger.DebugS("warper", $"Player tried to warp to '{component.ID}', which is not on a running map");
                _popupSystem.PopupEntity(Loc.GetString("warper-goes-nowhere", ("warper", args.Target)), args.User, Filter.Entities(args.User), true);
                return;
            }
        }

        var xform = entMan.GetComponent<TransformComponent>(args.User);
        xform.Coordinates = destXform.Coordinates;
        xform.AttachToGridOrMap();
        if (entMan.TryGetComponent(uid, out PhysicsComponent? phys))
            _physics.SetLinearVelocity(uid, Vector2.Zero);
    }

    private EntityUid? FindWarpPoint(string id)
    {
        var query = EntityQueryEnumerator<WarpPointComponent>();
        while (query.MoveNext(out var warpUid, out var warp))
        {
            if (warp.Location == id)
                return warpUid;
        }
        return null;
    }
}
