
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.KayMisaZlevels.Shared.Components;
using Content.KayMisaZlevels.Shared.Miscellaneous;
using Content.KayMisaZlevels.Shared.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared._KMZLevels.ZTransition;

public class SharedZTransitionStairsSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedZStackSystem _zStack = default!;

    private const string UpFixture = "upFixture";
    private const string DownFixture = "downFixture";

    public override void Initialize()
    {
        SubscribeLocalEvent<ZStairsComponent, StartCollideEvent>(OnTeleportStartCollide);
    }

    private void OnTeleportStartCollide(Entity<ZStairsComponent> ent, ref StartCollideEvent args)
    {
        if (!ShouldCollide(args.OurFixtureId, out var dir) || dir == null)
            return;

        if (TryComp<PhysicsComponent>(args.OtherEntity, out var othPhys) &&
            othPhys.BodyStatus == BodyStatus.InAir)
            return;

        if (!TryGetTargetMapUid(ent, (ZDirection) dir, out var mapUid))
            return;

        var user = args.OtherEntity;

        var otherCoords = _transform.GetMapCoordinates(user);
        var teleporter = _transform.GetMapCoordinates(ent);

        var targetPosition = _transform.GetWorldPosition(ent);
        var targetRotation = _transform.GetWorldRotation(ent);

        Vector2 offset;
        if (dir == ZDirection.Down)
            offset = targetRotation.ToWorldVec() * ent.Comp.Adjust;
        else
            offset = targetRotation.ToWorldVec() * (-ent.Comp.Adjust);

        var diff = otherCoords.Position - teleporter.Position;
        if (diff.Length() > 10)
            return;

        teleporter = teleporter.Offset(diff);
        var newPosition = new Vector2(teleporter.X, teleporter.Y) + offset;
        teleporter = new MapCoordinates(newPosition, teleporter.MapId);

        // Shit, i don't like RMC-14 code. Why are we teleports by HandlePulling? What the fuck?
        HandlePulling(user, (EntityUid) mapUid, teleporter);
    }

    private bool TryGetTargetMapUid(EntityUid ent, ZDirection dir, [NotNullWhen(true)] out EntityUid? mapUid)
    {
        mapUid = null;

        if (!TryComp<LinkedEntityComponent>(ent, out var linkComp) || linkComp.LinkedEntities.Count <= 0)
            return false;

        var target = linkComp.LinkedEntities.First();

        // FIXME: What the fuck? Why i trying to get components like that?
        if (!TryComp<TransformComponent>(ent, out var xformComp) || xformComp.MapUid is null)
            return false;
        if (!TryComp<TransformComponent>(target, out var targetXformComp) || targetXformComp.MapUid is null)
            return false;

        if (!_zStack.TryGetZStack((EntityUid) xformComp.MapUid, out var zStack))
            return false; // Not in a Z level containing space.

        var maps = zStack.Value.Comp.Maps;
        var mapIdx = maps.IndexOf((EntityUid) xformComp.MapUid);
        var mapTargetIdx = maps.IndexOf((EntityUid) targetXformComp.MapUid);

        switch (dir)
        {
            case ZDirection.Up:
                if (mapIdx >= mapTargetIdx)
                    return false;
                break;
            case ZDirection.Down:
                if (mapIdx <= mapTargetIdx)
                    return false;
                break;
            default:
                return false;
        }

        mapUid = targetXformComp.MapUid;
        return true;
    }

    private bool ShouldCollide(string ourId, out ZDirection? dir)
    {
        dir = null;

        string? targetFixture;
        switch (ourId)
        {
            case UpFixture:
                targetFixture = UpFixture;
                dir = ZDirection.Up;
                break;
            case DownFixture:
                targetFixture = DownFixture;
                dir = ZDirection.Down;
                break;
            default:
                return false;
        }

        return ourId == targetFixture;
    }

    public void HandlePulling(EntityUid user, EntityUid mapTarget, MapCoordinates teleport)
    {
        if (TryComp(user, out PullableComponent? otherPullable) &&
            otherPullable.Puller != null)
        {
            _pulling.TryStopPull(user, otherPullable, otherPullable.Puller.Value);
        }

        if (TryComp(user, out PullerComponent? puller) &&
            TryComp(puller.Pulling, out PullableComponent? pullable))
        {
            if (TryComp(puller.Pulling, out PullerComponent? otherPullingPuller) &&
                TryComp(otherPullingPuller.Pulling, out PullableComponent? otherPullingPullable))
            {
                _pulling.TryStopPull(otherPullingPuller.Pulling.Value, otherPullingPullable, puller.Pulling);
            }

            var pulling = puller.Pulling.Value;
            _pulling.TryStopPull(pulling, pullable, user);
            _transform.SetCoordinates(user, new EntityCoordinates(mapTarget, teleport.Position));
            _transform.SetCoordinates(pulling, new EntityCoordinates(mapTarget, teleport.Position));
            _pulling.TryStartPull(user, pulling);
        }
        else
        {
            _transform.SetCoordinates(user, new EntityCoordinates(mapTarget, teleport.Position));
        }
    }
}
