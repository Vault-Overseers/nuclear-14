﻿using Content.Server.Radiation;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class RadiateArtifactSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiateArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, RadiateArtifactComponent component, ArtifactActivatedEvent args)
    {
        var transform = Transform(uid);
        EntityManager.SpawnEntity(component.PulsePrototype, transform.Coordinates);
    }
}
