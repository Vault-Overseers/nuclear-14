using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective requirement that the mind's owned entity is part of a specific NPC faction.
/// </summary>
[RegisterComponent, Access(typeof(FactionRequirementSystem))]
public sealed partial class FactionRequirementComponent : Component
{
    [DataField(required: true)]
    public HashSet<string> Factions = new();
}
