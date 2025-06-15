using System.Diagnostics.CodeAnalysis;
using Content.KayMisaZlevels.Server.Components;
using Content.KayMisaZlevels.Shared.Components;
using Content.KayMisaZlevels.Shared.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.KayMisaZlevels.Server.Systems;

public sealed partial class ZStackSystem : SharedZStackSystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ZLoaderComponent, ComponentStartup>(ZLoaderStart);
        SubscribeLocalEvent<ZViewerComponent, EntParentChangedMessage>(OnViewerParentChange);
    }

    private void ZLoaderStart(Entity<ZLoaderComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Target is {} session)
            _viewSubscriber.AddViewSubscriber(ent, session); // Sight!!!!!!
    }

    private void OnViewerParentChange(Entity<ZViewerComponent> ent, ref EntParentChangedMessage args)
    {
        RebuildViewer(ent);
    }

    private void OnPlayerDetached(PlayerDetachedEvent msg)
    {
        RemComp<ZViewerComponent>(msg.Entity);
    }

    private void OnPlayerAttached(PlayerAttachedEvent msg)
    {
        var c = EnsureComp<ZViewerComponent>(msg.Entity);
        RebuildViewer(new(msg.Entity, c));
    }

    private void RecycleLoader(EntityUid uid)
    {
        QueueDel(uid); // If creating/destroying loaders becomes an issue sometime we fix it here.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_viewersInvalid)
        {
            var q2 = EntityQueryEnumerator<ZViewerComponent>();

            while (q2.MoveNext(out var uid, out var comp))
            {
                RebuildViewer(new(uid, comp));
            }

            _viewersInvalid = false;
        }

        var q = EntityQueryEnumerator<ZViewerComponent>();

        // This would be much better if we could create non-entity (i.e. coordinate with token) view subscriptions.
        // All this would save is the constant pos updates but it's not nothing.
        while (q.MoveNext(out var uid, out var comp))
        {
            var parentWorldPos = _xform.GetWorldPosition(uid);
            foreach (var loader in comp.Loaders)
            {
                _xform.SetWorldPosition(loader, parentWorldPos);
            }
        }
    }

    public void AddToStack(EntityUid mapToAdd, [NotNull] ref EntityUid? stack)
    {
        if (stack is null)
        {
            stack = Spawn(null, MapCoordinates.Nullspace);
            AddComp<ZStackTrackerComponent>(stack.Value);
        }

        var tracker = Comp<ZStackTrackerComponent>(stack.Value);
        tracker.Maps.Add(mapToAdd);

        AddComp(mapToAdd,
            new ZStackMemberComponent()
            {
                Tracker = stack.Value,
            });

        InvalidateViewers();
    }

    private bool _viewersInvalid = false;

    public void InvalidateViewers()
    {
        _viewersInvalid = true;
    }
}
