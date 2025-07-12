using Content.Shared._N14.Support;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._N14.Support;

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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VertibirdSupportComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VertibirdSupportComponent, MapInitEvent>(OnMapInit);
    }

    private void OnStartup(EntityUid uid, VertibirdSupportComponent component, ComponentStartup args)
    {
        component.StartTime = _timing.CurTime;
        component.ShotsFired = 0;
    }

    private void OnMapInit(EntityUid uid, VertibirdSupportComponent component, ref MapInitEvent args)
    {
        if (component.Target.MapId == MapId.Nullspace)
            component.Target = _transform.GetMapCoordinates(uid);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VertibirdSupportComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ShotsFired == 0 && now >= comp.StartTime + comp.Delay)
            {
                if (comp.ApproachSound != null)
                {
                    var coords = _transform.ToCoordinates(comp.Target);
                    _audio.PlayPvs(comp.ApproachSound, coords);
                }
            }

            var nextTime = comp.StartTime + comp.Delay + comp.ShotsFired * comp.ShotInterval;
            if (now < nextTime)
                continue;

            if (comp.ShotsFired >= comp.Shots)
            {
                QueueDel(uid);
                continue;
            }

            var offset = _random.NextVector2(-comp.Spread, comp.Spread);
            var pos = comp.Target.Offset(offset);

            _explosions.QueueExplosion(pos, comp.ExplosionType, comp.Intensity, comp.Slope, comp.MaxIntensity, canCreateVacuum: false);
            if (comp.FireSound != null)
            {
                var fireCoords = _transform.ToCoordinates(pos);
                _audio.PlayPvs(comp.FireSound, fireCoords);
            }

            comp.ShotsFired++;
            Dirty(uid, comp);
        }
    }

    public EntityUid ScheduleSupport(MapCoordinates target, TimeSpan delay, int shots = 3, TimeSpan? interval = null, float spread = 2f, string type = "Default", float intensity = 30f, float slope = 2f, float maxIntensity = 5f, SoundSpecifier? approach = null, SoundSpecifier? fire = null)
    {
        var ent = Spawn(null, target);
        var comp = EnsureComp<VertibirdSupportComponent>(ent);
        comp.Target = target;
        comp.Delay = delay;
        comp.Shots = shots;
        comp.ShotInterval = interval ?? TimeSpan.FromSeconds(1);
        comp.Spread = spread;
        comp.ExplosionType = type;
        comp.Intensity = intensity;
        comp.Slope = slope;
        comp.MaxIntensity = maxIntensity;
        comp.ApproachSound = approach;
        comp.FireSound = fire;
        comp.StartTime = _timing.CurTime;
        comp.ShotsFired = 0;
        Dirty(ent, comp);
        return ent;
    }
}
