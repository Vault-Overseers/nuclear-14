﻿using Content.Server.Disease;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles making people randomly cough/sneeze without a disease.
/// </summary>
public sealed class UncontrollableSnoughSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UncontrollableSnoughComponent, ComponentStartup>(SetupSnough);
    }

    private void SetupSnough(EntityUid uid, UncontrollableSnoughComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var snough in EntityQuery<UncontrollableSnoughComponent>())
        {
            snough.NextIncidentTime -= frameTime;

            if (snough.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            snough.NextIncidentTime +=
                _random.NextFloat(snough.TimeBetweenIncidents.X, snough.TimeBetweenIncidents.Y);

            if (snough.SnoughSound != null)
                _audioSystem.PlayPvs(snough.SnoughSound, snough.Owner);

            _diseaseSystem.SneezeCough(snough.Owner, null, snough.SnoughMessage, false);
        }
    }
}
