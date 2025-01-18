using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective requirement that the mind's owned entity is part of a specific NPC faction.
/// </summary>
[RegisterComponent, Access(typeof(FactionRequirementSystem))]
public sealed partial class FactionRequirementComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
}
