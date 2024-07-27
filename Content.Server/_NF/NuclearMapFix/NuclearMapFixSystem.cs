using Content.Server.Atmos.Components;
using Robust.Server.Console;

namespace Content.Server._NF.NuclearMapFix
{
    public sealed class NuclearMapFix : EntitySystem
    {
        [Dependency] private readonly IServerConsoleHost _host = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NuclearMapFixComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, NuclearMapFixComponent component, ComponentStartup args)
        {
            var query = AllEntityQuery<GridAtmosphereComponent, TransformComponent>();

            while (query.MoveNext(out var dummyatmos, out var comp))
            {
                var gridUid = comp.GridUid;
                _host.AppendCommand($"fixgridatmos {gridUid}");
                Logger.Error($"executed command on {gridUid})");
            }

        }
    }
}