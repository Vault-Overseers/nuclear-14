using Content.Shared.Stats;
using Content.Shared.Stats.StatModifier;

namespace Content.Server.Stats;

public sealed class StatsModifierSystem : SharedStatsModifierSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StatsModifierComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StatsModifierComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, StatsModifierComponent jittering, ComponentStartup args)
    {
        // if (!EntityManager.TryGetComponent(uid, out SharedStatsComponent? stats))
        //     return;
    }

    private void OnShutdown(EntityUid uid, StatsModifierComponent jittering, ComponentShutdown args)
    {

    }
}
