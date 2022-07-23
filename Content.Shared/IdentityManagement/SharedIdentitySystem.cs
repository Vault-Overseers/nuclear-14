﻿using Content.Shared.IdentityManagement.Components;
using Robust.Shared.Containers;

namespace Content.Shared.IdentityManagement;

public abstract class SharedIdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    private static string SlotName = "identity";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<IdentityBlockerComponent, SeeIdentityAttemptEvent>(OnSeeIdentity);
    }

    private void OnSeeIdentity(EntityUid uid, IdentityBlockerComponent component, SeeIdentityAttemptEvent args)
    {
        if (component.Enabled)
            args.Cancel();
    }

    protected virtual void OnComponentInit(EntityUid uid, IdentityComponent component, ComponentInit args)
    {
        component.IdentityEntitySlot = _container.EnsureContainer<ContainerSlot>(uid, SlotName);
    }
}
