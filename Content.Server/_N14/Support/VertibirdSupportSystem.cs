using Content.Shared._N14.Support;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Light.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using System;
using Robust.Shared.Maths;


namespace Content.Server._N14.Support
{

    /// <summary>
    /// Manages scheduled vertibird fire support.
    /// </summary>
    public sealed class VertibirdSupportSystem : SharedVertibirdSupportSystem
    {
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedRoofSystem _roof = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VertibirdSupportComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, VertibirdSupportComponent component, ComponentStartup args)
        {
            component.StartTime = TimeSpan.Zero;
            component.ShotsFired = 0;
            component.ApproachPlayed = false;
            if (component.LineAngle == Angle.Zero)
                component.LineAngle = _random.NextAngle();
        }


        public override void Update(float frameTime)
        {
            var now = _timing.CurTime;
            var query = EntityQueryEnumerator<VertibirdSupportComponent>();
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

                if (!comp.ApproachPlayed && now >= comp.StartTime + comp.ApproachDelay)
                {
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

                    if (comp.ApproachSound != null)
                    {
                        var coords = _transform.ToCoordinates(comp.Target);
                        _audio.PlayPvs(comp.ApproachSound, coords);
                    }

                    comp.ApproachPlayed = true;
                }

                var nextTime = comp.StartTime + comp.ApproachDelay + comp.Delay + comp.ShotsFired * comp.ShotInterval;
                if (now < nextTime)
                    continue;

                if (comp.ShotsFired >= comp.Shots)
                {
                    QueueDel(uid);
                    continue;
                }

                if (_mapManager.TryFindGridAt(comp.Target, out var gridUid1, out var grid1))
                {
                    var tile = grid1.WorldToTile(comp.Target.Position);
                    RoofComponent? roof2 = null;
                    if (Resolve(gridUid1, ref roof2, false))
                    {
                        var gridEnt = (gridUid1, grid1, roof2);
                        if (_roof.IsRooved(gridEnt, tile))
                        {
                            QueueDel(uid);
                            continue;
                        }
                    }
                }

                var forward = comp.LineAngle.ToWorldVec();
                var right = (comp.LineAngle + Angle.FromDegrees(90)).ToWorldVec();
                forward = forward.Normalized();
                right = right.Normalized();

                var progress = comp.Shots <= 1 ? 0f : comp.LineLength * (comp.ShotsFired / (float) (comp.Shots - 1));
                var lateral = _random.NextFloat(-comp.Spread, comp.Spread);
                var offset = forward * progress + right * lateral;
                var pos = comp.Target.Offset(offset);

                _explosions.QueueExplosion(
                    pos,
                    comp.ExplosionType,
                    comp.Intensity,
                    comp.Slope,
                    comp.MaxIntensity,
                    comp.TileBreakScale,
                    canCreateVacuum: false);
                if (comp.FireSound != null)
                {
                    var fireCoords = _transform.ToCoordinates(pos);
                    _audio.PlayPvs(comp.FireSound, fireCoords);
                }

                comp.ShotsFired++;
                Dirty(uid, comp);
            }
        }

        public EntityUid ScheduleSupport(
            MapCoordinates target,
            TimeSpan approachDelay,
            TimeSpan delay,
            int shots = 10,
            TimeSpan? interval = null,
            float spread = 4f,
            float lineLength = 10f,
            Angle? angle = null,
            string type = "Default",
            float intensity = 30f,
            float slope = 2f,
            float maxIntensity = 5f,
            float tileBreakScale = 1f,
            SoundSpecifier? approach = null,
            SoundSpecifier? fire = null
        )
        {
            var ent = Spawn(null, target);
            var comp = EnsureComp<VertibirdSupportComponent>(ent);
            comp.Target = MapCoordinates.Nullspace;
            comp.ApproachDelay = approachDelay;
            comp.Delay = delay;
            comp.Shots = shots;
            comp.ShotInterval = interval ?? TimeSpan.FromSeconds(0.1);
            comp.Spread = spread;
            comp.LineLength = lineLength;
            comp.LineAngle = angle ?? _random.NextAngle();
            comp.ExplosionType = type;
            comp.Intensity = intensity;
            comp.Slope = slope;
            comp.MaxIntensity = maxIntensity;
            comp.TileBreakScale = tileBreakScale;
            comp.ApproachSound = approach;
            comp.FireSound = fire;
            // StartTime will be initialized when the flare activates.
            comp.StartTime = TimeSpan.Zero;
            comp.ShotsFired = 0;
            Dirty(ent, comp);
            return ent;
        }
    }
}
