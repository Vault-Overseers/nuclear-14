using Content.KayMisaZlevels.Server.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.KayMisaZlevels.Server.Systems;

public sealed partial class ZStackSystem
{
    private void RebuildViewer(Entity<ZViewerComponent> ent)
    {
        var loaders = ent.Comp.Loaders;
        foreach (var loader in ent.Comp.Loaders)
        {
            RecycleLoader(loader);
        }

        loaders.Clear();

        if (!TryGetZStack(ent, out var stack))
        {
            return;
        }

        TryComp(ent, out ActorComponent? actor);
        var session = actor?.PlayerSession;
        if (session is null)
            Log.Error($"BUG: No session on a {nameof(RebuildViewer)} target!");

        if (session is not null)
            _viewSubscriber.AddViewSubscriber(stack.Value, session); // NOTE: We leak the subscription here, but this isn't a huge deal (it's only the z level's descriptor, no more)

        var xform = Transform(ent);
        var globalPos = _xform.GetWorldPosition(xform);

        foreach (var map in stack.Value.Comp.Maps)
        {
            var loader = SpawnAtPosition(null, new EntityCoordinates(map, globalPos));

            AddComp(loader,
                new ZLoaderComponent()
                {
                    Target = session
                });


            Transform(loader).GridTraversal = false; // Bad! Can result in viewers being eaten.

            loaders.Add(loader);
        }
    }
}
