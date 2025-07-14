using Content.Shared._N14.Support;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Light.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using System;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;


namespace Content.Server._N14.Support
{

    /// <summary>
    /// Handles simple timed artillery strikes.
    /// </summary>
    public sealed class ArtilleryStrikeSystem : SharedArtilleryStrikeSystem
    {
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedRoofSystem _roof = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArtilleryStrikeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<ArtilleryStrikeComponent, MapInitEvent>(OnMapInit);
        }

        private void OnStartup(EntityUid uid, ArtilleryStrikeComponent component, ComponentStartup args)
        {
            component.StartTime = TimeSpan.Zero;
        }

        private void OnMapInit(EntityUid uid, ArtilleryStrikeComponent component, ref MapInitEvent args)
        {
            if (component.Target.MapId == MapId.Nullspace)
                component.Target = _transform.GetMapCoordinates(uid);
        }

        public override void Update(float frameTime)
        {

            var now = _timing.CurTime;
            var query = EntityQueryEnumerator<ArtilleryStrikeComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (TryComp<ExpendableLightComponent>(uid, out var light))
                {
                    if (!light.Activated)
                        continue;

                    if (comp.StartTime == TimeSpan.Zero)
                        comp.StartTime = now;
                }
                else if (comp.StartTime == TimeSpan.Zero)
                {
                    comp.StartTime = now;
                }

                if (now < comp.StartTime + comp.Delay)
                    continue;

                if (comp.Target.MapId == MapId.Nullspace)
                    comp.Target = _transform.GetMapCoordinates(uid);

                if (_mapManager.TryFindGridAt(comp.Target, out var gridUid, out var grid))
                {
                    var tile = grid.WorldToTile(comp.Target.Position);
                    RoofComponent? roof = null;
                    if (Resolve(gridUid, ref roof, false))
                    {
                        var gridEnt = (gridUid, grid, roof);
                        if (_roof.IsRooved(gridEnt, tile))
                        {
                            QueueDel(uid);
                            continue;
                        }
                    }
                }

                _explosions.QueueExplosion(
                    comp.Target,
                    comp.ExplosionType,
                    comp.Intensity,
                    comp.Slope,
                    comp.MaxIntensity,
                    canCreateVacuum: false);
                QueueDel(uid);
            }
        }

        public EntityUid ScheduleStrike(
            MapCoordinates target,
            TimeSpan delay,
            string type = "Default",
            float intensity = 50f,
            float slope = 3f,
            float maxIntensity = 10f
        )
        {
            var ent = Spawn(null, target);
            var comp = EnsureComp<ArtilleryStrikeComponent>(ent);
            comp.Target = target;
            comp.Delay = delay;
            comp.ExplosionType = type;
            comp.Intensity = intensity;
            comp.Slope = slope;
            comp.MaxIntensity = maxIntensity;
            // StartTime will be initialized when the flare activates.
            comp.StartTime = TimeSpan.Zero;
            Dirty(ent, comp);
            return ent;
        }
    }
}
