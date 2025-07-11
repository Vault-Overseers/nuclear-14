using Content.Shared._N14.Support;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._N14.Support;

/// <summary>
/// Handles simple timed artillery strikes.
/// </summary>
public sealed class ArtilleryStrikeSystem : SharedArtilleryStrikeSystem
{
    [Dependency] private readonly ExplosionSystem _explosions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtilleryStrikeComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ArtilleryStrikeComponent component, ComponentStartup args)
    {
        component.StartTime = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ArtilleryStrikeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.StartTime + comp.Delay)
                continue;

            _explosions.QueueExplosion(comp.Target, comp.ExplosionType, comp.Intensity, comp.Slope, comp.MaxIntensity, canCreateVacuum: false);
            QueueDel(uid);
        }
    }

    public EntityUid ScheduleStrike(MapCoordinates target, TimeSpan delay, string type = "Default", float intensity = 50f, float slope = 3f, float maxIntensity = 10f)
    {
        var ent = Spawn(null, target);
        var comp = EnsureComp<ArtilleryStrikeComponent>(ent);
        comp.Target = target;
        comp.Delay = delay;
        comp.ExplosionType = type;
        comp.Intensity = intensity;
        comp.Slope = slope;
        comp.MaxIntensity = maxIntensity;
        comp.StartTime = _timing.CurTime;
        Dirty(ent, comp);
        return ent;
    }
}
