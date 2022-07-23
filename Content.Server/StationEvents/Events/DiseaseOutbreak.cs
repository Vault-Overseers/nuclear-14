using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Disease.Components;
using Content.Server.Disease;
using Content.Server.Station.Systems;
using Content.Shared.Disease;
using Content.Shared.MobState.Components;
using Content.Shared.Sound;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;
/// <summary>
/// Infects a couple people
/// with a random disease that isn't super deadly
/// </summary>
public sealed class DiseaseOutbreak : StationEventSystem
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;

    public override string Prototype => "DiseaseOutbreak";

    /// <summary>
    /// Disease prototypes I decided were not too deadly for a random event
    /// </summary>
    public readonly IReadOnlyList<string> NotTooSeriousDiseases = new[]
    {
        "SpaceCold",
        "VanAusdallsRobovirus",
        "VentCough",
        "AMIV",
        "SpaceFlu",
        "BirdFlew",
        "TongueTwister"
    };

    /// <summary>
    /// Finds 2-5 random, alive entities that can host diseases
    /// and gives them a randomly selected disease.
    /// They all get the same disease.
    /// </summary>
    public override void Started()
    {
        base.Started();
        HashSet<EntityUid> stationsToNotify = new();
        List<DiseaseCarrierComponent> aliveList = new();
        foreach (var (carrier, mobState) in EntityManager.EntityQuery<DiseaseCarrierComponent, MobStateComponent>())
        {
            if (!mobState.IsDead())
                aliveList.Add(carrier);
        }
        RobustRandom.Shuffle(aliveList);

        // We're going to filter the above out to only alive mobs. Might change after future mobstate rework
        var toInfect = RobustRandom.Next(2, 5);

        var diseaseName = RobustRandom.Pick(NotTooSeriousDiseases);

        if (!PrototypeManager.TryIndex(diseaseName, out DiseasePrototype? disease))
            return;

        // Now we give it to people in the list of living disease carriers earlier
        foreach (var target in aliveList)
        {
            if (toInfect-- == 0)
                break;

            _diseaseSystem.TryAddDisease(target.Owner, disease, target);

            var station = StationSystem.GetOwningStation(target.Owner);
            if(station == null) continue;
            stationsToNotify.Add((EntityUid) station);
        }
    }
}
